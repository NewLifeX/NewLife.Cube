import type { EChartsOption } from 'echarts';
import type { ChartType } from '@cube/api-core';
import { themeColor } from '@/core/utils/themeColor';

export interface ChartItem {
  key: string;
  label: string;
  value: unknown;
}

function num(v: unknown): number {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

function primaryColor(): string {
  return themeColor('--primary-6', 'rgb(22, 93, 255)');
}

const noSplit = { show: false };

/** 平台迷你图模板。禁止把用户自由 option 当新编。 */
export function buildMiniChartOption(
  chartType: ChartType | undefined,
  items: ChartItem[],
  color?: string,
): EChartsOption {
  const labels = items.map((i) => i.label || i.key);
  const values = items.map((i) => num(i.value));
  const c = color || primaryColor();
  const labelStyle = {
    show: true,
    color: themeColor('--color-text-2', '#4e5969'),
    fontSize: 11,
  };

  if (chartType === 'pie') {
    const top = items.slice(0, 6);
    const rest = items.slice(6);
    const data = top.map((i) => ({ name: i.label || i.key, value: num(i.value) }));
    if (rest.length) {
      data.push({ name: '其它', value: rest.reduce((s, i) => s + num(i.value), 0) });
    }
    return {
      color: [c, '#14C9C9', '#F7BA1E', '#722ED1', '#F5319D', '#00B42A', '#86909C'],
      tooltip: { trigger: 'item' },
      legend: { show: false },
      series: [
        {
          type: 'pie',
          radius: ['36%', '68%'],
          center: ['50%', '50%'],
          data,
          label: {
            show: true,
            formatter: '{b}\n{c}',
            fontSize: 11,
          },
        },
      ],
    };
  }

  if (chartType === 'hbar') {
    return {
      color: [c],
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: 48, right: 36, top: 8, bottom: 8 },
      xAxis: {
        type: 'value',
        splitLine: noSplit,
        axisLine: { show: false },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'category',
        data: labels,
        axisLabel: { hideOverlap: true, fontSize: 11 },
        axisTick: { show: false },
        splitLine: noSplit,
      },
      series: [
        {
          type: 'bar',
          data: values,
          label: { ...labelStyle, position: 'right' },
          itemStyle: { color: c, borderRadius: [0, 3, 3, 0] },
        },
      ],
    };
  }

  if (chartType === 'bar') {
    return {
      color: [c],
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: 8, right: 8, top: 28, bottom: 22 },
      xAxis: {
        type: 'category',
        data: labels,
        axisLabel: { hideOverlap: true, fontSize: 11 },
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
        splitLine: noSplit,
      },
      series: [
        {
          type: 'bar',
          data: values,
          label: { ...labelStyle, position: 'top' },
          itemStyle: { color: c, borderRadius: [3, 3, 0, 0] },
        },
      ],
    };
  }

  const spark = chartType === 'sparkline';
  return {
    color: [c],
    tooltip: spark ? { show: false } : { trigger: 'axis' },
    grid: spark
      ? { left: 0, right: 0, top: 4, bottom: 0 }
      : { left: 28, right: 8, top: 20, bottom: 22 },
    xAxis: {
      type: 'category',
      data: labels,
      show: !spark,
      boundaryGap: false,
      axisTick: { show: false },
      splitLine: noSplit,
    },
    yAxis: {
      type: 'value',
      show: !spark,
      splitLine: spark ? noSplit : noSplit,
      axisLine: { show: false },
      axisTick: { show: false },
    },
    series: [
      {
        type: 'line',
        data: values,
        showSymbol: !spark,
        smooth: spark,
        areaStyle: spark ? { opacity: 0.15, color: c } : undefined,
        itemStyle: { color: c },
        lineStyle: { color: c },
        label: spark ? { show: false } : { ...labelStyle, position: 'top' },
      },
    ],
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
