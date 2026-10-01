/** 行内动作请求方法与确认文案（OSC-2610011cd6） */

export function opsHttpMethod(dataAction?: string): 'GET' | 'POST' {
  return dataAction?.trim() ? 'POST' : 'GET';
}

/** 解析后的 path 以 /api/ 开头时，单元格点击发请求而不是路由跳转 */
export function isApiActionUrl(path: string): boolean {
  return path.startsWith('/api/');
}

/** 按显示名选择确认文案；未命中时用通用句 */
export function confirmText(label: string): string {
  if (label.includes('马上执行')) return '确认立即执行该作业？';
  if (label.includes('解封')) return '确认解除封禁？';
  if (label.includes('强制下线')) return '确认强制该用户下线？';
  return '确认执行该操作？';
}

export const CLEAR_PASSWORD_CONFIRM = '确认清空该用户密码？空密码时任意密码均可登录。';
export const REVOKE_TOKENS_CONFIRM = '确认吊销该用户的全部访问令牌？';

/** 用户抽屉是否显示清空密码 / 吊销令牌 */
export function showUserSecurityActions(
  typePath: string,
  mode: string,
  user: { isSystem?: boolean } | null | undefined,
  id: unknown,
): boolean {
  const norm = typePath.replace(/^\/+/, '').replace(/\/+$/, '').toLowerCase();
  if (norm !== 'admin/user') return false;
  if (mode === 'add') return false;
  if (user?.isSystem !== true) return false;
  const n = typeof id === 'number' ? id : Number(id);
  return Number.isFinite(n) && n > 0;
}
