/**
 * 密码规则解析单元测试（对齐 Vue usePasswordRules 测试）
 */
import { describe, expect, it } from 'vitest';
import { parsePasswordRules, PASSWORD_FALLBACK_MIN_LENGTH } from '@/utils/passwordRules';

describe('parsePasswordRules', () => {
  it('空串 / * 回退兜底最小长度', () => {
    const r1 = parsePasswordRules('');
    const r2 = parsePasswordRules('*');
    expect(r1[0].label).toContain(String(PASSWORD_FALLBACK_MIN_LENGTH));
    expect(r2[0].label).toContain(String(PASSWORD_FALLBACK_MIN_LENGTH));
  });

  it('解析长度 + 数字 + 大小写 + 特殊字符约束', () => {
    const strength = '^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^0-9a-zA-Z]).{8,20}$';
    const rules = parsePasswordRules(strength);
    const labels = rules.map((r) => r.label);
    expect(labels.some((l) => l.includes('数字'))).toBe(true);
    expect(labels.some((l) => l.includes('小写'))).toBe(true);
    expect(labels.some((l) => l.includes('大写'))).toBe(true);
    expect(labels.some((l) => l.includes('特殊字符'))).toBe(true);
    expect(labels.some((l) => l.includes('8-20 位'))).toBe(true);
  });

  it('规则校验函数生效', () => {
    const rules = parsePasswordRules('^(?=.*\\d)(?=.*[a-z]).{6,}$');
    const password = 'abc123';
    for (const r of rules) {
      expect(r.test(password)).toBe(true);
    }
    const weak = 'abc';
    const failed = rules.filter((r) => !r.test(weak));
    expect(failed.length).toBeGreaterThan(0);
  });

  it('无法解析的规则 → 整条正则校验', () => {
    const rules = parsePasswordRules('[a-z]+');
    expect(rules.length).toBe(1);
    expect(rules[0].test('hello')).toBe(true);
    expect(rules[0].test('123')).toBe(false);
  });

  it('无效正则回退兜底', () => {
    const rules = parsePasswordRules('([unclosed');
    expect(rules[0].label).toContain(String(PASSWORD_FALLBACK_MIN_LENGTH));
  });
});
