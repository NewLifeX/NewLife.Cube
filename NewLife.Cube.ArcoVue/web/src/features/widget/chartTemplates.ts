import type { EChartsOption } from 'echarts';
import type { ChartType, MeasureFn, WidgetChartOptions, WidgetMeasure } from '@newlifex/api-core';
import { themeColor } from '@/core/utils/themeColor';

export interface ChartItem {
  key: string;
  label: string;
  value: unknown;
  /** 多度量值（与 query.measures 顺序对齐，OSC-260920）；缺省回落到 value */
  values?: unknown[];
  [key: string]: unknown;
}

/** 图表渲染选项（OSC-260920）：度量序列 + 显示开关 */
export interface ChartRenderOptions {
  /** 有效度量（与 item.values 对齐）；空则单序列 Count(*) */
  measures?: WidgetMeasure[];
  /** 显示选项：图例/数据标签/坐标轴/网格线；缺省与旧模板一致（标签/轴开，图例/网格线关） */
  chartOptions?: WidgetChartOptions;
}

/** 度量函数中文名（序列显示名与配置抽屉共用） */
export const MEASURE_FN_LABELS: Record<MeasureFn, string> = {
  count: '计数',
  sum: '求和',
  avg: '平均值',
  min: '最小值',
  max: '最大值',
};

/** 数值类型名判定（与后端 WidgetQueryService.IsNumericType 对齐） */
export function isNumericTypeName(typeName?: string): boolean {
  return /int|long|decimal|double|single|float|byte|short|number/i.test(typeName || '');
}

/**
 * 字段可用的统计方式（OSC-260921）：
 * 数值且非主键/身份列 → 计数 / 求和 / 最大值 / 最小值 / 平均值；
 * 文本、日期、时间、编号（主键）等 → 仅计数；未选字段 → 仅计数。
 * 与后端 CheckMeasure/ResolveNumeric 的拒绝规则保持一致，避免提交后 400。
 */
export function measureFnOptionsFor(
  field?: { typeName?: string; primaryKey?: boolean } | null,
): MeasureFn[] {
  if (!field || field.primaryKey || !isNumericTypeName(field.typeName)) return ['count'];
  return ['count', 'sum', 'max', 'min', 'avg'];
}

/** 有效度量：query.measures 优先，其次单值 measure，缺省 Count(*)；最多 5 个 */
export function resolveChartMeasures(query?: {
  measures?: WidgetMeasure[];
  measure?: WidgetMeasure;
} | null): WidgetMeasure[] {
  const list = (query?.measures ?? []).filter((m) => m && m.fn);
  if (list.length) return list.slice(0, 5);
  if (query?.measure?.fn) return [query.measure];
  return [{ fn: 'count' }];
}

/** 序列显示名：优先保存时的 label，其次「字段（函数）」；Count(*) 为「记录数」 */
export function measureDisplayName(m: WidgetMeasure): string {
  const label = (m.label || '').trim();
  if (label) return label;
  const fn = MEASURE_FN_LABELS[m.fn] ?? m.fn;
  const field = (m.field || '').trim();
  return field ? `${field}（${fn}）` : '记录数';
}

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function primaryColor(): string {
  return themeColor('--primary-6', 'rgb(22, 93, 255)');
}

const noSplit = { show: false };
/** 多序列调色板（首色=部件主题色，其余与饼图同系） */
const PALETTE = ['#14C9C9', '#F7BA1E', '#722ED1', '#F5319D', '#00B42A', '#86909C'];

/** 平台迷你图模板。禁止把用户自由 option 当新编。OSC-260920：多度量序列 + 图例/数据标签/坐标轴/网格线 */
export function buildMiniChartOption(
  chartType: ChartType | undefined,
  items: ChartItem[],
  color?: string,
  opts?: ChartRenderOptions,
): EChartsOption {
  const measures = opts?.measures?.length ? opts.measures : [{ fn: 'count' as MeasureFn }];
  const co = opts?.chartOptions ?? {};
  const labels = items.map((i) => i.label || i.key);
  const c = color || primaryColor();
  const colors = [c, ...PALETTE];
  const showLabel = co.dataLabel !== false;
  const showAxis = co.axis !== false;
  const showGrid = co.gridLine === true;
  const showLegend = co.legend === true;

  /** 序列 idx 的取值：values 对齐优先，缺省回落到 value（单度量兼容） */
  const seriesValues = (idx: number): number[] =>
    items.map((it) => {
      const vs = Array.isArray(it.values) ? it.values : null;
      if (vs && vs.length > idx) return num(vs[idx]);
      return idx === 0 ? num(it.value) : 0;
    });
  /** 首度量取值（饼图等单维图表用） */
  const firstValue = (it: ChartItem): number =>
    num(Array.isArray(it.values) && it.values.length ? it.values[0] : it.value);

  const labelStyle = {
    show: showLabel,
    color: themeColor('--color-text-2', '#4e5969'),
    fontSize: 11,
  };
  const legend = {
    show: showLegend,
    top: 0,
    left: 'center',
    itemWidth: 10,
    itemHeight: 10,
    textStyle: { fontSize: 11 },
  };
  /** 网格线：开启时显示数值轴分隔线 */
  const gridLineStyle = showGrid
    ? { show: true, lineStyle: { color: themeColor('--color-border-2', '#e5e6eb'), type: 'dashed' as const } }
    : noSplit;

  if (chartType === 'pie') {
    // 饼图仅呈现首个度量（其余度量无几何位置），保留「其它」折叠
    const top = items.slice(0, 6);
    const rest = items.slice(6);
    const data = top.map((i) => ({ name: i.label || i.key, value: firstValue(i) }));
    if (rest.length) {
      data.push({ name: '其它', value: rest.reduce((s, i) => s + firstValue(i), 0) });
    }
    return {
      color: colors,
      tooltip: { trigger: 'item' },
      legend,
      series: [
        {
          type: 'pie',
          radius: ['36%', '68%'],
          center: ['50%', '50%'],
          data,
          label: {
            show: showLabel,
            formatter: '{b}\n{c}',
            fontSize: 11,
          },
        },
      ],
    };
  }

  if (chartType === 'hbar') {
    return {
      color: colors,
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      legend,
      grid: { left: 8, right: 28, top: showLegend ? 18 : 4, bottom: 4, containLabel: true },
      xAxis: {
        type: 'value',
        show: showAxis,
        splitLine: gridLineStyle,
        axisLine: { show: false },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'category',
        data: labels,
        show: showAxis,
        axisLabel: { hideOverlap: true, fontSize: 11 },
        axisTick: { show: false },
        splitLine: noSplit,
      },
      series: measures.map((m, idx) => ({
        type: 'bar' as const,
        name: measureDisplayName(m),
        data: seriesValues(idx),
        label: { ...labelStyle, position: 'right' as const },
        itemStyle: { color: colors[idx % colors.length], borderRadius: [0, 3, 3, 0] },
      })),
    };
  }

  if (chartType === 'bar') {
    return {
      color: colors,
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      legend,
      grid: { left: 4, right: 4, top: showLegend ? 34 : 22, bottom: 0, containLabel: true },
      xAxis: {
        type: 'category',
        data: labels,
        show: showAxis,
        axisLabel: { hideOverlap: true, fontSize: 11, margin: 4 },
        axisTick: { show: false },
        axisLine: { show: true },
        splitLine: noSplit,
      },
      yAxis: {
        type: 'value',
        show: true,
        axisLabel: { show: false },
        axisTick: { show: false },
        axisLine: { show: false },
        splitLine: gridLineStyle,
      },
      series: measures.map((m, idx) => ({
        type: 'bar' as const,
        name: measureDisplayName(m),
        data: seriesValues(idx),
        label: { ...labelStyle, position: 'top' as const },
        itemStyle: { color: colors[idx % colors.length], borderRadius: [3, 3, 0, 0] },
      })),
    };
  }

  const spark = chartType === 'sparkline';
  return {
    color: colors,
    tooltip: spark ? { show: false } : { trigger: 'axis' },
    legend: spark ? { show: false } : legend,
    grid: spark
      ? { left: 0, right: 0, top: 4, bottom: 0 }
      : { left: 4, right: 4, top: showLegend ? 32 : 20, bottom: 0, containLabel: true },
    xAxis: {
      type: 'category',
      data: labels,
      show: !spark && showAxis,
      boundaryGap: false,
      axisTick: { show: false },
      axisLabel: { hideOverlap: true, fontSize: 11, margin: 4 },
      splitLine: noSplit,
    },
    yAxis: {
      type: 'value',
      show: !spark && showAxis,
      splitLine: gridLineStyle,
      axisLine: { show: false },
      axisTick: { show: false },
    },
    series: measures.map((m, idx) => ({
      type: 'line' as const,
      name: measureDisplayName(m),
      data: seriesValues(idx),
      showSymbol: !spark,
      smooth: spark,
      areaStyle: spark && idx === 0 ? { opacity: 0.15, color: colors[0] } : undefined,
      itemStyle: { color: colors[idx % colors.length] },
      lineStyle: { color: colors[idx % colors.length] },
      label: spark ? { show: false } : { ...labelStyle, position: 'top' as const },
    })),
  };
}

/** 图表模板选项（配置抽屉） */
export const CHART_TYPE_OPTIONS: {
  value: ChartType;
  label: string;
  icon: string;
}[] = [
  { value: 'sparkline', label: '迷你折线', icon: 'chart-line' },
  { value: 'line', label: '折线', icon: 'chart-line' },
  { value: 'bar', label: '柱状', icon: 'chart-histogram' },
  { value: 'hbar', label: '条形', icon: 'chart-histogram-one' },
  { value: 'pie', label: '饼图', icon: 'chart-pie' },
];
