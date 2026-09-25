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
  const keyword = ref('');
  const page = ref(1);
  const pageSize = ref(20);
  const total = ref(0);

  async function load() {
    loading.value = true;
    try {
      const res = await cubeApi.workflow.started({
        page: page.value,
        pageSize: pageSize.value,
        q: keyword.value.trim() || undefined,
      });
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

  function refresh() {
    void load();
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
    keyword,
    page,
    pageSize,
    total,
    load,
    refresh,
    search,
    changePage,
    changePageSize,
    openProgress,
  };
}
