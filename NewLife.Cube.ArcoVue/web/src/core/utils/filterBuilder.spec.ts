import { describe, expect, it } from 'vitest';
import {
  isRangeControl,
  newFilterDraftRow,
  buildCondForm,
  applyCondKey,
  resetCondForField,
  draftToFilter,
  filterToDraftRows,
  type FilterDraftRow,
} from './filterBuilder';
import type { ViewFilterCondition } from './viewProfile';

describe('filterBuilder 筛选构建器草稿逻辑 (OSC-0015)', () => {
  it('isRangeControl 识别范围型搜索控件', () => {
    expect(isRangeControl('numberRange')).toBe(true);
    expect(isRangeControl('dateRange')).toBe(true);
    expect(isRangeControl('datetimeRange')).toBe(true);
    expect(isRangeControl('timeRange')).toBe(true);
    expect(isRangeControl('text')).toBe(false);
    expect(isRangeControl('select')).toBe(false);
  });

  it('newFilterDraftRow 生成空条件行', () => {
    const row = newFilterDraftRow();
    expect(row.cond).toEqual({ field: '', op: 'eq', value: undefined });
    expect(row.form).toEqual({});
  });

  it('buildCondForm 构建值绑定 form（含 _min/_max）', () => {
    const form = buildCondForm({ field: 'Age', op: 'between', value: 1, value2: 5 });
    expect(form).toEqual({ Age: 1, Age_min: 1, Age_max: 5 });
    // 无字段：空 form
    expect(buildCondForm({ field: '', op: 'eq' })).toEqual({});
  });

  it('applyCondKey 按 _min/_max 同步条件值', () => {
    const cond: ViewFilterCondition = { field: 'Age', op: 'between', value: undefined, value2: undefined };
    applyCondKey(cond, 'Age_min', 3);
    applyCondKey(cond, 'Age_max', 9);
    expect(cond.value).toBe(3);
    expect(cond.value2).toBe(9);
    // 无关键不修改
    applyCondKey(cond, 'Age', 'x');
    expect(cond.value).toBe(3);
  });

  it('resetCondForField 非范围字段重置为 eq 并清值', () => {
    const cond: ViewFilterCondition = { field: 'Age', op: 'between', value: 1, value2: 5 };
    resetCondForField(cond, false);
    expect(cond).toEqual({ field: 'Age', op: 'eq', value: undefined, value2: undefined });
  });

  it('resetCondForField 范围字段保留 op（仅清值）', () => {
    const cond: ViewFilterCondition = { field: 'Age', op: 'between', value: 1, value2: 5 };
    resetCondForField(cond, true);
    expect(cond.op).toBe('between');
    expect(cond.value).toBeUndefined();
    expect(cond.value2).toBeUndefined();
  });

  it('draftToFilter 丢弃空条件并归一化逻辑', () => {
    const rows: FilterDraftRow[] = [
      { cond: { field: 'Name', op: 'eq', value: 'a' }, form: {} },
      { cond: { field: '', op: 'eq', value: undefined }, form: {} },
      { cond: { field: 'Age', op: 'between', value: 1, value2: 5 }, form: {} },
    ];
    const f = draftToFilter('any', rows);
    expect(f.logic).toBe('any');
    expect(f.conditions).toHaveLength(2);
    expect(f.conditions[0]).toEqual({ field: 'Name', op: 'eq', value: 'a' });
    expect(f.conditions[1]).toEqual({ field: 'Age', op: 'between', value: 1, value2: 5 });
    // 非法 op（如 contains）被丢弃
    const bad = draftToFilter('all', [
      { cond: { field: 'Name', op: 'contains', value: 'a' } as never, form: {} },
    ]);
    expect(bad.conditions).toEqual([]);
  });

  it('filterToDraftRows 展开方案为草稿行并携带 form', () => {
    const rows = filterToDraftRows({
      logic: 'all',
      conditions: [
        { field: 'Name', op: 'eq', value: 'a' },
        { field: 'Age', op: 'between', value: 1, value2: 5 },
      ],
    });
    expect(rows).toHaveLength(2);
    expect(rows[0].cond).toEqual({ field: 'Name', op: 'eq', value: 'a' });
    expect(rows[0].form).toEqual({ Name: 'a', Name_min: 'a', Name_max: undefined });
    expect(rows[1].form).toEqual({ Age: 1, Age_min: 1, Age_max: 5 });
  });
});
