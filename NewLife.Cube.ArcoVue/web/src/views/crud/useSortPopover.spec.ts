import { describe, expect, it } from 'vitest';
import { orderSortCandidates } from './useSortPopover';

describe('orderSortCandidates', () => {
  it('索引字段和主键排在前面，组内保持原序', () => {
    const ordered = orderSortCandidates([
      { name: 'Remark', displayName: '备注' },
      { name: 'Name', displayName: '名称', indexed: true },
      { name: 'Content', displayName: '内容' },
      { name: 'ID', displayName: '编号', primaryKey: true },
      { name: 'CreateTime', displayName: '时间', indexed: true },
    ]);
    expect(ordered.map((f) => f.name)).toEqual(['Name', 'ID', 'CreateTime', 'Remark', 'Content']);
  });

  it('没有索引信息时保持原序', () => {
    const ordered = orderSortCandidates([
      { name: 'A' },
      { name: 'B' },
    ]);
    expect(ordered.map((f) => f.name)).toEqual(['A', 'B']);
  });
});
