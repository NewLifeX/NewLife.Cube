import { describe, expect, it } from 'vitest';
import { bucketInboxByDate } from './inboxBucket';

type Row = { id: number; createTime?: string };

const row = (id: number, createTime?: string): Row => ({ id, createTime });

describe('bucketInboxByDate', () => {
  // 2026-10-15 周四；本周一 = 10-12；本月 1 日 = 10-01
  const now = new Date(2026, 9, 15, 15, 0, 0);

  it('buckets items into today / week / month / earlier with near-to-far order', () => {
    const items = [
      row(1, '2026-10-15 08:00:00'), // 今天
      row(2, '2026-10-13 09:30:00'), // 本周（周二）
      row(3, '2026-10-12 00:30:00'), // 本周（周一，边界内）
      row(4, '2026-10-05 12:00:00'), // 本月
      row(5, '2026-10-01 00:00:00'), // 本月（1 日 00:00 边界）
      row(6, '2026-09-30 23:59:59'), // 更长时间
    ];

    const groups = bucketInboxByDate(items, now);

    expect(groups.map((g) => g.key)).toEqual(['today', 'week', 'month', 'earlier']);
    expect(groups.map((g) => g.label)).toEqual(['今天', '本周', '本月', '更长时间']);
    expect(groups[0].items.map((x) => x.id)).toEqual([1]);
    expect(groups[1].items.map((x) => x.id)).toEqual([2, 3]);
    expect(groups[2].items.map((x) => x.id)).toEqual([4, 5]);
    expect(groups[3].items.map((x) => x.id)).toEqual([6]);
  });

  it('skips empty buckets', () => {
    const groups = bucketInboxByDate([row(1, '2026-10-15 08:00:00')], now);

    expect(groups).toHaveLength(1);
    expect(groups[0].key).toBe('today');
  });

  it('accepts backend wall-clock formats with T or space separator', () => {
    const groups = bucketInboxByDate(
      [row(1, '2026-10-15T08:00:00'), row(2, '2026-10-15 09:00:00')],
      now,
    );

    expect(groups).toHaveLength(1);
    expect(groups[0].items.map((x) => x.id)).toEqual([1, 2]);
  });

  it('puts invalid or missing createTime into earlier', () => {
    const groups = bucketInboxByDate([row(1), row(2, ''), row(3, 'not-a-date')], now);

    expect(groups).toHaveLength(1);
    expect(groups[0].key).toBe('earlier');
    expect(groups[0].items.map((x) => x.id)).toEqual([1, 2, 3]);
  });

  it('keeps input order inside a bucket', () => {
    const groups = bucketInboxByDate(
      [row(9, '2026-10-15 12:00:00'), row(8, '2026-10-15 08:00:00')],
      now,
    );

    expect(groups[0].items.map((x) => x.id)).toEqual([9, 8]);
  });

  it('returns empty list for empty input', () => {
    expect(bucketInboxByDate([], now)).toEqual([]);
  });

  it('treats items in the current week but previous month as week', () => {
    // now = 2026-10-03 周六；本周一 = 09-28（跨月）
    const crossMonthNow = new Date(2026, 9, 3, 15, 0, 0);
    const groups = bucketInboxByDate([row(1, '2026-09-29 10:00:00')], crossMonthNow);

    expect(groups).toHaveLength(1);
    expect(groups[0].key).toBe('week');
  });
});
