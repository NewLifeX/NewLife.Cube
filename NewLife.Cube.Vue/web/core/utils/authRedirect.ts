/** 根据路由运行时基址提取应用内路径。 */
export function appRelativePath(path: string): string {
  const base = (window.router?.options.history.base || (window as unknown as { routerBase?: string }).routerBase || '').replace(/\/$/, '');
  return base && (path === base || path.startsWith(base + '/')) ? path.slice(base.length) || '/' : path;
}

/** 只接受同源站内回跳，认证页回退首页以防循环。 */
export function normalizeAuthRedirect(value: unknown): string {
  if (typeof value !== 'string' || !value.trim() || /[\\\x00-\x1f\x7f]/.test(value)) return '/';
  try {
    if (!value.startsWith('/') && !/^https?:\/\//i.test(value)) return '/';
    const url = new URL(value, window.location.origin);
    if (url.origin !== window.location.origin || url.username || url.password) return '/';
    if (/^\/(?:login|loading|unauthorized)\/?$/i.test(appRelativePath(url.pathname))) return '/';
    return url.pathname + url.search;
  } catch { return '/'; }
}
