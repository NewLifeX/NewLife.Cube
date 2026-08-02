import type { ViewProfileModel } from '@cube/api-core';
import type { FieldMeta } from '@/core/types/field';
import {
  normalizeMapping,
  parseViewKind,
  seedMapping,
  type ViewKind,
  type ViewMapping,
} from '@/core/utils/viewMapping';

export type { ViewKind, ViewMapping } from '@/core/utils/viewMapping';

export interface ColumnPref {
  key: string;
  visible: boolean;
  width?: number;
  frozen?: 'left' | false;
  /** 自定义列头标签；空则回落元数据 displayName */
  title?: string;
}

export interface ViewSort {
  field: string;
  desc: boolean;
}

/** 视图级样式与工具条（存入 ViewsJson，供配置弹层「自定义配置」） */
export type WidthMode = 'default' | 'fill';
export type HeightMode = 'default' | 'fit' | 'fill';

export interface ViewChrome {
  bgPreset?: 'default' | 'custom';
  bgColor?: string;
  bgOpacity?: number;
  bgBlur?: number;
  widthMode?: WidthMode;
  heightMode?: HeightMode;
  /** 顶部栏 */
  showFilter?: boolean;
  showGroup?: boolean;
  showSort?: boolean;
  showSearch?: boolean;
  allowAdd?: boolean;
  addButtonText?: string;
  customButton?: boolean;
  /** 列表区 */
  showPager?: boolean;
  allowViewDetail?: boolean;
  allowDelete?: boolean;
  expandRow?: boolean;
}

export interface NamedView {
  id: string;
  name: string;
  view: ViewKind;
  columns: ColumnPref[];
  sort?: ViewSort | null;
  chrome?: ViewChrome;
  /** 类型专属字段映射（存 ViewsJson；不写 ganttJson/cardJson） */
  mapping?: ViewMapping;
}

export const DEFAULT_CHROME: Required<ViewChrome> = {
  bgPreset: 'default',
  bgColor: '#FFFFFF',
  bgOpacity: 100,
  bgBlur: 0,
  widthMode: 'default',
  heightMode: 'default',
  showFilter: true,
  showGroup: true,
  showSort: true,
  showSearch: true,
  allowAdd: true,
  addButtonText: '添加记录',
  customButton: false,
  showPager: true,
  allowViewDetail: true,
  allowDelete: false,
  expandRow: false,
};

export interface EntityViewState {
  views: NamedView[];
  activeViewId: string;
  view: ViewKind;
}

export interface ViewSeedOptions {
  defaultView?: ViewKind | String | null;
}

export const DEFAULT_VIEW_ID = 'default';
export const DEFAULT_VIEW_NAME = '默认列表';

function resolveSeedViewKind(raw?: ViewKind | String | null): ViewKind {
  return parseViewKind(typeof raw === 'string' ? raw : raw?.toString());
}

function parseJsonArray(raw: string | null | undefined): unknown[] | null {
  if (!raw || typeof raw !== 'string') return null;
  try {
    const v = JSON.parse(raw) as unknown;
    return Array.isArray(v) ? v : null;
  } catch {
    return null;
  }
}

export function normalizeColumn(raw: unknown): ColumnPref | null {
  if (!raw || typeof raw !== 'object') return null;
  const o = raw as Record<string, unknown>;
  const key = typeof o.key === 'string' ? o.key : typeof o.name === 'string' ? o.name : '';
  if (!key || key === '__ops') return null;
  const title =
    typeof o.title === 'string' && o.title.trim() ? o.title.trim() : undefined;
  return {
    key,
    visible: o.visible !== false,
    width: typeof o.width === 'number' && o.width > 0 ? o.width : undefined,
    frozen: o.frozen === 'left' ? 'left' : false,
    title,
  };
}

/** 元数据字段 ∪ 已存偏好：未知 key 丢弃；新字段默认 visible；meta 空时保留合法 prefs */
export function mergeColumns(
  metaKeys: string[],
  prefs: ColumnPref[] | null | undefined,
): ColumnPref[] {
  const keys = metaKeys.filter(Boolean);
  const metaSet = new Set(keys);
  const prefMap = new Map<string, ColumnPref>();
  for (const p of prefs || []) {
    const n = normalizeColumn(p);
    if (!n) continue;
    if (metaSet.size === 0 || metaSet.has(n.key)) prefMap.set(n.key, n);
  }

  const ordered: ColumnPref[] = [];
  for (const p of prefs || []) {
    const n = normalizeColumn(p);
    if (!n) continue;
    if (metaSet.size > 0 && !metaSet.has(n.key)) continue;
    if (!ordered.some((x) => x.key === n.key)) {
      ordered.push(prefMap.get(n.key) || n);
    }
  }
  for (const key of keys) {
    if (!ordered.some((x) => x.key === key)) {
      ordered.push({ key, visible: true, frozen: false });
    }
  }
  return ordered;
}

/** 用最新元数据重合并所有命名视图的列（修复空 columns 脏数据） */
export function rematchStateColumns(state: EntityViewState, metaKeys: string[]): EntityViewState {
  if (!metaKeys.length) return state;
  return {
    ...state,
    views: state.views.map((v) => ({
      ...v,
      columns: mergeColumns(metaKeys, v.columns),
    })),
  };
}

function boolOr(v: unknown, fallback: boolean): boolean {
  return typeof v === 'boolean' ? v : fallback;
}

function normalizeChrome(raw: unknown): ViewChrome | undefined {
  if (!raw || typeof raw !== 'object') return undefined;
  const o = raw as Record<string, unknown>;
  const widthMode = o.widthMode === 'fill' ? 'fill' : 'default';
  const heightMode =
    o.heightMode === 'fit' || o.heightMode === 'fill' ? o.heightMode : 'default';
  return {
    bgPreset: o.bgPreset === 'custom' ? 'custom' : 'default',
    bgColor: typeof o.bgColor === 'string' && o.bgColor ? o.bgColor : '#FFFFFF',
    bgOpacity: typeof o.bgOpacity === 'number' ? Math.min(100, Math.max(0, o.bgOpacity)) : 100,
    bgBlur: typeof o.bgBlur === 'number' ? Math.min(100, Math.max(0, o.bgBlur)) : 0,
    widthMode,
    heightMode,
    showFilter: boolOr(o.showFilter, true),
    showGroup: boolOr(o.showGroup, true),
    showSort: boolOr(o.showSort, true),
    showSearch: boolOr(o.showSearch, true),
    allowAdd: boolOr(o.allowAdd, true),
    addButtonText:
      typeof o.addButtonText === 'string' && o.addButtonText.trim()
        ? o.addButtonText.trim()
        : '添加记录',
    customButton: boolOr(o.customButton, false),
    showPager: boolOr(o.showPager, true),
    allowViewDetail: boolOr(o.allowViewDetail, true),
    allowDelete: boolOr(o.allowDelete, false),
    expandRow: boolOr(o.expandRow, false),
  };
}

export function seedDefaultView(
  metaKeys: string[],
  columns?: ColumnPref[],
  defaultView?: ViewKind | String | null,
): NamedView {
  return {
    id: DEFAULT_VIEW_ID,
    name: DEFAULT_VIEW_NAME,
    view: resolveSeedViewKind(defaultView),
    columns: columns ?? mergeColumns(metaKeys, null),
    sort: null,
    chrome: { ...DEFAULT_CHROME },
  };
}

export function parseNamedViews(
  raw: string | null | undefined,
  metaKeys: string[],
  defaultView?: ViewKind | String | null,
): NamedView[] {
  const arr = parseJsonArray(raw);
  if (!arr?.length) return [seedDefaultView(metaKeys, undefined, defaultView)];

  const views: NamedView[] = [];
  for (const item of arr) {
    if (!item || typeof item !== 'object') continue;
    const o = item as Record<string, unknown>;
    const id = typeof o.id === 'string' && o.id ? o.id : `v-${views.length + 1}`;
    let name = typeof o.name === 'string' && o.name ? o.name : DEFAULT_VIEW_NAME;
    // 统一历史种子名「列表」→「默认列表」
    if (id === DEFAULT_VIEW_ID && name === '列表') name = DEFAULT_VIEW_NAME;
    const view = parseViewKind(o.view);
    const colsRaw = Array.isArray(o.columns) ? (o.columns as unknown[]) : [];
    const columns = mergeColumns(
      metaKeys,
      colsRaw.map(normalizeColumn).filter(Boolean) as ColumnPref[],
    );
    let sort: ViewSort | null = null;
    if (o.sort && typeof o.sort === 'object') {
      const s = o.sort as Record<string, unknown>;
      if (typeof s.field === 'string' && s.field) {
        sort = { field: s.field, desc: !!s.desc };
      }
    }
    // 解析阶段无 FieldMeta 时先原样保留 mapping；load 后 rematchMapping 校正
    let mapping: ViewMapping | undefined;
    if (o.mapping && typeof o.mapping === 'object') {
      mapping = o.mapping as ViewMapping;
    }
    views.push({
      id,
      name,
      view,
      columns,
      sort,
      chrome: normalizeChrome(o.chrome) ?? { ...DEFAULT_CHROME },
      mapping,
    });
  }
  return views.length ? views : [seedDefaultView(metaKeys, undefined, defaultView)];
}

/** 线缆 → 状态；兼容仅有 columnsJson 的旧数据 */
export function stateFromWire(
  model: ViewProfileModel | null | undefined,
  metaKeys: string[],
  opts?: ViewSeedOptions,
): EntityViewState {
  const seedView = model?.view || opts?.defaultView;
  if (!model) {
    const v = seedDefaultView(metaKeys, undefined, seedView);
    return { views: [v], activeViewId: v.id, view: v.view };
  }

  let views = parseNamedViews(model.viewsJson, metaKeys, seedView);
  if ((!model.viewsJson || !parseJsonArray(model.viewsJson)?.length) && model.columnsJson) {
    const cols = parseJsonArray(model.columnsJson)
      ?.map(normalizeColumn)
      .filter(Boolean) as ColumnPref[] | undefined;
    views = [seedDefaultView(metaKeys, mergeColumns(metaKeys, cols), seedView)];
  }

  let activeViewId =
    typeof model.activeViewId === 'string' && model.activeViewId
      ? model.activeViewId
      : views[0].id;
  if (!views.some((v) => v.id === activeViewId)) activeViewId = views[0].id;

  const active = views.find((v) => v.id === activeViewId)!;
  return {
    views,
    activeViewId,
    view: active.view,
  };
}

export function stateToWirePayload(
  typePath: string,
  state: EntityViewState,
): Partial<ViewProfileModel> & { typePath: string } {
  const active = state.views.find((v) => v.id === state.activeViewId) || state.views[0];
  return {
    typePath,
    view: active.view,
    activeViewId: active.id,
    viewsJson: JSON.stringify(state.views),
    columnsJson: JSON.stringify(active.columns),
    version: 1,
  };
}

/** 用 FieldMeta 校正所有命名视图的 mapping */
export function rematchStateMappings(
  state: EntityViewState,
  fields: FieldMeta[],
): EntityViewState {
  if (!fields.length) return state;
  return {
    ...state,
    views: state.views.map((v) => ({
      ...v,
      mapping: normalizeMapping(v.view, v.mapping, fields),
    })),
  };
}

export function getActiveView(state: EntityViewState): NamedView {
  return state.views.find((v) => v.id === state.activeViewId) || state.views[0];
}

/** 左冻结列数（不含 checkbox；含连续 frozen:left 的可见列） */
export function frozenLeftCount(columns: ColumnPref[]): number {
  let n = 0;
  for (const c of columns) {
    if (!c.visible) continue;
    if (c.frozen === 'left') n += 1;
    else break;
  }
  return n;
}

export function buildSortPayload(sort: ViewSort | null | undefined): {
  sort?: string;
  desc?: boolean;
} {
  if (!sort?.field) return {};
  return { sort: sort.field, desc: !!sort.desc };
}

export function createTableView(
  state: EntityViewState,
  name: string,
  metaKeys: string[],
): EntityViewState {
  return createNamedView(state, name, 'table', metaKeys);
}

export function createNamedView(
  state: EntityViewState,
  name: string,
  kind: ViewKind,
  metaKeys: string[],
  fields?: FieldMeta[],
): EntityViewState {
  const trimmed = name.trim() || '未命名';
  if (state.views.some((v) => v.name === trimmed)) {
    throw new Error('视图名称已存在');
  }
  const id = `v-${Date.now().toString(36)}`;
  const active = getActiveView(state);
  const mapping =
    fields && fields.length ? seedMapping(kind, fields) : undefined;
  const next: NamedView = {
    id,
    name: trimmed,
    view: kind,
    columns: mergeColumns(metaKeys, active.columns.map((c) => ({ ...c }))),
    sort: active.sort ? { ...active.sort } : null,
    chrome: { ...(active.chrome || DEFAULT_CHROME) },
    mapping,
  };
  return {
    views: [...state.views, next],
    activeViewId: id,
    view: kind,
  };
}

export function removeView(state: EntityViewState, id: string): EntityViewState {
  if (state.views.length <= 1) throw new Error('至少保留一个视图');
  const views = state.views.filter((v) => v.id !== id);
  const activeViewId =
    state.activeViewId === id ? views[0].id : state.activeViewId;
  const active = views.find((v) => v.id === activeViewId) || views[0];
  return { views, activeViewId, view: active.view };
}

export function duplicateView(state: EntityViewState, id: string): EntityViewState {
  const src = state.views.find((v) => v.id === id);
  if (!src) throw new Error('视图不存在');
  let name = `${src.name} 副本`;
  let n = 2;
  while (state.views.some((v) => v.name === name)) {
    name = `${src.name} 副本${n++}`;
  }
  const newId = `v-${Date.now().toString(36)}`;
  const next: NamedView = {
    ...src,
    id: newId,
    name,
    columns: src.columns.map((c) => ({ ...c })),
    sort: src.sort ? { ...src.sort } : null,
    chrome: src.chrome ? { ...src.chrome } : { ...DEFAULT_CHROME },
    mapping: src.mapping ? { ...src.mapping } : undefined,
  };
  return {
    views: [...state.views, next],
    activeViewId: newId,
    view: next.view,
  };
}

export function renameView(state: EntityViewState, id: string, name: string): EntityViewState {
  const trimmed = name.trim();
  if (!trimmed) throw new Error('名称不能为空');
  if (state.views.some((v) => v.id !== id && v.name === trimmed)) {
    throw new Error('视图名称已存在');
  }
  return {
    ...state,
    views: state.views.map((v) => (v.id === id ? { ...v, name: trimmed } : v)),
  };
}

export function patchActiveColumns(
  state: EntityViewState,
  columns: ColumnPref[],
): EntityViewState {
  return {
    ...state,
    views: state.views.map((v) =>
      v.id === state.activeViewId ? { ...v, columns: columns.map((c) => ({ ...c })) } : v,
    ),
  };
}

export function patchActiveSort(
  state: EntityViewState,
  sort: ViewSort | null,
): EntityViewState {
  return {
    ...state,
    views: state.views.map((v) =>
      v.id === state.activeViewId ? { ...v, sort } : v,
    ),
  };
}

export function patchActiveChrome(
  state: EntityViewState,
  chrome: ViewChrome,
): EntityViewState {
  return {
    ...state,
    views: state.views.map((v) =>
      v.id === state.activeViewId
        ? { ...v, chrome: { ...(v.chrome || DEFAULT_CHROME), ...chrome } }
        : v,
    ),
  };
}

export function patchActiveMapping(
  state: EntityViewState,
  mapping: ViewMapping | undefined,
): EntityViewState {
  return {
    ...state,
    views: state.views.map((v) =>
      v.id === state.activeViewId ? { ...v, mapping } : v,
    ),
  };
}

export function resolveChrome(view: NamedView | null | undefined): Required<ViewChrome> {
  return { ...DEFAULT_CHROME, ...(view?.chrome || {}) };
}
