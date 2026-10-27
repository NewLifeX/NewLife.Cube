// Vuetify 皮肤重置密码表单校验单测：必填、长度与两次密码一致
import { describe, expect, it } from 'vitest';
import { FORGOT_PASSWORD_PATH, validateResetPassword, type ResetPasswordForm } from '@/utils/forgot-password';

function makeForm(overrides: Partial<ResetPasswordForm> = {}): ResetPasswordForm {
  return {
    username: 'stone',
    channel: 'Sms',
    code: '123456',
    newPassword: 'NewPass123',
    confirmPassword: 'NewPass123',
    ...overrides,
  };
}

describe('重置密码表单校验', () => {
  it('填写完整时通过', () => {
    expect(validateResetPassword(makeForm())).toBeNull();
  });

  it('账号与验证码必填', () => {
    expect(validateResetPassword(makeForm({ username: '' }))).toEqual(['username', '请输入手机号或邮箱']);
    expect(validateResetPassword(makeForm({ code: '' }))).toEqual(['code', '请输入验证码']);
  });

  it('新密码不少于 6 位', () => {
    expect(validateResetPassword(makeForm({ newPassword: '12345', confirmPassword: '12345' }))).toEqual([
      'newPassword',
      '密码不少于6位',
    ]);
  });

  it('两次密码必须一致', () => {
    expect(validateResetPassword(makeForm({ confirmPassword: 'Other123' }))).toEqual(['confirmPassword', '两次密码不一致']);
  });

  it('找回密码入口路由固定为 /forgot-password', () => {
    expect(FORGOT_PASSWORD_PATH).toBe('/forgot-password');
  });
});
