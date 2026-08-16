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

/** OAuth 列表：优先 oauth[]；后端 CamelCase 把 OAuth 序列化为 oAuth */
export function resolveOAuthProviders(cfg: LoginConfig | null | undefined): OAuthProvider[] {
  if (!cfg) return [];
  const raw = cfg as LoginConfig & { oAuth?: OAuthProvider[] };
  if (Array.isArray(cfg.oauth) && cfg.oauth.length) return cfg.oauth;
  if (Array.isArray(raw.oAuth) && raw.oAuth.length) return raw.oAuth;
  if (Array.isArray(cfg.providers) && cfg.providers.length) return cfg.providers;
  return [];
}

export function isRegisterEnabled(cfg: LoginConfig | null | undefined): boolean {
  if (!cfg) return false;
  if (cfg.register?.enabled != null) return !!cfg.register.enabled;
  return !!cfg.allowRegister;
}

export function needLoginCaptcha(cfg: LoginConfig | null | undefined): boolean {
  return !!cfg?.login?.captcha;
}

export function needChallenge(cfg: LoginConfig | null | undefined): boolean {
  return !!cfg?.security?.challengeRequired;
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
