/** OAuth 提供者 */
export interface OAuthProvider {
  name: string;
  logo: string;
  nickName: string;
}

/** 登录配置（来自 /Admin/Cube/GetLoginConfig） */
export interface LoginConfig {
  displayName: string;
  logo: string;
  allowLogin: boolean;
  allowRegister: boolean;
  enableSms: boolean;
  enableMail: boolean;
  loginTip: string;
  providers: OAuthProvider[];
}

/** 站点信息（来自 /Auth/SiteInfo） */
export interface SiteInfo {
  displayName: string;
  copyright: string;
  registration: string;
  loginTip: string;
  logo: string;
  /** 登录页 Logo，留空时前端使用皮肤内置 */
  loginLogo: string;
  /** 登录页背景图，留空时前端使用皮肤内置 */
  loginBg: string;
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
export interface RegisterModel {
  username: string;
  email: string;
  password: string;
  password2: string;
}