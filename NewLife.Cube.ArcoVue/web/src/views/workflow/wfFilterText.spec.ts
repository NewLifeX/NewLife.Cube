import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import { filterValueLabelResolver, viewFilterSummary } from './wfFilterText';

const fields: FieldMeta[] = [
  {
    name: 'Type',
    displayName: '类型',
    typeName: 'DepartmentTypes',
    dataSource: { '1': '公司', '2': '部门', '3': '小组' },
  },
  { name: 'Name', displayName: '名称', typeName: 'String' },
];

describe('viewFilterSummary 人话摘要', () => {
  it('字段与值都取友好名（枚举 2 → 部门）', () => {
    const raw = { logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '2' }] };
    const s = viewFilterSummary(
      raw,
      (n) => fields.find((f) => f.name === n)?.displayName || n,
      filterValueLabelResolver(fields),
    );
    expect(s).toBe('类型 等于 部门');
  });

  it('数组值逐个映射；未命中回退原值', () => {
    const raw = { logic: 'any', conditions: [{ field: 'Type', op: 'eq', value: ['2', '9'] }] };
    const s = viewFilterSummary(raw, undefined, filterValueLabelResolver(fields));
    expect(s).toBe('Type 等于 部门/9');
  });

  it('不传 valueLabel 保持原样（兼容既有调用）', () => {
    const raw = { logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '2' }] };
    expect(viewFilterSummary(raw)).toBe('Type 等于 2');
  });
});

describe('filterValueLabelResolver 枚举值友好名', () => {
  it('命中 dataSource 返回标签（字符串/数字值均可）', () => {
    expect(filterValueLabelResolver(fields)('Type', '2')).toBe('部门');
    expect(filterValueLabelResolver(fields)('Type', 2)).toBe('部门');
  });

  it('未命中 / 无 dataSource / 空值返回 null', () => {
    expect(filterValueLabelResolver(fields)('Type', '9')).toBeNull();
    expect(filterValueLabelResolver(fields)('Other', '2')).toBeNull();
    expect(filterValueLabelResolver(fields)('Name', 'x')).toBeNull();
    expect(filterValueLabelResolver(fields)('Type', '')).toBeNull();
    expect(filterValueLabelResolver(fields)('Type', null)).toBeNull();
  });
});
