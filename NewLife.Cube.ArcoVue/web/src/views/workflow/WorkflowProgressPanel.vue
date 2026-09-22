<script setup lang="ts">
/**
 * 审批进度（抽屉或内嵌）。节点流程为主，全部动态折叠；主按钮同意/驳回，更多含回退。
 */
import { computed } from 'vue';
import { formatTime } from '@/core/utils/datetime';
import { renderAiMarkdown } from '@/core/utils/aiMarkdown';
import { useWorkflowProgressPanel } from './useWorkflowProgressPanel';
import WorkflowRecipientPicker from './WorkflowRecipientPicker.vue';
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
  statusMeta,
  flowNodes,
  flags,
  rollbackOptions,
  timelineItems,
  writableFieldNames,
  opinionVisible,
  opinionKind,
  opinionText,
  phrases,
  saving,
  openApprove,
  openReject,
  openWithdraw,
  openAddSign,
  openTransfer,
  openCc,
  openRollback,
  openRecord,
  confirmOpinion,
  pickPhrase,
  targetVisible,
  targetKind,
  targetComment,
  targetBefore,
  targetNodeId,
  targetRecipients,
  targetSaving,
  confirmTarget,
  TARGET_TITLE,
} = useWorkflowProgressPanel({ visible, instanceId: () => props.instanceId ?? null });

const summaryHtml = computed(() => renderAiMarkdown(detail.value?.summary || ''));
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
      <span class="wf-progress__title">审批进度</span>
    </template>

    <div v-if="loading" class="wf-progress__loading">
      <a-spin />
    </div>
    <a-empty v-else-if="error" :description="error" />
    <a-empty v-else-if="!detail" description="暂无审批" />

    <template v-else>
      <div class="wf-progress__head">
        <a-tag :color="statusMeta.color">{{ statusMeta.text }}</a-tag>
        <span v-if="detail.title" class="wf-progress__inst-title">{{ detail.title }}</span>
        <span class="wf-progress__def">{{ detail.definition?.name || `定义 #${detail.definition?.id ?? ''}` }}</span>
        <a-button type="text" size="mini" @click="openRecord">打开记录</a-button>
      </div>

      <div v-if="detail.subjects?.length" class="wf-progress__subjects">
        <div v-for="s in detail.subjects" :key="String(s.id)" class="wf-progress__subject">
          <span class="wf-progress__subject-key">{{ s.entityKey }}</span>
          <a-typography-text ellipsis>{{ s.title || '—' }}</a-typography-text>
        </div>
      </div>

      <div v-if="summaryHtml" class="wf-progress__summary" v-html="summaryHtml" />

      <a-alert
        v-if="writableFieldNames.length"
        type="info"
        class="wf-progress__writable"
        show-icon
      >
        当前节点可写字段：{{ writableFieldNames.join('、') }}
        <template #action>
          <a-button type="text" size="mini" @click="openRecord">打开记录修改</a-button>
        </template>
      </a-alert>

      <div v-if="flowNodes.length" class="wf-progress__nodes">
        <div
          v-for="n in flowNodes"
          :key="n.key"
          class="wf-node"
          :class="{ 'wf-node--current': n.current }"
        >
          <div class="wf-node__head">
            <b>{{ n.title }}</b>
            <a-tag v-for="b in n.badges" :key="b" size="small">{{ b }}</a-tag>
            <a-tag v-if="n.current" color="orangered" size="small">待你处理</a-tag>
          </div>
          <div v-if="n.hint" class="wf-node__hint">{{ n.hint }}</div>
          <div v-for="p in n.people" :key="p.key" class="wf-node__person">
            <span>{{ p.label }}</span>
            <a-tag :color="p.statusColor" size="small">{{ p.statusText }}</a-tag>
            <span v-if="p.comment" class="wf-node__comment">{{ p.comment }}</span>
            <span v-if="p.time" class="wf-node__time">{{ formatTime(p.time) }}</span>
          </div>
        </div>
      </div>

      <a-collapse v-if="timelineItems.length" :bordered="false" class="wf-progress__all">
        <a-collapse-item key="all">
          <template #header>全部动态</template>
          <a-timeline>
            <a-timeline-item
              v-for="it in timelineItems"
              :key="it.key"
              :label="it.time ? formatTime(it.time) : ''"
            >
              <b>{{ it.title }}</b>
              <span class="wf-progress__user">{{ it.user }}</span>
              <div v-if="it.content" class="wf-progress__content">{{ it.content }}</div>
            </a-timeline-item>
          </a-timeline>
        </a-collapse-item>
      </a-collapse>

      <div v-if="detail.attachments?.length" class="wf-progress__attachments">
        <div class="wf-progress__attachments-title">发起附件</div>
        <a
          v-for="a in detail.attachments"
          :key="String(a.id)"
          class="wf-progress__att"
          :href="a.url || '#'"
          target="_blank"
          rel="noopener"
        >
          {{ a.fileName || a.title || a.id }}
        </a>
      </div>

      <div v-if="myTask || canWithdraw" class="wf-progress__ops">
        <template v-if="myTask">
          <a-button type="primary" @click="openApprove">同意</a-button>
          <a-button status="danger" @click="openReject">驳回</a-button>
          <a-dropdown trigger="hover">
            <a-button>更多 <icon-park type="down" /></a-button>
            <template #content>
              <a-doption v-if="flags.allowAddSign" @click="openAddSign">加签</a-doption>
              <a-doption v-if="flags.allowTransfer" @click="openTransfer">转办</a-doption>
              <a-doption @click="openCc">知会</a-doption>
              <a-doption v-if="flags.allowRollback" @click="openRollback">回退</a-doption>
            </template>
          </a-dropdown>
        </template>
        <a-button v-if="canWithdraw" status="warning" @click="openWithdraw">撤回</a-button>
      </div>
    </template>

    <a-modal
      v-model:visible="opinionVisible"
      :title="opinionKind === 'approve' ? '同意' : opinionKind === 'reject' ? '驳回' : '撤回流程'"
      :on-before-ok="confirmOpinion"
      :ok-loading="saving"
    >
      <div v-if="phrases.length" class="wf-opinion-phrases">
        <a-tag
          v-for="p in phrases"
          :key="p.id"
          class="wf-opinion-phrase"
          :bordered="false"
          @click="pickPhrase(p)"
        >
          {{ p.text }}
        </a-tag>
      </div>
      <a-textarea v-model="opinionText" :max-length="500" placeholder="意见（可选）" allow-clear />
    </a-modal>

    <a-modal
      v-model:visible="targetVisible"
      :title="TARGET_TITLE[targetKind]"
      :on-before-ok="confirmTarget"
      :ok-loading="targetSaving"
    >
      <a-form :model="{}" layout="vertical">
        <a-form-item v-if="targetKind === 'addSign'" label="位置">
          <a-radio-group v-model="targetBefore" type="button" size="small">
            <a-radio :value="false">后加签（我办完再给对方）</a-radio>
            <a-radio :value="true">前加签（先给对方，再回到我）</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item v-if="targetKind === 'rollback'" label="回退到">
          <a-select v-model="targetNodeId" :options="rollbackOptions.map((t) => ({ value: t.nodeId, label: t.name }))" />
        </a-form-item>
        <a-form-item v-if="targetKind !== 'rollback'" label="接收人">
          <WorkflowRecipientPicker
            v-model:kind="targetRecipients.kind"
            v-model:model-value="targetRecipients.ids"
            :multiple="targetKind !== 'transfer'"
            :placeholder="targetKind === 'transfer' ? '选择转办人' : '选择接收人'"
          />
        </a-form-item>
        <a-form-item label="附言">
          <a-textarea v-model="targetComment" :max-length="500" placeholder="附言（可选）" allow-clear />
        </a-form-item>
      </a-form>
    </a-modal>
  </component>
</template>

<style scoped>
.wf-progress--embed {
  display: flex;
  flex-direction: column;
  min-height: 0;
  height: 100%;
}
.wf-progress__loading {
  padding: 40px;
  text-align: center;
}
.wf-progress__head {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
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
.wf-progress__all {
  margin-top: 8px;
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
.wf-progress__ops {
  position: sticky;
  bottom: 0;
  display: flex;
  gap: 8px;
  padding-top: 12px;
  margin-top: 16px;
  border-top: 1px solid var(--color-border-2);
  background: var(--color-bg-5);
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
