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
import type { CubeApi, UserInfo, MenuItem, ResetPasswordModel } from '@cube/api-core';
import { AuthLogic, ForgotPasswordLogic, type AuthState, type ForgotPasswordState } from './index';

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

export interface ZustandForgotPasswordState extends ForgotPasswordState {
  sendCode: (username: string, channel: string) => Promise<boolean>;
  resendCode: (username: string, channel: string) => Promise<boolean>;
  confirmReset: (model: ResetPasswordModel) => Promise<boolean>;
  reset: () => void;
}

/**
 * 创建 Zustand 忘记密码 Store
 *
 * @param api - CubeApi 实例
 *
 * @example
 * ```ts
 * import { createZustandForgotPasswordStore } from '@cube/auth-logic/zustand';
 * export const useForgotPasswordStore = createZustandForgotPasswordStore(api);
 * ```
 */
export function createZustandForgotPasswordStore(api: CubeApi): StoreApi<ZustandForgotPasswordState> {
  let logic: ForgotPasswordLogic;

  return create<ZustandForgotPasswordState>((set) => {
    logic = new ForgotPasswordLogic(api, (partial) => set(partial as Partial<ZustandForgotPasswordState>));

    return {
      step: 'input',
      sending: false,
      submitting: false,
      countdown: 0,
      error: '',

      sendCode: (username, channel) => logic.sendCode(username, channel),
      resendCode: (username, channel) => logic.resendCode(username, channel),
      confirmReset: (model) => logic.confirmReset(model),
      reset: () => logic.reset(),
    };
  });
}
