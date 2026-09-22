import { ref } from 'vue';
import type { WorkflowTaskItem } from '@newlifex/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { useAppStore } from '@/stores/app';
import { useWorkflowTaskList } from './useWorkflowTaskList';
import { wfIdOf, type WfId } from './useWorkflowProgress';

/**
 * 任务中心页编排（OSC-26090347f1 T8c）：点「进度」/行打开进度抽屉（与「我发起的」一致，不再宽屏分栏）。
 */
export function useWorkflowTaskPage(kind: 'todo' | 'done') {
  const wl = useWorkflowTaskList(kind);
  const progressInstanceId = ref<string | null>(null);
  const progressVisible = ref(false);

  /** 行级审批弹层 */
  const rowOpinionVisible = ref(false);
  const rowOpinionKind = ref<'approve' | 'reject'>('approve');
  const rowOpinionText = ref('');
  const rowOpinionSaving = ref(false);
  const rowOpinionTask = ref<WorkflowTaskItem | null>(null);

  function openProgress(row: WorkflowTaskItem) {
    const id = wfIdOf(row.instanceId);
    if (!id) return;
    progressInstanceId.value = id;
    progressVisible.value = true;
  }

  function closeProgress() {
    progressVisible.value = false;
  }

  function openRowOpinion(row: WorkflowTaskItem, kind2: 'approve' | 'reject') {
    rowOpinionTask.value = row;
    rowOpinionKind.value = kind2;
    rowOpinionText.value = '';
    rowOpinionVisible.value = true;
  }

  async function confirmRowOpinion(): Promise<boolean> {
    const task = rowOpinionTask.value;
    const taskId = wfIdOf(task?.id);
    if (!task || !taskId) return false;
    rowOpinionSaving.value = true;
    try {
      if (rowOpinionKind.value === 'approve') {
        await cubeApi.workflow.approve(taskId, { comment: rowOpinionText.value || undefined });
      } else {
        await cubeApi.workflow.reject(taskId, { comment: rowOpinionText.value || undefined });
      }
      Message.success(rowOpinionKind.value === 'approve' ? '已同意' : '已驳回');
      rowOpinionVisible.value = false;
      await wl.load();
      void useAppStore().refreshWorkflowMeta();
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '操作失败'));
      return false;
    } finally {
      rowOpinionSaving.value = false;
    }
  }

  function refresh() {
    void wl.load();
  }

  /** 打开业务记录：跳实体列表（详情由通用路由承接），仅当 typePath 已知 */
  async function openRecord(row: WorkflowTaskItem) {
    if (!row.typePath) return;
    const key = await resolveSubjectKey(row.instanceId);
    const base = `/${String(row.typePath).replace(/^\/+/, '')}`;
    const target = key ? `${base}/${encodeURIComponent(key)}` : base;
    window.location.hash = `#${target}`;
  }

  /** 取实例首条主体 EntityKey 作为记录主键 */
  async function resolveSubjectKey(instanceId: WfId): Promise<string> {
    const id = wfIdOf(instanceId);
    if (!id) return '';
    try {
      const res = await cubeApi.workflow.instance(id);
      const first = (res.data?.subjects ?? [])[0];
      return first?.entityKey ?? '';
    } catch {
      return '';
    }
  }

  void wl.load();

  return {
    ...wl,
    progressInstanceId,
    progressVisible,
    openProgress,
    closeProgress,
    openRecord,
    refresh,
    rowOpinionVisible,
    rowOpinionKind,
    rowOpinionText,
    rowOpinionSaving,
    openRowOpinion,
    confirmRowOpinion,
  };
}
