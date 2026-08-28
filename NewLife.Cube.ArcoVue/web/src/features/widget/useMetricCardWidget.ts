import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import type { WidgetQueryResult } from '@cube/api-core';
import { ensureEchartsTheme, initEcharts } from '@/core/utils/echartsTheme';
import { themeColor } from '@/core/utils/themeColor';
import { resolveWorkbenchIcon } from '@/core/utils/workbench';
import type { WidgetCardProps } from './context';
import { buildMiniChartOption } from './chartTemplates';
import { normalizeTypePath } from './legacy';

const COLORS: Record<string, string> = {
  blue: '', // 运行时取主题色
  green: '#00b42a',
  cyan: '#14c9c9',
  orange: '#ff7d00',
  red: '#f53f3f',
  purple: '#722ed1',
  grey: 'var(--color-text-3)',
};

export function useMetricCardWidget(props: WidgetCardProps) {
  const router = useRouter();
  const sparkEl = ref<HTMLElement | null>(null);
  let chart: ReturnType<typeof initEcharts> | null = null;

  const valueText = computed(() => {
    const syn = props.widget.syntheticValue;
    if (syn !== undefined && syn !== null) return String(syn);
    const v = (props.result as WidgetQueryResult | undefined)?.value;
    if (v == null) return '—';
    return String(v);
  });
  /** 数字下方标签：自定义指标用 style.badge；平台 named 用接口 trend */
  const labelText = computed(() => {
    const badge = (props.widget.style?.badge || '').trim();
    if (badge) return badge;
    const t = (props.result as WidgetQueryResult | undefined)?.trend;
    return t == null || t === '' ? '' : String(t);
  });
  const sparkItems = computed(() => (props.result as WidgetQueryResult | undefined)?.items ?? []);
  const sparkOption = computed(() =>
    sparkItems.value.length
      ? buildMiniChartOption('sparkline', sparkItems.value, themeColor('--primary-6', 'rgb(22, 93, 255)'))
      : null,
  );
  const color = computed(() => {
    const key = props.widget.style?.color ?? 'blue';
    if (!key || key === 'blue') return themeColor('--primary-6', 'rgb(22, 93, 255)');
    return COLORS[key] || themeColor('--primary-6', 'rgb(22, 93, 255)');
  });
  const icon = computed(() =>
    resolveWorkbenchIcon(props.widget.style?.icon, props.widget.source?.widgetName) || 'dashboard',
  );

  watch(
    () => sparkOption.value,
    async (opt) => {
      await nextTick();
      if (!sparkEl.value || !opt) return;
      await ensureEchartsTheme(undefined);
      if (!chart) chart = initEcharts(sparkEl.value);
      chart.setOption(opt);
    },
    { immediate: true },
  );

  onBeforeUnmount(() => {
    chart?.dispose();
    chart = null;
  });

  function onClick() {
    const fromStyle = props.widget.style?.clickUrl;
    const fromData = (props.result as WidgetQueryResult | undefined)?.url;
    const url = (fromStyle || fromData || '').toString();
    if (url) {
      if (/^https?:/i.test(url)) window.open(url, '_blank');
      else router.push(url.startsWith('/') ? url : `/${url}`);
      return;
    }
    const tp = normalizeTypePath(props.widget.source?.typePath);
    if (tp) router.push(`/${tp}`);
  }

  return { valueText, labelText, sparkOption, color, icon, sparkEl, onClick };
}
