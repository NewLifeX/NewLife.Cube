import { describe, expect, it } from 'vitest';
import { formatDateTime } from './datetime';

describe('formatDateTime', () => {
  it('formats ISO string as local YYYY-MM-DD HH:mm:ss', () => {
    expect(formatDateTime('2026-08-02T03:04:05')).toBe('2026-08-02 03:04:05');
  });

  it('formats Date instance with zero-padded fields', () => {
    expect(formatDateTime(new Date(2026, 0, 2, 3, 4, 5))).toBe('2026-01-02 03:04:05');
  });

  it('returns - for empty/invalid input', () => {
    expect(formatDateTime(null)).toBe('-');
    expect(formatDateTime(undefined)).toBe('-');
    expect(formatDateTime('')).toBe('-');
    expect(formatDateTime('not-a-date')).toBe('-');
  });
});
