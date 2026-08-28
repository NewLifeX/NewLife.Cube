import { computed, inject, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import { ensureEchartsTheme, initEcharts } from '@/core/utils/echartsTheme';
import type { WidgetCardProps } from './context';
import { WIDGET_SURFACE_KEY } from './context';
import { applyChartData } from '@/core/utils/viewProfile';

export function useLegacyChartWidget(props: WidgetCardProps) {
  const ctx = inject(WIDGET_SURFACE_KEY, null);
  const chartEl = ref<HTMLElement | null>(null);
  let chart: ReturnType<typeof initEcharts> | null = null;
  const option = computed(() => {
    const data = ctx?.legacyChartData;
    if (Array.isArray(data) && data.length) return data[0];
    const opt = props.widget.chartOption;
    if (opt && typeof opt === 'object') return applyChartData(opt, []);
    return null;
  });
  const loading = computed(() => !!ctx?.legacyChartLoading);
  const error = computed(() => ctx?.legacyChartError || props.error || '');

  watch(
    () => option.value,
    async (opt) => {
      await nextTick();
      if (!chartEl.value || !opt || typeof opt !== 'object') return;
      await ensureEchartsTheme(undefined);
      if (!chart) chart = initEcharts(chartEl.value);
      chart.setOption(opt as import('echarts').EChartsOption, true);
    },
    { immediate: true },
  );

  onBeforeUnmount(() => {
    chart?.dispose();
    chart = null;
  });

  return { chartEl, loading, error, option };
}
