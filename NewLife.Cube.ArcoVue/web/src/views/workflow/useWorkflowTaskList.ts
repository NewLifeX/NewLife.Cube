import { computed, ref } from 'vue';
import type { WorkflowTaskItem, WorkflowBatchResultItem } from '@newlifex/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { formatDateTime } from '@/core/utils/datetime';
import { useAppStore } from '@/stores/app';

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

/**
 * 截止倒计时（壁钟差）：剩 X / 已超时 X；无 DueTime 返回 null。
 * 未设超时的任务 DueTime 是 DateTime.MinValue，JSON 为 "0001-01-01 00:00:00"。
 * 这种非 ISO 字符串会被 JS Date 解析成 2001-01-01，必须在解析前丢掉。
 */
export function dueText(
  dueTime: string | undefined | null,
  now = Date.now(),
): { text: string; overdue: boolean } | null {
  if (!dueTime) return null;
  const raw = dueTime.trim();
  if (!raw || raw.startsWith('0001')) return null;
  const iso = raw.includes('T') ? raw : raw.replace(' ', 'T');
  const t = new Date(iso).getTime();
  if (!Number.isFinite(t) || new Date(t).getFullYear() < 1970) return null;
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

/** 实体友好名（无 typeName 时回落路径末段） */
export function entityLabel(typeName?: string, typePath?: string): string {
  if (typeName?.trim()) return typeName.trim();
  const path = (typePath || '').trim();
  if (!path) return '—';
  const slash = path.lastIndexOf('/');
  return slash >= 0 && slash < path.length - 1 ? path.slice(slash + 1) : path;
}

/** 列表摘要：去掉粗 Markdown 标记后截断 */
export function summaryPlain(summary?: string, max = 80): string {
  if (!summary?.trim()) return '';
  const plain = summary
    .replace(/```[\s\S]*?```/g, ' ')
    .replace(/!\[[^\]]*]\([^)]*\)/g, ' ')
    .replace(/\[([^\]]*)]\([^)]*\)/g, '$1')
    .replace(/[#>*_`~]/g, '')
    .replace(/\s+/g, ' ')
    .trim();
  if (!plain) return '';
  return plain.length > max ? `${plain.slice(0, max)}…` : plain;
}

/** 是否办理节点任务（nodeType 由后端快照解析下发；缺省按审批处理） */
export function isHandleTask(row: WorkflowTaskItem): boolean {
  return row.nodeType === 'oa.handle';
}

/** 行操作矩阵（design §4.2）：审批行同意/驳回；办理行只有「已办理」；均可看进度；主操作不再打开进度 */
export interface WfRowActions {
  /** 同意（审批行） */
  canApprove: boolean;
  /** 驳回（仅审批行；办理行不显示） */
  canReject: boolean;
  /** 已办理（办理行） */
  canHandle: boolean;
  /** 更多：转交/加签/回退 */
  canMore: boolean;
  /** 进度入口（标题或「进度」） */
  canProgress: boolean;
}

export function rowActionsOf(row: WorkflowTaskItem, kind: 'todo' | 'done'): WfRowActions {
  const open = ['pending', 'active'].includes(String(row.status ?? '').toLowerCase());
  const actionable = kind === 'todo' && open;
  const handle = isHandleTask(row);
  return {
    canApprove: actionable && !handle,
    canReject: actionable && !handle,
    canHandle: actionable && handle,
    canMore: actionable,
    canProgress: true,
  };
}

export function useWorkflowTaskList(kind: 'todo' | 'done') {
  const rows = ref<WorkflowTaskItem[]>([]);
  const loading = ref(false);
  /** 任务雪花 Id：字符串透传 */
  const selected = ref<string[]>([]);
  const batchVisible = ref(false);
  const batchKind = ref<'approve' | 'reject'>('approve');
  const batchComment = ref('');
  const batchSaving = ref(false);
  const keyword = ref('');
  const page = ref(1);
  const pageSize = ref(20);
  const total = ref(0);

  async function load() {
    loading.value = true;
    try {
      const params = {
        page: page.value,
        pageSize: pageSize.value,
        q: keyword.value.trim() || undefined,
      };
      const res = kind === 'todo' ? await cubeApi.workflow.todo(params) : await cubeApi.workflow.done(params);
      const data = res?.data;
      if (Array.isArray(data)) {
        rows.value = data;
        total.value = data.length;
      } else {
        rows.value = data?.list ?? [];
        total.value = data?.total ?? rows.value.length;
      }
    } catch (err) {
      rows.value = [];
      total.value = 0;
      Message.error(formatApiError(err, '加载失败'));
    } finally {
      loading.value = false;
    }
  }

  function search(q: string) {
    keyword.value = q;
    page.value = 1;
    void load();
  }

  function changePage(p: number) {
    page.value = p;
    void load();
  }

  function changePageSize(size: number) {
    pageSize.value = size;
    page.value = 1;
    void load();
  }

  const hasBatch = computed(() => selected.value.length > 0);

  /** 勾选/取消（勾选仅可处理任务） */
  function toggleSelect(id: string | number, on: boolean) {
    const sid = String(id);
    if (on) {
      if (!selected.value.includes(sid)) selected.value = [...selected.value, sid];
    } else {
      selected.value = selected.value.filter((x) => x !== sid);
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

  /** 批量同意（后端 BatchApprove）/ 批量驳回（后端 BatchReject，跳过办理任务）；部分失败逐条汇总 */
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
        const res = await cubeApi.workflow.batchReject({ ids, comment });
        results.push(...((res.data ?? []) as WorkflowBatchResultItem[]));
      }
      const failed = results.filter((r) => !r.ok && !r.skipped);
      const skipped = results.filter((r) => r.skipped === true).length;
      const okCount = results.filter((r) => r.ok).length;
      const label = batchKind.value === 'approve' ? '同意' : '驳回';
      const skipTip = skipped > 0 ? `，已跳过 ${skipped} 条办理任务` : '';
      if (failed.length > 0) {
        Message.warning(
          `成功 ${okCount}/${results.length}${skipTip}。失败：${failed.map((f) => `#${f.id} ${f.error || ''}`).join('；')}`,
        );
      } else if (skipped > 0) {
        Message.warning(`已批量${label} ${okCount} 条${skipTip}`);
      } else {
        Message.success(`已批量${label} ${okCount} 条`);
      }
      batchVisible.value = false;
      selected.value = [];
      await load();
      void useAppStore().refreshWorkflowMeta();
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
    keyword,
    page,
    pageSize,
    total,
    search,
    changePage,
    changePageSize,
    toggleSelect,
    openBatch,
    confirmBatch,
    formatDateTime,
  };
}
