import type { DashboardConfig, WidgetInstance } from '@newlifex/api-core';

/** GetChartData 结果归一：null 与非数组为空数组；已是数组则原样返回。 */
export function normalizeChartOptions(data: unknown): unknown[] {
  return Array.isArray(data) ? data : [];
}

/** 用户统计图：失败出警告且不画图；空数组不画图；最多取前 2 张。 */
export function resolveUserStatChartView(
  failed: boolean,
  data: unknown,
): { charts: unknown[]; showAlert: boolean } {
  if (failed) return { charts: [], showAlert: true };
  return { charts: normalizeChartOptions(data).slice(0, 2), showAlert: false };
}

/** 固定开发者图部件 id 前缀（运行时注入，不入仪表盘持久化） */
export const DEV_CHART_ID_PREFIX = 'dev-chart-';

export function isFixedDeveloperChart(id: string | undefined | null): boolean {
  return String(id ?? '').startsWith(DEV_CHART_ID_PREFIX);
}

/** 从 ECharts option 取标题；无则回落。 */
export function chartOptionTitle(opt: unknown, fallback: string): string {
  if (!opt || typeof opt !== 'object' || Array.isArray(opt)) return fallback;
  const title = (opt as { title?: unknown }).title;
  if (typeof title === 'string' && title.trim()) return title.trim();
  if (title && typeof title === 'object' && !Array.isArray(title)) {
    const text = (title as { text?: unknown }).text;
    if (typeof text === 'string' && text.trim()) return text.trim();
  }
  return fallback;
}

/** 去掉 option.title，避免与部件标题条重复。 */
export function stripChartOptionTitle(opt: unknown): unknown {
  if (!opt || typeof opt !== 'object' || Array.isArray(opt)) return opt;
  const copy = { ...(opt as Record<string, unknown>) };
  delete copy.title;
  return copy;
}

/** boxplot 五项须升序 [min,Q1,median,Q3,max]，乱序会导致箱体塌成横线。 */
export function sortBoxplotDatum(row: unknown): number[] | unknown {
  if (!Array.isArray(row) || row.length < 5) return row;
  const nums = row.slice(0, 5).map((v) => Number(v));
  if (nums.some((n) => !Number.isFinite(n))) return row;
  return [...nums].sort((a, b) => a - b);
}

/**
 * 固定洞察图 option 整理：去标题、隐图例、紧凑 grid；
 * boxplot 数据升序；为箱线/K 线略增高绘图区由样式配合。
 */
export function prepareFixedChartOption(opt: unknown): unknown {
  if (!opt || typeof opt !== 'object' || Array.isArray(opt)) return opt;
  const src = opt as Record<string, unknown>;
  const next: Record<string, unknown> = { ...src };
  delete next.title;

  // 部件标题条已有名称，图内图例占高度且与标题重复
  next.legend = { show: false };

  if (!next.grid || typeof next.grid !== 'object' || Array.isArray(next.grid)) {
    next.grid = { left: '3%', right: '3%', top: 12, bottom: 28, containLabel: true };
  }

  const seriesRaw = next.series;
  if (Array.isArray(seriesRaw)) {
    next.series = seriesRaw.map((s) => {
      if (!s || typeof s !== 'object' || Array.isArray(s)) return s;
      const ser = { ...(s as Record<string, unknown>) };
      if (ser.type === 'boxplot' && Array.isArray(ser.data)) {
        ser.data = ser.data.map((row) => sortBoxplotDatum(row));
      }
      return ser;
    });
  }

  const xAxis = next.xAxis;
  if (xAxis && typeof xAxis === 'object' && !Array.isArray(xAxis)) {
    const xa = { ...(xAxis as Record<string, unknown>) };
    const label =
      xa.axisLabel && typeof xa.axisLabel === 'object' && !Array.isArray(xa.axisLabel)
        ? { ...(xa.axisLabel as Record<string, unknown>) }
        : {};
    if (label.hideOverlap == null) label.hideOverlap = true;
    xa.axisLabel = label;
    next.xAxis = xa;
  }

  return next;
}

/** 固定图是否需要更高绘图区（箱线 / K 线） */
export function isTallFixedChart(opt: unknown): boolean {
  if (!opt || typeof opt !== 'object' || Array.isArray(opt)) return false;
  const series = (opt as { series?: unknown }).series;
  if (!Array.isArray(series)) return false;
  return series.some((s) => {
    const t = s && typeof s === 'object' ? (s as { type?: unknown }).type : null;
    return t === 'boxplot' || t === 'candlestick';
  });
}

/**
 * 将开发者 GetChartData 合成洞察区固定图表部件：
 * 整行（w=12）；折线略压；箱线/K 线略增高。仅用图表部件外观，不进设置。
 */
export function synthesizeDeveloperChartWidgets(
  charts: unknown[],
  typePath: string,
): WidgetInstance[] {
  const list = normalizeChartOptions(charts).slice(0, 2);
  return list.map((opt, index) => {
    const title = chartOptionTitle(opt, `图表 ${index + 1}`);
    const tall = isTallFixedChart(opt);
    return {
      id: `${DEV_CHART_ID_PREFIX}${index}`,
      kind: 'legacyChart' as const,
      title,
      layout: { w: 12, h: tall ? 4 : 3, order: index },
      source: { provider: 'entity.aggregate' as const, typePath },
      query: {},
      style: { chartType: 'bar' as const },
      chartOption: prepareFixedChartOption(opt),
      chartIndex: index,
      fixed: true,
      tall,
    };
  });
}

/** 有开发者图时：固定图顶置整行，其后保留用户自定义部件。 */
export function mergeDeveloperCharts(
  base: DashboardConfig,
  charts: unknown[],
  typePath: string,
): DashboardConfig {
  const dev = synthesizeDeveloperChartWidgets(charts, typePath);
  if (!dev.length) return base;
  const others = (base.widgets ?? []).filter(
    (w) => w.kind !== 'legacyChart' && !isFixedDeveloperChart(w.id),
  );
  const shifted = others.map((w, i) => ({
    ...w,
    layout: { ...w.layout, order: dev.length + i },
  }));
  return { version: 1, widgets: [...dev, ...shifted] };
}
