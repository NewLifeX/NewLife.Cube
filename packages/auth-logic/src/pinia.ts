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
import { AuthLogic, ForgotPasswordLogic, type AuthState, type ForgotPasswordState } from './index';

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

/**
 * 创建 Pinia 忘记密码 Store
 *
 * @param api - CubeApi 实例
 * @param storeId - Store ID，默认 'forgotPassword'
 *
 * @example
 * ```ts
 * import { createPiniaForgotPasswordStore } from '@cube/auth-logic/pinia';
 * export const useForgotPasswordStore = createPiniaForgotPasswordStore(api);
 * ```
 */
export function createPiniaForgotPasswordStore(api: CubeApi, storeId = 'forgotPassword') {
  let logic: ForgotPasswordLogic | null = null;

  return defineStore(storeId, {
    state: (): ForgotPasswordState => ({
      step: 'input',
      sending: false,
      submitting: false,
      countdown: 0,
      error: '',
    }),
    actions: {
      _getLogic(): ForgotPasswordLogic {
        if (!logic) {
          logic = new ForgotPasswordLogic(api, (partial) => {
            Object.assign(this.$state, partial);
          });
        }
        return logic;
      },
      async sendCode(username: string, channel: string) {
        return this._getLogic().sendCode(username, channel);
      },
      async resendCode(username: string, channel: string) {
        return this._getLogic().resendCode(username, channel);
      },
      async confirmReset(model: import('@cube/api-core').ResetPasswordModel) {
        return this._getLogic().confirmReset(model);
      },
      reset() {
        this._getLogic().reset();
      },
    },
  });
}
