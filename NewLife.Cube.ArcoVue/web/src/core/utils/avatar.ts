/**
 * 无头像时展示的用户名首字符
 *
 * 中文等非 ASCII 取首字；英文字母取首字母并大写；空名回落 '?'。
 *
 * @example avatarInitial('管理员') === '管'
 * @example avatarInitial('admin') === 'A'
 * @example avatarInitial('') === '?'
 */
export function avatarInitial(name?: string | null): string {
  const n = (name ?? '').trim();
  if (!n) return '?';
  const ch = Array.from(n)[0];
  return /[a-zA-Z]/.test(ch) ? ch.toUpperCase() : ch;
}
