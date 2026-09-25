<script setup lang="ts">
/**
 * 批量提交抽屉（OSC-26090347f1 T8c，IA §3.1）：
 * 已选 N 条 → 定义下拉 → 标题 → 流程摘要（Markdown）→ 附件 → 提交。
 */
import { computed, ref } from 'vue';
import RichEditor from '@/components/RichEditor.vue';
import { renderAiMarkdown } from '@/core/utils/aiMarkdown';
import { filesFromUploadChange, useSubmitApproval, WF_SUBMIT_MAX } from './useSubmitApproval';
import WfInstanceGraph from './WfInstanceGraph.vue';
import WorkflowRecipientPicker from './WorkflowRecipientPicker.vue';

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
/** 激活的页签：仅在激活「查看流程」时挂载只读画布，避免隐藏容器初始化 FlowGram 导致浮层错位遮挡「提交审批」表单 */
const activeTab = ref('submit');

const {
  definitionsLoading,
  definitionId,
  summary,
  title,
  files,
  saving,
  truncated,
  selectedCount,
  usableDefinitions,
  selectedDefinition,
  steps,
  picks,
  setPick,
  canSubmit,
  submit,
  wfDefinitionIdOf,
} = useSubmitApproval({
  typePath: () => props.typePath,
  ids: () => props.ids,
  visible,
});

const summaryPreview = computed(() => renderAiMarkdown(summary.value || ''));
const pickSteps = computed(() => steps.value.filter((s) => s.kind === 'starterPick'));
/** 查看流程画布：优先定义发布快照（实例运行版本），回落设计草稿 */
const pickedGraphJson = computed(() => {
  const d = selectedDefinition.value as { publishedGraphJson?: string; graphJson?: string } | null;
  return d?.publishedGraphJson ?? d?.graphJson ?? null;
});

function onDefinitionId(v: unknown) {
  definitionId.value = wfDefinitionIdOf(v) || null;
}

function onFilesChange(fileList: unknown, current?: unknown) {
  files.value = filesFromUploadChange(fileList, current);
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

      <a-tabs
        v-model:active-key="activeTab"
        class="wf-submit__root-tabs"
        default-active-key="submit"
        size="small"
      >
        <a-tab-pane key="submit" title="提交审批">
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

            <a-form-item v-for="s in pickSteps" :key="s.id" :label="s.title" required>
              <WorkflowRecipientPicker
                :model-value="picks[s.id] ?? []"
                :multiple="s.multiple"
                :placeholder="s.multiple ? '选择审批人（可多人）' : '选择审批人'"
                @update:model-value="(ids: number[]) => setPick(s.id, ids)"
              />
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
        </a-tab-pane>

        <a-tab-pane key="graph" title="查看流程">
          <WfInstanceGraph v-if="activeTab === 'graph' && pickedGraphJson" :graph-json="pickedGraphJson" />
          <div v-else class="wf-submit__hint">请先选择流程定义</div>
        </a-tab-pane>
      </a-tabs>
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
/* 根 tabs 撑满抽屉内容区，「查看流程」画布随之拉伸填满页签（四边留白 = 外层 body 既有间距） */
.wf-submit {
  height: 100%;
}
.wf-submit__root-tabs {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.wf-submit__root-tabs :deep(.arco-tabs-content) {
  flex: 1;
  min-height: 0;
}
.wf-submit__root-tabs :deep(.arco-tabs-content-list) {
  height: 100%;
}
.wf-submit__root-tabs :deep(.arco-tabs-content-item-active),
.wf-submit__root-tabs :deep(.arco-tabs-pane) {
  height: 100%;
}
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
