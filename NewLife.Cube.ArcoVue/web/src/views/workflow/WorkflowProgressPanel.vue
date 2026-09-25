<script setup lang="ts">
/**
 * 审批进度（抽屉或内嵌）。节点流程为主，全部动态折叠；主按钮同意/驳回，更多含回退。
 */
import { computed, ref } from 'vue';
import { formatTime } from '@/core/utils/datetime';
import { renderAiMarkdown } from '@/core/utils/aiMarkdown';
import { useWorkflowProgressPanel } from './useWorkflowProgressPanel';
import WfInstanceGraph from './WfInstanceGraph.vue';
import WfStatusMark from './WfStatusMark.vue';
import WorkflowTaskActions from './WorkflowTaskActions.vue';
import type { WfId } from './useWorkflowProgress';

const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    instanceId?: WfId | null;
    embed?: boolean;
  }>(),
  { embed: false },
);

const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void }>();

const visible = computed({
  get: () => (props.embed ? !!props.instanceId : props.modelValue),
  set: (v: boolean) => emit('update:modelValue', v),
});

const {
  detail,
  loading,
  error,
  myTask,
  canWithdraw,
  isHandle,
  isCc,
  opinionEvents,
  flags,
  rollbackOptions,
  starterAttachments,
  writableFieldNames,
  preview,
  openAttachment,
  closePreview,
  phrases,
  reload,
} = useWorkflowProgressPanel({ visible, instanceId: () => props.instanceId ?? null });

const summaryHtml = computed(() => renderAiMarkdown(detail.value?.summary || ''));
/** 激活的页签：仅在激活「查看流程」时挂载只读画布，避免隐藏容器初始化 FlowGram 导致浮层错位遮挡其他页签 */
const activeTab = ref('progress');
const phraseTexts = computed(() => phrases.value.map((p) => p.text).filter(Boolean));
const actionVariant = computed<'approve' | 'handle' | 'read' | 'none'>(() => {
  if (!myTask.value) return 'none';
  if (isHandle.value) return 'handle';
  if (isCc.value) return 'read';
  return 'approve';
});
</script>

<template>
  <component
    :is="embed ? 'div' : 'a-drawer'"
    v-bind="
      embed
        ? { class: 'wf-progress wf-progress--embed' }
        : {
            visible,
            width: 560,
            footer: false,
            unmountOnClose: true,
            class: 'wf-progress',
          }
    "
    @update:visible="(v: boolean) => (visible = v)"
  >
    <template v-if="!embed" #title>
      <span class="wf-progress__title">{{ detail?.title || '流程实例' }}</span>
    </template>

    <div v-if="loading" class="wf-progress__loading">
      <a-spin />
    </div>
    <a-empty v-else-if="error" :description="error" />
    <a-empty v-else-if="!detail" description="暂无审批" />

    <template v-else>
      <a-alert v-if="writableFieldNames.length" type="info" class="wf-progress__writable" show-icon>
        当前节点可写字段：{{ writableFieldNames.join('、') }}
      </a-alert>

      <a-tabs
        v-model:active-key="activeTab"
        class="wf-progress__tabs"
        default-active-key="progress"
        size="small"
      >
        <template #extra>
          <div class="wf-progress__tab-extra">
            <WfStatusMark :status="detail.status" />
            <span class="wf-progress__def">{{ detail.definition?.name || '流程' }}</span>
          </div>
        </template>
        <a-tab-pane key="progress" title="审批进度">
          <a-card class="wf-progress__card" :bordered="true" size="small">
            <div class="wf-progress__card-title">{{ detail.title || '未命名流程' }}</div>
            <div v-if="summaryHtml" class="wf-progress__summary" v-html="summaryHtml" />
            <div v-else class="wf-progress__muted">暂无摘要</div>
            <div class="wf-progress__file-label">附件</div>
            <div v-if="!starterAttachments.length" class="wf-progress__muted">暂无附件</div>
            <div v-for="a in starterAttachments" :key="a.id" class="wf-progress__file">
              <span class="wf-progress__file-name">{{ a.fileName }}</span>
              <a-button v-if="a.url" type="text" size="mini" @click="openAttachment(a)">
                <icon-park type="preview-open" /> 预览
              </a-button>
              <a v-if="a.url" class="wf-progress__dl" :href="a.url" target="_blank" rel="noopener" download>
                <icon-park type="download" /> 下载
              </a>
            </div>
          </a-card>

          <a-collapse :default-active-key="['opinions']" :bordered="false" class="wf-progress__all">
            <a-collapse-item key="opinions" header="审批意见">
              <a-timeline>
                <a-timeline-item v-for="ev in opinionEvents" :key="ev.key" :label="ev.time ? formatTime(ev.time) : ''">
                  <div class="wf-progress__ev">
                    <a-tag size="small">{{ ev.actionText }}</a-tag>
                    <b v-if="ev.nodeTitle">{{ ev.nodeTitle }}</b>
                    <span class="wf-progress__user">{{ ev.user }}</span>
                  </div>
                  <div v-if="ev.content" class="wf-progress__content">{{ ev.content }}</div>
                  <div v-if="ev.attachments?.length" class="wf-progress__file">
                    <template v-for="a in ev.attachments" :key="a.id">
                      <span class="wf-progress__file-name">{{ a.fileName }}</span>
                      <a-button v-if="a.url" type="text" size="mini" @click="openAttachment(a)">预览</a-button>
                      <a v-if="a.url" class="wf-progress__dl" :href="a.url" download target="_blank" rel="noopener">下载</a>
                    </template>
                  </div>
                </a-timeline-item>
              </a-timeline>
            </a-collapse-item>
          </a-collapse>
        </a-tab-pane>

        <a-tab-pane key="graph" title="查看流程">
          <WfInstanceGraph v-if="activeTab === 'graph'" :graph-json="detail?.graphSnapshot" :detail="detail" />
        </a-tab-pane>
      </a-tabs>

      <WorkflowTaskActions
        v-if="myTask || canWithdraw"
        class="wf-progress__ops"
        appearance="solid"
        popup-position="tl"
        :variant="actionVariant"
        :show-more="!!myTask && !isHandle && !isCc"
        :show-withdraw="canWithdraw"
        :task-id="myTask?.id"
        :instance-id="detail.id"
        :node-id="myTask?.nodeId"
        :assignee-id="myTask?.assigneeId"
        :candidate="myTask?.candidate"
        :phrases="phraseTexts"
        :rollback-options="rollbackOptions"
        :allow-transfer="flags.allowTransfer"
        :allow-add-sign="flags.allowAddSign"
        :allow-rollback="flags.allowRollback"
        :allow-cc="!!myTask && !isHandle && !isCc"
        @done="reload()"
      />
    </template>

    <a-modal
      :visible="!!preview"
      :title="preview?.title || '预览'"
      :footer="false"
      width="720px"
      unmount-on-close
      @cancel="closePreview"
    >
      <img v-if="preview?.kind === 'image'" class="wf-preview__img" :src="preview.url" :alt="preview.title" />
      <iframe v-else-if="preview?.kind === 'pdf'" class="wf-preview__frame" :src="preview.url" title="预览" />
      <pre v-else-if="preview?.kind === 'text'" class="wf-preview__text">{{ preview.text }}</pre>
    </a-modal>
  </component>
</template>

<style scoped>
/* tabs 撑满抽屉内容区，「查看流程」画布随之拉伸填满页签（四边留白 = 外层 body/tabs 既有间距） */
.wf-progress__tabs {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.wf-progress__tabs :deep(.arco-tabs-content) {
  flex: 1;
  min-height: 0;
}
.wf-progress__tabs :deep(.arco-tabs-content-list) {
  height: 100%;
}
.wf-progress__tabs :deep(.arco-tabs-content-item-active),
.wf-progress__tabs :deep(.arco-tabs-pane) {
  height: 100%;
}
.wf-progress--embed {
  display: flex;
  flex-direction: column;
  width: 100%;
  min-height: 0;
  height: 100%;
}
.wf-progress--embed :deep(.arco-tabs) {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: auto;
}
.wf-progress--embed .wf-progress__ops {
  flex: none;
  margin-top: auto;
}
.wf-progress__loading {
  padding: 40px;
  text-align: center;
}
.wf-progress__head,
.wf-progress__tab-extra {
  display: flex;
  align-items: center;
  flex-wrap: nowrap;
  gap: 8px;
}
.wf-progress__head {
  margin-bottom: 12px;
}
.wf-progress__tab-extra {
  margin-left: 12px;
}
.wf-progress__inst-title {
  font-weight: 600;
}
.wf-progress__subjects {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 8px 12px;
  margin-bottom: 12px;
  background: var(--color-fill-1);
  border-radius: 4px;
}
.wf-progress__subject {
  display: flex;
  gap: 8px;
  min-width: 0;
}
.wf-progress__subject-key {
  color: var(--color-text-3);
  font-size: 12px;
  white-space: nowrap;
}
.wf-progress__summary {
  margin-bottom: 12px;
  padding: 8px 12px;
  background: var(--color-fill-1);
  border-radius: 4px;
  font-size: 13px;
  line-height: 1.6;
}
.wf-progress__writable {
  margin-bottom: 12px;
}
.wf-progress__nodes {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.wf-node {
  padding: 10px 12px;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  background: var(--color-bg-2);
}
.wf-node--current {
  border-color: var(--color-primary-6);
  box-shadow: 0 0 0 1px var(--color-primary-light-3);
}
.wf-node__head {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
  margin-bottom: 6px;
}
.wf-node__hint,
.wf-node__comment {
  color: var(--color-text-3);
  font-size: 12px;
}
.wf-node__atts {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  width: 100%;
}
.wf-node__att {
  font-size: 12px;
}
.wf-node__person {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  font-size: 13px;
  padding: 2px 0;
}
.wf-node__time {
  margin-left: auto;
  color: var(--color-text-4);
  font-size: 12px;
}
.wf-progress__user {
  color: var(--color-text-3);
  font-size: 12px;
  margin-left: 8px;
}
.wf-progress__content {
  margin-top: 2px;
  color: var(--color-text-2);
  white-space: pre-wrap;
}
.wf-progress__attachments {
  margin-top: 12px;
  padding-top: 8px;
  border-top: 1px solid var(--color-border-2);
}
.wf-progress__attachments-title {
  margin-bottom: 6px;
  color: var(--color-text-2);
  font-size: 13px;
}
.wf-progress__att {
  display: block;
  font-size: 13px;
  line-height: 1.8;
}
.wf-progress__card {
  width: 100%;
  margin-bottom: 12px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.wf-progress__all {
  margin-top: 8px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  overflow: hidden;
}
.wf-progress__card-title {
  font-weight: 600;
  margin-bottom: 6px;
}
.wf-progress__muted,
.wf-progress__file-label {
  margin-top: 8px;
  font-size: 12px;
  color: var(--color-text-3);
}
.wf-progress__file {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
}
.wf-progress__file-name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.wf-progress__dl {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  font-size: 12px;
}
.wf-progress__ev {
  display: flex;
  align-items: center;
  gap: 6px;
}
.wf-preview__img {
  max-width: 100%;
}
.wf-preview__frame {
  width: 100%;
  height: 70vh;
  border: 0;
}
.wf-preview__text {
  max-height: 70vh;
  overflow: auto;
  white-space: pre-wrap;
}
.wf-progress__ops {
  display: flex;
  gap: 8px;
  margin-top: 16px;
}
.wf-opinion-phrases {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 8px;
}
.wf-opinion-phrase {
  cursor: pointer;
  background: var(--color-fill-2);
}
</style>
