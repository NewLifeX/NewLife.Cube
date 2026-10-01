export interface AccountLink {
  title: string;
  path: string;
  query?: Record<string, string>;
  disabled: boolean;
}

/** 有限正整数才作为 userId；否则除用户统计外禁用。 */
export function buildAccountLinks(userId: number | undefined): AccountLink[] {
  const ok = typeof userId === 'number' && Number.isInteger(userId) && userId > 0;
  const query = ok ? { userId: String(userId) } : undefined;
  return [
    { title: '在线会话', path: '/Admin/UserOnline', query, disabled: !ok },
    { title: '用户统计', path: '/Admin/UserStat', disabled: false },
    { title: '访问令牌', path: '/Admin/UserToken', query, disabled: !ok },
    { title: '登录日志', path: '/Admin/Log', query, disabled: !ok },
    { title: 'OAuth 日志', path: '/Admin/OAuthLog', query, disabled: !ok },
  ];
}
