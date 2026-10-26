/**
 * 工作台卡片布局（拖动排序 / 隐藏 / 持久化）纯逻辑
 *
 * 后端 `Admin/Index/GetWidgetLayout|SaveWidgetLayout|ResetWidgetLayout` 以
 * `{ [卡片名]: { sort, hide } }` 形式按用户持久化。本模块只做「布局 ⇄ 卡片列表」的归一化，
 * 与 Vue 响应式解耦，便于单测覆盖。
 */
import type { WorkbenchWidget } from '../../types/workbench';

/** 单个卡片的布局项 */
export interface WidgetLayout {
  /** 排序位置，越小越靠前 */
  sort?: number;
  /** 是否隐藏 */
  hide?: boolean;
}

/** 卡片名 → 布局项 */
export type WidgetLayoutMap = Record<string, WidgetLayout>;

/** 应用布局后的卡片分组 */
export interface RankedWidgets {
  /** 可见卡片，已按 sort 排序 */
  visible: WorkbenchWidget[];
  /** 隐藏卡片，已按 sort 排序 */
  hidden: WorkbenchWidget[];
}

/**
 * 把后端布局应用到卡片列表
 *
 * 未在布局中登记的卡片按原始顺序排在已登记卡片之后；`hide` 为 true 的卡片进入 hidden。
 *
 * @param widgets 后端下发的卡片列表
 * @param layout 用户布局
 * @returns 可见与隐藏两组卡片
 * @example
 * ```ts
 * const { visible, hidden } = mergeWidgetsWithLayout(widgets, { Profile: { sort: 0, hide: true } });
 * ```
 */
export function mergeWidgetsWithLayout(widgets: WorkbenchWidget[], layout?: WidgetLayoutMap | null): RankedWidgets {
  const source = widgets ?? [];
  const map = layout ?? {};
  const originalIndex = new Map(source.map((item, index) => [item.name, index]));

  const sortOf = (name: string): number => {
    const sort = map[name]?.sort;
    if (typeof sort === 'number' && Number.isFinite(sort)) return sort;
    return originalIndex.get(name) ?? Number.MAX_SAFE_INTEGER;
  };

  const sorted = [...source].sort((a, b) => sortOf(a.name) - sortOf(b.name));

  return {
    visible: sorted.filter((item) => !map[item.name]?.hide),
    hidden: sorted.filter((item) => Boolean(map[item.name]?.hide)),
  };
}

/**
 * 由当前可见/隐藏顺序生成待保存的布局
 *
 * @param visible 可见卡片（顺序即显示顺序）
 * @param hidden 隐藏卡片（顺序即恢复顺序）
 * @returns 卡片名 → 布局项
 */
export function buildWidgetLayout(visible: WorkbenchWidget[], hidden: WorkbenchWidget[]): WidgetLayoutMap {
  const layout: WidgetLayoutMap = {};
  (visible ?? []).forEach((item, index) => {
    layout[item.name] = { sort: index, hide: false };
  });
  (hidden ?? []).forEach((item, index) => {
    layout[item.name] = { sort: (visible?.length ?? 0) + index, hide: true };
  });
  return layout;
}

/**
 * 把卡片从 from 位置移动到 to 位置
 *
 * @param list 当前可见卡片
 * @param from 被拖动卡片名
 * @param to 目标卡片名（放到它前面）
 * @returns 新数组；找不到名称时原样返回拷贝
 */
export function moveWidget(list: WorkbenchWidget[], from: string, to: string): WorkbenchWidget[] {
  const source = [...(list ?? [])];
  const fromIndex = source.findIndex((item) => item.name === from);
  const toIndex = source.findIndex((item) => item.name === to);
  if (fromIndex < 0 || toIndex < 0 || fromIndex === toIndex) return source;

  const [moved] = source.splice(fromIndex, 1);
  source.splice(toIndex, 0, moved);
  return source;
}
