/**
 * @newlifex/auth-logic/zustand — Zustand 适配器
 *
 * 将 AuthLogic 桥接为 Zustand store，供 React 系皮肤使用。
 *
 * @example
 * ```ts
 * import { createZustandAuthStore } from '@newlifex/auth-logic/zustand';
 * import api from '@/api';
 * export const useUserStore = createZustandAuthStore(api);
 * ```
 */

import { create, type StoreApi, type UseBoundStore } from 'zustand';
import { type CubeApi, type UserInfo, type MenuItem, type ResetPasswordModel, type RegisterModel, type OAuthPendingInfo, type ApiResponse, type LoginResult, type AuthCategory } from '@newlifex/api-core';
import { AuthLogic, ForgotPasswordLogic, RegisterLogic, type AuthState, type ForgotPasswordState, type RegisterState } from './index';

export interface ZustandAuthState extends AuthState {
  isLoggedIn: () => boolean;
  /**
   * 密码登录（自动尝试 RSA-OAEP Challenge 加密）。
   * 返回完整响应供调用方检查 pendingActivation / mfa_required 等。
   * @param remember 记住登录状态（保存密码）。true 时后端把令牌有效期延长到 365 天，重开系统免登录
   */
  login: (username: string, password: string, captchaId?: string, captchaCode?: string, remember?: boolean) => Promise<ApiResponse<LoginResult>>;
  /** 验证码登录（手机/邮箱，category 为 'mobile' | 'mail'） */
  loginByCode: (username: string, code: string, category: AuthCategory, captchaId?: string, captchaCode?: string) => Promise<ApiResponse<LoginResult>>;
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
export function createZustandAuthStore(api: CubeApi): UseBoundStore<StoreApi<ZustandAuthState>> {
  let logic: AuthLogic;

  return create<ZustandAuthState>((set, get) => {
    logic = new AuthLogic(api, (partial) => set(partial));

    return {
      userInfo: null,
      permissions: [],
      menus: [],

      isLoggedIn: () => !!api.tokenManager.getToken(),

      login: (username, password, captchaId, captchaCode, remember) =>
        logic.login(username, password, captchaId, captchaCode, remember),

      loginByCode: (username, code, category, captchaId, captchaCode) =>
        logic.loginByCode(username, code, category, captchaId, captchaCode),

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
 * import { createZustandForgotPasswordStore } from '@newlifex/auth-logic/zustand';
 * export const useForgotPasswordStore = createZustandForgotPasswordStore(api);
 * ```
 */
export function createZustandForgotPasswordStore(api: CubeApi): UseBoundStore<StoreApi<ZustandForgotPasswordState>> {
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

export interface ZustandRegisterState extends RegisterState {
  sendSmsCode: (mobile: string) => Promise<boolean>;
  sendMailCode: (email: string) => Promise<boolean>;
  loadOAuthPendingInfo: (token: string) => Promise<OAuthPendingInfo | null>;
  registerByPassword: (model: Omit<RegisterModel, 'category'>) => Promise<boolean>;
  registerByPhone: (model: Omit<RegisterModel, 'category'>) => Promise<boolean>;
  registerByEmail: (model: Omit<RegisterModel, 'category'>) => Promise<boolean>;
  registerByOAuth: (model: Omit<RegisterModel, 'category'>) => Promise<boolean>;
  reset: () => void;
}

/** 创建 Zustand 注册 Store */
export function createZustandRegisterStore(api: CubeApi): UseBoundStore<StoreApi<ZustandRegisterState>> {
  let logic: RegisterLogic;

  return create<ZustandRegisterState>((set) => {
    logic = new RegisterLogic(api, (partial) => set(partial as Partial<ZustandRegisterState>));

    return {
      sending: false,
      submitting: false,
      countdown: 0,
      error: '',
      oauthPending: null,

      sendSmsCode: (mobile) => logic.sendRegisterCode(mobile, 'Sms'),
      sendMailCode: (email) => logic.sendRegisterCode(email, 'Mail'),
      loadOAuthPendingInfo: (token) => logic.loadOAuthPendingInfo(token),
      registerByPassword: (model) => logic.register({ ...model, category: '' }),
      registerByPhone: (model) => logic.register({ ...model, category: 'mobile' }),
      registerByEmail: (model) => logic.register({ ...model, category: 'mail' }),
      registerByOAuth: (model) => logic.register({ ...model, category: 'oauth' }),
      reset: () => logic.reset(),
    };
  });
}
