import { computed, reactive, ref, watch, type Ref } from 'vue';
import type { WorkflowPhrase } from '@newlifex/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { actionLabel, useWorkflowProgress, wfIdOf, type WfId } from './useWorkflowProgress';
import type { RecipientKind } from './recipient';

/**
 * 审批进度抽屉编排（OSC-26090347f1 T8c，IA §3.2）：.vue 只做模板绑定。
 * 抽屉可见才加载实例；切换实例重置意见/接收人弹层；持有常用语与意见/接收人弹层状态与提交动作。
 */
export function useWorkflowProgressPanel(input: {
  /** 抽屉可见性（computed） */
  visible: Ref<boolean>;
  /** 目标实例 Id（getter，雪花 string） */
  instanceId: () => WfId | null | undefined;
}) {
  /** 当前可见实例 Id（抽屉关闭置空停止加载） */
  const activeInstanceId = ref<string | null>(null);

  /** 意见弹层（同意/驳回/撤回共用） */
  const opinionVisible = ref(false);
  const opinionKind = ref<'approve' | 'reject' | 'withdraw'>('approve');
  const opinionText = ref('');
  const phrases = ref<WorkflowPhrase[]>([]);
  const saving = ref(false);

  /** 接收人弹层（加签/转办/知会） */
  const targetVisible = ref(false);
  const targetKind = ref<'addSign' | 'transfer' | 'cc'>('addSign');
  const targetComment = ref('');
  const targetBefore = ref(false);
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

  function openTarget(kind: 'addSign' | 'transfer' | 'cc') {
    if (!wf.myTask.value) {
      Message.warning('当前无待办任务');
      return;
    }
    targetKind.value = kind;
    targetComment.value = '';
    targetBefore.value = false;
    targetRecipients.kind = 'users';
    targetRecipients.ids = [];
    targetVisible.value = true;
  }

  async function confirmTarget(): Promise<boolean> {
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
      const ok = await wf.transferAction(targetKind.value, payload);
      if (ok) targetVisible.value = false;
      return ok;
    } finally {
      targetSaving.value = false;
    }
  }

  /**
   * 时间轴：优先展示「发起流程」合成条（摘要/标题/发起意见）；
   * 跳过 comments 中 action=start，避免与合成条重复。
   */
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
    const comments = d.comments ?? [];
    const showSyntheticStart = !!(d.summary || d.startComment || d.title || d.createTime);
    const startContent =
      [d.title ? `标题：${d.title}` : '', d.summary || '', d.startComment || '']
        .filter(Boolean)
        .join('\n') || undefined;
    if (showSyntheticStart) {
      items.push({
        key: 'start',
        title: '发起流程',
        user: d.starterId ? `发起人 #${d.starterId}` : '发起人',
        time: d.createTime,
        content: startContent,
      });
    }
    for (const c of comments) {
      // 合成条已含发起信息时，跳过 comments 中 action=start，避免双条
      if (showSyntheticStart && String(c.action ?? '').toLowerCase() === 'start') continue;
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
  };

  return {
    detail: wf.detail,
    loading: wf.loading,
    error: wf.error,
    myTask: wf.myTask,
    canWithdraw: wf.canWithdraw,
    statusMeta: wf.statusMeta,
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
  };
}
