import { create } from 'zustand';
import type { UserInfo, MenuItem } from '@newlifex/cube';
import { getMenuPermission } from '@newlifex/cube';
import api from '@/api';

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
    if (res.data?.accessToken) {
      api.tokenManager.setToken(res.data.accessToken);
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
    return getMenuPermission(get().menus, path);
  },
}));
