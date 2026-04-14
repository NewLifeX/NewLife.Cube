/**
 * @cube/auth-logic/pinia — Pinia 适配器
 *
 * 将 AuthLogic 桥接为 Pinia store，供 Vue 系皮肤使用。
 *
 * @example
 * ```ts
 * import { createPiniaAuthStore } from '@cube/auth-logic/pinia';
 * import api from '@/api';
 * export const useUserStore = createPiniaAuthStore(api);
 * ```
 */

import { defineStore } from 'pinia';
import type { CubeApi, UserInfo, MenuItem } from '@cube/api-core';
import { AuthLogic, type AuthState } from './index';

/**
 * 创建 Pinia 用户认证 Store
 *
 * @param api - CubeApi 实例
 * @param storeId - Store ID，默认 'user'
 */
export function createPiniaAuthStore(api: CubeApi, storeId = 'user') {
  let logic: AuthLogic | null = null;

  return defineStore(storeId, {
    state: (): AuthState => ({
      userInfo: null,
      permissions: [],
      menus: [],
    }),
    getters: {
      isLoggedIn(): boolean {
        return !!api.tokenManager.getToken();
      },
      displayName(state): string {
        return state.userInfo?.displayName ?? state.userInfo?.name ?? '';
      },
      avatar(state): string {
        return state.userInfo?.avatar ?? '';
      },
    },
    actions: {
      /** 获取/初始化 AuthLogic 实例 */
      _getLogic(): AuthLogic {
        if (!logic) {
          logic = new AuthLogic(api, (partial) => {
            if (partial.userInfo !== undefined) this.userInfo = partial.userInfo;
            if (partial.permissions !== undefined) this.permissions = partial.permissions;
            if (partial.menus !== undefined) this.menus = partial.menus;
          });
        }
        return logic;
      },
      async login(username: string, password: string) {
        return this._getLogic().login(username, password);
      },
      async logout() {
        return this._getLogic().logout();
      },
      async fetchUserInfo(): Promise<UserInfo> {
        return this._getLogic().fetchUserInfo();
      },
      async fetchMenus(): Promise<MenuItem[]> {
        return this._getLogic().fetchMenus();
      },
      getMenuPermission(path: string): Record<string, string> {
        return this._getLogic().getMenuPermission(path);
      },
    },
  });
}
