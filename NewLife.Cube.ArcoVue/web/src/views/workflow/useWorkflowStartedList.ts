import { ref } from 'vue';
import type { WorkflowInstanceItem } from '@newlifex/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { wfIdOf } from './useWorkflowProgress';

/**
 * 我发起的审批页编排（OSC-26090347f1 T8c）：实例列表加载 + 进度抽屉状态。.vue 只做模板绑定。
 */
export function useWorkflowStartedList() {
  const rows = ref<WorkflowInstanceItem[]>([]);
  const loading = ref(false);
  const progressInstanceId = ref<string | null>(null);
  const progressVisible = ref(false);

  async function load() {
    loading.value = true;
    try {
      const res = await cubeApi.workflow.started({ pageSize: 50 });
      const list = res?.data;
      rows.value = Array.isArray(list) ? list : [];
    } catch (err) {
      rows.value = [];
      Message.error(formatApiError(err, '加载失败'));
    } finally {
      loading.value = false;
    }
  }

  function refresh() {
    void load();
  }

  function openProgress(row: WorkflowInstanceItem) {
    const id = wfIdOf(row.id);
    if (!id) return;
    progressInstanceId.value = id;
    progressVisible.value = true;
  }

  // 构造即加载
  void load();

  return {
    rows,
    loading,
    progressInstanceId,
    progressVisible,
    load,
    refresh,
    openProgress,
  };
}
