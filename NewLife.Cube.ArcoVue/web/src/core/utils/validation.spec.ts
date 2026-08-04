import { describe, expect, it } from 'vitest';
import { fieldFormatRules } from './validation';
import type { FieldMeta } from '../types/field';

function base(over: Partial<FieldMeta> = {}): FieldMeta {
  return { name: 'x', typeName: 'String', ...over };
}

function runValidator(field: FieldMeta, value: unknown): string | undefined {
  const rules = fieldFormatRules(field);
  if (!rules.length) return undefined;
  let captured: string | undefined;
  rules[0].validator!(value, (err) => {
    captured = err;
  });
  return captured;
}

describe('fieldFormatRules', () => {
  it('returns no rule for unrelated field', () => {
    expect(fieldFormatRules(base({ name: 'Name' }))).toHaveLength(0);
  });

  it('detects mobile by itemType', () => {
    const f = base({ name: 'Mobile', itemType: 'mobile' });
    expect(runValidator(f, '13812345678')).toBeUndefined();
    expect(runValidator(f, '1234')).toBe('手机号格式不正确');
  });

  it('detects email by name (Mail)', () => {
    const f = base({ name: 'Mail' });
    expect(runValidator(f, 'a@b.com')).toBeUndefined();
    expect(runValidator(f, 'not-mail')).toBe('邮箱格式不正确');
  });

  it('detects email by name (Email)', () => {
    const f = base({ name: 'Email' });
    expect(runValidator(f, 'user@x.io')).toBeUndefined();
    expect(runValidator(f, 'user@')).toBe('邮箱格式不正确');
  });

  it('detects phone by name (Phone1 stem)', () => {
    const f = base({ name: 'Phone1' });
    expect(runValidator(f, '010-12345678')).toBeUndefined();
    expect(runValidator(f, '12345')).toBe('电话号码格式不正确');
  });

  it('detects url by name (Website)', () => {
    const f = base({ name: 'Website' });
    expect(runValidator(f, 'https://x.com')).toBeUndefined();
    expect(runValidator(f, 'not a url')).toBe('网址格式不正确');
  });

  it('does not validate empty value (required handled elsewhere)', () => {
    expect(runValidator(base({ name: 'Mail' }), '')).toBeUndefined();
    expect(runValidator(base({ name: 'Mail' }), null)).toBeUndefined();
  });
});
