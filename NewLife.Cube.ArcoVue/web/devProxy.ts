/** 开发服务器代理前缀（须与后端 Auth/MFA 等路径对齐） */
export const DEV_PROXY_TARGET = 'http://localhost:5000';

export const DEV_PROXY_PREFIXES = [
  '/Admin',
  '/Auth',
  '/Mfa',
  '/Cube',
  '/Sso',
  '/api',
] as const;

export type DevProxyPrefix = (typeof DEV_PROXY_PREFIXES)[number];

export function createDevProxy(): Record<
  DevProxyPrefix,
  { target: string; changeOrigin: boolean }
> {
  return Object.fromEntries(
    DEV_PROXY_PREFIXES.map((prefix) => [
      prefix,
      { target: DEV_PROXY_TARGET, changeOrigin: true },
    ]),
  ) as Record<DevProxyPrefix, { target: string; changeOrigin: boolean }>;
}
