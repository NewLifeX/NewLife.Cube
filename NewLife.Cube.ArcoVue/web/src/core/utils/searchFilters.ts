/**
 * 搜索条件正规化（OSC-0012）。
 * 负责从 search 字段元数据收集合法 key、按 key 集清理搜索参数、解析 URL 搜索参数，
 * 供 DefaultList 的 effectiveSearch 与 FiltersJson 保存/读取共用。
 */
import type { FieldMeta } from '@/core/types/field';
import { resolveSearchControl } from '@/core/utils/fieldControl';
import { getValueByKey } from '@/core/utils/url';
import type { ViewFilter } from '@/core/utils/viewProfile';

/** 范围型搜索控件：以 `字段_min`/`字段_max` 两个 key 提交，其余以字段名一个 key 提交 */
const RANGE_CONTROLS: ReadonlySet<string> = new Set([
  'numberRange',
  'dateRange',
  'datetimeRange',
  'timeRange',
]);

/** 收集当前 search 字段的合法搜索 key 集合（含范围字段的 _min/_max 后缀） */
export function collectSearchKeys(fields: FieldMeta[]): Set<string> {
  const keys = new Set<string>();
  for (const f of fields) {
    if (!f.name) continue;
    if (RANGE_CONTROLS.has(resolveSearchControl(f))) {
      keys.add(`${f.name}_min`);
      keys.add(`${f.name}_max`);
    } else {
      keys.add(f.name);
    }
  }
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
 * 供 QueryInsightPanel 洞察统计区展示。
 */
export function resolveStatEntries(
  statData: Record<string, unknown> | null,
): { key: string; value: string }[] {
  if (!statData) return [];
  return Object.entries(statData)
    .filter(([k, v]) => k && v != null)
    .map(([k, v]) => ({ key: k, value: String(v) }));
}

/**
 * 筛选构建器方案 → 扁平搜索参数（OSC-0015）。
 * - eq：`{ 字段: 值 }`；值数组（多选字段）→ 逗号分隔字符串。
 * - between：`{ 字段_min, 字段_max }`，单侧填值只输出对应参数。
 * - 结果统一经 cleanSearchParams 按合法 key 集清理未知/空值。
 * - clientOnly：logic=any 且条件数>1 时后端无法表达跨字段 OR，需前端对已加载数据二次过滤。
 */
export function filterToSearchParams(
  filter: ViewFilter | null | undefined,
  _fields: FieldMeta[],
  keys: Set<string>,
): { params: Record<string, unknown>; clientOnly: boolean } {
  const empty = { params: {} as Record<string, unknown>, clientOnly: false };
  if (!filter || !filter.conditions.length) return empty;

  const params: Record<string, unknown> = {};
  for (const c of filter.conditions) {
    if (c.op === 'between') {
      if (!isEmptyValue(c.value)) params[`${c.field}_min`] = c.value;
      if (!isEmptyValue(c.value2)) params[`${c.field}_max`] = c.value2;
    } else {
      if (Array.isArray(c.value)) {
        const joined = c.value.map((v) => String(v)).filter(Boolean).join(',');
        if (joined) params[c.field] = joined;
      } else if (!isEmptyValue(c.value)) {
        params[c.field] = c.value;
      }
    }
  }
  return {
    params: cleanSearchParams(params, keys),
    clientOnly: filter.logic === 'any' && filter.conditions.length > 1,
  };
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

/** 单条件对单行的匹配（OSC-0015，客户端二次过滤用） */
function matchCondition(
  row: Record<string, unknown>,
  cond: { field: string; op: 'eq' | 'between'; value?: unknown; value2?: unknown },
): boolean {
  const raw = getValueByKey(row, cond.field);
  if (cond.op === 'between') {
    if (raw == null || raw === '') return false;
    const loOk =
      cond.value == null || cond.value === ''
        ? true
        : compareValues(raw, cond.value) !== 'lt';
    const hiOk =
      cond.value2 == null || cond.value2 === ''
        ? true
        : compareValues(raw, cond.value2) !== 'gt';
    return loOk && hiOk;
  }
  // eq：数组（多选）任一命中；标量宽松字符串比较
  if (Array.isArray(cond.value)) {
    const want = cond.value.map((v) => String(v));
    if (Array.isArray(raw)) return raw.some((r) => want.includes(String(r)));
    return want.includes(String(raw));
  }
  if (raw == null) return cond.value == null;
  if (Array.isArray(raw)) return raw.some((r) => String(r) === String(cond.value ?? ''));
  return String(raw) === String(cond.value ?? '');
}

/**
 * 客户端筛选谓词（OSC-0015）。用于 logic=any 且条件数>1 的后端无法表达场景，
 * 对已加载数据行做二次过滤；logic=all 时后端已保证，一般无需调用。
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
