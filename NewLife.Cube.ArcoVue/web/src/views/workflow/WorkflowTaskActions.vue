<script setup lang="ts">
/**
 * 审批操作条：同意 / 驳回 / 更多 / 已办理 / 已阅 / 撤回。
 * 宽屏 480px 点击气泡，窄屏全宽对话框。待办列表与进度面板共用。
 */
import { computed, onBeforeUnmount, ref } from 'vue';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import WorkflowMoreActions from './WorkflowMoreActions.vue';
import WorkflowOpinionForm from './WorkflowOpinionForm.vue';
import type { RecipientKind } from './recipient';
import { rollbackTargets, type RollbackTarget } from './wfProgressFlow';
import { submitTaskMore, submitTaskOpinion, submitWithdraw, type MoreOp, type OpinionOp } from './workflowTaskOps';
import { wfIdOf, type WfId } from './useWorkflowProgress';

const props = withDefaults(
  defineProps<{
    /** approve：同意+驳回；handle：已办理；read：已阅；none：只有撤回等附加按钮 */
    variant?: 'approve' | 'handle' | 'read' | 'none';
    showMore?: boolean;
    showWithdraw?: boolean;
    /** text：表格行；solid：抽屉底部 */
    appearance?: 'text' | 'solid';
    taskId?: WfId | null;
    instanceId?: WfId | null;
    nodeId?: string;
    assigneeId?: number;
    candidate?: number[];
    phrases?: string[];
    /** 传入则不再请求实例 */
    rollbackOptions?: RollbackTarget[];
    allowTransfer?: boolean;
    allowAddSign?: boolean;
    allowRollback?: boolean;
    allowCc?: boolean;
    popupPosition?: 'br' | 'tl' | 'top' | 'bottom';
  }>(),
  {
    variant: 'approve',
    showMore: true,
    showWithdraw: false,
    appearance: 'text',
    phrases: () => [],
    allowTransfer: true,
    allowAddSign: true,
    allowRollback: true,
    allowCc: false,
    popupPosition: 'br',
  },
);

const emit = defineEmits<{ (e: 'done'): void }>();

const userId = computed(() => useUserStore().userInfo?.id as number | undefined);
const narrow = ref(typeof window !== 'undefined' && window.innerWidth < 768);
const onResize = () => {
  narrow.value = window.innerWidth < 768;
};
if (typeof window !== 'undefined') {
  window.addEventListener('resize', onResize);
  onBeforeUnmount(() => window.removeEventListener('resize', onResize));
}

const opinionVisible = ref(false);
const opinionKind = ref<OpinionOp>('approve');
const opinionText = ref('');
const opinionFiles = ref<File[]>([]);
const opinionSaving = ref(false);

const moreVisible = ref(false);
const moreMode = ref<MoreOp>('transfer');
const moreBefore = ref(false);
const moreText = ref('');
const moreRecipientKind = ref<RecipientKind>('users');
const moreRecipients = ref<number[]>([]);
const moreRollbackNode = ref('');
const moreOptions = ref<RollbackTarget[]>([]);
const moreSaving = ref(false);

const canMoreSubmit = computed(() =>
  moreMode.value === 'rollback' ? !!moreRollbackNode.value : moreRecipients.value.length > 0,
);

const opinionTitle = computed(() => {
  if (opinionKind.value === 'reject') return '驳回';
  if (opinionKind.value === 'handle') return '已办理';
  if (opinionKind.value === 'read') return '已阅';
  if (opinionKind.value === 'withdraw') return '撤回';
  return '同意';
});

function resetOpinion(kind: OpinionOp) {
  opinionKind.value = kind;
  opinionText.value = '';
  opinionFiles.value = [];
}

function onOpinionVisible(kind: OpinionOp, open: boolean) {
  if (open) {
    moreVisible.value = false;
    const same = opinionVisible.value && opinionKind.value === kind;
    if (!same) resetOpinion(kind);
    opinionVisible.value = true;
    return;
  }
  opinionVisible.value = false;
}

function firstMoreMode(): MoreOp {
  if (props.allowTransfer) return 'transfer';
  if (props.allowAddSign) return 'addSign';
  if (props.allowCc) return 'cc';
  return 'rollback';
}

async function onMoreVisible(open: boolean) {
  if (!open) {
    moreVisible.value = false;
    return;
  }
  const same = moreVisible.value;
  opinionVisible.value = false;
  if (!same) {
    moreMode.value = firstMoreMode();
    moreBefore.value = false;
    moreText.value = '';
    moreRecipientKind.value = 'users';
    moreRecipients.value = [];
    moreRollbackNode.value = '';
    moreOptions.value = props.rollbackOptions ?? [];
    if (!props.rollbackOptions) {
      const iid = wfIdOf(props.instanceId);
      if (iid) {
        try {
          const res = await cubeApi.workflow.instance(iid);
          moreOptions.value = rollbackTargets(res.data ?? null, props.nodeId);
        } catch {
          moreOptions.value = [];
        }
      }
    }
    moreRollbackNode.value = moreOptions.value[0]?.nodeId || '';
  }
  moreVisible.value = true;
}

async function submitOpinion() {
    opinionSaving.value = true;
    try {
      const kind = opinionKind.value;
      const ok =
        kind === 'withdraw'
          ? await submitWithdraw(props.instanceId, opinionText.value)
          : await submitTaskOpinion({
              taskId: props.taskId,
              instanceId: props.instanceId ?? undefined,
              userId: userId.value,
              assigneeId: props.assigneeId,
              candidate: props.candidate,
              kind,
              text: opinionText.value,
              files: opinionFiles.value,
            });
    if (ok) {
      opinionVisible.value = false;
      emit('done');
    }
  } finally {
    opinionSaving.value = false;
  }
}

async function submitMore() {
  moreSaving.value = true;
  try {
    const ok = await submitTaskMore({
      taskId: props.taskId,
      userId: userId.value,
      assigneeId: props.assigneeId,
      candidate: props.candidate,
      mode: moreMode.value,
      before: moreBefore.value,
      text: moreText.value,
      recipientKind: moreRecipientKind.value,
      recipients: moreRecipients.value,
      rollbackNode: moreRollbackNode.value,
    });
    if (ok) {
      moreVisible.value = false;
      emit('done');
    }
  } finally {
    moreSaving.value = false;
  }
}
</script>

<template>
  <div class="wf-actions" :class="appearance === 'solid' ? 'wf-actions--solid' : 'wf-actions--text'">
    <template v-if="variant === 'handle' || variant === 'read'">
      <a-popover
        v-if="!narrow"
        trigger="click"
        :position="popupPosition"
        :content-style="{ width: '480px' }"
        :popup-visible="opinionVisible && (opinionKind === 'handle' || opinionKind === 'read')"
        @popup-visible-change="(v: boolean) => onOpinionVisible(variant === 'handle' ? 'handle' : 'read', v)"
      >
        <a-button :type="appearance === 'solid' ? 'primary' : 'text'" :size="appearance === 'solid' ? 'small' : 'mini'">
          {{ variant === 'handle' ? '已办理' : '已阅' }}
        </a-button>
        <template #content>
          <WorkflowOpinionForm
            :kind="variant === 'handle' ? 'handle' : 'read'"
            :text="opinionText"
            :files="opinionFiles"
            :phrases="phrases"
            :saving="opinionSaving"
            @update:text="(v: string) => (opinionText = v)"
            @update:files="(v: File[]) => (opinionFiles = v)"
            @submit="submitOpinion()"
          />
        </template>
      </a-popover>
      <a-button
        v-else
        :type="appearance === 'solid' ? 'primary' : 'text'"
        :size="appearance === 'solid' ? 'small' : 'mini'"
        @click="onOpinionVisible(variant === 'handle' ? 'handle' : 'read', true)"
      >
        {{ variant === 'handle' ? '已办理' : '已阅' }}
      </a-button>
    </template>

    <template v-else-if="variant === 'approve'">
      <a-popover
        v-if="!narrow"
        trigger="click"
        :position="popupPosition"
        :content-style="{ width: '480px' }"
        :popup-visible="opinionVisible && opinionKind === 'approve'"
        @popup-visible-change="(v: boolean) => onOpinionVisible('approve', v)"
      >
        <a-button
          :type="appearance === 'solid' ? 'primary' : 'text'"
          :size="appearance === 'solid' ? 'small' : 'mini'"
          :status="appearance === 'text' ? 'success' : undefined"
        >
          同意
        </a-button>
        <template #content>
          <WorkflowOpinionForm
            kind="approve"
            :text="opinionText"
            :files="opinionFiles"
            :phrases="phrases"
            :saving="opinionSaving"
            @update:text="(v: string) => (opinionText = v)"
            @update:files="(v: File[]) => (opinionFiles = v)"
            @submit="submitOpinion()"
          />
        </template>
      </a-popover>
      <a-button
        v-else
        :type="appearance === 'solid' ? 'primary' : 'text'"
        :size="appearance === 'solid' ? 'small' : 'mini'"
        :status="appearance === 'text' ? 'success' : undefined"
        @click="onOpinionVisible('approve', true)"
      >
        同意
      </a-button>

      <a-popover
        v-if="!narrow"
        trigger="click"
        :position="popupPosition"
        :content-style="{ width: '480px' }"
        :popup-visible="opinionVisible && opinionKind === 'reject'"
        @popup-visible-change="(v: boolean) => onOpinionVisible('reject', v)"
      >
        <a-button
          status="danger"
          :type="appearance === 'solid' ? 'primary' : 'text'"
          :size="appearance === 'solid' ? 'small' : 'mini'"
        >
          驳回
        </a-button>
        <template #content>
          <WorkflowOpinionForm
            kind="reject"
            :text="opinionText"
            :files="opinionFiles"
            :phrases="phrases"
            :saving="opinionSaving"
            @update:text="(v: string) => (opinionText = v)"
            @update:files="(v: File[]) => (opinionFiles = v)"
            @submit="submitOpinion()"
          />
        </template>
      </a-popover>
      <a-button v-else status="danger" :type="appearance === 'solid' ? 'primary' : 'text'" :size="appearance === 'solid' ? 'small' : 'mini'" @click="onOpinionVisible('reject', true)">
        驳回
      </a-button>
    </template>

    <a-popover
      v-if="showMore && !narrow"
      trigger="click"
      :position="popupPosition"
      :content-style="{ width: '480px' }"
      :popup-visible="moreVisible"
      @popup-visible-change="onMoreVisible"
    >
      <a-button :type="appearance === 'solid' ? undefined : 'text'" :size="appearance === 'solid' ? 'small' : 'mini'">
        更多 <icon-park type="down" />
      </a-button>
      <template #content>
        <WorkflowMoreActions
          v-model:mode="moreMode"
          v-model:before="moreBefore"
          v-model:text="moreText"
          v-model:recipients="moreRecipients"
          v-model:recipient-kind="moreRecipientKind"
          v-model:rollback-node="moreRollbackNode"
          :options="moreOptions"
          :can-submit="canMoreSubmit"
          :saving="moreSaving"
          :allow-transfer="allowTransfer"
          :allow-add-sign="allowAddSign"
          :allow-rollback="allowRollback"
          :allow-cc="allowCc"
          @submit="submitMore()"
        />
      </template>
    </a-popover>
    <a-button
      v-else-if="showMore"
      :type="appearance === 'solid' ? undefined : 'text'"
      :size="appearance === 'solid' ? 'small' : 'mini'"
      @click="onMoreVisible(true)"
    >
      更多 <icon-park type="down" />
    </a-button>

    <a-popover
      v-if="showWithdraw && !narrow"
      trigger="click"
      :position="popupPosition"
      :content-style="{ width: '480px' }"
      :popup-visible="opinionVisible && opinionKind === 'withdraw'"
      @popup-visible-change="(v: boolean) => onOpinionVisible('withdraw', v)"
    >
      <a-button status="warning" :type="appearance === 'solid' ? undefined : 'text'" :size="appearance === 'solid' ? 'small' : 'mini'">
        撤回
      </a-button>
      <template #content>
        <WorkflowOpinionForm
          kind="withdraw"
          :allow-files="false"
          :text="opinionText"
          :files="opinionFiles"
          :phrases="phrases"
          :saving="opinionSaving"
          @update:text="(v: string) => (opinionText = v)"
          @update:files="(v: File[]) => (opinionFiles = v)"
          @submit="submitOpinion()"
        />
      </template>
    </a-popover>
    <a-button
      v-else-if="showWithdraw"
      status="warning"
      :type="appearance === 'solid' ? undefined : 'text'"
      :size="appearance === 'solid' ? 'small' : 'mini'"
      @click="onOpinionVisible('withdraw', true)"
    >
      撤回
    </a-button>

    <a-modal
      v-if="narrow"
      :visible="opinionVisible"
      width="100%"
      :footer="false"
      :title="opinionTitle"
      unmount-on-close
      @cancel="opinionVisible = false"
    >
      <WorkflowOpinionForm
        :kind="opinionKind"
        :allow-files="opinionKind !== 'withdraw'"
        :text="opinionText"
        :files="opinionFiles"
        :phrases="phrases"
        :saving="opinionSaving"
        @update:text="(v: string) => (opinionText = v)"
        @update:files="(v: File[]) => (opinionFiles = v)"
        @submit="submitOpinion()"
      />
    </a-modal>
    <a-modal
      v-if="narrow"
      :visible="moreVisible"
      width="100%"
      :footer="false"
      title="更多操作"
      unmount-on-close
      @cancel="moreVisible = false"
    >
      <WorkflowMoreActions
        v-model:mode="moreMode"
        v-model:before="moreBefore"
        v-model:text="moreText"
        v-model:recipients="moreRecipients"
        v-model:recipient-kind="moreRecipientKind"
        v-model:rollback-node="moreRollbackNode"
        :options="moreOptions"
        :can-submit="canMoreSubmit"
        :saving="moreSaving"
        :allow-transfer="allowTransfer"
        :allow-add-sign="allowAddSign"
        :allow-rollback="allowRollback"
        :allow-cc="allowCc"
        @submit="submitMore()"
      />
    </a-modal>
  </div>
</template>

<style scoped>
.wf-actions {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}
.wf-actions--text {
  gap: 4px;
}
</style>
