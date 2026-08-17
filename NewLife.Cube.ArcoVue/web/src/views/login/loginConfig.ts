import type { LoginConfig, OAuthProvider } from '@cube/api-core';

export type LoginTabKey = 'password' | 'sms' | 'mail';

export interface LoginTabSpec {
  key: LoginTabKey;
  title: string;
}

/** 按 LoginConfig.login.* 解析可见登录通道（顺序：password → sms → mail） */
export function resolveLoginTabs(cfg: LoginConfig | null | undefined): LoginTabSpec[] {
  const login = cfg?.login;
  const tabs: LoginTabSpec[] = [];

  if (login) {
    if (login.password === true) tabs.push({ key: 'password', title: '密码登录' });
    if (login.sms === true) tabs.push({ key: 'sms', title: '手机验证码' });
    if (login.mail === true) tabs.push({ key: 'mail', title: '邮箱验证码' });
    // password 未显式 false 且无其它通道时给密码，避免空 Tabs
    if (tabs.length === 0 && login.password !== false) {
      tabs.push({ key: 'password', title: '密码登录' });
    }
    return tabs;
  }

  // 旧字段兜底（仅当嵌套 login 完全缺失）
  if (cfg) {
    if (cfg.allowLogin !== false) tabs.push({ key: 'password', title: '密码登录' });
    if (cfg.enableSms) tabs.push({ key: 'sms', title: '手机验证码' });
    if (cfg.enableMail) tabs.push({ key: 'mail', title: '邮箱验证码' });
  } else {
    tabs.push({ key: 'password', title: '密码登录' });
  }
  return tabs;
}

/** 从 LoginConfig 读取布尔功能开关（兼容 camelCase / PascalCase） */
function pickFeatureFlag(
  cfg: LoginConfig | null | undefined,
  camel: 'enableTenant',
  pascal: 'EnableTenant',
): boolean | undefined {
  if (!cfg) return undefined;
  const raw = cfg as LoginConfig & Record<string, unknown>;
  const v = raw[camel] ?? raw[pascal];
  if (typeof v === 'boolean') return v;
  return undefined;
}

/** 登录页是否显示租户 Code（魔方设置 EnableTenant） */
export function isTenantLoginEnabled(cfg: LoginConfig | null | undefined): boolean {
  return pickFeatureFlag(cfg, 'enableTenant', 'EnableTenant') === true;
}

/**
 * 登录页是否显示第三方登录：仅看可见 oauth 列表。
 * EnableOAuthServer 表示 Cube 作 OAuth 服务端，不控制客户端第三方登录。
 */
export function isOAuthLoginEnabled(cfg: LoginConfig | null | undefined): boolean {
  return resolveOAuthProviders(cfg).length > 0;
}

/** OAuth 列表：优先 oauth[]；兼容 oAuth / providers / Remark */
export function resolveOAuthProviders(cfg: LoginConfig | null | undefined): OAuthProvider[] {
  if (!cfg) return [];

  const raw = cfg as LoginConfig & { oAuth?: OAuthProvider[] };
  let list: OAuthProvider[] = [];
  if (Array.isArray(cfg.oauth) && cfg.oauth.length) list = cfg.oauth;
  else if (Array.isArray(raw.oAuth) && raw.oAuth.length) list = raw.oAuth;
  else if (Array.isArray(cfg.providers) && cfg.providers.length) list = cfg.providers;
  return list.map((p) => {
    const ext = p as OAuthProvider & { Remark?: string; NickName?: string };
    return {
      ...p,
      nickName: p.nickName || ext.NickName,
      remark: p.remark || ext.Remark,
    };
  });
}

export function isRegisterEnabled(cfg: LoginConfig | null | undefined): boolean {
  if (!cfg) return false;
  if (cfg.register?.enabled != null) return !!cfg.register.enabled;
  return !!cfg.allowRegister;
}

export function needLoginCaptcha(cfg: LoginConfig | null | undefined): boolean {
  return !!cfg?.login?.captcha;
}

/** 发短信/邮件验证码前是否需要图片验证码（CaptchaScene 位 4） */
export function needSendCodeCaptcha(cfg: LoginConfig | null | undefined): boolean {
  const login = cfg?.login as { sendCodeCaptcha?: boolean; SendCodeCaptcha?: boolean } | undefined;
  return !!(login?.sendCodeCaptcha ?? login?.SendCodeCaptcha);
}

export function needChallenge(cfg: LoginConfig | null | undefined): boolean {
  return !!cfg?.security?.challengeRequired;
}

export { resolveStartPage, mapStartPageToSpa, toSpaPath } from './startPage';

/**
 * 客户端校验密码强度。pattern 为空或 `*` 表示不限制；非法正则时跳过客户端校验。
 * @returns 错误文案；通过则 null
 */
export function validatePasswordStrength(
  password: string,
  pattern?: string | null,
): string | null {
  if (!password) return '请输入密码';
  const raw = (pattern ?? '').trim();
  if (!raw || raw === '*') return null;
  try {
    const re = new RegExp(raw);
    if (!re.test(password)) return '密码不符合系统强度要求';
  } catch {
    return null;
  }
  return null;
}

/**
 * SSO 登录跳转 URL（必须含 source=front-end，以便回跳 #token=）
 * @param name OAuth 应用名
 * @param redirect 登录成功后前端路由（相对或绝对）
 * @param origin window.location.origin
 */
export function buildSsoLoginUrl(
  name: string,
  redirect?: string | null,
  origin: string = typeof window !== 'undefined' ? window.location.origin : '',
): string {
  const q = redirect ? `?redirect=${encodeURIComponent(redirect)}` : '';
  const returnUrl = `${origin}/login${q}`;
  return `/Sso/Login/${encodeURIComponent(name)}?source=front-end&r=${encodeURIComponent(returnUrl)}`;
}

/** 从登录失败 message 提取 MFA token */
export function extractMfaToken(message?: string | null): string | null {
  if (!message) return null;
  const m = message.match(/mfa_required:(\S+)/);
  return m?.[1] ?? null;
}

/**
 * 归一化登录页 Logo/背景资源 URL：补全前导 /、统一斜杠；已是绝对地址则原样返回。
 */
export function normalizeLoginAssetUrl(raw: string | null | undefined): string {
  if (!raw || typeof raw !== 'string') return '';
  const s = raw.trim().replace(/\\/g, '/');
  if (!s) return '';
  if (/^(https?:|data:|blob:)/i.test(s)) return s;
  if (s.startsWith('//')) return s;
  return s.startsWith('/') ? s : `/${s}`;
}

/** 登录页 Logo（魔方设置 LoginLogo；兼容旧字段 logo） */
export function resolveLoginLogoUrl(cfg: LoginConfig | null | undefined): string {
  if (!cfg) return '';
  return normalizeLoginAssetUrl(cfg.loginLogo || cfg.logo || '');
}

/** 解析 location.hash 中的 token / refreshToken */
export function parseHashTokens(hash: string): { token?: string; refreshToken?: string } {
  const raw = hash.startsWith('#') ? hash.slice(1) : hash;
  if (!raw) return {};
  const params = new URLSearchParams(raw.includes('=') ? raw : `token=${raw}`);
  const token = params.get('token') || params.get('accessToken') || undefined;
  const refreshToken = params.get('refreshToken') || params.get('RefreshToken') || undefined;
  return {
    ...(token ? { token } : {}),
    ...(refreshToken ? { refreshToken } : {}),
  };
}
