/**
 * 分享/嵌入模式：URL 带 embed=1（及可选 token）时隐藏系统导航与菜单。
 */

const SHARE_FLAG_KEY = 'cube.shareEmbed';

export function readQueryEmbed(search?: string): { embed: boolean; token: string; viewId: string } {
  const q = new URLSearchParams(search ?? (typeof window !== 'undefined' ? window.location.search : ''));
  const embed = q.get('embed') === '1' || q.get('embed') === 'true';
  const token = (q.get('token') || q.get('Token') || '').trim();
  const viewId = (q.get('viewId') || q.get('vid') || '').trim();
  return { embed, token, viewId };
}

export function isEmbedMode(): boolean {
  if (typeof window === 'undefined') return false;
  try {
    if (sessionStorage.getItem(SHARE_FLAG_KEY) === '1') return true;
  } catch {
    /* ignore */
  }
  return readQueryEmbed().embed;
}

/** 进入分享页时写入会话标记（令牌由路由守卫 persistSession） */
export function enterEmbedMode(): void {
  try {
    sessionStorage.setItem(SHARE_FLAG_KEY, '1');
  } catch {
    /* ignore */
  }
}

export function clearEmbedMode(): void {
  try {
    sessionStorage.removeItem(SHARE_FLAG_KEY);
  } catch {
    /* ignore */
  }
}
