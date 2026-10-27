/**
 * Angular 找回密码表单校验。
 *
 * 与 Vuetify/MUI 皮肤的 `utils/forgot-password.ts` 保持一致（框架无关），
 * 约束对齐后端 `Auth/ResetPassword`：验证码必填、新密码不少于 6 位、两次输入一致。
 */

/** 重置密码表单输入 */
export interface ResetPasswordForm {
  username: string;
  channel: 'Sms' | 'Mail';
  code: string;
  newPassword: string;
  confirmPassword: string;
}

/** 重置密码校验结果：命中字段与提示文案，校验通过返回 null */
export type ResetPasswordError = [keyof ResetPasswordForm, string];

/** 校验重置密码表单 */
export function validateResetPassword(form: ResetPasswordForm): ResetPasswordError | null {
  if (!form.username) return ['username', '请输入手机号或邮箱'];
  if (!form.code) return ['code', '请输入验证码'];
  if (!form.newPassword) return ['newPassword', '请输入新密码'];
  if (form.newPassword.length < 6) return ['newPassword', '密码不少于6位'];
  if (!form.confirmPassword) return ['confirmPassword', '请再次输入新密码'];
  if (form.newPassword !== form.confirmPassword) return ['confirmPassword', '两次密码不一致'];

  return null;
}

/** 找回密码入口路由 */
export const FORGOT_PASSWORD_PATH = '/forgot-password';
