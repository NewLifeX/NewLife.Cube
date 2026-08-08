/**
 * 搜索条件正规化（OSC-0012）。
 * 负责从 search 字段元数据收集合法 key、按 key 集清理搜索参数、解析 URL 搜索参数，
 * 供 DefaultList 的 effectiveSearch 与 FiltersJson 保存/读取共用。
 */
import type { FieldMeta } from '@/core/types/field';
import { getValueByKey } from '@/core/utils/url';
import type { ViewFilter, ViewFilterCondition } from '@/core/utils/viewProfile';

/** 保留搜索键（OSC-0016）：Q=全字段模糊，dtStart/dtEnd=主时间区间（后端 Search(Pager) 内置通用参数） */
export const RESERVED_SEARCH_KEYS: readonly string[] = ['Q', 'dtStart', 'dtEnd'];

/** 收集当前 search 字段的合法搜索 key 集合（字段名 ∪ 保留键 Q/dtStart/dtEnd） */
export function collectSearchKeys(fields: FieldMeta[]): Set<string> {
  const keys = new Set<string>();
  for (const f of fields) {
    if (!f.name) continue;
    keys.add(f.name);
  }
  for (const k of RESERVED_SEARCH_KEYS) keys.add(k);
  return keys;
}

/** 空值判定：null/undefined/空字符串/空数组视为无值；false 与 0 合法且保留 */
function isEmptyValue(v: unknown): boolean {
  if (v == null) return true;
  if (typeof v === 'string') return v.length === 0;
  if (Array.isArray(v)) return v.length === 0;
  return false;
}

/**
 * 按合法 key 集清理搜索参数：移除未知/失效字段与空值，false/0/合法数组保留。
 * 用于已保存筛选读取、保存前归一与请求参数构造。
 */
export function cleanSearchParams(
  params: Record<string, unknown>,
  keys: Set<string>,
): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(params)) {
    if (!keys.has(k)) continue;
    if (isEmptyValue(v)) continue;
    out[k] = v;
  }
  return out;
}

/**
 * 从路由 query 解析搜索参数：仅保留合法 key 且非空的值；标量统一为字符串，数组保留。
 * URL 只影响当前会话，不写入个人存储。
 */
export function parseUrlSearch(
  query: Record<string, unknown>,
  keys: Set<string>,
): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  for (const [k, raw] of Object.entries(query)) {
    if (!keys.has(k)) continue;
    if (raw == null) continue;
    const v = Array.isArray(raw) ? raw : String(raw);
    if (isEmptyValue(v)) continue;
    out[k] = v;
  }
  return out;
}

/**
 * 统计标签条目：仅保留 stat 中非 null 的字段（空 stat 返回空数组，展示「暂无统计」而非编造 0）。
 * 供 InsightPanel 洞察统计区展示。
 */
export function resolveStatEntries(
  statData: Record<string, unknown> | null,
): { key: string; value: string }[] {
  if (!statData) return [];
  return Object.entries(statData)
    .filter(([k, v]) => k && v != null)
    .map(([k, v]) => ({ key: k, value: String(v) }));
}

/** 宽松比较两值：数值优先，否则字符串字典序；任一空返回 'na' */
function compareValues(a: unknown, b: unknown): 'lt' | 'eq' | 'gt' | 'na' {
  if (a == null || a === '' || b == null || b === '') return 'na';
  const an = Number(a);
  const bn = Number(b);
  if (!Number.isNaN(an) && !Number.isNaN(bn)) return an < bn ? 'lt' : an > bn ? 'gt' : 'eq';
  const as = String(a);
  const bs = String(b);
  return as < bs ? 'lt' : as > bs ? 'gt' : 'eq';
}

/** 等值匹配：数组（多选）任一命中；标量宽松字符串比较 */
function matchEq(raw: unknown, value: unknown): boolean {
  if (Array.isArray(value)) {
    const want = value.map((v) => String(v));
    if (Array.isArray(raw)) return raw.some((r) => want.includes(String(r)));
    return want.includes(String(raw));
  }
  if (raw == null) return value == null;
  if (Array.isArray(raw)) return raw.some((r) => String(r) === String(value ?? ''));
  return String(raw) === String(value ?? '');
}

/** 单条件对单行的匹配（OSC-0015 纯前端过滤） */
function matchCondition(row: Record<string, unknown>, cond: ViewFilterCondition): boolean {
  const raw = getValueByKey(row, cond.field);
  switch (cond.op) {
    case 'eq':
      return matchEq(raw, cond.value);
    case 'neq':
      return !matchEq(raw, cond.value);
    case 'contains':
      return String(raw ?? '').includes(String(cond.value ?? ''));
    case 'notContains':
      return !String(raw ?? '').includes(String(cond.value ?? ''));
    case 'isNull':
      return raw == null || raw === '';
    case 'notNull':
      return raw != null && raw !== '';
    case 'gt':
      return compareValues(raw, cond.value) === 'gt';
    case 'gte': {
      // 空值行返回 'na'，不得把空值纳入「大于等于」（与 isNull 语义区分）
      const cmp = compareValues(raw, cond.value);
      return cmp === 'gt' || cmp === 'eq';
    }
    case 'lt':
      return compareValues(raw, cond.value) === 'lt';
    case 'lte': {
      const cmp = compareValues(raw, cond.value);
      return cmp === 'lt' || cmp === 'eq';
    }
    case 'after':
      return compareValues(raw, cond.value) === 'gt';
    case 'before':
      return compareValues(raw, cond.value) === 'lt';
  }
  return false;
}

/**
 * 客户端筛选谓词（OSC-0015）：对已加载数据行按筛选方案匹配。
 * 业务重写 Search / 树控制器后端不应用通用等值过滤，前端兜底过滤保证筛选生效；
 * 普通控制器后端已过滤时幂等。eq/neq/contains/notContains/isNull/notNull/gt/gte/lt/lte/after/before 全支持。
 */
export function matchesViewFilter(
  row: Record<string, unknown>,
  filter: ViewFilter,
  _fields: FieldMeta[],
): boolean {
  if (!filter.conditions.length) return true;
  const results = filter.conditions.map((c) => matchCondition(row, c));
  return filter.logic === 'any' ? results.some(Boolean) : results.every(Boolean);
}
