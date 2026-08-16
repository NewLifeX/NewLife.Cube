/** X-Tenant 头规则：有 code 才带头；平台空串不带头 */
export function resolveTenantHeader(code: string | null | undefined): Record<string, string> {
  if (code != null && code !== '') return { 'X-Tenant': code };
  return {};
}
