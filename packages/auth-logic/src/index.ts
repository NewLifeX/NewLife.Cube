/**
 * @cube/auth-logic — 魔方前端认证业务逻辑（框架无关核心）
 *
 * 将登录、登出、用户信息获取、菜单加载、权限查询等业务逻辑
 * 封装为框架无关的纯逻辑类，各框架通过适配器（Pinia/Zustand/Svelte）桥接。
 */

import type { CubeApi, UserInfo, MenuItem, ResetPasswordModel } from '@cube/api-core';
import { findMenu, getMenuPermission } from '@cube/page-utils';
import { encryptPassword } from '@cube/api-core';

/** 认证状态快照 */
export interface AuthState {
  userInfo: UserInfo | null;
  permissions: string[];
  menus: MenuItem[];
}

/** 状态变更回调 */
export type AuthStateUpdater = (partial: Partial<AuthState>) => void;

/**
 * 认证业务逻辑核心类
 *
 * 不依赖任何 UI 框架，通过构造时注入的 `update` 回调通知状态变更。
 */
export class AuthLogic {
  private api: CubeApi;
  private update: AuthStateUpdater;
  private state: AuthState;

  constructor(api: CubeApi, update: AuthStateUpdater, initialState?: Partial<AuthState>) {
    this.api = api;
    this.update = update;
    this.state = {
      userInfo: null,
      permissions: [],
      menus: [],
      ...initialState,
    };
  }

  /** 当前状态快照（只读） */
  getState(): Readonly<AuthState> {
    return this.state;
  }

  /** 是否已登录 */
  get isLoggedIn(): boolean {
    return !!this.api.tokenManager.getToken();
  }

  /** 密码登录（自动尝试 RSA-OAEP Challenge 加密，服务端不支持时降级明文） */
  async login(username: string, password: string) {
    let finalPassword = password;
    let pkey: string | undefined;
    try {
      const challengeRes = await this.api.user.getChallenge();
      const challenge = challengeRes.data;
      if (challenge?.publicKey) {
        finalPassword = await encryptPassword(password, challenge.publicKey);
        pkey = challenge.pkey;
      }
    } catch {
      // Challenge 接口不可达或加密失败，降级为明文传输
    }
    const res = await this.api.user.login({ username, password: finalPassword, ...(pkey ? { pkey } : {}) });
    if (res.data?.accessToken) {
      this.api.tokenManager.setToken(res.data.accessToken);
    }
    return res;
  }

  /** 登出 */
  async logout() {
    try { await this.api.user.logout(); } catch { /* ignore */ }
    this.api.tokenManager.clearToken();
    this.state = { userInfo: null, permissions: [], menus: [] };
    this.update(this.state);
  }

  /** 获取用户信息并解析权限 */
  async fetchUserInfo(): Promise<UserInfo> {
    const res = await this.api.user.info();
    const info = res.data;
    const permissions = info?.permission?.split(',').filter(Boolean) ?? [];
    this.state.userInfo = info;
    this.state.permissions = permissions;
    this.update({ userInfo: info, permissions });
    return info;
  }

  /** 获取菜单树 */
  async fetchMenus(): Promise<MenuItem[]> {
    const res = await this.api.menu.getMenuTree();
    const menus = res.data ?? [];
    this.state.menus = menus;
    this.update({ menus });
    return menus;
  }

  /** 获取指定路径的菜单权限映射 */
  getMenuPermission(path: string): Record<string, string> {
    return getMenuPermission(this.state.menus, path);
  }

  /** 递归查找菜单 */
  findMenu(path: string): MenuItem | undefined {
    return findMenu(this.state.menus, path);
  }
}

// 重新导出类型供适配器使用
export type { UserInfo, MenuItem, CubeApi, ResetPasswordModel } from '@cube/api-core';
export { findMenu, getMenuPermission, checkAuth, Auth } from '@cube/page-utils';
export type { AuthCode } from '@cube/page-utils';

// ─────────────────────────────────────────────
// 忘记密码（重置密码）业务逻辑
// ─────────────────────────────────────────────

/** 忘记密码流程步骤 */
export type ForgotStep = 'input' | 'verify';

/** 忘记密码状态 */
export interface ForgotPasswordState {
  /** 当前步骤：input=输入账号发码，verify=输入验证码设置新密码 */
  step: ForgotStep;
  /** 正在发送验证码 */
  sending: boolean;
  /** 正在提交重置 */
  submitting: boolean;
  /** 倒计时秒数（0 表示可重新发送） */
  countdown: number;
  /** 错误信息 */
  error: string;
}

/** 忘记密码状态变更回调 */
export type ForgotStateUpdater = (partial: Partial<ForgotPasswordState>) => void;

/**
 * 忘记密码业务逻辑核心类
 *
 * 封装两步重置密码流程（发码→重置），不依赖任何 UI 框架。
 * 倒计时由内部 setInterval 驱动，通过 `update` 回调通知框架。
 */
export class ForgotPasswordLogic {
  private api: CubeApi;
  private update: ForgotStateUpdater;
  private _timer: ReturnType<typeof setInterval> | null = null;

  state: ForgotPasswordState = {
    step: 'input',
    sending: false,
    submitting: false,
    countdown: 0,
    error: '',
  };

  constructor(api: CubeApi, update: ForgotStateUpdater) {
    this.api = api;
    this.update = update;
  }

  private setState(partial: Partial<ForgotPasswordState>) {
    Object.assign(this.state, partial);
    this.update(partial);
  }

  /** 发送忘记密码验证码（步骤1） */
  async sendCode(username: string, channel: string) {
    if (!username) {
      this.setState({ error: '请输入手机号或邮箱' });
      return false;
    }
    this.setState({ sending: true, error: '' });
    try {
      await this.api.user.sendCode({ channel, username, action: 'reset' });
      this.setState({ step: 'verify', sending: false });
      this._startCountdown();
      return true;
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
      this.setState({ sending: false, error: msg });
      return false;
    }
  }

  /** 重新发送验证码 */
  async resendCode(username: string, channel: string) {
    if (this.state.countdown > 0) return false;
    return this.sendCode(username, channel);
  }

  /** 提交新密码重置（步骤2） */
  async confirmReset(model: ResetPasswordModel) {
    if (!model.code) {
      this.setState({ error: '请输入验证码' });
      return false;
    }
    if (!model.newPassword) {
      this.setState({ error: '请输入新密码' });
      return false;
    }
    if (model.newPassword !== model.confirmPassword) {
      this.setState({ error: '两次密码输入不一致' });
      return false;
    }
    this.setState({ submitting: true, error: '' });
    try {
      await this.api.user.resetPassword(model);
      this.setState({ submitting: false });
      this._clearCountdown();
      return true;
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '重置失败，请重试';
      this.setState({ submitting: false, error: msg });
      return false;
    }
  }

  /** 重置为初始状态 */
  reset() {
    this._clearCountdown();
    this.setState({ step: 'input', sending: false, submitting: false, countdown: 0, error: '' });
  }

  private _startCountdown(seconds = 60) {
    this._clearCountdown();
    this.setState({ countdown: seconds });
    this._timer = setInterval(() => {
      const next = this.state.countdown - 1;
      this.setState({ countdown: next });
      if (next <= 0) this._clearCountdown();
    }, 1000);
  }

  private _clearCountdown() {
    if (this._timer) {
      clearInterval(this._timer);
      this._timer = null;
    }
  }
}
