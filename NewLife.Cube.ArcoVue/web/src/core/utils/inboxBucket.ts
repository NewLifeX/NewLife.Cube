import { parseWallClock } from './datetime';

/**
 * 站内通知日期分桶：今天 / 本周 / 本月 / 更长时间（从近到远，空桶不输出）。
 * 分桶优先级：今天 > 本周（周一起算）> 本月 > 更长时间；时间以壁钟时间解析（parseWallClock），
 * 缺失或无法解析的时间归入「更长时间」。
 */

export type InboxBucketKey = 'today' | 'week' | 'month' | 'earlier';

export interface InboxBucket<T> {
  key: InboxBucketKey;
  label: string;
  items: T[];
}

/** 分桶显示名（顺序即展示顺序：从近到远） */
export const INBOX_BUCKET_LABELS: Record<InboxBucketKey, string> = {
  today: '今天',
  week: '本周',
  month: '本月',
  earlier: '更长时间',
};

const BUCKET_ORDER: InboxBucketKey[] = ['today', 'week', 'month', 'earlier'];

/** 壁钟时间 → 本地时间戳；无效返回 NaN */
function wallClockTime(v: unknown): number {
  const w = parseWallClock(v);
  if (!w) return NaN;
  return new Date(w.y, w.m - 1, w.d, w.h, w.mi, w.s).getTime();
}

/** 当天 00:00:00 */
function startOfDay(now: Date): number {
  return new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
}

/** 本周一 00:00:00（一周从周一开始） */
function startOfWeek(now: Date): number {
  const d = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const offset = (d.getDay() + 6) % 7;
  d.setDate(d.getDate() - offset);
  return d.getTime();
}

/** 本月 1 日 00:00:00 */
function startOfMonth(now: Date): number {
  return new Date(now.getFullYear(), now.getMonth(), 1).getTime();
}

/**
 * 按日期把通知分桶。输入顺序在桶内保持不变（后端按时间倒序下发）。
 * @param items 通知列表，需含 createTime
 * @param now 参考时间，默认当前时间（便于测试注入）
 * @returns 非空桶列表，顺序：今天 → 本周 → 本月 → 更长时间
 */
export function bucketInboxByDate<T extends { createTime?: unknown }>(
  items: readonly T[],
  now: Date = new Date(),
): InboxBucket<T>[] {
  const dayStart = startOfDay(now);
  const weekStart = startOfWeek(now);
  const monthStart = startOfMonth(now);

  const buckets = new Map<InboxBucketKey, T[]>();
  for (const item of items ?? []) {
    const t = wallClockTime(item?.createTime);
    let key: InboxBucketKey;
    if (t >= dayStart) key = 'today';
    else if (t >= weekStart) key = 'week';
    else if (t >= monthStart) key = 'month';
    else key = 'earlier';

    const list = buckets.get(key);
    if (list) list.push(item);
    else buckets.set(key, [item]);
  }

  return BUCKET_ORDER.filter((k) => buckets.has(k)).map((k) => ({
    key: k,
    label: INBOX_BUCKET_LABELS[k],
    items: buckets.get(k)!,
  }));
}
