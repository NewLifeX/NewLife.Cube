/** OAuth 提供者 */
export interface OAuthProvider {
  name: string;
  logo: string;
  nickName: string;
}

/** 登录配置（来自 /Auth/LoginConfig） */
export interface LoginConfig {
  displayName: string;
  logo: string;
  allowLogin: boolean;
  allowRegister: boolean;
  enableSms: boolean;
  enableMail: boolean;
  enableSmsRegister?: boolean;
  enableMailRegister?: boolean;
  loginTip: string;
  providers: OAuthProvider[];
}

/** 站点信息（来自 /Cube/SiteInfo） */
export interface SiteInfo {
  displayName: string;
  copyright: string;
  registration: string;
  loginTip: string;
  logo: string;
  /** 登录页 Logo，留空时前端使用皮肤内置 */
  loginLogo: string;
  /** 登录页背景图，留空时前端使用皮肤内置 */
  loginBackground: string;
}

/** 发送验证码请求 */
export interface SendCodeModel {
  /** 渠道：Sms / Mail */
  channel: 'Sms' | 'Mail';
  /** 手机号或邮箱 */
  username: string;
  action?: string;
}

/** 验证码登录请求 */
export interface LoginByCodeModel {
  /** 手机号或邮箱 */
  username: string;
  /** 验证码 */
  password: string;
  /** 登录类型：1=手机，2=邮箱 */
  loginCategory: 1 | 2;
}

/** 注册请求 */
export enum RegisterCategory {
  Password = 0,
  Phone = 1,
  Email = 2,
  OAuthBind = 3,
}

/** OAuth 回跳待注册信息 */
export interface OAuthPendingInfo {
  provider?: string;
  username?: string;
  email?: string;
  mobile?: string;
  avatar?: string;
}

/** 登录/注册结果 */
export interface LoginResult {
  token?: string;
  accessToken?: string;
  refreshToken?: string;
  expireIn?: number;
}

/** 注册请求 */
export interface RegisterModel {
  registerCategory?: RegisterCategory;
  username?: string;
  email?: string;
  mobile?: string;
  password: string;
  confirmPassword?: string;
  password2?: string;
  code?: string;
  oauthToken?: string;
}

/** 忘记密码/重置密码请求 */
export interface ResetPasswordModel {
  /** 手机号或邮符1 */
  username: string;
  /** 验证码 */
  code: string;
  /** 新密码 */
  newPassword: string;
  /** 确认密码 */
  confirmPassword: string;
}

/** Challenge 响应（用于密码加密登录） */
export interface ChallengeResult {
  /** 挑战标识，登录时传回 challengeId 字段 */
  challengeId: string;
  /** PEM SPKI 格式 RSA 公鑰 */
  publicKey: string;
}