/**
 * 搜索条件正规化（OSC-0012）。
 * 负责从 search 字段元数据收集合法 key、按 key 集清理搜索参数、解析 URL 搜索参数，
 * 供 DefaultList 的 effectiveSearch 与 FiltersJson 保存/读取共用。
 */
import type { FieldMeta } from '@/core/types/field';
import { resolveSearchControl } from '@/core/utils/fieldControl';

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
