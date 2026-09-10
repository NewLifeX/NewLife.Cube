<script setup lang="ts">
/**
 * 审批进度抽屉（OSC-26090347f1 T8c，IA §3.2）：薄 .vue，逻辑见 useWorkflowProgressPanel。
 */
import { computed } from 'vue';
import { formatTime } from '@/core/utils/datetime';
import { useWorkflowProgressPanel } from './useWorkflowProgressPanel';
import WorkflowRecipientPicker from './WorkflowRecipientPicker.vue';
import type { WfId } from './useWorkflowProgress';

const props = defineProps<{
  modelValue: boolean;
  instanceId?: WfId | null;
}>();

const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void }>();

const visible = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v),
});

const {
  detail,
  loading,
  error,
  myTask,
  canWithdraw,
  statusMeta,
  timelineItems,
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
  confirmOpinion,
  pickPhrase,
  targetVisible,
  targetKind,
  targetComment,
  targetBefore,
  targetRecipients,
  targetSaving,
  confirmTarget,
  TARGET_TITLE,
} = useWorkflowProgressPanel({ visible, instanceId: () => props.instanceId ?? null });
</script>

<template>
  <a-drawer
    :visible="visible"
    :width="560"
    :footer="false"
    unmount-on-close
    @update:visible="(v: boolean) => (visible = v)"
  >
    <template #title>
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
        <a-typography-text v-if="detail.definition" type="secondary" style="font-size: 12px">
          v{{ detail.definition.version }}
        </a-typography-text>
      </div>

      <div v-if="detail.subjects?.length" class="wf-progress__subjects">
        <div v-for="s in detail.subjects" :key="String(s.id)" class="wf-progress__subject">
          <span class="wf-progress__subject-key">{{ s.entityKey }}</span>
          <a-typography-text ellipsis>{{ s.title || '—' }}</a-typography-text>
        </div>
      </div>

      <a-timeline v-if="timelineItems.length" class="wf-progress__timeline">
        <a-timeline-item
          v-for="it in timelineItems"
          :key="it.key"
          :label="it.time ? formatTime(it.time) : ''"
        >
          <div class="wf-progress__item">
            <b>{{ it.title }}</b>
            <span class="wf-progress__user">{{ it.user }}</span>
          </div>
          <div v-if="it.content" class="wf-progress__content">{{ it.content }}</div>
        </a-timeline-item>
      </a-timeline>
      <a-empty v-else description="暂无处理记录" />

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

      <!-- 操作条 -->
      <div v-if="myTask || canWithdraw" class="wf-progress__ops">
        <template v-if="myTask">
          <a-button type="primary" @click="openApprove">同意</a-button>
          <a-button status="danger" @click="openReject">驳回</a-button>
          <a-dropdown trigger="hover">
            <a-button>更多 <icon-park type="down" /></a-button>
            <template #content>
              <a-doption @click="openAddSign">加签</a-doption>
              <a-doption @click="openTransfer">转办</a-doption>
              <a-doption @click="openCc">知会</a-doption>
            </template>
          </a-dropdown>
        </template>
        <a-button v-if="canWithdraw" status="warning" @click="openWithdraw">
          撤回
        </a-button>
      </div>
    </template>

    <!-- 意见弹层：同意/驳回/撤回（审批节点不上传附件；发起附件见进度区） -->
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

    <!-- 接收人弹层：加签/转办/知会 -->
    <a-modal
      v-model:visible="targetVisible"
      :title="TARGET_TITLE[targetKind]"
      :on-before-ok="confirmTarget"
      :ok-loading="targetSaving"
    >
      <a-form :model="{}" layout="vertical">
        <a-form-item v-if="targetKind === 'addSign'" label="位置">
          <a-radio-group v-model="targetBefore" type="button" size="small">
            <a-radio :value="false">后加签</a-radio>
            <a-radio :value="true">前加签</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item label="接收人">
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
  </a-drawer>
</template>

<style scoped>
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
.wf-progress__timeline {
  margin-top: 8px;
}
.wf-progress__item {
  display: flex;
  gap: 8px;
  align-items: baseline;
}
.wf-progress__user {
  color: var(--color-text-3);
  font-size: 12px;
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
