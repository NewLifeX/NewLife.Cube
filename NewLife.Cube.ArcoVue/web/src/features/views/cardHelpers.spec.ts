import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import { buildCardBodyFields, isCardBodyFieldFullRow } from './cardHelpers';

function f(partial: Partial<FieldMeta> & { name: string }): FieldMeta {
  return { typeName: 'String', ...partial };
}

function cols(names: string[]): ColumnPref[] {
  return names.map((key) => ({ key, visible: true }));
}

describe('isCardBodyFieldFullRow', () => {
  it('multi-line/rich-text controls always full row', () => {
    expect(isCardBodyFieldFullRow(f({ name: 'Remark', itemType: 'textarea' }), '短')).toBe(true);
    expect(isCardBodyFieldFullRow(f({ name: 'Html', itemType: 'richtext' }), '')).toBe(true);
    expect(isCardBodyFieldFullRow(f({ name: 'Html', itemType: 'HTML' }), 'x')).toBe(true);
    expect(isCardBodyFieldFullRow(f({ name: 'Multi', itemType: 'multiline' }), 'x')).toBe(true);
  });

  it('falls back to unicode code-point length >= 33', () => {
    expect(isCardBodyFieldFullRow(undefined, 'a'.repeat(32))).toBe(false);
    expect(isCardBodyFieldFullRow(undefined, 'a'.repeat(33))).toBe(true);
    // emoji 按码位计：4 emoji + 28 字符 = 32 码位 → 非整行；+29 = 33 → 整行
    expect(isCardBodyFieldFullRow(undefined, '😀'.repeat(4) + 'a'.repeat(28))).toBe(false);
    expect(isCardBodyFieldFullRow(undefined, '😀'.repeat(4) + 'a'.repeat(29))).toBe(true);
    expect(isCardBodyFieldFullRow(undefined, '-')).toBe(false);
  });

  it('short text on non-multiline field is not full row', () => {
    expect(isCardBodyFieldFullRow(f({ name: 'Name', itemType: 'text' }), 'short')).toBe(false);
  });

  it('marks 备注/说明/评论 semantic fields fullRow regardless of length', () => {
    expect(
      isCardBodyFieldFullRow(f({ name: 'Remark', displayName: '备注', itemType: 'text' }), '短'),
    ).toBe(true);
    expect(
      isCardBodyFieldFullRow(f({ name: 'Note', displayName: '说明', itemType: 'text' }), 'x'),
    ).toBe(true);
    expect(
      isCardBodyFieldFullRow(f({ name: 'Comment', displayName: '评论', itemType: 'text' }), ''),
    ).toBe(true);
  });
});

describe('buildCardBodyFields', () => {
  const fields = [
    f({ name: 'Name', typeName: 'String' }),
    f({ name: 'Status', typeName: 'Enum', dataSource: { a: 'A' } }),
    f({ name: 'Remark', typeName: 'String', itemType: 'textarea' }),
    f({ name: 'Code', typeName: 'String' }),
    f({ name: 'N1', typeName: 'String' }),
    f({ name: 'N2', typeName: 'String' }),
    f({ name: 'N3', typeName: 'String' }),
    f({ name: 'N4', typeName: 'String' }),
    f({ name: 'N5', typeName: 'String' }),
    f({ name: 'N6', typeName: 'String' }),
  ];
  const columns = cols(fields.map((x) => x.name));
  const record: Record<string, unknown> = {
    Name: '标题',
    Status: 'a',
    Remark: '多行',
    Code: 'C',
    N1: '1',
    N2: '2',
    N3: '3',
    N4: '4',
    N5: '5',
    N6: '6',
  };

  it('excludes title/image/kanban group keys', () => {
    const out = buildCardBodyFields(record, columns, fields, ['Name', 'Status', 'Photo']);
    expect(out.some((x) => x.key === 'Name')).toBe(false);
    expect(out.some((x) => x.key === 'Status')).toBe(false);
  });

  it('caps body fields at 8', () => {
    const out = buildCardBodyFields(record, columns, fields, ['Name']);
    expect(out.length).toBeLessThanOrEqual(8);
    expect(out).toHaveLength(8);
  });

  it('marks multiline field fullRow', () => {
    const out = buildCardBodyFields(record, columns, fields, ['Name']);
    const remark = out.find((x) => x.key === 'Remark');
    expect(remark?.value).toBe('多行');
    expect(remark?.fullRow).toBe(true);
  });

  it('resolves dataSource label and marks long value fullRow', () => {
    const long = '长'.repeat(33);
    const out = buildCardBodyFields({ ...record, Code: long }, columns, fields, ['Name']);
    const status = out.find((x) => x.key === 'Status');
    expect(status?.value).toBe('A');
    expect(status?.fullRow).toBe(false);
    const code = out.find((x) => x.key === 'Code');
    expect(code?.value).toBe(long);
    expect(code?.fullRow).toBe(true);
  });
});
