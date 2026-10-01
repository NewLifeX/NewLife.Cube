import { computed, ref, watch, type ComputedRef, type Ref } from 'vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { resolveUserStatChartView } from '@/core/utils/chartOptions';

function isUserStatType(typePath: string): boolean {
  return typePath.replace(/^\/+|\/+$/g, '').toLowerCase() === 'admin/userstat';
}

/**
 * 仅 Admin/UserStat 请求 GetChartData。参数与列表关键字、筛选同一对象。
 * 空数组不画图；最多前 2 张；失败只出警告。渲染挂在 InsightPanel 洞察区。
 */
export function useUserStatChart(
  typePath: Ref<string> | ComputedRef<string>,
  requestParams: Ref<Record<string, unknown>> | ComputedRef<Record<string, unknown>>,
) {
  const active = computed(() => isUserStatType(typePath.value));
  const charts = ref<unknown[]>([]);
  const error = ref('');
  let seq = 0;

  async function load() {
    const my = ++seq;
    if (!active.value) {
      charts.value = [];
      error.value = '';
      return;
    }
    try {
      const res = await cubeApi.page.getChartData(typePath.value, requestParams.value);
      if (my !== seq) return;
      const view = resolveUserStatChartView(false, res.data);
      charts.value = view.charts;
      error.value = '';
    } catch (err) {
      if (my !== seq) return;
      const view = resolveUserStatChartView(true, null);
      charts.value = view.charts;
      error.value = formatApiError(err, '图表加载失败');
    }
  }

  watch(
    [active, requestParams],
    () => {
      void load();
    },
    { deep: true, immediate: true },
  );

  return { active, charts, error };
}
