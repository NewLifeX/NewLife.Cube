/** 开发服务器代理目标（Cube 后端） */
export const DEV_PROXY_TARGET = 'http://localhost:5000';

/**
 * 固定前缀：认证 / 魔方核心 / 兼容旧路径。
 * 业务 Area（如 /School/Class/GetPage）由下方 PascalCase 通配覆盖。
 */
export const DEV_PROXY_PREFIXES = [
  '/Admin',
  '/Auth',
  '/Mfa',
  '/Cube',
  '/Sso',
  '/api',
  '/Uploads',
  '/Content',
] as const;

export type DevProxyPrefix = (typeof DEV_PROXY_PREFIXES)[number];

type ProxyReq = { headers: { accept?: string }; url?: string };

/**
 * HTML 导航进 Vite SPA；XHR/fetch（Accept 通常无 text/html）转发后端。
 * 根因：仅代理 /Admin 时，/School/.../GetPage 会落到 index.html，list 被解析为空。
 */
export function shouldBypassToSpa(req: ProxyReq): string | undefined {
  const accept = req.headers.accept || '';
  if (accept.includes('text/html')) return '/index.html';
  return undefined;
}

const proxyOptions = {
  target: DEV_PROXY_TARGET,
  changeOrigin: true,
  bypass: shouldBypassToSpa,
};

/** 业务 Area：首段 PascalCase，如 /School/Class/GetPage、/Erp/Order */
export const DEV_PROXY_AREA_PATTERN = '^/[A-Z][A-Za-z0-9]*/';

export function createDevProxy(): Record<string, typeof proxyOptions> {
  const proxy: Record<string, typeof proxyOptions> = {};
  for (const prefix of DEV_PROXY_PREFIXES) {
    proxy[prefix] = { ...proxyOptions };
  }
  proxy[DEV_PROXY_AREA_PATTERN] = { ...proxyOptions };
  return proxy;
}
