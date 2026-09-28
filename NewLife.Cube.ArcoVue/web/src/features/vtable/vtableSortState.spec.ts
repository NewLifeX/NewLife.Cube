import { describe, expect, it } from 'vitest';
import { toVTableSortOption, toVTableSortState, vtableSortKey } from './vtableSortState';

describe('toVTableSortState', () => {
  it('空排序不画表头状态', () => {
    expect(toVTableSortState(null)).toBeNull();
    expect(toVTableSortState([])).toBeNull();
    expect(toVTableSortState(undefined)).toBeNull();
  });

  it('单列与多列都保留方向，多列不丢掉第二列', () => {
    expect(toVTableSortState({ field: 'Name', desc: true })).toEqual([
      { field: 'Name', order: 'desc' },
    ]);
    expect(
      toVTableSortState([
        { field: 'Name', desc: true },
        { field: 'Type', desc: false },
      ]),
    ).toEqual([
      { field: 'Name', order: 'desc' },
      { field: 'Type', order: 'asc' },
    ]);
  });

  it('交给 VTable 的空排序是空数组，避免 multipleSort 把 null 包成 [null]', () => {
    expect(toVTableSortOption(null)).toEqual([]);
    expect(toVTableSortOption([])).toEqual([]);
    expect(toVTableSortOption({ field: 'Name', desc: false })).toEqual([
      { field: 'Name', order: 'asc' },
    ]);
    expect(vtableSortKey(null)).toBe('');
    expect(vtableSortKey([{ field: 'Name', desc: true }])).toBe('Name:desc');
  });

  it('丢掉无字段项并截断到 3 列', () => {
    expect(
      toVTableSortState([
        { field: '', desc: false },
        { field: 'A', desc: false },
        { field: 'B', desc: true },
        { field: 'C', desc: false },
        { field: 'D', desc: true },
      ]),
    ).toEqual([
      { field: 'A', order: 'asc' },
      { field: 'B', order: 'desc' },
      { field: 'C', order: 'asc' },
    ]);
  });
});
