/**
 * @cube/auth-logic/zustand — Zustand 适配器
 *
 * 将 AuthLogic 桥接为 Zustand store，供 React 系皮肤使用。
 *
 * @example
 * ```ts
 * import { createZustandAuthStore } from '@cube/auth-logic/zustand';
 * import api from '@/api';
 * export const useUserStore = createZustandAuthStore(api);
 * ```
 */

import { create, type StoreApi } from 'zustand';
import type { CubeApi, UserInfo, MenuItem } from '@cube/api-core';
import { AuthLogic, type AuthState } from './index';

export interface ZustandAuthState extends AuthState {
  isLoggedIn: () => boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  fetchUserInfo: () => Promise<UserInfo>;
  fetchMenus: () => Promise<MenuItem[]>;
  getMenuPermission: (path: string) => Record<string, string>;
}

/**
 * 创建 Zustand 用户认证 Store
 *
 * @param api - CubeApi 实例
 */
export function createZustandAuthStore(api: CubeApi): StoreApi<ZustandAuthState> {
  let logic: AuthLogic;

  return create<ZustandAuthState>((set, get) => {
    logic = new AuthLogic(api, (partial) => set(partial));

    return {
      userInfo: null,
      permissions: [],
      menus: [],

      isLoggedIn: () => !!api.tokenManager.getToken(),

      login: async (username, password) => {
        const res = await logic.login(username, password);
      },

      logout: async () => {
        await logic.logout();
      },

      fetchUserInfo: async () => {
        return logic.fetchUserInfo();
      },

      fetchMenus: async () => {
        return logic.fetchMenus();
      },

      getMenuPermission: (path: string) => {
        return logic.getMenuPermission(path);
      },
    };
  });
}
