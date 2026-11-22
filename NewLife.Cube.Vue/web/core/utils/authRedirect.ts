/** 根据路由运行时基址提取应用内路径。 */
export function appRelativePath(path: string): string {
  const base = (window.router?.options.history.base || (window as unknown as { routerBase?: string }).routerBase || '').replace(/\/$/, '');
  return base && (path === base || path.startsWith(base + '/')) ? path.slice(base.length) || '/' : path;
}

/** 只接受同源站内回跳，认证页回退首页以防循环。 */
export function normalizeAuthRedirect(value: unknown): string {
  // 拦截反斜杠与控制字符，避免拼出协议相对地址（//host）或头部注入
  const hasUnsafeChar = (s: string) => [...s].some((ch) => ch === '\\' || ch.charCodeAt(0) < 0x20 || ch.charCodeAt(0) === 0x7f);
  if (typeof value !== 'string' || !value.trim() || hasUnsafeChar(value)) return '/';
  try {
    if (!value.startsWith('/') && !/^https?:\/\//i.test(value)) return '/';
    const url = new URL(value, window.location.origin);
    if (url.origin !== window.location.origin || url.username || url.password) return '/';
    if (/^\/(?:login|loading|unauthorized)\/?$/i.test(appRelativePath(url.pathname))) return '/';
    return url.pathname + url.search;
  } catch { return '/'; }
}
