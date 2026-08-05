import { describe, expect, it } from 'vitest';
import {
  defaultBadgeColumnWidth,
  isBadgeField,
  isEnableField,
  resolveCellBadge,
  resolveCellLabel,
} from './fieldBadge';
import type { FieldMeta } from '@/core/types/field';

function f(partial: Partial<FieldMeta>): FieldMeta {
  return {
    name: 'x',
    typeName: 'String',
    ...partial,
  };
}

describe('fieldBadge', () => {
  it('detects boolean/select as badge fields', () => {
    expect(isBadgeField(f({ typeName: 'Boolean' }))).toBe(true);
    expect(
      isBadgeField(f({ typeName: 'Int32', dataSource: { '1': '男', '0': '女' } })),
    ).toBe(true);
    expect(isBadgeField(f({ typeName: 'String' }))).toBe(false);
  });

  it('resolves labels from dataSource without network', () => {
    const field = f({ typeName: 'Boolean', dataSource: { true: '是', false: '否', '1': '是', '0': '否' } });
    expect(resolveCellLabel(field, true)).toBe('是');
    expect(resolveCellLabel(field, 0)).toBe('否');
  });

  it('builds colored badge for enable-like boolean', () => {
    const field = f({ name: 'Enable', typeName: 'Boolean' });
    const b = resolveCellBadge(field, true);
    expect(b?.label).toBe('是');
    expect(b?.tone).toBe('success');
    expect(defaultBadgeColumnWidth(field)).toBeGreaterThanOrEqual(56);
  });

  it('detects Enable field for clickable toggle', () => {
    expect(isEnableField(f({ name: 'Enable', typeName: 'Boolean' }))).toBe(true);
    expect(isEnableField(f({ name: 'enable', typeName: 'Boolean' }))).toBe(true);
    expect(isEnableField(f({ name: 'Status', typeName: 'Int32' }))).toBe(false);
    expect(isEnableField(f({ name: 'Enabled', typeName: 'Boolean' }))).toBe(false);
  });
});
