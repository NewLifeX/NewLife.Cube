/**
 * 找回密码校验逻辑单测
 *
 * 只测纯函数（渠道推断 + 两步校验），不渲染组件，避免依赖 i18n 与 Element Plus。
 */
import { describe, expect, it } from 'vitest';
import {
  ResetPasswordMinLength,
  guessChannel,
  validateResetAccount,
  validateResetConfirm,
  type ResetPasswordForm,
} from '../utils/forgotPassword';

/** 构造一组合法表单，按需覆盖字段 */
function form(overrides: Partial<ResetPasswordForm> = {}): ResetPasswordForm {
  return {
    username: 'dev@example.com',
    channel: 'Mail',
    code: '123456',
    newPassword: 'NewPass123!',
    confirmPassword: 'NewPass123!',
    ...overrides,
  };
}

describe('guessChannel', () => {
  it('含 @ 判定为邮箱', () => {
    expect(guessChannel('dev@example.com')).toBe('Mail');
  });

  it('纯数字（手机号）判定为短信', () => {
    expect(guessChannel(' 13800000000 ')).toBe('Sms');
  });
});

describe('validateResetAccount', () => {
  it('空账号提示补全', () => {
    expect(validateResetAccount('   ')).toEqual({ field: 'username', key: 'forgot.enterUsername' });
  });

  it('非空账号通过', () => {
    expect(validateResetAccount('dev@example.com')).toBeNull();
  });
});

describe('validateResetConfirm', () => {
  it('缺验证码时命中验证码字段', () => {
    expect(validateResetConfirm(form({ code: ' ' }))?.field).toBe('code');
  });

  it('缺新密码时命中新密码字段', () => {
    expect(validateResetConfirm(form({ newPassword: '' }))?.field).toBe('newPassword');
  });

  it('新密码过短时按最小长度提示', () => {
    const issue = validateResetConfirm(form({ newPassword: 'a'.repeat(ResetPasswordMinLength - 1), confirmPassword: 'a'.repeat(ResetPasswordMinLength - 1) }));

    expect(issue).toEqual({
      field: 'newPassword',
      key: 'password.minLength',
      params: { min: ResetPasswordMinLength },
    });
  });

  it('缺确认密码时命中确认密码字段', () => {
    expect(validateResetConfirm(form({ confirmPassword: '' }))?.field).toBe('confirmPassword');
  });

  it('两次密码不一致时提示不一致', () => {
    expect(validateResetConfirm(form({ confirmPassword: 'Other123!' }))).toEqual({
      field: 'confirmPassword',
      key: 'form.passwordMismatch',
    });
  });

  it('合法表单通过', () => {
    expect(validateResetConfirm(form())).toBeNull();
  });
});
