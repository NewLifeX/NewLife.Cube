import { create } from 'zustand';
import type { UserInfo, MenuItem } from '@cube/api-core';
import api from '@/api';

/** 递归查找菜单树中 URL 匹配的节点 */
function findMenu(menus: MenuItem[], path: string): MenuItem | undefined {
  for (const m of menus) {
    if (m.url && path.toLowerCase().endsWith(m.url.toLowerCase())) return m;
    if (m.children?.length) {
      const found = findMenu(m.children, path);
      if (found) return found;
    }
  }
  return undefined;
}

interface UserState {
  userInfo: UserInfo | null;
  permissions: string[];
  menus: MenuItem[];
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  fetchUserInfo: () => Promise<UserInfo>;
  fetchMenus: () => Promise<void>;
  getMenuPermission: (path: string) => Record<string, string>;
}

export const useUserStore = create<UserState>((set, get) => ({
  userInfo: null,
  permissions: [],
  menus: [],

  login: async (username, password) => {
    const res = await api.user.login({ username, password });
    if (res.data?.token) {
      api.tokenManager.setToken(res.data.token);
    }
  },

  logout: async () => {
    try { await api.user.logout(); } catch { /* ignore */ }
    api.tokenManager.clearToken();
    set({ userInfo: null, permissions: [], menus: [] });
  },

  fetchUserInfo: async () => {
    const res = await api.user.info();
    const info = res.data;
    set({
      userInfo: info,
      permissions: info?.permission?.split(',').filter(Boolean) ?? [],
    });
    return info;
  },

  fetchMenus: async () => {
    try {
      const res = await api.menu.getMenuTree();
      if (res.data) set({ menus: res.data });
    } catch { /* ignore */ }
  },

  getMenuPermission: (path: string) => {
    const item = findMenu(get().menus, path);
    return item?.permissions ?? {};
  },
}));
