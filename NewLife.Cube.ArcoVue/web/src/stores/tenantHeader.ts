/** X-Tenant 头规则：多租户关闭或无 code 时不带头；平台空串不带头 */
export function resolveTenantHeader(code: string | null | undefined): Record<string, string> {
  try {
    if (sessionStorage.getItem('cube.tenant.enabled') === '0') return {};
  } catch {
    /* ignore */
  }
  if (code != null && code !== '') return { 'X-Tenant': code };
  return {};
}

/** 401 / 登出：清除租户会话，避免下一用户沿用旧 X-Tenant 被 EnsureTenantUser 串绑 */
export function clearTenantSession(): void {
  try {
    sessionStorage.removeItem('cube.tenant.code');
    sessionStorage.removeItem('cube.tenant.enabled');
  } catch {
    /* ignore */
  }
}
