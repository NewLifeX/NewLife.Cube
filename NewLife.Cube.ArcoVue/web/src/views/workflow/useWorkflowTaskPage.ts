import { ref } from 'vue';
import type { WorkflowTaskItem } from '@cube/api-core';
import cubeApi from '@/api';
import { useWorkflowTaskList } from './useWorkflowTaskList';

/**
 * 任务中心页编排（OSC-26090347f1 T8c）：待办/已办列表加载 + 进度抽屉状态 + 打开业务记录。
 * .vue 只做模板绑定，本文件持有全部页面逻辑（构造即拉取）。
 */
export function useWorkflowTaskPage(kind: 'todo' | 'done') {
  const wl = useWorkflowTaskList(kind);
  const progressInstanceId = ref<number | null>(null);
  const progressVisible = ref(false);

  function openProgress(row: WorkflowTaskItem) {
    if (!row.instanceId) return;
    progressInstanceId.value = row.instanceId;
    progressVisible.value = true;
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
  async function resolveSubjectKey(instanceId: number): Promise<string> {
    try {
      const res = await cubeApi.workflow.instance(instanceId);
      const first = (res.data?.subjects ?? [])[0];
      return first?.entityKey ?? '';
    } catch {
      return '';
    }
  }

  // 构造即加载（页面 setup 即触发；不依赖 DOM）
  void wl.load();

  return {
    ...wl,
    progressInstanceId,
    progressVisible,
    openProgress,
    openRecord,
    refresh,
  };
}
