import { ref } from 'vue';
import type { WorkflowTaskItem } from '@newlifex/api-core';
import cubeApi from '@/api';
import { useWorkflowTaskList } from './useWorkflowTaskList';
import { wfIdOf, type WfId } from './useWorkflowProgress';

/**
 * 任务中心页编排：列表、批量意见、进度抽屉。
 * 行内同意 / 驳回 / 更多由 WorkflowTaskActions 自己完成，这里不再持有弹层状态。
 */
export function useWorkflowTaskPage(kind: 'todo' | 'done') {
  const wl = useWorkflowTaskList(kind);
  const progressInstanceId = ref<string | null>(null);
  const progressVisible = ref(false);

  function openProgress(row: WorkflowTaskItem) {
    const id = wfIdOf(row.instanceId);
    if (!id) return;
    progressInstanceId.value = id;
    progressVisible.value = true;
  }

  function closeProgress() {
    progressVisible.value = false;
  }

  /** 常用语（行内意见气泡一键填入） */
  const phrases = ref<string[]>([]);
  async function loadPhrases() {
    try {
      const res = await cubeApi.workflow.phrases();
      phrases.value = (res.data ?? []).map((p) => p.text).filter(Boolean);
    } catch {
      phrases.value = [];
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
  void loadPhrases();

  return {
    ...wl,
    phrases,
    progressInstanceId,
    progressVisible,
    openProgress,
    closeProgress,
    openRecord,
    refresh,
  };
}
