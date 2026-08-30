/**
 * 日期时间字段分组分桶（前端聚合）。
 *
 * 当分组字段是日期时间类型时，按“壁钟时间”把记录归入固定的时间桶，
 * 由近到远排序：今天 → 最近3天 → 本周 → 这个月早些时候 → 上月 → 今年早些时候 → 很久以前。
 *
 * 分桶标签以有序前缀“${序号}·${标签}”写入记录临时字段作为 VTable groupBy 键，
 * 保证组间按时间近远排序，展示时经 stripTimeBucketLabel 去掉前缀。
 */

import { parseWallClock } from './datetime';

/** 时间桶展示标签（按近到远） */
export const TIME_BUCKET_LABELS = [
  '今天',
  '最近3天',
  '本周',
  '这个月早些时候',
  '上月',
  '今年早些时候',
  '很久以前',
] as const;

/** 无法解析出时间的记录归入的桶标签（排在最后，多级分组时显示“未明确时间”） */
export const UNKNOWN_TIME_LABEL = '未明确时间';
const UNKNOWN_TIME_KEY = `7·${UNKNOWN_TIME_LABEL}`;

/** 可作为日期时间分组的 typeName（排除纯时间 TimeOnly：无日期无法分桶） */
const DATETIME_BUCKET_TYPES: ReadonlySet<string> = new Set([
  'DateTime',
  'DateTimeOffset',
  'DateOnly',
]);

/** 字段是否为可做日期时间分桶的类型 */
export function isDateTimeBucketType(typeName: string | undefined): boolean {
  return !!typeName && DATETIME_BUCKET_TYPES.has(typeName.trim());
}

/** 一天毫秒数 */
const DAY_MS = 24 * 60 * 60 * 1000;

function startOfDay(d: Date): number {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
}

/** 本周（周一起）零点时间戳。用固定 24h 回退，仅日维度分桶可接受；跨 DST 切换可能偏移 1 小时，不用于精确时刻 */
function startOfWeek(d: Date): number {
  const day = (d.getDay() + 6) % 7; // 周一=0 … 周日=6
  return startOfDay(d) - day * DAY_MS;
}

/** 本月一日零点时间戳 */
function startOfMonth(d: Date): number {
  return new Date(d.getFullYear(), d.getMonth(), 1).getTime();
}

/** 上月一日零点时间戳 */
function startOfPrevMonth(d: Date): number {
  return new Date(d.getFullYear(), d.getMonth() - 1, 1).getTime();
}

/** 今年一月一日零点时间戳 */
function startOfYear(d: Date): number {
  return new Date(d.getFullYear(), 0, 1).getTime();
}

/**
 * 计算单个时间值所属的时间桶键（“${序号}·${标签}”）。
 * 无效/空值归入“未明确时间”桶（键 “7·未明确时间”，排在最后）。now 缺省取当前时间（便于测试注入）。
 */
export function timeBucketOf(value: unknown, now: Date = new Date()): string {
  const w = parseWallClock(value);
  if (!w) return UNKNOWN_TIME_KEY;
  const d = new Date(w.y, w.m - 1, w.d, w.h, w.mi, w.s);
  if (Number.isNaN(d.getTime())) return UNKNOWN_TIME_KEY;
  const t = d.getTime();
  const today = startOfDay(now);
  if (t >= today) return `0·${TIME_BUCKET_LABELS[0]}`;
  if (t >= today - 3 * DAY_MS) return `1·${TIME_BUCKET_LABELS[1]}`;
  if (t >= startOfWeek(now)) return `2·${TIME_BUCKET_LABELS[2]}`;
  if (t >= startOfMonth(now)) return `3·${TIME_BUCKET_LABELS[3]}`;
  if (t >= startOfPrevMonth(now)) return `4·${TIME_BUCKET_LABELS[4]}`;
  if (t >= startOfYear(now)) return `5·${TIME_BUCKET_LABELS[5]}`;
  return `6·${TIME_BUCKET_LABELS[6]}`;
}

/**
 * 去掉时间桶键的排序前缀，返回纯展示标签。
 * 非字符串（如未分组）返回 undefined，由调用方回落“未分组”。
 */
export function stripTimeBucketLabel(value: unknown): string | undefined {
  if (typeof value !== 'string') return undefined;
  const parts = value.split('·');
  return parts.length > 1 ? parts[1] : value;
}

/**
 * 时间排序值（毫秒时间戳），用于分组视图把记录按日期近到远排序。
 * 无效/空值返回 -Infinity，排到最后（未分组）。
 */
export function timeSortValue(value: unknown): number {
  const w = parseWallClock(value);
  if (!w) return -Infinity;
  const d = new Date(w.y, w.m - 1, w.d, w.h, w.mi, w.s);
  return Number.isNaN(d.getTime()) ? -Infinity : d.getTime();
}
