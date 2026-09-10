<script setup lang="ts">
/**
 * 批量提交抽屉（OSC-26090347f1 T8c，IA §3.1）：
 * 已选 N 条 → 定义下拉 → 标题 → 流程摘要（Markdown）→ 发起意见 → 附件 → 提交。
 */
import { computed, ref } from 'vue';
import RichEditor from '@/components/RichEditor.vue';
import { renderAiMarkdown } from '@/core/utils/aiMarkdown';
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

const summaryPane = ref<'edit' | 'preview'>('edit');

const {
  definitionsLoading,
  definitionId,
  comment,
  summary,
  title,
  files,
  phrases,
  saving,
  truncated,
  selectedCount,
  usableDefinitions,
  canSubmit,
  submit,
  wfDefinitionIdOf,
} = useSubmitApproval({
  typePath: () => props.typePath,
  ids: () => props.ids,
  visible,
});

const summaryPreview = computed(() => renderAiMarkdown(summary.value || ''));

function onDefinitionId(v: unknown) {
  definitionId.value = wfDefinitionIdOf(v) || null;
}

function onFilesChange(_: unknown, fileList: { file?: File }[]) {
  files.value = (fileList ?? []).map((f) => f.file!).filter(Boolean);
}

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
    :visible="visible"
    :width="520"
    :footer="true"
    unmount-on-close
    @update:visible="(v: boolean) => (visible = v)"
    @cancel="onCancel"
  >
    <template #title>批量提交</template>

    <div class="wf-submit">
      <div class="wf-submit__count">
        已选 <b>{{ selectedCount }}</b> 条记录
        <span v-if="truncated > 0" class="wf-submit__truncate">
          本次仅提交前 {{ WF_SUBMIT_MAX }} 条（其余 {{ truncated }} 条未纳入）
        </span>
      </div>

      <a-form :model="{}" layout="vertical">
        <a-form-item label="审批流程" required>
          <a-select
            :model-value="definitionId ?? undefined"
            :loading="definitionsLoading"
            placeholder="选择流程定义"
            :not-found-content="usableDefinitions.length ? undefined : '无可用流程定义（请先发布）'"
            @update:model-value="onDefinitionId"
          >
            <a-option
              v-for="d in usableDefinitions"
              :key="wfDefinitionIdOf(d.id)"
              :value="wfDefinitionIdOf(d.id)"
              :label="`${d.name}（v${d.version}）`"
            >
              {{ d.name }}（v{{ d.version }}）
            </a-option>
          </a-select>
          <div v-if="usableDefinitions.length === 0" class="wf-submit__hint">
            {{ typePath }} 暂无已发布流程定义。请管理员到流程定义页发布后再提交。
          </div>
        </a-form-item>

        <a-form-item label="流程标题">
          <a-input v-model="title" :max-length="200" allow-clear />
        </a-form-item>

        <a-form-item label="流程摘要">
          <a-tabs v-model:active-key="summaryPane" type="rounded" size="small" class="wf-submit__tabs">
            <a-tab-pane key="edit" title="编辑">
              <div class="wf-submit__field">
                <RichEditor v-model="summary" />
                <div class="wf-submit__hint">支持 Markdown / 富文本，将写入流程实例「流程摘要」</div>
              </div>
            </a-tab-pane>
            <a-tab-pane key="preview" title="预览">
              <div class="wf-submit__field">
                <div v-if="summary" class="wf-submit__md" v-html="summaryPreview" />
                <a-empty v-else description="暂无摘要" />
              </div>
            </a-tab-pane>
          </a-tabs>
        </a-form-item>

        <a-form-item label="发起意见">
          <a-auto-complete
            v-model="comment"
            class="wf-submit__field"
            :data="phrases"
            :max-length="500"
            allow-clear
            placeholder="输入意见，或从常用语中选择"
          />
        </a-form-item>

        <a-form-item label="附件">
          <a-upload
            :auto-upload="false"
            :limit="10"
            multiple
            tip="可选，提交后写入附件表，审批人可在进度中查看"
            @change="onFilesChange"
          />
        </a-form-item>
      </a-form>
    </div>

    <template #footer>
      <a-button style="margin-right: 8px" @click="onCancel">取消</a-button>
      <a-button type="primary" :disabled="!canSubmit" :loading="saving" @click="onSubmit">
        提交
      </a-button>
    </template>
  </a-drawer>
</template>

<style scoped>
.wf-submit__count {
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
.wf-submit__tabs {
  width: 100%;
}
.wf-submit__tabs :deep(.arco-tabs-nav) {
  margin-bottom: 8px;
}
.wf-submit__field {
  width: 100%;
}
.wf-submit__md {
  width: 100%;
  min-height: 120px;
  box-sizing: border-box;
  padding: 8px 12px;
  border: 1px solid var(--color-border-2);
  border-radius: 4px;
  line-height: 1.6;
  word-break: break-word;
}
.wf-submit__md :deep(pre) {
  overflow: auto;
}
</style>
