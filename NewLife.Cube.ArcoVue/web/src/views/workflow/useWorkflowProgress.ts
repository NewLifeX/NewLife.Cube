import { computed, ref, unref, watch, type Ref } from 'vue';
import type { WorkflowInstanceDetail, WorkflowTaskItem } from '@cube/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';

/**
 * 审批进度面板逻辑（OSC-26090347f1 T8c，IA §3.2）：
 * 加载实例详情（主体+任务+意见时间轴），识别当前用户可办任务并驱动同意/驳回/加签/转办/知会/撤回。
 * 纯 helper（instanceStatusMeta/actionLabel/pickMyTask）可单测。
 * 实例/任务 Id 全程 string，禁止 Number() 丢雪花精度。
 */

export type WfId = string | number;

export function wfIdOf(id: WfId | null | undefined): string {
  if (id == null || id === '') return '';
  if (typeof id === 'string') return id.trim();
  if (typeof id === 'number' && Number.isFinite(id) && id > 0) return String(Math.trunc(id));
  const s = String(id).trim();
  return s === '0' ? '' : s;
}

/** 实例状态展示（后端大写 Running/Approved/...） */
export function instanceStatusMeta(status: string | undefined): { text: string; color: string } {
  switch ((status ?? '').toLowerCase()) {
    case 'running':
      return { text: '审批中', color: 'orange' };
    case 'approved':
      return { text: '已通过', color: 'green' };
    case 'rejected':
      return { text: '已驳回', color: 'red' };
    case 'withdrawn':
      return { text: '已撤回', color: 'gray' };
    case 'cancelled':
      return { text: '已作废', color: 'gray' };
    default:
      return { text: status || '未知', color: 'gray' };
  }
}

/** 意见动作中文 */
export function actionLabel(action: string | undefined): string {
  switch ((action ?? '').toLowerCase()) {
    case 'start':
      return '发起';
    case 'approve':
      return '同意';
    case 'reject':
      return '驳回';
    case 'addsign':
    case 'add_sign':
      return '加签';
    case 'transfer':
      return '转办';
    case 'cc':
      return '知会';
    case 'rollback':
      return '回退';
    case 'withdraw':
      return '撤回';
    case 'cancel':
      return '作废';
    default:
      return action || '处理';
  }
}

/** 从详情任务中挑出当前用户可办任务（候选人 or 被指派；依次签未轮到 visible=false 排除） */
export function pickMyTask(
  detail: WorkflowInstanceDetail | null,
  userId: number | undefined,
): WorkflowTaskItem | null {
  if (!detail?.tasks || !userId) return null;
  const active = ['pending', 'active'];
  for (const t of detail.tasks) {
    if (!t.visible && t.visible !== undefined) continue;
    const st = String(t.status ?? '').toLowerCase();
    if (!active.includes(st)) continue;
    const isCandidate = (t.candidate ?? []).includes(userId);
    const isAssignee = t.assigneeId === userId;
    if (isCandidate || isAssignee) return t;
  }
  return null;
}

/** 面板实例详情加载与操作；instanceId 支持 ref 或普通值（雪花 string） */
export function useWorkflowProgress(
  instanceId: Ref<WfId | null | undefined> | WfId | null | undefined,
) {
  const userStore = useUserStore();
  const detail = ref<WorkflowInstanceDetail | null>(null);
  const loading = ref(false);
  const error = ref('');
  const seq = ref(0);

  async function load() {
    const id = wfIdOf(unref(instanceId));
    if (!id) {
      detail.value = null;
      return;
    }
    const s = ++seq.value;
    loading.value = true;
    error.value = '';
    try {
      const res = await cubeApi.workflow.instance(id);
      if (s !== seq.value) return;
      detail.value = res.data ?? null;
    } catch (err) {
      if (s !== seq.value) return;
      error.value = formatApiError(err, '加载失败');
      detail.value = null;
    } finally {
      if (s === seq.value) loading.value = false;
    }
  }

  watch(
    () => wfIdOf(unref(instanceId)),
    () => void load(),
    { immediate: true },
  );

  const userId = computed(() => userStore.userInfo?.id as number | undefined);
  const isStarter = computed(
    () => !!detail.value && !!userId.value && detail.value.starterId === userId.value,
  );
  /** 我当前待办任务（无则操作条隐藏） */
  const myTask = computed(() => pickMyTask(detail.value, userId.value));
  /** 发起人可撤回（实例 Running） */
  const canWithdraw = computed(
    () => isStarter.value && String(detail.value?.status ?? '').toLowerCase() === 'running',
  );
  const statusMeta = computed(() => instanceStatusMeta(detail.value?.status));

  /** 单任务操作；或签未认领先 Claim 再执行 */
  async function runTaskAction(action: (id: string) => Promise<unknown>) {
    const task = myTask.value;
    const taskId = wfIdOf(task?.id);
    if (!task || !taskId) return false;
    try {
      if (task.assigneeId !== userId.value && (task.candidate ?? []).includes(userId.value as number)) {
        try {
          await cubeApi.workflow.claim(taskId);
        } catch {
          /* 后端或签引擎允许未认领直接动作时忽略 */
        }
      }
      await action(taskId);
      Message.success('操作成功');
      await load();
      void useAppStore().refreshWorkflowMeta();
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '操作失败'));
      return false;
    }
  }

  async function approve(comment: string) {
    return runTaskAction((id) => cubeApi.workflow.approve(id, { comment: comment || undefined }));
  }

  async function reject(comment: string) {
    return runTaskAction((id) => cubeApi.workflow.reject(id, { comment: comment || undefined }));
  }

  /** 加签/转办/知会（需先选接收人）。addSign 支持 before 前/后加签 */
  async function transferAction(
    kind: 'addSign' | 'transfer' | 'cc',
    payload: { kind: string; ids: number[]; comment?: string; before?: boolean },
  ) {
    const task = myTask.value;
    const taskId = wfIdOf(task?.id);
    if (!task || !taskId) return false;
    const to = { kind: payload.kind, users: [], roles: [], departments: [] } as Record<string, unknown>;
    to[payload.kind] = payload.ids;
    try {
      const fn =
        kind === 'addSign'
          ? cubeApi.workflow.addSign
          : kind === 'transfer'
            ? cubeApi.workflow.transfer
            : cubeApi.workflow.cc;
      await fn(taskId, {
        to: to as never,
        comment: payload.comment || undefined,
        ...(kind === 'addSign' ? { before: payload.before ?? false } : {}),
      });
      Message.success('操作成功');
      await load();
      void useAppStore().refreshWorkflowMeta();
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '操作失败'));
      return false;
    }
  }

  /** 发起人撤回 */
  async function withdraw(comment: string) {
    const id = wfIdOf(detail.value?.id);
    if (!id) return false;
    try {
      await cubeApi.workflow.withdraw(id, { comment: comment || undefined });
      Message.success('已撤回');
      await load();
      void useAppStore().refreshWorkflowMeta();
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '撤回失败'));
      return false;
    }
  }

  return {
    detail,
    loading,
    error,
    myTask,
    canWithdraw,
    isStarter,
    statusMeta,
    userId,
    load,
    approve,
    reject,
    transferAction,
    withdraw,
  };
}
