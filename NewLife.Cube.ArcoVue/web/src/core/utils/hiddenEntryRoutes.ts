/** 隐藏菜单静态路由（OSC-2610011ff2）。只注册地址，不改 Menu.Visible。 */
export const HIDDEN_ENTRY_ROUTES = [
  { path: 'Admin/UserOnline', title: '在线会话' },
  { path: 'Admin/UserStat', title: '用户统计' },
  { path: 'Admin/UserToken', title: '访问令牌' },
  { path: 'Admin/UserConnect', title: '用户链接' },
  { path: 'Admin/OAuthConfig', title: 'OAuth 设置' },
  { path: 'Admin/OAuthLog', title: 'OAuth 日志' },
  { path: 'Admin/AccessRule', title: '访问规则' },
  { path: 'Admin/SmsConfig', title: '短信设置' },
  { path: 'Admin/MailConfig', title: '邮件设置' },
  { path: 'Admin/Parameter', title: '字典参数' },
  { path: 'Admin/TenantUser', title: '租户用户' },
  { path: 'Cube/AppLog', title: '应用日志' },
] as const;

export const HIDDEN_ENTRY_PATHS = HIDDEN_ENTRY_ROUTES.map((item) => item.path);

/** 去掉首尾斜杠与查询后，与白名单忽略大小写比较。多一段或少一段为 false。 */
export function isHiddenEntryPath(path: string): boolean {
  const bare = String(path ?? '')
    .trim()
    .replace(/[?#].*$/, '')
    .replace(/^\/+|\/+$/g, '');
  if (!bare) return false;
  const key = bare.toLowerCase();
  return HIDDEN_ENTRY_PATHS.some((p) => p.toLowerCase() === key);
}
