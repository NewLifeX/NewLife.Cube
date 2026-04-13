import { defineStore } from 'pinia';
import type { UserInfo } from '@cube/api-core';
import api from '@/api';

export const useUserStore = defineStore('user', {
  state: () => ({
    userInfo: null as UserInfo | null,
    /** 权限码列表 */
    permissions: [] as string[],
  }),
  getters: {
    isLoggedIn: (state) => !!api.tokenManager.getToken(),
    displayName: (state) => state.userInfo?.displayName ?? state.userInfo?.name ?? '',
    avatar: (state) => state.userInfo?.avatar ?? '',
  },
  actions: {
    /** 登录 */
    async login(username: string, password: string) {
      const res = await api.user.login({ username, password });
      if (res.data?.token) {
        api.tokenManager.setToken(res.data.token);
      }
      return res;
    },
    /** 登出 */
    async logout() {
      try { await api.user.logout(); } catch { /* ignore */ }
      api.tokenManager.clearToken();
      this.userInfo = null;
      this.permissions = [];
    },
    /** 获取用户信息 */
    async fetchUserInfo() {
      const res = await api.user.info();
      this.userInfo = res.data;
      // 解析权限字符串
      if (res.data?.permission) {
        this.permissions = res.data.permission.split(',').filter(Boolean);
      }
      return res.data;
    },
  },
});
