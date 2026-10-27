/**
 * Angular 找回密码表单校验单元测试（SKIN-3）。
 *
 * 与注册入口测试一致，使用 Node 内置测试运行器直接跑 `.ts`，
 * 不引入浏览器测试依赖。运行：`pnpm --filter ./NewLife.Cube.Angular/web run test:unit`
 */

import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { FORGOT_PASSWORD_PATH, validateResetPassword, type ResetPasswordForm } from '../src/app/utils/forgot-password.ts';

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
    assert.equal(validateResetPassword(makeForm()), null);
  });

  it('账号与验证码必填', () => {
    assert.deepEqual(validateResetPassword(makeForm({ username: '' })), ['username', '请输入手机号或邮箱']);
    assert.deepEqual(validateResetPassword(makeForm({ code: '' })), ['code', '请输入验证码']);
  });

  it('新密码不少于 6 位', () => {
    assert.deepEqual(validateResetPassword(makeForm({ newPassword: '12345', confirmPassword: '12345' })), [
      'newPassword',
      '密码不少于6位',
    ]);
  });

  it('确认密码必填且需与密码一致', () => {
    assert.deepEqual(validateResetPassword(makeForm({ confirmPassword: '' })), ['confirmPassword', '请再次输入新密码']);
    assert.deepEqual(validateResetPassword(makeForm({ confirmPassword: 'Other123' })), ['confirmPassword', '两次密码不一致']);
  });

  it('找回密码入口路由固定为 /forgot-password', () => {
    assert.equal(FORGOT_PASSWORD_PATH, '/forgot-password');
  });
});
