/**
 * 第三方账号绑定工具（配合 /Sso/Bind）。
 */

/** 绑定/解绑路径键：后端 GetClient 按 OAuthConfig.Name 解析 */
export function resolveOAuthBindKey(item: { name?: string; id?: number | string }): string {
  return (item.name || String(item.id ?? '')).trim();
}
