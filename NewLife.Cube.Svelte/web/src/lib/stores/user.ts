import { writable, derived, get } from 'svelte/store';
import { getApi } from '$lib/api';
import type { MenuItem } from '@cube/api-core';

interface UserInfo {
  displayName?: string;
  name?: string;
  roleName?: string;
  avatar?: string;
}

export const user = writable<UserInfo | null>(null);
export const menus = writable<MenuItem[]>([]);
export const isLoggedIn = derived(user, ($u) => !!$u);
export const displayName = derived(user, ($u) => $u?.displayName || $u?.name || '');

/** 递归查找菜单树中 URL 匹配的节点 */
function findMenu(items: MenuItem[], path: string): MenuItem | undefined {
  for (const m of items) {
    if (m.url && path.toLowerCase().endsWith(m.url.toLowerCase())) return m;
    if (m.children?.length) {
      const found = findMenu(m.children, path);
      if (found) return found;
    }
  }
  return undefined;
}

/** 获取指定路径的菜单权限 */
export function getMenuPermission(path: string): Record<string, string> {
  const item = findMenu(get(menus), path);
  return item?.permissions ?? {};
}

export async function login(username: string, password: string): Promise<boolean> {
  try {
    const res = await getApi().user.login(username, password);
    if (res?.data) {
      user.set(res.data);
      return true;
    }
    return false;
  } catch {
    return false;
  }
}

export async function logout() {
  try { await getApi().user.logout(); } catch { /* ignore */ }
  user.set(null);
  menus.set([]);
}

export async function fetchUserInfo() {
  try {
    const res = await getApi().user.info();
    if (res?.data) user.set(res.data);
  } catch { /* ignore */ }
}

export async function fetchMenus() {
  try {
    const res = await getApi().menu.getMenuTree();
    if (res?.data) menus.set(res.data);
  } catch { /* ignore */ }
}
