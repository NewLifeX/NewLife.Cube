import { describe, expect, it } from 'vitest';
import {
  formatDate,
  formatDateTime,
  formatTime,
  fromPickerValue,
  inferDateKind,
  parseWallClock,
  toPickerValue,
} from './datetime';

describe('parseWallClock', () => {
  it('parses naive ISO string ignoring no timezone', () => {
    expect(parseWallClock('2026-08-02T03:04:05')).toEqual({
      y: 2026,
      m: 8,
      d: 2,
      h: 3,
      mi: 4,
      s: 5,
    });
  });

  it('parses UTC-designated string as wall-clock (no shift)', () => {
    // "2026-08-02T03:04:05Z" must keep 03:04:05, not convert to local
    expect(parseWallClock('2026-08-02T03:04:05Z')).toEqual({
      y: 2026,
      m: 8,
      d: 2,
      h: 3,
      mi: 4,
      s: 5,
    });
  });

  it('parses offset-designated string as wall-clock', () => {
    expect(parseWallClock('2026-08-02T03:04:05+08:00')).toEqual({
      y: 2026,
      m: 8,
      d: 2,
      h: 3,
      mi: 4,
      s: 5,
    });
  });

  it('parses date-only string', () => {
    expect(parseWallClock('2026-01-02')).toEqual({
      y: 2026,
      m: 1,
      d: 2,
      h: 0,
      mi: 0,
      s: 0,
    });
  });

  it('returns null for empty/invalid input', () => {
    expect(parseWallClock(null)).toBeNull();
    expect(parseWallClock(undefined)).toBeNull();
    expect(parseWallClock('')).toBeNull();
    expect(parseWallClock('not-a-date')).toBeNull();
  });
});

describe('formatDateTime', () => {
  it('formats naive ISO string as YYYY-MM-DD HH:mm:ss', () => {
    expect(formatDateTime('2026-08-02T03:04:05')).toBe('2026-08-02 03:04:05');
  });

  it('does not shift UTC-designated time to local', () => {
    expect(formatDateTime('2026-08-02T03:04:05Z')).toBe('2026-08-02 03:04:05');
  });

  it('formats Date instance with zero-padded fields', () => {
    expect(formatDateTime(new Date(2026, 0, 2, 3, 4, 5))).toBe('2026-01-02 03:04:05');
  });

  it('returns - for empty/invalid input', () => {
    expect(formatDateTime(null)).toBe('-');
    expect(formatDateTime('')).toBe('-');
    expect(formatDateTime('not-a-date')).toBe('-');
  });
});

describe('formatDate / formatTime', () => {
  it('formats date only', () => {
    expect(formatDate('2026-08-02T03:04:05')).toBe('2026-08-02');
  });
  it('formats time only', () => {
    expect(formatTime('2026-08-02T03:04:05')).toBe('03:04:05');
  });
  it('returns - for invalid', () => {
    expect(formatDate('')).toBe('-');
    expect(formatTime(null)).toBe('-');
  });
});

describe('inferDateKind', () => {
  it('uses itemType hint first', () => {
    expect(inferDateKind({ typeName: 'DateTime', itemType: 'date' })).toBe('date');
    expect(inferDateKind({ typeName: 'DateTime', itemType: 'time' })).toBe('time');
    expect(inferDateKind({ typeName: 'DateTime', itemType: 'datetime' })).toBe('datetime');
  });
  it('TimeSpan → time', () => {
    expect(inferDateKind({ typeName: 'TimeSpan' })).toBe('time');
  });
  it('DateTime defaults to datetime', () => {
    expect(inferDateKind({ typeName: 'DateTime' })).toBe('datetime');
  });
});

describe('toPickerValue / fromPickerValue', () => {
  it('strips timezone for picker (no shift)', () => {
    expect(toPickerValue('2026-08-02T03:04:05Z', 'datetime')).toBe('2026-08-02 03:04:05');
    expect(toPickerValue('2026-08-02', 'date')).toBe('2026-08-02');
    expect(toPickerValue('2026-08-02T03:04:05', 'time')).toBe('03:04:05');
  });
  it('returns undefined for empty', () => {
    expect(toPickerValue('', 'datetime')).toBeUndefined();
    expect(toPickerValue(null, 'date')).toBeUndefined();
  });
  it('serializes picker output back to naive string', () => {
    expect(fromPickerValue('2026-08-02T03:04:05', 'datetime')).toBe('2026-08-02 03:04:05');
    expect(fromPickerValue('2026-08-02', 'date')).toBe('2026-08-02');
    expect(fromPickerValue('03:04:05', 'time')).toBe('03:04:05');
  });
});
