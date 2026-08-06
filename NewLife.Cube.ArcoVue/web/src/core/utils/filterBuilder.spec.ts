import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import {
  resolveFieldFilterKind,
  FILTER_OPS_BY_KIND,
  FILTER_OP_LABELS,
  opNeedsValue,
  isPersonField,
  newFilterDraftRow,
  resetCondForField,
  draftToFilter,
  filterToDraftRows,
  type FilterDraftRow,
} from './filterBuilder';
import type { ViewFilterCondition } from './viewProfile';

function f(partial: Partial<FieldMeta> & { name: string }): FieldMeta {
  return { typeName: 'String', ...partial };
}

describe('filterBuilder 字段类别与操作符矩阵 (OSC-0015)', () => {
  it('resolveFieldFilterKind 按类型归类字段', () => {
    expect(resolveFieldFilterKind(f({ name: 'Type', typeName: 'Enum' }))).toBe('enum');
    expect(resolveFieldFilterKind(f({ name: 'Enable', typeName: 'Boolean' }))).toBe('enum');
    expect(
      resolveFieldFilterKind(f({ name: 'Status', typeName: 'Int32', dataSource: { '1': 'x' } })),
    ).toBe('enum');
    expect(resolveFieldFilterKind(f({ name: 'DeptId', typeName: 'Int32', lovCode: 'List.Dept' }))).toBe('enum');
    expect(resolveFieldFilterKind(f({ name: 'Age', typeName: 'Int32' }))).toBe('number');
    expect(resolveFieldFilterKind(f({ name: 'Salary', typeName: 'Double' }))).toBe('number');
    expect(resolveFieldFilterKind(f({ name: 'CreateTime', typeName: 'DateTime' }))).toBe('datetime');
    expect(resolveFieldFilterKind(f({ name: 'Name', typeName: 'String' }))).toBe('string');
  });

  it('人员字段识别（创建者/更新者/创建人员/更新人员）', () => {
    expect(resolveFieldFilterKind(f({ name: 'CreateUserID', typeName: 'Int32' }))).toBe('person');
    expect(resolveFieldFilterKind(f({ name: 'UpdateUserID', typeName: 'Int32' }))).toBe('person');
    expect(resolveFieldFilterKind(f({ name: 'Creator', typeName: 'Int32' }))).toBe('person');
    expect(resolveFieldFilterKind(f({ name: 'CreatorName', typeName: 'String' }))).toBe('person');
    expect(isPersonField(f({ name: 'CreateUserID' }))).toBe(true);
    expect(isPersonField(f({ name: 'Name' }))).toBe(false);
  });

  it('各类别操作符矩阵符合需求', () => {
    expect(FILTER_OPS_BY_KIND.enum).toEqual(['eq', 'neq', 'isNull', 'notNull']);
    expect(FILTER_OPS_BY_KIND.string).toEqual([
      'eq',
      'neq',
      'contains',
      'notContains',
      'isNull',
      'notNull',
    ]);
    expect(FILTER_OPS_BY_KIND.person).toEqual(['eq', 'neq']);
    expect(FILTER_OPS_BY_KIND.number).toEqual([
      'eq',
      'neq',
      'gt',
      'gte',
      'lt',
      'lte',
      'isNull',
      'notNull',
    ]);
    expect(FILTER_OPS_BY_KIND.datetime).toEqual(['eq', 'after', 'before', 'isNull', 'notNull']);
  });

  it('数字类别不含范围 between', () => {
    expect(FILTER_OPS_BY_KIND.number).not.toContain('between');
  });

  it('opNeedsValue：为空/不为空不需要值控件', () => {
    expect(opNeedsValue('eq')).toBe(true);
    expect(opNeedsValue('contains')).toBe(true);
    expect(opNeedsValue('isNull')).toBe(false);
    expect(opNeedsValue('notNull')).toBe(false);
  });

  it('FILTER_OP_LABELS 覆盖全部操作符', () => {
    for (const op of [
      'eq',
      'neq',
      'contains',
      'notContains',
      'isNull',
      'notNull',
      'gt',
      'gte',
      'lt',
      'lte',
      'after',
      'before',
    ] as const) {
      expect(FILTER_OP_LABELS[op]).toBeTruthy();
    }
  });
});

describe('filterBuilder 草稿逻辑 (OSC-0015)', () => {
  it('newFilterDraftRow 生成空条件行', () => {
    const row = newFilterDraftRow();
    expect(row.cond).toEqual({ field: '', op: 'eq', value: undefined });
  });

  it('resetCondForField 重置 op 为 eq 并清值', () => {
    const cond: ViewFilterCondition = { field: 'Age', op: 'gt', value: 1, value2: 5 };
    resetCondForField(cond, 'number');
    expect(cond).toEqual({ field: 'Age', op: 'eq', value: undefined, value2: undefined });
  });

  it('draftToFilter 丢弃空条件/非法 op 并保留 isNull 无值条件', () => {
    const rows: FilterDraftRow[] = [
      { cond: { field: 'Name', op: 'eq', value: 'a' } },
      { cond: { field: '', op: 'eq', value: undefined } },
      { cond: { field: 'Enable', op: 'isNull' } },
      { cond: { field: 'Age', op: 'gt', value: 18 } },
      { cond: { field: 'Name', op: 'badOp' as never, value: 'x' } },
    ];
    const f = draftToFilter('all', rows);
    expect(f.conditions).toHaveLength(3);
    expect(f.conditions[0]).toEqual({ field: 'Name', op: 'eq', value: 'a' });
    expect(f.conditions[1]).toEqual({ field: 'Enable', op: 'isNull' });
    expect(f.conditions[2]).toEqual({ field: 'Age', op: 'gt', value: 18 });
  });

  it('filterToDraftRows 展开方案为草稿行', () => {
    const rows = filterToDraftRows({
      logic: 'all',
      conditions: [
        { field: 'Name', op: 'contains', value: 'a' },
        { field: 'Age', op: 'gte', value: 18 },
      ],
    });
    expect(rows).toHaveLength(2);
    expect(rows[0].cond).toEqual({ field: 'Name', op: 'contains', value: 'a' });
    expect(rows[1].cond).toEqual({ field: 'Age', op: 'gte', value: 18 });
  });
});
