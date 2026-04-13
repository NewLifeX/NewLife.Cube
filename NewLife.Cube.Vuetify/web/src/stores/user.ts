import { defineStore } from 'pinia';
import type { UserInfo, MenuItem } from '@cube/api-core';
import cubeApi from '@/api';

export const useUserStore = defineStore('user', {
  state: () => ({
    user: null as UserInfo | null,
    menus: [] as MenuItem[],
    permissions: [] as string[],
  }),
  getters: {
    isLoggedIn: (state) => !!state.user,
    displayName: (state) => state.user?.displayName || state.user?.name || '',
  },
  actions: {
    async login(username: string, password: string) {
      const res = await cubeApi.user.login(username, password);
      if (res.data) {
        await this.fetchUserInfo();
        await this.fetchMenus();
      }
      return res;
    },
    async logout() {
      await cubeApi.user.logout();
      this.user = null;
      this.menus = [];
      this.permissions = [];
    },
    async fetchUserInfo() {
      const res = await cubeApi.user.getInfo();
      if (res.data) this.user = res.data;
    },
    async fetchMenus() {
      const res = await cubeApi.menu.getMenuTree();
      if (res.data) this.menus = res.data;
    },
  },
});
