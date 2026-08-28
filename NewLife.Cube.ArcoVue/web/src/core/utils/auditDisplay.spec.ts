import { describe, expect, it } from 'vitest';
import { filterDetailAuditFields } from './auditDisplay';
import type { FieldMeta } from '../types/field';

const f = (name: string): FieldMeta => ({ name, typeName: 'String' });

describe('filterDetailAuditFields', () => {
  it('有创建者/更新者时隐藏创建用户/更新用户', () => {
    const out = filterDetailAuditFields([
      f('Name'),
      f('CreateUser'),
      f('CreateUserID'),
      f('UpdateUser'),
      f('UpdateUserID'),
      f('CreateTime'),
    ]);
    expect(out.map((x) => x.name)).toEqual([
      'Name',
      'CreateUser',
      'UpdateUser',
      'CreateTime',
    ]);
  });

  it('没有名称列时保留 ID，避免审计信息空白', () => {
    const out = filterDetailAuditFields([f('CreateUserID'), f('UpdateUserID')]);
    expect(out.map((x) => x.name)).toEqual(['CreateUserID', 'UpdateUserID']);
  });

  it('仅有创建者时只藏创建用户', () => {
    const out = filterDetailAuditFields([
      f('CreateUser'),
      f('CreateUserID'),
      f('UpdateUserID'),
    ]);
    expect(out.map((x) => x.name)).toEqual(['CreateUser', 'UpdateUserID']);
  });

  it('空数组安全', () => {
    expect(filterDetailAuditFields([])).toEqual([]);
  });
});
