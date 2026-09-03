import { computed, ref } from 'vue';
import type { WorkflowTaskItem, WorkflowBatchResultItem } from '@cube/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { formatDateTime } from '@/core/utils/datetime';

/**
 * 待办/已办任务中心逻辑（OSC-26090347f1 T8c，IA §3.3）：
 * 加载 todo/done 任务列表；勾选批量同意/驳回（上限 50，逐条结果）；行点击打开进度抽屉。
 * 纯 helper（modeLabel/taskStatusMeta/dueText/taskTitle）可单测。
 */

/** 节点模式中文 */
export function modeLabel(mode: string | undefined): string {
  switch ((mode ?? '').toLowerCase()) {
    case 'or':
      return '或签';
    case 'and':
      return '会签';
    case 'sequence':
      return '依次签';
    default:
      return mode || '';
  }
}

/** 任务状态展示（后端大写 Pending/Active/Done/...） */
export function taskStatusMeta(status: string | undefined): { text: string; color: string } {
  switch ((status ?? '').toLowerCase()) {
    case 'pending':
      return { text: '待处理', color: 'orange' };
    case 'active':
      return { text: '处理中', color: 'blue' };
    case 'done':
      return { text: '已完成', color: 'green' };
    case 'rejected':
      return { text: '已驳回', color: 'red' };
    case 'cancelled':
      return { text: '已取消', color: 'gray' };
    case 'transferred':
      return { text: '已转办', color: 'gray' };
    default:
      return { text: status || '未知', color: 'gray' };
  }
}

/** 截止倒计时（壁钟差）：剩 X / 已超时 X；无 DueTime 返回 null */
export function dueText(
  dueTime: string | undefined,
  now = Date.now(),
): { text: string; overdue: boolean } | null {
  if (!dueTime) return null;
  const t = new Date(dueTime).getTime();
  if (!Number.isFinite(t)) return null;
  const diff = t - now;
  const abs = Math.abs(diff);
  const totalMinutes = Math.floor(abs / 60000);
  const days = Math.floor(totalMinutes / 1440);
  const hours = Math.floor((totalMinutes % 1440) / 60);
  const minutes = totalMinutes % 60;
  const part =
    days > 0 ? `${days} 天 ${hours} 时` : hours > 0 ? `${hours} 时 ${minutes} 分` : `${minutes} 分`;
  return diff >= 0 ? { text: `剩 ${part}`, overdue: false } : { text: `已超时 ${part}`, overdue: true };
}

/** 任务行标题兜底 */
export function taskTitle(t: WorkflowTaskItem): string {
  return t.title || `${t.typePath || '记录'} #${t.instanceId}`;
}

export function useWorkflowTaskList(kind: 'todo' | 'done') {
  const rows = ref<WorkflowTaskItem[]>([]);
  const loading = ref(false);
  const selected = ref<number[]>([]);
  const batchVisible = ref(false);
  const batchKind = ref<'approve' | 'reject'>('approve');
  const batchComment = ref('');
  const batchSaving = ref(false);
  const pageSize = 50;

  async function load() {
    loading.value = true;
    try {
      const res = kind === 'todo' ? await cubeApi.workflow.todo({ pageSize }) : await cubeApi.workflow.done({ pageSize });
      rows.value = res.data ?? [];
    } catch (err) {
      rows.value = [];
      Message.error(formatApiError(err, '加载失败'));
    } finally {
      loading.value = false;
    }
  }

  const hasBatch = computed(() => selected.value.length > 0);

  /** 勾选/取消（勾选仅可处理任务） */
  function toggleSelect(id: number, on: boolean) {
    if (on) {
      if (!selected.value.includes(id)) selected.value = [...selected.value, id];
    } else {
      selected.value = selected.value.filter((x) => x !== id);
    }
  }

  function openBatch(kind2: 'approve' | 'reject') {
    if (selected.value.length === 0) {
      Message.warning('请先勾选任务');
      return;
    }
    batchKind.value = kind2;
    batchComment.value = '';
    batchVisible.value = true;
  }

  /** 批量同意（后端 BatchApprove，≤50）/ 批量驳回（逐条 Reject 循环）；部分失败逐条汇总 */
  async function confirmBatch(): Promise<boolean> {
    if (selected.value.length === 0) return false;
    batchSaving.value = true;
    try {
      const ids = selected.value;
      const comment = batchComment.value || undefined;
      const results: WorkflowBatchResultItem[] = [];
      if (batchKind.value === 'approve') {
        const res = await cubeApi.workflow.batchApprove({ ids, comment });
        results.push(...((res.data ?? []) as WorkflowBatchResultItem[]));
      } else {
        for (const id of ids) {
          try {
            await cubeApi.workflow.reject(id, { comment });
            results.push({ id, ok: true });
          } catch (err) {
            results.push({ id, ok: false, error: formatApiError(err, '') });
          }
        }
      }
      const failed = results.filter((r) => !r.ok);
      const okCount = results.length - failed.length;
      const label = batchKind.value === 'approve' ? '同意' : '驳回';
      if (failed.length > 0) {
        Message.warning(
          `成功 ${okCount}/${results.length}。失败：${failed.map((f) => `#${f.id} ${f.error || ''}`).join('；')}`,
        );
      } else {
        Message.success(`已批量${label} ${okCount} 条`);
      }
      batchVisible.value = false;
      selected.value = [];
      await load();
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '批量操作失败'));
      return false;
    } finally {
      batchSaving.value = false;
    }
  }

  return {
    rows,
    loading,
    selected,
    batchVisible,
    batchKind,
    batchComment,
    batchSaving,
    hasBatch,
    load,
    toggleSelect,
    openBatch,
    confirmBatch,
    formatDateTime,
  };
}
