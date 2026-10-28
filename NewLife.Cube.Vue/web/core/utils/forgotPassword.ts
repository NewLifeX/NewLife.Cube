/**
 * 找回密码（忘记密码）表单模型与校验
 *
 * 校验规则与后端 `AuthController.ResetPassword` 的入参约束保持一致：
 * 账号只接受手机号或邮箱（后端按格式分发到短信/邮件验证码通道）、验证码必填、
 * 新密码不少于 {@link ResetPasswordMinLength} 位、两次输入一致。
 *
 * 本文件只返回「字段 + 词表键 + 插值参数」，不直接取词表文案，
 * 好处是校验逻辑可以脱离 i18n 单测（`core/__tests__/forgotPassword.spec.ts`）。
 */

/** 发码渠道 */
export type ResetPasswordChannel = 'Sms' | 'Mail';

/** 找回密码表单 */
export interface ResetPasswordForm {
  /** 手机号或邮箱 */
  username: string;
  /** 发码渠道 */
  channel: ResetPasswordChannel;
  /** 验证码 */
  code: string;
  /** 新密码 */
  newPassword: string;
  /** 确认密码 */
  confirmPassword: string;
}

/** 可能出错的字段（channel 由 {@link guessChannel} 自动决定，不参与校验） */
export type ResetPasswordField = 'username' | 'code' | 'newPassword' | 'confirmPassword';

/** 校验问题：字段 + 词表键 + 插值参数 */
export interface ResetPasswordIssue {
  /** 出错的字段 */
  field: ResetPasswordField;
  /** i18n 词表键 */
  key: string;
  /** 词表插值参数 */
  params?: Record<string, unknown>;
}

/** 新密码最小长度，与后端 `PasswordService` 的弱密码门限一致 */
export const ResetPasswordMinLength = 6;

/**
 * 判断账号应走哪条发码通道
 *
 * 含 `@` 视为邮箱，其余按手机号处理；仅用于给渠道下拉一个合理默认值，
 * 最终的账号格式以后端校验为准（后端会在格式不合法时直接报错）。
 *
 * @param username 账号输入
 * @returns 'Mail' 或 'Sms'
 */
export function guessChannel(username: string): ResetPasswordChannel {
  return username.trim().includes('@') ? 'Mail' : 'Sms';
}

/**
 * 校验第一步：账号是否可发码
 *
 * @param username 账号输入
 * @returns 校验问题，通过时返回 null
 */
export function validateResetAccount(username: string): ResetPasswordIssue | null {
  if (!username.trim()) return { field: 'username', key: 'forgot.enterUsername' };

  return null;
}

/**
 * 校验第二步：验证码与新密码
 *
 * @param form 表单当前值
 * @returns 校验问题，通过时返回 null
 */
export function validateResetConfirm(form: ResetPasswordForm): ResetPasswordIssue | null {
  if (!form.code.trim()) return { field: 'code', key: 'security.enterCode' };
  if (!form.newPassword) return { field: 'newPassword', key: 'security.enterPassword' };
  if (form.newPassword.length < ResetPasswordMinLength)
    return { field: 'newPassword', key: 'password.minLength', params: { min: ResetPasswordMinLength } };
  if (!form.confirmPassword) return { field: 'confirmPassword', key: 'security.enterPassword' };
  if (form.newPassword !== form.confirmPassword) return { field: 'confirmPassword', key: 'form.passwordMismatch' };

  return null;
}
