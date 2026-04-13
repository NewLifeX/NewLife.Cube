import { create } from 'zustand';
import type { UserInfo } from '@cube/api-core';
import api from '@/api';

interface UserState {
  userInfo: UserInfo | null;
  permissions: string[];
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  fetchUserInfo: () => Promise<UserInfo>;
}

export const useUserStore = create<UserState>((set) => ({
  userInfo: null,
  permissions: [],

  login: async (username, password) => {
    const res = await api.user.login({ username, password });
    if (res.data?.token) {
      api.tokenManager.setToken(res.data.token);
    }
  },

  logout: async () => {
    try { await api.user.logout(); } catch { /* ignore */ }
    api.tokenManager.clearToken();
    set({ userInfo: null, permissions: [] });
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
}));
