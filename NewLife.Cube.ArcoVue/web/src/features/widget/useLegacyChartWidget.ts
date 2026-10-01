import { computed, inject, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { ensureEchartsTheme, initEcharts, type EChartsHandle } from '@/core/utils/echartsTheme';
import { isFixedDeveloperChart, prepareFixedChartOption } from '@/core/utils/chartOptions';
import type { WidgetCardProps } from './context';
import { WIDGET_SURFACE_KEY } from './context';
import { applyChartData } from '@/core/utils/viewProfile';

function readChartIndex(widget: WidgetCardProps['widget']): number {
  const raw = (widget as Record<string, unknown>).chartIndex;
  return typeof raw === 'number' && raw >= 0 ? raw : 0;
}

export function useLegacyChartWidget(props: WidgetCardProps) {
  const ctx = inject(WIDGET_SURFACE_KEY, null);
  const chartEl = ref<HTMLElement | null>(null);
  let chart: EChartsHandle | null = null;
  let ro: ResizeObserver | null = null;
  let raf = 0;
  const fixed = computed(() => isFixedDeveloperChart(props.widget.id));

  const option = computed(() => {
    let raw: unknown = null;
    const data = ctx?.legacyChartData;
    if (Array.isArray(data) && data.length) {
      const idx = readChartIndex(props.widget);
      const hit = data[idx] ?? data[0];
      if (hit && typeof hit === 'object') raw = hit;
    }
    if (!raw) {
      const own = props.widget.chartOption;
      if (own && typeof own === 'object') {
        raw = fixed.value ? own : applyChartData(own, []);
      }
    }
    if (!raw || typeof raw !== 'object') return null;
    // 固定图：去标题/隐图例/boxplot 升序，避免箱体塌缩
    return fixed.value ? prepareFixedChartOption(raw) : raw;
  });
  const loading = computed(() => !!ctx?.legacyChartLoading);
  const error = computed(() => ctx?.legacyChartError || props.error || '');

  function disposeChart() {
    if (raf) cancelAnimationFrame(raf);
    raf = 0;
    ro?.disconnect();
    ro = null;
    chart?.dispose();
    chart = null;
  }

  function scheduleResize() {
    const el = chartEl.value;
    if (!el || !chart) return;
    const paint = () => {
      if (el.clientWidth > 0 && el.clientHeight > 0) chart?.resize();
    };
    paint();
    if (raf) cancelAnimationFrame(raf);
    raf = requestAnimationFrame(() => {
      paint();
      raf = requestAnimationFrame(paint);
    });
  }

  function bindResize(el: HTMLElement) {
    if (ro || typeof ResizeObserver === 'undefined') return;
    ro = new ResizeObserver(() => scheduleResize());
    ro.observe(el);
  }

  async function render() {
    await nextTick();
    const el = chartEl.value;
    const opt = option.value;
    if (!el || error.value || !opt || typeof opt !== 'object') {
      if (!opt) disposeChart();
      return;
    }
    try {
      await ensureEchartsTheme(undefined);
      if (!chart || chart.getDom() !== el) {
        chart?.dispose();
        chart = await initEcharts(el);
        bindResize(el);
      }
      chart.setOption(opt as import('echarts').EChartsOption, true);
      scheduleResize();
    } catch (err) {
      console.error('[LegacyChart] echarts render failed', err);
    }
  }

  onMounted(() => {
    void render();
  });

  watch(
    [() => option.value, chartEl, error],
    () => {
      void render();
    },
  );

  onBeforeUnmount(disposeChart);

  return { chartEl, loading, error, option, fixed };
}
