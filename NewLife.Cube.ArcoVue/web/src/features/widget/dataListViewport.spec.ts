import { describe, expect, it } from 'vitest';
import {
  DATA_LIST_DEFAULT_VISIBLE,
  DATA_LIST_ROW_PX,
  dataListScrollY,
  normalizeDataListLimit,
  rotateDataListWindow,
  visibleDataListRows,
} from './dataListViewport';

describe('visibleDataListRows', () => {
  it('defaults to at most 7 rows for normal layout.h', () => {
    expect(visibleDataListRows(undefined)).toBe(DATA_LIST_DEFAULT_VISIBLE);
    expect(visibleDataListRows(4)).toBe(DATA_LIST_DEFAULT_VISIBLE);
  });

  it('uses fewer rows when the widget is shorter', () => {
    expect(visibleDataListRows(3)).toBeLessThan(DATA_LIST_DEFAULT_VISIBLE);
    expect(visibleDataListRows(3)).toBeGreaterThanOrEqual(1);
  });
});

describe('dataListScrollY', () => {
  it('is visibleRows × row height', () => {
    expect(dataListScrollY(5)).toBe(5 * DATA_LIST_ROW_PX);
    expect(dataListScrollY(8)).toBe(8 * DATA_LIST_ROW_PX);
  });
});

describe('normalizeDataListLimit', () => {
  it('defaults and clamps to 300; -1 means all', () => {
    expect(normalizeDataListLimit(undefined)).toBe(30);
    expect(normalizeDataListLimit(20)).toBe(20);
    expect(normalizeDataListLimit(999)).toBe(300);
    expect(normalizeDataListLimit(0)).toBe(30);
    expect(normalizeDataListLimit(-1)).toBe(-1);
  });
});

describe('rotateDataListWindow', () => {
  it('returns all rows when count fits the window', () => {
    expect(rotateDataListWindow([1, 2, 3], 0, 5)).toEqual([1, 2, 3]);
  });

  it('rotates a circular window of fixed size', () => {
    const items = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    expect(rotateDataListWindow(items, 0, 3)).toEqual([1, 2, 3]);
    expect(rotateDataListWindow(items, 1, 3)).toEqual([2, 3, 4]);
    expect(rotateDataListWindow(items, 8, 3)).toEqual([9, 10, 1]);
    expect(rotateDataListWindow(items, 9, 3)).toEqual([10, 1, 2]);
  });

  it('handles empty input', () => {
    expect(rotateDataListWindow([], 0, 5)).toEqual([]);
  });
});
