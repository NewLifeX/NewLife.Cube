/**
 * 列表页「表格 / 图表」视图切换逻辑
 *
 * 各皮肤（Vue / React / Svelte 家族）都实现了同一套图表视图行为：切换按钮在两种视图间来回切换、
 * 图表接口返回非数组时按空列表处理、离开图表视图时释放 ECharts 实例。
 * 统一收敛到本模块，避免多皮肤口径漂移，并让行为可单测。
 */

// ======================== 类型 ========================

/** 列表视图模式。表格视图为主视图，图表视图按需加载图表数据 */
export type ListViewMode = 'table' | 'chart';

// ======================== 视图切换 ========================

/** 在表格视图与图表视图之间切换 */
export function toggleListView(mode: ListViewMode): ListViewMode {
  return mode === 'table' ? 'chart' : 'table';
}

/** 切换到指定视图后是否需要释放图表实例。进入图表视图时由渲染逻辑初始化，离开时才释放 */
export function shouldDisposeCharts(next: ListViewMode): boolean {
  return next !== 'chart';
}

// ======================== 数据归一化 ========================

/** 归一化图表接口返回。非数组或空数组统一返回空数组，供各皮肤渲染「暂无图表」空状态 */
export function normalizeChartList(payload: unknown): Record<string, unknown>[] {
  if (!Array.isArray(payload) || payload.length === 0) return [];

  return payload as Record<string, unknown>[];
}
