import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import type { WidgetQueryResult } from '@cube/api-core';
import { buildDrillViewFilter } from '@/core/utils/searchFilters';
import { ensureEchartsTheme, initEcharts } from '@/core/utils/echartsTheme';
import type { WidgetCardProps } from './context';
import { buildMiniChartOption, type ChartItem } from './chartTemplates';
import { normalizeTypePath } from './legacy';

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

/** 从 ECharts click 参数解析维度 key */
export function resolveChartDimKey(
  params: { dataIndex?: number; name?: string },
  items: ChartItem[],
): string {
  const idx = params.dataIndex;
  if (typeof idx === 'number' && idx >= 0 && idx < items.length) {
    const it = items[idx];
    return String(it.key || it.label || '').trim();
  }
  const name = (params.name || '').trim();
  if (!name) return '';
  const hit = items.find((i) => i.label === name || i.key === name);
  return String(hit?.key || hit?.label || name).trim();
}

export function useMiniChartWidget(props: WidgetCardProps) {
  const router = useRouter();
  const chartEl = ref<HTMLElement | null>(null);
  let chart: ReturnType<typeof initEcharts> | null = null;
  let ro: ResizeObserver | null = null;
  let raf = 0;

  const items = computed(() => readChartItems(props.result));
  const empty = computed(() => !props.loading && !props.error && items.value.length === 0);
  const option = computed(() =>
    buildMiniChartOption(props.widget.style?.chartType ?? 'bar', items.value),
  );
  const drillable = computed(() => {
    const tp = normalizeTypePath(props.widget.source?.typePath);
    const url =
      props.widget.style?.clickUrl ||
      (props.result as WidgetQueryResult | undefined)?.url;
    return !!tp || !!url;
  });

  function disposeChart() {
    if (raf) cancelAnimationFrame(raf);
    raf = 0;
    ro?.disconnect();
    ro = null;
    chart?.off('click');
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

  function navigateDrill(dimKey?: string) {
    const tp = normalizeTypePath(props.widget.source?.typePath);
    const dimField =
      (props.widget.query?.groupBy || props.widget.query?.timeField || '').trim();
    const key = (dimKey || '').trim();

    // 点击柱/点：写入 viewFilter（与筛选构建器同构），列表页从 URL 应用
    if (tp && dimField && key && key !== '其它') {
      const vf = buildDrillViewFilter(dimField, key);
      void router.push({
        path: `/${tp}`,
        query: { viewFilter: JSON.stringify(vf) },
      });
      return;
    }

    const fromStyle = props.widget.style?.clickUrl;
    const fromData = (props.result as WidgetQueryResult | undefined)?.url;
    const url = (fromStyle || fromData || '').toString();
    if (url) {
      if (/^https?:/i.test(url)) window.open(url, '_blank');
      else void router.push(url.startsWith('/') ? url : `/${url}`);
      return;
    }
    if (tp) void router.push(`/${tp}`);
  }

  function onChartClick(params: { componentType?: string; dataIndex?: number; name?: string }) {
    if (params.componentType && params.componentType !== 'series') return;
    const key = resolveChartDimKey(params, items.value);
    navigateDrill(key);
  }

  async function render() {
    await nextTick();
    const el = chartEl.value;
    if (!el || props.error || items.value.length === 0) {
      if (items.value.length === 0) disposeChart();
      return;
    }
    try {
      await ensureEchartsTheme(undefined);
      if (!chart || chart.getDom() !== el) {
        chart?.dispose();
        chart = initEcharts(el);
        bindResize(el);
      }
      chart.setOption(option.value, true);
      chart.off('click');
      chart.on('click', onChartClick);
      el.style.cursor = drillable.value ? 'pointer' : '';
      scheduleResize();
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
      () => props.error,
      () => items.value.length,
      () => [props.widget.layout?.w, props.widget.layout?.h] as const,
      drillable,
    ],
    () => {
      void render();
    },
  );

  onBeforeUnmount(() => disposeChart());

  return { chartEl, option, empty, items, drillable };
}
