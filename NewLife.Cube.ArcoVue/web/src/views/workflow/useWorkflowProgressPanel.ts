import { computed, reactive, ref, watch, type Ref } from 'vue';
import type { WorkflowPhrase } from '@newlifex/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { actionLabel, useWorkflowProgress, wfIdOf, type WfId } from './useWorkflowProgress';
import { buildProgressFlow, nodeFlags, rollbackTargets } from './wfProgressFlow';
import type { RecipientKind } from './recipient';

/**
 * 审批进度面板编排（OSC-26090347f1 T8c / T13）：节点流程 + 意见折叠 + 回退。
 */
export function useWorkflowProgressPanel(input: {
  visible: Ref<boolean>;
  instanceId: () => WfId | null | undefined;
}) {
  const activeInstanceId = ref<string | null>(null);

  const opinionVisible = ref(false);
  const opinionKind = ref<'approve' | 'reject' | 'withdraw'>('approve');
  const opinionText = ref('');
  const phrases = ref<WorkflowPhrase[]>([]);
  const saving = ref(false);

  const targetVisible = ref(false);
  const targetKind = ref<'addSign' | 'transfer' | 'cc' | 'rollback'>('addSign');
  const targetComment = ref('');
  const targetBefore = ref(false);
  const targetNodeId = ref('');
  const targetRecipients = reactive<{ kind: RecipientKind; ids: number[] }>({
    kind: 'users',
    ids: [],
  });
  const targetSaving = ref(false);

  watch(
    () => (input.visible.value ? wfIdOf(input.instanceId()) || null : null),
    (v) => {
      activeInstanceId.value = v;
      opinionText.value = '';
      targetRecipients.kind = 'users';
      targetRecipients.ids = [];
    },
    { immediate: true },
  );

  const wf = useWorkflowProgress(activeInstanceId);

  const flowNodes = computed(() => buildProgressFlow(wf.detail.value, wf.userId.value));
  const flags = computed(() => nodeFlags(wf.detail.value, wf.myTask.value?.nodeId));
  const rollbackOptions = computed(() => rollbackTargets(wf.detail.value, wf.myTask.value?.nodeId));
  /** 当前用户可写字段（InstanceDetail.writableFields；无待办则为空） */
  const writableFieldNames = computed(() => {
    const d = wf.detail.value as { writableFields?: unknown } | null;
    if (!d || !wf.myTask.value) return [] as string[];
    const raw = d.writableFields;
    if (!Array.isArray(raw)) return [];
    return raw.map(String).filter(Boolean);
  });

  async function loadPhrases() {
    try {
      const res = await cubeApi.workflow.phrases();
      phrases.value = res.data ?? [];
    } catch {
      phrases.value = [];
    }
  }
  void loadPhrases();

  function pickPhrase(p: WorkflowPhrase) {
    opinionText.value = p.text;
  }

  function openOpinion(kind: 'approve' | 'reject' | 'withdraw') {
    if (!wf.myTask.value && kind !== 'withdraw') {
      Message.warning('当前无待办任务');
      return;
    }
    opinionKind.value = kind;
    opinionText.value = '';
    opinionVisible.value = true;
  }

  async function confirmOpinion(): Promise<boolean> {
    saving.value = true;
    try {
      const ok =
        opinionKind.value === 'withdraw'
          ? await wf.withdraw(opinionText.value)
          : opinionKind.value === 'approve'
            ? await wf.approve(opinionText.value)
            : await wf.reject(opinionText.value);
      if (ok) {
        opinionVisible.value = false;
        await wf.load();
      }
      return ok;
    } finally {
      saving.value = false;
    }
  }

  function openTarget(kind: 'addSign' | 'transfer' | 'cc' | 'rollback') {
    if (!wf.myTask.value) {
      Message.warning('当前无待办任务');
      return;
    }
    if (kind === 'rollback' && !rollbackOptions.value.length) {
      Message.warning('还没有可回退的已办节点');
      return;
    }
    targetKind.value = kind;
    targetComment.value = '';
    targetBefore.value = false;
    targetNodeId.value = rollbackOptions.value[0]?.nodeId || '';
    targetRecipients.kind = 'users';
    targetRecipients.ids = [];
    targetVisible.value = true;
  }

  async function confirmTarget(): Promise<boolean> {
    if (targetKind.value === 'rollback') {
      if (!targetNodeId.value) {
        Message.warning('请选择回退到哪一步');
        return false;
      }
      targetSaving.value = true;
      try {
        const ok = await wf.rollback(targetNodeId.value, targetComment.value);
        if (ok) targetVisible.value = false;
        return ok;
      } finally {
        targetSaving.value = false;
      }
    }
    if (targetRecipients.ids.length === 0) {
      Message.warning('请选择接收人');
      return false;
    }
    targetSaving.value = true;
    try {
      const payload = {
        kind: targetRecipients.kind,
        ids: targetRecipients.ids,
        comment: targetComment.value || undefined,
        ...(targetKind.value === 'addSign' ? { before: targetBefore.value } : {}),
      };
      const ok = await wf.transferAction(targetKind.value as 'addSign' | 'transfer' | 'cc', payload);
      if (ok) targetVisible.value = false;
      return ok;
    } finally {
      targetSaving.value = false;
    }
  }

  const timelineItems = computed(() => {
    const items: {
      key: string;
      title: string;
      user: string;
      time?: string;
      content?: string;
    }[] = [];
    const d = wf.detail.value;
    if (!d) return items;
    for (const c of d.comments ?? []) {
      items.push({
        key: `c${c.id}`,
        title: actionLabel(c.action),
        user: c.createUser || `#${c.taskId ?? ''}`,
        time: c.createTime,
        content: c.content || undefined,
      });
    }
    return items;
  });

  const TARGET_TITLE: Record<string, string> = {
    addSign: '加签',
    transfer: '转办',
    cc: '知会',
    rollback: '回退到已办节点',
  };

  return {
    detail: wf.detail,
    loading: wf.loading,
    error: wf.error,
    myTask: wf.myTask,
    canWithdraw: wf.canWithdraw,
    statusMeta: wf.statusMeta,
    flowNodes,
    flags,
    rollbackOptions,
    writableFieldNames,
    timelineItems,
    opinionVisible,
    opinionKind,
    opinionText,
    phrases,
    saving,
    openApprove: () => openOpinion('approve'),
    openReject: () => openOpinion('reject'),
    openWithdraw: () => openOpinion('withdraw'),
    openAddSign: () => openTarget('addSign'),
    openTransfer: () => openTarget('transfer'),
    openCc: () => openTarget('cc'),
    openRollback: () => openTarget('rollback'),
    openRecord: wf.openRecord,
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
  };
}
