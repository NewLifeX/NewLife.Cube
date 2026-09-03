<script setup lang="ts">
/**
 * 提交审批抽屉（OSC-26090347f1 T8c，IA §3.1）：
 * 已选 N 条摘要 → 定义下拉（Enable+Published+TypePath 匹配）→ 发起意见（可选）→ 提交。
 * 提交成功 emit submitted（由宿主关闭并刷新列表）。
 */
import { computed } from 'vue';
import { useSubmitApproval, WF_SUBMIT_MAX } from './useSubmitApproval';

const props = defineProps<{
  modelValue: boolean;
  typePath: string;
  ids: (string | number)[];
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'submitted'): void;
  (e: 'cancel'): void;
}>();

const visible = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v),
});

const {
  definitionsLoading,
  definitionId,
  comment,
  phrases,
  saving,
  truncated,
  selectedCount,
  usableDefinitions,
  canSubmit,
  submit,
} = useSubmitApproval({
  typePath: props.typePath,
  ids: props.ids,
  visible,
});

function onCancel() {
  visible.value = false;
  emit('cancel');
}

async function onSubmit() {
  const ok = await submit();
  if (ok) {
    visible.value = false;
    emit('submitted');
  }
}
</script>

<template>
  <a-drawer
    :model-value="visible"
    :width="480"
    :footer="false"
    unmount-on-close
    @update:model-value="(v: boolean) => (visible = v)"
    @cancel="onCancel"
  >
    <template #title>提交审批</template>

    <div class="wf-submit">
      <div class="wf-submit__summary">
        已选 <b>{{ selectedCount }}</b> 条记录
        <span v-if="truncated > 0" class="wf-submit__truncate">
          本次仅提交前 {{ WF_SUBMIT_MAX }} 条（其余 {{ truncated }} 条未纳入）
        </span>
      </div>

      <a-form :model="{}" layout="vertical">
        <a-form-item label="审批流程" required>
          <a-select
            v-model="definitionId"
            :loading="definitionsLoading"
            placeholder="选择流程定义"
            :not-found-content="usableDefinitions.length ? undefined : '无可用流程定义（请先发布）'"
          >
            <a-option
              v-for="d in usableDefinitions"
              :key="d.id"
              :value="d.id"
              :label="`${d.name}（v${d.version}）`"
            >
              {{ d.name }}（v{{ d.version }}）
            </a-option>
          </a-select>
          <div v-if="usableDefinitions.length === 0" class="wf-submit__hint">
            {{ typePath }} 暂无已发布流程定义。请管理员到流程定义页发布后再提交。
          </div>
        </a-form-item>

        <a-form-item label="发起意见">
          <a-textarea
            v-model="comment"
            :max-length="500"
            placeholder="意见（可选）"
            allow-clear
          />
          <div v-if="phrases.length" class="wf-submit__phrases">
            <a-tag
              v-for="(t, i) in phrases"
              :key="`${t}-${i}`"
              class="wf-submit__phrase"
              :bordered="false"
              @click="comment = t"
            >
              {{ t }}
            </a-tag>
          </div>
        </a-form-item>
      </a-form>
    </div>

    <template #footer>
      <a-button style="margin-right: 8px" @click="onCancel">取消</a-button>
      <a-button
        type="primary"
        :disabled="!canSubmit"
        :loading="saving"
        @click="onSubmit"
      >
        提交
      </a-button>
    </template>
  </a-drawer>
</template>

<style scoped>
.wf-submit__summary {
  margin-bottom: 12px;
  color: var(--color-text-2);
}
.wf-submit__truncate {
  margin-left: 4px;
  color: var(--color-warning-6);
  font-size: 12px;
}
.wf-submit__hint {
  margin-top: 4px;
  color: var(--color-text-3);
  font-size: 12px;
}
.wf-submit__phrases {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 6px;
}
.wf-submit__phrase {
  cursor: pointer;
  background: var(--color-fill-2);
}
</style>
