import { describe, expect, it } from 'vitest';
import { formatFieldValue } from './fieldFormat';
import type { FieldMeta } from '../types/field';

function base(over: Partial<FieldMeta> = {}): FieldMeta {
  return { name: 'x', typeName: 'String', ...over };
}

describe('formatFieldValue', () => {
  it('returns - for empty raw', () => {
    expect(formatFieldValue(base({ name: 'Name' }), { Name: '' })).toBe('-');
    expect(formatFieldValue(base({ name: 'Name' }), { Name: null })).toBe('-');
  });

  it('formats DateTime as wall-clock (no Z shift)', () => {
    const f = base({ name: 'CreateTime', typeName: 'DateTime' });
    expect(formatFieldValue(f, { CreateTime: '2026-08-02T03:04:05Z' })).toBe(
      '2026-08-02 03:04:05',
    );
  });

  it('formats date-only itemType as YYYY-MM-DD', () => {
    const f = base({ name: 'Birthday', typeName: 'DateTime', itemType: 'date' });
    expect(formatFieldValue(f, { Birthday: '2026-08-02T03:04:05' })).toBe('2026-08-02');
  });

  it('formats time itemType as HH:mm:ss', () => {
    const f = base({ name: 'WorkTime', typeName: 'DateTime', itemType: 'time' });
    expect(formatFieldValue(f, { WorkTime: '2026-08-02T03:04:05' })).toBe('03:04:05');
  });

  it('resolves dataSource label', () => {
    const f = base({ name: 'Enable', typeName: 'Boolean', dataSource: { true: '启用', false: '禁用' } });
    expect(formatFieldValue(f, { Enable: true })).toBe('启用');
    expect(formatFieldValue(f, { Enable: false })).toBe('禁用');
  });

  it('falls back to String(raw) for plain text', () => {
    const f = base({ name: 'Name' });
    expect(formatFieldValue(f, { Name: '张三' })).toBe('张三');
  });

  it('uses labelCache for LOV fields', () => {
    const f = base({ name: 'RoleId', typeName: 'Int32', lovCode: 'Role' });
    const opts = { labelCache: { Role: { '1': '管理员' } } };
    expect(formatFieldValue(f, { RoleId: 1 }, opts)).toBe('管理员');
  });
});
