/**
 * 工作台卡片布局（拖动排序 / 隐藏 / 持久化）纯逻辑
 *
 * 后端 `Admin/Index/GetWidgetLayout|SaveWidgetLayout|ResetWidgetLayout` 以
 * `{ [卡片名]: { sort, hide } }` 形式按用户持久化。本模块只做「布局 ⇄ 卡片列表」的归一化与接口封装，
 * 与具体 UI 框架无关，各皮肤（含 Vue）共用同一套口径。
 */

import type { CubeApi } from '../api';
import { isSuccessCode } from './workbench';

/** 工作台聚合接口所在控制器路径 */
const WORKBENCH_TYPE = 'Admin/Index';

/** 单个卡片的布局项 */
export interface WidgetLayout {
  /** 排序位置，越小越靠前 */
  sort?: number;
  /** 是否隐藏 */
  hide?: boolean;
}

/** 卡片名 → 布局项 */
export type WidgetLayoutMap = Record<string, WidgetLayout>;

/** 参与布局的最小卡片契约。各皮肤的工作台卡片视图模型均满足该形状 */
export interface LayoutWidget {
  /** 卡片名，作为布局的唯一键 */
  name: string;
}

/** 应用布局后的卡片分组 */
export interface RankedWidgets<T extends LayoutWidget> {
  /** 可见卡片，已按 sort 排序 */
  visible: T[];
  /** 隐藏卡片，已按 sort 排序 */
  hidden: T[];
}

/**
 * 把后端布局应用到卡片列表
 *
 * 未在布局中登记的卡片按原始顺序排在已登记卡片之后；`hide` 为 true 的卡片进入 hidden。
 *
 * @param widgets 后端下发的卡片列表
 * @param layout 用户布局
 * @returns 可见与隐藏两组卡片
 */
export function mergeWidgetsWithLayout<T extends LayoutWidget>(widgets: T[], layout?: WidgetLayoutMap | null): RankedWidgets<T> {
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
export function buildWidgetLayout<T extends LayoutWidget>(visible: T[], hidden: T[]): WidgetLayoutMap {
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
 * 把卡片移动到目标卡片的位置
 *
 * 语义与 Vue 皮肤既有实现一致：被拖动卡片插入到目标卡片所在的索引位置，
 * 向后拖动时表现为落在目标卡片之后。
 *
 * @param list 当前可见卡片
 * @param from 被拖动卡片名
 * @param to 目标卡片名
 * @returns 新数组；名称找不到时原样返回拷贝
 */
export function moveWidget<T extends LayoutWidget>(list: T[], from: string, to: string): T[] {
  const source = [...(list ?? [])];
  const fromIndex = source.findIndex((item) => item.name === from);
  const toIndex = source.findIndex((item) => item.name === to);
  if (fromIndex < 0 || toIndex < 0 || fromIndex === toIndex) return source;

  const [moved] = source.splice(fromIndex, 1);
  source.splice(toIndex, 0, moved);
  return source;
}

/**
 * 创建工作台卡片布局加载器
 *
 * 三个动作都以「是否成功」返回，提示文案由各皮肤自行决定。
 */
export function createWidgetLayoutLoader(api: CubeApi) {
  return {
    /** 读取用户布局。接口不可用时回退默认顺序，不打断工作台主流程 */
    async load(): Promise<WidgetLayoutMap> {
      try {
        const res = await api.page.getAction<WidgetLayoutMap>(WORKBENCH_TYPE, 'GetWidgetLayout');
        if (isSuccessCode(res?.code) && res.data && typeof res.data === 'object') return res.data;
      } catch {
        // 布局属于增强信息，失败时按后端默认顺序渲染
      }
      return {};
    },

    /** 保存布局 */
    async save(layout: WidgetLayoutMap): Promise<boolean> {
      const res = await api.page.postAction(WORKBENCH_TYPE, 'SaveWidgetLayout', layout);
      return isSuccessCode(res?.code);
    },

    /** 重置布局为后端默认 */
    async reset(): Promise<boolean> {
      const res = await api.page.postAction(WORKBENCH_TYPE, 'ResetWidgetLayout');
      return isSuccessCode(res?.code);
    },
  };
}
