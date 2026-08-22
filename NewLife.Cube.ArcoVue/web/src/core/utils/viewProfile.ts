import type { ViewProfileModel } from '@cube/api-core';
import type { FieldMeta } from '@/core/types/field';
import { cleanSearchParams, collectSearchKeys } from '@/core/utils/searchFilters';
import {
  normalizeMapping,
  parseViewKind,
  seedMapping,
  type ViewKind,
  type ViewMapping,
} from '@/core/utils/viewMapping';

export type { ViewKind, ViewMapping } from '@/core/utils/viewMapping';

export type ColumnFrozen = 'left' | 'right' | false;

export interface ColumnPref {
  key: string;
  visible: boolean;
  width?: number;
  frozen?: ColumnFrozen;
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
  /** 工具栏 */
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
  /** 受限洞察配置（OSC-0012）：每个命名视图仅一个，可独立启用统计标签与一张固定图表 */
  insight?: ViewInsight;
  /** 筛选构建器方案（OSC-0015）：多条件等于/范围，AND/OR 逻辑；随视图保存并并入请求 */
  filter?: ViewFilter;
  /** 多级分组字段列表（OSC-0015）：有序、最多 3 个；随视图保存并纯前端分组 */
  group?: ViewGroup;
  /** 条件填色规则（OSC-26081903c0）：单条件；空/缺省表示无填色 */
  format?: ViewFormatRule[];
  /** 原始线缆对象（保留未知顶层属性，round-trip 不丢后续 OSC 字段） */
  _raw?: Record<string, unknown>;
}

/** 受限洞察配置。双开关均为 false 表示关闭；仅一个为 true 表示单项；均为 true 表示统计+图表同显。
 *  `chartOption` 为用户在 InsightPanel 配置的一张 ECharts option 模板（OSC-260819e483 P5），
 *  保存前剔除运行时数据（dataset.source / series[].data）；不持久化列表快照。 */
export interface ViewInsight {
  showStat: boolean;
  showChart: boolean;
  /** 用户配置的 ECharts option 模板；无图或缺省为 undefined */
  chartOption?: unknown;
}

/** chartOption 清洗后最大字节数（OSC-260819e483 P5）；超限拒绝保存 */
export const CHART_OPTION_MAX_BYTES = 32 * 1024;

/** 筛选条件操作符（OSC-0015 纯前端过滤）：按字段类别开放可用集合 */
export type ViewFilterOp =
  | 'eq' // 等于
  | 'neq' // 不等于
  | 'contains' // 包含（字符）
  | 'notContains' // 不包含（字符）
  | 'isNull' // 为空
  | 'notNull' // 不为空
  | 'gt' // 大于
  | 'gte' // 大于或等于
  | 'lt' // 小于
  | 'lte' // 小于或等于
  | 'after' // 晚于（日期）
  | 'before'; // 早于（日期）

/** 全部合法操作符（normalizeFilter 校验用） */
export const FILTER_OPS: readonly ViewFilterOp[] = [
  'eq',
  'neq',
  'contains',
  'notContains',
  'isNull',
  'notNull',
  'gt',
  'gte',
  'lt',
  'lte',
  'after',
  'before',
];

/** 筛选构建器单个条件（OSC-0015） */
export interface ViewFilterCondition {
  /** 字段名（filterFields 中 canonical name） */
  field: string;
  /** 操作符（按字段类别开放，见 filterBuilder.FILTER_OPS_BY_KIND） */
  op: ViewFilterOp;
  /** 值；isNull/notNull 无值；eq/neq 可为标量或数组（多选字段） */
  value?: unknown;
  /** 保留字段（历史 between 上界；新操作符不再使用） */
  value2?: unknown;
}

/** 筛选构建器方案（OSC-0015）：条件组 + 组级逻辑 */
export interface ViewFilter {
  /** 条件组逻辑：all=且(AND)，any=或(OR) */
  logic: 'all' | 'any';
  /** 条件列表；空数组表示无筛选 */
  conditions: ViewFilterCondition[];
}

/** 多级分组字段列表（OSC-0015）：有序、最多 3 个；空数组表示无分组 */
export type ViewGroup = string[];

/** 填色作用范围（OSC-26081903c0）；下拉顺序 cell → side → row → column */
export type FormatApply = 'cell' | 'side' | 'row' | 'column';

export const FORMAT_APPLIES: readonly FormatApply[] = ['cell', 'side', 'row', 'column'];

export interface ViewFormatRule {
  id: string;
  apply: FormatApply;
  color: string;
  field: string;
  op: ViewFilterOp;
  value?: unknown;
  /** 命中单元格/行文字加粗；缺省 false */
  bold?: boolean;
}

const FORMAT_COLOR_RE = /^#[0-9A-Fa-f]{6}$/;
const MAX_FORMAT_RULES = 50;

export function generateFormatId(): string {
  const rand = Math.floor(Math.random() * 36 ** 4)
    .toString(36)
    .padStart(4, '0');
  return `f_${Date.now().toString(36)}${rand}`;
}

function canonicalMetaKey(name: string, metaKeys: string[]): string {
  const hit = metaKeys.find((k) => k.toLowerCase() === name.toLowerCase());
  return hit || name;
}

/**
 * 宽容归一化填色规则。非数组→[]；含嵌套 filter 的条目丢弃；非法 apply/color 丢弃；
 * op 非法改为 eq；最多 50；缺 field 仍保留。
 */
export function normalizeFormat(raw: unknown, metaKeys: string[] = []): ViewFormatRule[] {
  if (!Array.isArray(raw)) return [];
  const used = new Set<string>();
  const out: ViewFormatRule[] = [];
  for (const item of raw) {
    if (!item || typeof item !== 'object' || Array.isArray(item)) continue;
    const o = item as Record<string, unknown>;
    if (o.filter && typeof o.filter === 'object') continue;
    const apply = (FORMAT_APPLIES as readonly string[]).includes(o.apply as string)
      ? (o.apply as FormatApply)
      : null;
    if (!apply) continue;
    const color = typeof o.color === 'string' ? o.color : '';
    if (!FORMAT_COLOR_RE.test(color)) continue;
    const fieldRaw = typeof o.field === 'string' ? o.field.trim() : '';
    const field = fieldRaw ? canonicalMetaKey(fieldRaw, metaKeys) : '';
    const op = (FILTER_OPS as readonly string[]).includes(o.op as string)
      ? (o.op as ViewFilterOp)
      : 'eq';
    let id = typeof o.id === 'string' && o.id && !used.has(o.id) ? o.id : generateFormatId();
    if (used.has(id)) id = generateFormatId();
    used.add(id);
    const rule: ViewFormatRule = { id, apply, color, field, op };
    if (o.value !== undefined) rule.value = o.value;
    if (o.bold === true) rule.bold = true;
    out.push(rule);
    if (out.length >= MAX_FORMAT_RULES) break;
  }
  return out;
}

/** 空筛选方案（无任何条件） */
export function emptyViewFilter(): ViewFilter {
  return { logic: 'all', conditions: [] };
}

/** 条件值是否为空（与 cleanSearchParams 语义一致：false/0 合法保留） */
function isFilterValueEmpty(v: unknown): boolean {
  if (v == null) return true;
  if (typeof v === 'string') return v.length === 0;
  if (Array.isArray(v)) return v.length === 0;
  return false;
}

/**
 * 宽容归一化筛选方案（OSC-0015）。
 * 缺失/非法 → 空方案；logic 非 all/any → all；conditions 逐条过滤非法项。
 * 未知顶层属性与未知条件扩展字段在 round-trip 时由调用方保留（serializeNamedView 透传 _raw）。
 * 字段有效性清理由 filterToSearchParams 在应用时按合法 key 集完成。
 */
export function normalizeFilter(raw: unknown): ViewFilter {
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return emptyViewFilter();
  const o = raw as Record<string, unknown>;
  const logic = o.logic === 'any' ? 'any' : 'all';
  const conditions: ViewFilterCondition[] = [];
  if (Array.isArray(o.conditions)) {
    for (const c of o.conditions) {
      if (!c || typeof c !== 'object' || Array.isArray(c)) continue;
      const co = c as Record<string, unknown>;
      const field = typeof co.field === 'string' ? co.field.trim() : '';
      if (!field) continue;
      const op = (FILTER_OPS as readonly string[]).includes(co.op as string)
        ? (co.op as ViewFilterOp)
        : null;
      if (!op) continue;
      // 为空/不为空无值要求；其余操作符需要非空值
      if (op !== 'isNull' && op !== 'notNull' && isFilterValueEmpty(co.value)) continue;
      const cond: ViewFilterCondition = { field, op };
      if (co.value !== undefined) cond.value = co.value;
      if (co.value2 !== undefined) cond.value2 = co.value2;
      conditions.push(cond);
    }
  }
  return { logic, conditions };
}

/**
 * 归一化分组字段列表（OSC-0015）：仅保留非空字符串、去重、上限 3 个。
 */
export function normalizeGroup(raw: unknown): ViewGroup {
  if (!Array.isArray(raw)) return [];
  const seen = new Set<string>();
  const out: string[] = [];
  for (const f of raw) {
    if (typeof f !== 'string' || !f.trim()) continue;
    const name = f.trim();
    if (seen.has(name)) continue;
    seen.add(name);
    out.push(name);
    if (out.length >= 3) break;
  }
  return out;
}

/** 预定义查询条目（OSC-0016） */
export interface SavedQuery {
  /** 唯一 id；生成规则 `q_` + Date.now().toString(36) + 4 位随机 base36 */
  id: string;
  /** 查询名；trim 后 1~50 字符 */
  name: string;
  /** 查询参数：经 cleanSearchParams 清理的平坦键值（含 Q/dtStart/dtEnd 保留键） */
  params: Record<string, unknown>;
}

/** QueriesJson 线缆格式（OSC-0016） */
export interface SavedQueriesWire {
  version: 1;
  queries: SavedQuery[];
}

export const SAVED_QUERIES_VERSION = 1;

/** 生成预定义查询条目 id：`q_` + 时间戳 base36 + 4 位随机 base36 */
export function generateQueryId(): string {
  const rand = Math.floor(Math.random() * 36 ** 4)
    .toString(36)
    .padStart(4, '0');
  return `q_${Date.now().toString(36)}${rand}`;
}

/** 空 QueriesJson 线缆 */
export function emptySavedQueries(): SavedQueriesWire {
  return { version: SAVED_QUERIES_VERSION, queries: [] };
}

/** 归一化单条预定义查询；非法/空参数/空名称返回 null（OSC-0016 §3.1） */
export function normalizeSavedQuery(
  raw: unknown,
  searchFields: FieldMeta[],
  usedIds?: Set<string>,
): SavedQuery | null {
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return null;
  const o = raw as Record<string, unknown>;
  const name = typeof o.name === 'string' ? o.name.trim() : '';
  if (!name) return null;
  const params = cleanSearchParams(
    o.params && typeof o.params === 'object' && !Array.isArray(o.params)
      ? (o.params as Record<string, unknown>)
      : {},
    collectSearchKeys(searchFields),
  );
  if (Object.keys(params).length === 0) return null;
  const id =
    typeof o.id === 'string' && o.id && !usedIds?.has(o.id) ? o.id : generateQueryId();
  usedIds?.add(id);
  return { id, name: name.slice(0, 50), params };
}

/**
 * 宽容解析 QueriesJson（OSC-0016）。
 * 缺失/空串/解析失败/非对象/version 不符 → 空列表；queries 非数组 → []；
 * 逐条：非对象/空 name/空 params 丢弃，name 截断 50，id 非法或重复重新生成。
 */
export function parseQueriesWire(
  raw: string | null | undefined,
  searchFields: FieldMeta[],
): SavedQueriesWire {
  if (!raw || typeof raw !== 'string') return emptySavedQueries();
  let v: unknown;
  try {
    v = JSON.parse(raw);
  } catch {
    return emptySavedQueries();
  }
  if (!v || typeof v !== 'object' || Array.isArray(v)) return emptySavedQueries();
  const o = v as Record<string, unknown>;
  if (o.version !== SAVED_QUERIES_VERSION) return emptySavedQueries();
  const queries: SavedQuery[] = [];
  if (Array.isArray(o.queries)) {
    const usedIds = new Set<string>();
    for (const q of o.queries) {
      const saved = normalizeSavedQuery(q, searchFields, usedIds);
      if (saved) queries.push(saved);
    }
  }
  return { version: SAVED_QUERIES_VERSION, queries };
}

/** 序列化 QueriesJson 线缆；空列表序列化为 {"version":1,"queries":[]} */
export function serializeQueriesWire(wire: SavedQueriesWire): string {
  return JSON.stringify(wire);
}

/** FiltersJson 线缆格式（OSC-0012）。key 为 NamedView.id，值为经过搜索正规化的平坦搜索参数 */
export interface SavedFiltersWire {
  version: 1;
  views: Record<string, Record<string, unknown>>;
}

export const SAVED_FILTERS_VERSION = 1;

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
  allowDelete: true,
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
    frozen: o.frozen === 'left' || o.frozen === 'right' ? o.frozen : false,
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
  return arrangeFrozenColumns(ordered);
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
    allowDelete: boolOr(o.allowDelete, true),
    expandRow: boolOr(o.expandRow, false),
  };
}

/**
 * 归一化受限洞察配置（OSC-0012）。
 * 缺失/非法 → 双开关关闭；兼容早期草案 `mode`：stat→仅 showStat，chart→仅 showChart，none/其他→都关闭。
 * `chartOption`（OSC-260819e483 P5）：仅接受 JSON 对象；非对象归一化为缺省。
 */
export function normalizeInsight(raw: unknown): ViewInsight {
  if (!raw || typeof raw !== 'object') return { showStat: false, showChart: false };
  const o = raw as Record<string, unknown>;
  const opt = validChartOption(o.chartOption);
  if (typeof o.mode === 'string') {
    if (o.mode === 'stat') return { showStat: true, showChart: false };
    if (o.mode === 'chart')
      return { showStat: false, showChart: true, ...(opt !== undefined ? { chartOption: opt } : {}) };
    return { showStat: false, showChart: false };
  }
  return {
    showStat: o.showStat === true,
    showChart: o.showChart === true,
    ...(opt !== undefined ? { chartOption: opt } : {}),
  };
}

/** chartOption 合法值：非数组 JSON 对象；其余归一化为缺省（design：禁止函数，JSON 本无函数） */
function validChartOption(v: unknown): unknown {
  return v && typeof v === 'object' && !Array.isArray(v) ? v : undefined;
}

/**
 * 深拷贝并剔除 ECharts option 中的运行时数据：`dataset.source` 与每个 `series[i].data`。
 * 禁止把列表快照写进 Profile（OSC-260819e483 P5）。
 */
export function stripChartData(option: unknown): unknown {
  if (Array.isArray(option)) return option.map((x) => stripChartData(x));
  if (!option || typeof option !== 'object') return option;
  const obj = option as Record<string, unknown>;
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(obj)) {
    // dataset.source：运行时列表数据
    if (k === 'source') continue;
    // series[i].data：运行时序列数据
    if (k === 'data' && Array.isArray(v)) continue;
    out[k] = stripChartData(v);
  }
  return out;
}

/**
 * 用当前列表行填充 ECharts option 模板（OSC-260819e483 P5）：
 * 写入 `dataset.source = rows`（行对象键 = GetPage 列表字段名）；无 `dataset` 则补上。
 * 数据随当前 GetList（含 search/viewFilter），不另开大通道。
 */
export function applyChartData(option: unknown, rows: Record<string, unknown>[]): Record<string, unknown> {
  const copy = (option && typeof option === 'object'
    ? JSON.parse(JSON.stringify(option))
    : {}) as Record<string, unknown>;
  const ds =
    copy.dataset && typeof copy.dataset === 'object' && !Array.isArray(copy.dataset)
      ? (copy.dataset as Record<string, unknown>)
      : {};
  ds.source = rows;
  copy.dataset = ds;
  return copy;
}

/** 序列化 insight：保留原始未知扩展字段，仅覆盖本号管理的双开关与 chartOption（OSC-260819e483 P5） */
function serializeInsight(insight: ViewInsight | undefined, raw: unknown): Record<string, unknown> {
  const base: Record<string, unknown> =
    raw && typeof raw === 'object' && !Array.isArray(raw)
      ? { ...(raw as Record<string, unknown>) }
      : {};
  delete base.mode;
  base.showStat = insight?.showStat === true;
  base.showChart = insight?.showChart === true;
  if (insight?.chartOption !== undefined) {
    // 保存前清洗：剔除 dataset.source / series[].data；清洗后超 32KB 拒绝保存（不截断半写）
    const clean = stripChartData(insight.chartOption);
    if (JSON.stringify(clean).length > CHART_OPTION_MAX_BYTES) {
      throw new Error(`图表配置超过 ${Math.floor(CHART_OPTION_MAX_BYTES / 1024)}KB 限制，保存已取消`);
    }
    base.chartOption = clean;
  } else {
    delete base.chartOption;
  }
  return base;
}

/** 序列化单个命名视图：保留未知顶层属性，已知域由状态对象覆盖（round-trip 不丢后续 OSC 字段） */
export function serializeNamedView(v: NamedView): Record<string, unknown> {
  const raw: Record<string, unknown> = {};
  if (v._raw && typeof v._raw === 'object') {
    for (const [k, val] of Object.entries(v._raw)) {
      if (MANAGED_VIEW_KEYS.has(k)) continue;
      raw[k] = val;
    }
  }
  raw.id = v.id;
  raw.name = v.name;
  raw.view = v.view;
  if (v.columns) raw.columns = v.columns;
  if (v.sort) raw.sort = v.sort;
  if (v.chrome) raw.chrome = v.chrome;
  if (v.mapping !== undefined) raw.mapping = v.mapping;
  if (v.insight) raw.insight = serializeInsight(v.insight, v._raw?.insight);
  if (v.filter) raw.filter = { ...v.filter, conditions: v.filter.conditions.map((c) => ({ ...c })) };
  if (v.group && v.group.length) raw.group = [...v.group];
  if (v.format && v.format.length) {
    raw.format = v.format.map((r) => {
      const o: Record<string, unknown> = {
        id: r.id,
        apply: r.apply,
        color: r.color,
        field: r.field,
        op: r.op,
      };
      if (r.value !== undefined) o.value = r.value;
      if (r.bold) o.bold = true;
      return o;
    });
  }
  return raw;
}

/** 本号管理的命名视图键；序列化时优先由状态对象覆盖 */
const MANAGED_VIEW_KEYS = new Set([
  'id',
  'name',
  'view',
  'columns',
  'sort',
  'chrome',
  'mapping',
  'insight',
  'filter',
  'group',
  'format',
]);

/** 空 FiltersJson 线缆 */
export function emptySavedFilters(): SavedFiltersWire {
  return { version: SAVED_FILTERS_VERSION, views: {} };
}

/**
 * 宽容解析 FiltersJson（OSC-0012）。
 * 缺失、空串、非对象、未知 version、views 非法均归一为 `{ version: 1, views: {} }`；
 * 仅保留对象类型的视图筛选条目，损坏单条数据降级为空条件。
 */
export function parseSavedFilters(raw: string | null | undefined): SavedFiltersWire {
  if (!raw || typeof raw !== 'string') return emptySavedFilters();
  let v: unknown;
  try {
    v = JSON.parse(raw);
  } catch {
    return emptySavedFilters();
  }
  if (!v || typeof v !== 'object' || Array.isArray(v)) return emptySavedFilters();
  const o = v as Record<string, unknown>;
  if (o.version !== SAVED_FILTERS_VERSION) return emptySavedFilters();
  if (!o.views || typeof o.views !== 'object' || Array.isArray(o.views)) {
    return emptySavedFilters();
  }
  const views: Record<string, Record<string, unknown>> = {};
  for (const [k, val] of Object.entries(o.views as Record<string, unknown>)) {
    if (val && typeof val === 'object' && !Array.isArray(val)) {
      views[k] = val as Record<string, unknown>;
    }
  }
  return { version: SAVED_FILTERS_VERSION, views };
}

/** 序列化 FiltersJson 线缆 */
export function serializeSavedFilters(wire: SavedFiltersWire): string {
  return JSON.stringify(wire);
}

/** 读取指定命名视图的已保存筛选；无则 undefined */
export function getSavedViewFilters(
  wire: SavedFiltersWire,
  viewId: string,
): Record<string, unknown> | undefined {
  return wire.views[viewId];
}

/** 以完整筛选对象替换指定视图的已保存筛选（只影响当前视图） */
export function setSavedViewFilters(
  wire: SavedFiltersWire,
  viewId: string,
  filters: Record<string, unknown>,
): SavedFiltersWire {
  return {
    ...wire,
    views: { ...wire.views, [viewId]: { ...filters } },
  };
}

/** 清除指定视图的已保存筛选（删除该 key）；不存在则原样返回 */
export function clearSavedViewFilters(
  wire: SavedFiltersWire,
  viewId: string,
): SavedFiltersWire {
  if (!(viewId in wire.views)) return wire;
  const views = { ...wire.views };
  delete views[viewId];
  return { ...wire, views };
}

/** 视图域来源（OSC-0014）：personal > template > system */
export type ViewDomainSource = 'personal' | 'template' | 'system';

/** 筛选域来源（OSC-0014）：personal > template > system */
export type FilterDomainSource = 'personal' | 'template' | 'system';

/** 判定 ViewsJson 是否 present（含实际命名视图；空数组/空对象视为未配置） */
export function hasViewsDomain(raw: string | null | undefined): boolean {
  const arr = parseJsonArray(raw);
  return !!arr?.length;
}

/** 判定 FiltersJson 是否 present（含实际筛选条目；空 views map 视为未配置） */
export function hasFiltersDomain(raw: string | null | undefined): boolean {
  const wire = parseSavedFilters(raw);
  return Object.keys(wire.views).length > 0;
}

/** 表单布局模式：新增 / 编辑 / 详情（OSC-0013） */
export type FormMode = 'add' | 'edit' | 'detail';

/** 单个模式的受限表单布局（仅展示偏好，不改变元数据/权限/校验/提交载荷） */
export interface FormLayout {
  /** 字段顺序（canonical FieldMeta.name；未列字段按元数据原序追加） */
  order: string[];
  /** 隐藏字段（仅展示隐藏，不能绕过必填/校验/提交） */
  hidden: string[];
  /** 折叠的既有 Category（非空且当前字段集中存在才生效） */
  collapsedCategories: string[];
}

/** FormJson 线缆格式（OSC-0013）。add/edit/detail 独立配置；写入时只替换当前模式 */
export interface FormJsonWire {
  version: 1;
  add?: FormLayout;
  edit?: FormLayout;
  detail?: FormLayout;
}

export const FORM_JSON_VERSION = 1;

/** 空表单布局 */
export function emptyFormLayout(): FormLayout {
  return { order: [], hidden: [], collapsedCategories: [] };
}

/** 空 FormJson 线缆 */
export function emptyFormJson(): FormJsonWire {
  return { version: FORM_JSON_VERSION };
}

function isFormLayout(v: unknown): v is FormLayout {
  if (!v || typeof v !== 'object' || Array.isArray(v)) return false;
  const o = v as Record<string, unknown>;
  return (
    Array.isArray(o.order) &&
    Array.isArray(o.hidden) &&
    Array.isArray(o.collapsedCategories)
  );
}

/**
 * 宽容解析 FormJson（OSC-0013）。
 * 缺失、空串、数组、无效 JSON 或 version 不支持：归一到空线缆；
 * 仅保留结构合法的模式布局；未知顶层字段由 round-trip 丢弃（本号只管理三模式）。
 */
export function parseFormJson(raw: string | null | undefined): FormJsonWire {
  if (!raw || typeof raw !== 'string') return emptyFormJson();
  let v: unknown;
  try {
    v = JSON.parse(raw);
  } catch {
    return emptyFormJson();
  }
  if (!v || typeof v !== 'object' || Array.isArray(v)) return emptyFormJson();
  const o = v as Record<string, unknown>;
  if (o.version !== FORM_JSON_VERSION) return emptyFormJson();
  const wire: FormJsonWire = { version: FORM_JSON_VERSION };
  for (const mode of ['add', 'edit', 'detail'] as const) {
    if (isFormLayout(o[mode])) wire[mode] = { ...o[mode] };
  }
  return wire;
}

/** 序列化 FormJson 线缆 */
export function serializeFormJson(wire: FormJsonWire): string {
  return JSON.stringify(wire);
}

/** 读取指定模式的布局；无则 null */
export function getFormModeLayout(wire: FormJsonWire, mode: FormMode): FormLayout | null {
  return wire[mode] ?? null;
}

/** 以完整布局替换指定模式（只影响当前模式，保留另两模式） */
export function setFormModeLayout(
  wire: FormJsonWire,
  mode: FormMode,
  layout: FormLayout,
): FormJsonWire {
  return { ...wire, [mode]: { ...layout } };
}

/** 清除指定模式布局（恢复该模式默认）；不存在则原样返回 */
export function clearFormModeLayout(wire: FormJsonWire, mode: FormMode): FormJsonWire {
  if (!wire[mode]) return wire;
  const next = { ...wire };
  delete next[mode];
  return next;
}

/**
 * 由三模式本地编辑态构建 FormJson 线缆（OSC-0013 手动保存）。
 * 过滤全空布局（order/hidden/collapsedCategories 均为空，等同默认），避免冗余 key 落库。
 */
export function buildFormJsonWire(
  layouts: Record<FormMode, FormLayout>,
): FormJsonWire {
  const wire: FormJsonWire = { version: FORM_JSON_VERSION };
  for (const mode of ['add', 'edit', 'detail'] as const) {
    const l = layouts[mode];
    if (l && (l.order.length || l.hidden.length || l.collapsedCategories.length)) {
      wire[mode] = { ...l };
    }
  }
  return wire;
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
      insight: normalizeInsight(o.insight),
      filter: normalizeFilter(o.filter),
      group: normalizeGroup(o.group),
      format: (() => {
        const f = normalizeFormat(o.format, metaKeys);
        return f.length ? f : undefined;
      })(),
      // 保留原始线缆对象，round-trip 不丢未知顶层属性（后续 OSC 扩展字段）
      _raw: o,
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
    viewsJson: JSON.stringify(state.views.map(serializeNamedView)),
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

/** 左冻结列数（可见且 frozen:left；调用方须先 arrangeFrozenColumns 以保证这些列在最左） */
export function frozenLeftCount(columns: ColumnPref[]): number {
  let n = 0;
  for (const c of columns) {
    if (c.visible && c.frozen === 'left') n += 1;
  }
  return n;
}

/** 右冻结列数（可见且 frozen:right；调用方须先 arrangeFrozenColumns 以保证这些列在最右） */
export function frozenRightCount(columns: ColumnPref[]): number {
  let n = 0;
  for (const c of columns) {
    if (c.visible && c.frozen === 'right') n += 1;
  }
  return n;
}

/**
 * 可见列分区：左冻结 → 未冻结 → 右冻结，隐藏列置末。
 * 组内保持原相对顺序。
 */
export function arrangeFrozenColumns(columns: ColumnPref[]): ColumnPref[] {
  const left: ColumnPref[] = [];
  const mid: ColumnPref[] = [];
  const right: ColumnPref[] = [];
  const hidden: ColumnPref[] = [];
  for (const c of columns) {
    if (!c.visible) hidden.push(c);
    else if (c.frozen === 'left') left.push(c);
    else if (c.frozen === 'right') right.push(c);
    else mid.push(c);
  }
  columns.length = 0;
  columns.push(...left, ...mid, ...right, ...hidden);
  return columns;
}

/** 切换该列左冻结（与右冻结互斥），并归位到最左 */
export function applyFrozenLeftTo(columns: ColumnPref[], key: string): ColumnPref[] {
  const col = columns.find((c) => c.key === key);
  if (!col) return columns;
  col.frozen = col.frozen === 'left' ? false : 'left';
  return arrangeFrozenColumns(columns);
}

/** 切换该列右冻结（与左冻结互斥），并归位到最右 */
export function applyFrozenRightTo(columns: ColumnPref[], key: string): ColumnPref[] {
  const col = columns.find((c) => c.key === key);
  if (!col) return columns;
  col.frozen = col.frozen === 'right' ? false : 'right';
  return arrangeFrozenColumns(columns);
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
  chromeOverride?: Partial<ViewChrome>,
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
    // chromeOverride 用于创建时按用户权限覆盖默认（如 allowDelete=用户是否有删除权限）
    chrome: { ...(active.chrome || DEFAULT_CHROME), ...(chromeOverride || {}) },
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

/**
 * 恢复视图：把指定视图重置为创建时的默认状态（保留 id/名称/删除权限；
 * 列/排序/映射/筛选/分组/洞察恢复默认，等价重新创建该类型视图的配置）。
 */
export function restoreNamedView(
  state: EntityViewState,
  id: string,
  metaKeys: string[],
  fields?: FieldMeta[],
): EntityViewState {
  const src = state.views.find((v) => v.id === id);
  if (!src) throw new Error('视图不存在');
  const mapping = fields && fields.length ? seedMapping(src.view, fields) : undefined;
  const next: NamedView = {
    id: src.id,
    name: src.name,
    view: src.view,
    columns: mergeColumns(metaKeys, []),
    sort: null,
    // 删除权限为权限相关字段（创建时按用户删除权限写入），恢复时保留，其余外观回默认
    chrome: {
      ...DEFAULT_CHROME,
      ...(src.chrome?.allowDelete !== undefined
        ? { allowDelete: src.chrome.allowDelete }
        : {}),
    },
    mapping,
  };
  return {
    ...state,
    views: state.views.map((v) => (v.id === id ? next : v)),
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

/** 更新当前命名视图的受限洞察配置（OSC-0012） */
export function patchActiveInsight(
  state: EntityViewState,
  insight: ViewInsight,
): EntityViewState {
  return {
    ...state,
    views: state.views.map((v) =>
      v.id === state.activeViewId ? { ...v, insight: { ...insight } } : v,
    ),
  };
}

/** 更新当前命名视图的筛选构建器方案（OSC-0015）；空方案等价清除 */
export function patchActiveFilter(
  state: EntityViewState,
  filter: ViewFilter,
): EntityViewState {
  const next = normalizeFilter(filter);
  const hasFilter = next.conditions.length > 0;
  return {
    ...state,
    views: state.views.map((v) => {
      if (v.id !== state.activeViewId) return v;
      if (!hasFilter) {
        const { filter: _f, ...rest } = v;
        return rest as NamedView;
      }
      return { ...v, filter: next };
    }),
  };
}

/** 更新当前命名视图的多级分组字段（OSC-0015）；空数组等价清除 */
export function patchActiveGroup(state: EntityViewState, group: ViewGroup): EntityViewState {
  const next = normalizeGroup(group);
  return {
    ...state,
    views: state.views.map((v) => {
      if (v.id !== state.activeViewId) return v;
      if (!next.length) {
        const { group: _g, ...rest } = v;
        return rest as NamedView;
      }
      return { ...v, group: next };
    }),
  };
}

/** 更新当前命名视图的填色规则；空数组等价清除 */
export function patchActiveFormat(state: EntityViewState, format: ViewFormatRule[]): EntityViewState {
  const next = normalizeFormat(format);
  return {
    ...state,
    views: state.views.map((v) => {
      if (v.id !== state.activeViewId) return v;
      if (!next.length) {
        const { format: _f, ...rest } = v;
        return rest as NamedView;
      }
      return { ...v, format: next };
    }),
  };
}

export function resolveChrome(view: NamedView | null | undefined): Required<ViewChrome> {
  return { ...DEFAULT_CHROME, ...(view?.chrome || {}) };
}
