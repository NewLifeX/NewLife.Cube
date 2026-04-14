import { defineStore } from 'pinia';
import type { UserInfo, MenuItem } from '@cube/api-core';
import cubeApi from '@/api';

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
      const res = await cubeApi.user.login({ username, password });
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
      const res = await cubeApi.user.info();
      if (res.data) this.user = res.data;
    },
    async fetchMenus() {
      const res = await cubeApi.menu.getMenuTree();
      if (res.data) this.menus = res.data;
    },
    /** 获取指定路径的菜单权限 */
    getMenuPermission(path: string): Record<string, string> {
      const item = findMenu(this.menus, path);
      return item?.permissions ?? {};
    },
  },
});
