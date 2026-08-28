import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { ensureEchartsTheme, initEcharts } from '@/core/utils/echartsTheme';
import type { WidgetCardProps } from './context';
import { buildMiniChartOption, type ChartItem } from './chartTemplates';
import { minHeightOf } from './useWidgetGrid';

/** 兼容 items/Items 与 key/Key 混用 */
export function readChartItems(result: unknown): ChartItem[] {
  const r = result as Record<string, unknown> | null | undefined;
  if (!r) return [];
  const raw = r.items ?? r.Items;
  if (!Array.isArray(raw)) return [];
  return raw.map((it) => {
    const o = (it && typeof it === 'object' ? it : {}) as Record<string, unknown>;
    return {
      key: String(o.key ?? o.Key ?? ''),
      label: String(o.label ?? o.Label ?? o.key ?? o.Key ?? ''),
      value: o.value ?? o.Value,
    };
  });
}

export function useMiniChartWidget(props: WidgetCardProps) {
  const chartEl = ref<HTMLElement | null>(null);
  let chart: ReturnType<typeof initEcharts> | null = null;
  let ro: ResizeObserver | null = null;
  let raf = 0;

  const items = computed(() => readChartItems(props.result));
  const empty = computed(() => !props.loading && !props.error && items.value.length === 0);
  const option = computed(() =>
    buildMiniChartOption(props.widget.style?.chartType ?? 'bar', items.value),
  );
  const chartHeight = computed(() => {
    const cell = minHeightOf(props.widget.layout.h ?? 3);
    return Math.max(72, cell - 36);
  });

  function disposeChart() {
    if (raf) cancelAnimationFrame(raf);
    raf = 0;
    ro?.disconnect();
    ro = null;
    chart?.dispose();
    chart = null;
  }

  function bindResize(el: HTMLElement) {
    if (ro || typeof ResizeObserver === 'undefined') return;
    ro = new ResizeObserver(() => {
      if (el.clientWidth > 0) chart?.resize();
    });
    ro.observe(el);
  }

  async function render() {
    await nextTick();
    const el = chartEl.value;
    if (!el || props.loading || props.error || items.value.length === 0) {
      if (items.value.length === 0) disposeChart();
      return;
    }
    try {
      await ensureEchartsTheme(undefined);
      if (!chart || chart.getDom() !== el) {
        chart?.dispose();
        chart = initEcharts(el);
      }
      chart.setOption(option.value, true);
      const paint = () => {
        if (el.clientWidth > 0) chart?.resize();
      };
      paint();
      raf = requestAnimationFrame(() => {
        paint();
        raf = requestAnimationFrame(paint);
      });
      bindResize(el);
    } catch (err) {
      console.error('[MiniChart] echarts render failed', err);
    }
  }

  onMounted(() => {
    void render();
  });

  watch(
    [
      () => option.value,
      chartEl,
      () => props.loading,
      () => props.error,
      () => items.value.length,
      chartHeight,
    ],
    () => {
      void render();
    },
  );

  onBeforeUnmount(() => disposeChart());

  return { chartEl, chartHeight, option, empty, items };
}
