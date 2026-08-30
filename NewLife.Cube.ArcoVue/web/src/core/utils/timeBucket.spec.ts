import { describe, expect, it } from 'vitest';
import {
  TIME_BUCKET_LABELS,
  UNKNOWN_TIME_LABEL,
  isDateTimeBucketType,
  timeBucketOf,
  stripTimeBucketLabel,
  timeSortValue,
} from './timeBucket';

/** 固定“当前时间”：2026-08-30 12:00（周日） */
const now = new Date(2026, 7, 30, 12, 0, 0);

describe('timeBucketOf', () => {
  it('今天：同日任意时刻归入今天', () => {
    expect(timeBucketOf('2026-08-30T09:00:00', now)).toBe('0·今天');
  });

  it('最近3天：今天之前 3 天内', () => {
    expect(timeBucketOf('2026-08-29T09:00:00', now)).toBe('1·最近3天');
    expect(timeBucketOf('2026-08-27T23:59:59', now)).toBe('1·最近3天');
  });

  it('本周：本周内、最近3天之外', () => {
    expect(timeBucketOf('2026-08-25T09:00:00', now)).toBe('2·本周');
    expect(timeBucketOf('2026-08-24T00:00:00', now)).toBe('2·本周');
  });

  it('这个月早些时候：本月内、本周之外', () => {
    expect(timeBucketOf('2026-08-10T09:00:00', now)).toBe('3·这个月早些时候');
    expect(timeBucketOf('2026-08-01T00:00:00', now)).toBe('3·这个月早些时候');
  });

  it('上月：上一个自然月', () => {
    expect(timeBucketOf('2026-07-15T09:00:00', now)).toBe('4·上月');
    expect(timeBucketOf('2026-07-31T23:59:59', now)).toBe('4·上月');
  });

  it('今年早些时候：今年内、上月之前', () => {
    expect(timeBucketOf('2026-03-01T09:00:00', now)).toBe('5·今年早些时候');
    expect(timeBucketOf('2026-01-01T00:00:00', now)).toBe('5·今年早些时候');
  });

  it('很久以前：早于今年', () => {
    expect(timeBucketOf('2025-12-31T23:59:59', now)).toBe('6·很久以前');
  });

  it('无效 / 空值归入未明确时间桶', () => {
    expect(timeBucketOf('', now)).toBe('7·未明确时间');
    expect(timeBucketOf(null, now)).toBe('7·未明确时间');
    expect(timeBucketOf(undefined, now)).toBe('7·未明确时间');
    expect(timeBucketOf('not-a-date', now)).toBe('7·未明确时间');
    expect(UNKNOWN_TIME_LABEL).toBe('未明确时间');
  });

  it('分桶标签顺序为近到远', () => {
    expect(TIME_BUCKET_LABELS).toEqual([
      '今天',
      '最近3天',
      '本周',
      '这个月早些时候',
      '上月',
      '今年早些时候',
      '很久以前',
    ]);
  });
});

describe('isDateTimeBucketType', () => {
  it('日期时间类型可做分桶', () => {
    expect(isDateTimeBucketType('DateTime')).toBe(true);
    expect(isDateTimeBucketType('DateTimeOffset')).toBe(true);
    expect(isDateTimeBucketType('DateOnly')).toBe(true);
  });

  it('纯时间与其它类型不可分桶', () => {
    expect(isDateTimeBucketType('TimeOnly')).toBe(false);
    expect(isDateTimeBucketType('String')).toBe(false);
    expect(isDateTimeBucketType('Int32')).toBe(false);
    expect(isDateTimeBucketType(undefined)).toBe(false);
  });
});

describe('stripTimeBucketLabel', () => {
  it('去掉排序前缀返回纯标签', () => {
    expect(stripTimeBucketLabel('3·这个月早些时候')).toBe('这个月早些时候');
    expect(stripTimeBucketLabel('0·今天')).toBe('今天');
  });

  it('非字符串返回 undefined（未分组回落）', () => {
    expect(stripTimeBucketLabel(undefined)).toBeUndefined();
    expect(stripTimeBucketLabel(null)).toBeUndefined();
  });
});

describe('timeSortValue', () => {
  it('返回壁钟时间戳，近的更大（用于降序近到远）', () => {
    const v = timeSortValue('2026-08-30T09:00:00');
    const older = timeSortValue('2026-08-01T09:00:00');
    expect(v).toBeGreaterThan(older);
  });

  it('无效 / 空值返回 -Infinity（排最后）', () => {
    expect(timeSortValue('')).toBe(-Infinity);
    expect(timeSortValue(null)).toBe(-Infinity);
    expect(timeSortValue(undefined)).toBe(-Infinity);
    expect(timeSortValue('not-a-date')).toBe(-Infinity);
  });
});
