import { writable, derived, get } from 'svelte/store';
import { getApi } from '$lib/api';
import type { MenuItem } from '@newlifex/cube';
import { getMenuPermission as resolveMenuPermission } from '@newlifex/cube';

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

/** 获取指定路径的菜单权限 */
export function getMenuPermission(path: string): Record<string, string> {
  return resolveMenuPermission(get(menus), path);
}

export async function login(username: string, password: string): Promise<boolean> {
  try {
    const res = await getApi().user.login({ username, password });
    if (res?.data?.accessToken) {
      // 登录结果只含令牌，用户资料由 /Auth/Info 获取
      await fetchUserInfo();
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
