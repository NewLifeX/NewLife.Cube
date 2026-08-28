import { fallbackHeightOf, minHeightOf } from './useWidgetGrid';

/** 紧凑行高（含边线）：1px 内边距 + 18px 行高 + 1px 底边 */
export const DATA_LIST_ROW_PX = 22;
/** 默认最多可见数据行数 */
export const DATA_LIST_DEFAULT_VISIBLE = 7;
/** 标题+内边距等占用，用于从卡片 minHeight 反推表体预算 */
const CARD_CHROME_PX = 56;

/**
 * 默认档最多 7 行；更矮按预算减少，更高按预算增加。
 * 不用网格撑开后的 clientHeight，避免同行高部件把列表“撑”出十几行。
 */
export function visibleDataListRows(layoutH?: number | null): number {
  const defaultH = fallbackHeightOf('dataList');
  const h = layoutH && layoutH > 0 ? layoutH : defaultH;
  const bodyBudget = Math.max(0, minHeightOf(h) - CARD_CHROME_PX);
  const byBudget = Math.max(1, Math.floor(bodyBudget / DATA_LIST_ROW_PX));
  if (h <= defaultH) {
    return Math.min(DATA_LIST_DEFAULT_VISIBLE, byBudget);
  }
  return Math.max(DATA_LIST_DEFAULT_VISIBLE, byBudget);
}

export function dataListScrollY(visibleRows: number): number {
  const n = Math.max(1, visibleRows);
  return n * DATA_LIST_ROW_PX;
}

/**
 * 可见窗口循环取行（每秒上移一行、到底回到顶部）。
 * 不依赖 Arco Table 内部 scrollTop（Scrollbar 下常无真实纵向滚动端口）。
 */
export function rotateDataListWindow<T>(items: T[], offset: number, size: number): T[] {
  if (!items.length || size <= 0) return [];
  if (items.length <= size) return items.slice();
  const n = items.length;
  const start = ((Math.floor(offset) % n) + n) % n;
  const out: T[] = new Array(size);
  for (let i = 0; i < size; i++) out[i] = items[(start + i) % n];
  return out;
}

/** 配置项：拉取条数；-1 = 全部（后端不截断） */
export const DATA_LIST_LIMIT_ALL = -1;
export const DATA_LIST_LIMIT_OPTIONS = [10, 20, 30, 50, 100, 300, DATA_LIST_LIMIT_ALL] as const;
export const DATA_LIST_LIMIT_MAX = 300;
export const DATA_LIST_LIMIT_DEFAULT = 30;

export function normalizeDataListLimit(raw: unknown): number {
  const n = Number(raw);
  if (!Number.isFinite(n)) return DATA_LIST_LIMIT_DEFAULT;
  if (n === DATA_LIST_LIMIT_ALL) return DATA_LIST_LIMIT_ALL;
  if (n <= 0) return DATA_LIST_LIMIT_DEFAULT;
  return Math.min(DATA_LIST_LIMIT_MAX, Math.max(1, Math.floor(n)));
}

export function formatDataListLimitLabel(n: number): string {
  return n === DATA_LIST_LIMIT_ALL ? '全部' : String(n);
}
