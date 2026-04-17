/**
 * @cube/auth-logic — 魔方前端认证业务逻辑（框架无关核心）
 *
 * 将登录、登出、用户信息获取、菜单加载、权限查询等业务逻辑
 * 封装为框架无关的纯逻辑类，各框架通过适配器（Pinia/Zustand/Svelte）桥接。
 */

import { RegisterCategory, type CubeApi, type UserInfo, type MenuItem, type ResetPasswordModel, type RegisterModel, type OAuthPendingInfo, type LoginCategory } from '@cube/api-core';
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
    let challengeId: string | undefined;
    try {
      const challengeRes = await this.api.user.getChallenge();
      const challenge = challengeRes.data;
      if (challenge?.publicKey) {
        finalPassword = await encryptPassword(password, challenge.publicKey);
        challengeId = challenge.challengeId;
      }
    } catch {
      // Challenge 接口不可达或加密失败，降级为明文传输
    }
    const res = await this.api.user.login({ username, password: finalPassword, ...(challengeId ? { challengeId } : {}) });
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

  /** 发送验证码登录短信/邮件 */
  async sendLoginCode(username: string, channel: LoginCategory extends string ? LoginCategory : 'mobile' | 'mail') {
    return this.api.user.sendCode({ channel: channel as string, username });
  }

  /** 验证码登录（手机/邮箱） */
  async loginByCode(username: string, code: string, loginCategory: LoginCategory) {
    const res = await this.api.user.loginByCode({ username, password: code, loginCategory });
    if (res.data?.accessToken) {
      this.api.tokenManager.setToken(res.data.accessToken);
    }
    return res;
  }

  /**
   * 生成 OAuth 第三方登录跳转 URL
   * @param provider OAuth 提供商名，如 'github'、'wechat'
   * @param returnUrl 登录成功后回跳的前端路由，默认为当前页面
   */
  buildOAuthUrl(provider: string, returnUrl?: string): string {
    const base = (this.api.client.defaults.baseURL ?? '').replace(/\/$/, '');
    const r = encodeURIComponent(returnUrl ?? window.location.href);
    return `${base}/Sso/Login/${provider}?r=${r}`;
  }
}

// 重新导出类型供适配器使用
export type { UserInfo, MenuItem, CubeApi, ResetPasswordModel, RegisterModel, OAuthPendingInfo } from '@cube/api-core';
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

// ─────────────────────────────────────────────
// 注册业务逻辑
// ─────────────────────────────────────────────

/** 注册状态 */
export interface RegisterState {
  sending: boolean;
  submitting: boolean;
  countdown: number;
  error: string;
  oauthPending: OAuthPendingInfo | null;
}

/** 注册状态变更回调 */
export type RegisterStateUpdater = (partial: Partial<RegisterState>) => void;

/** 注册逻辑（密码注册/验证码注册/OAuth回跳注册） */
export class RegisterLogic {
  private api: CubeApi;
  private update: RegisterStateUpdater;
  private _timer: ReturnType<typeof setInterval> | null = null;

  state: RegisterState = {
    sending: false,
    submitting: false,
    countdown: 0,
    error: '',
    oauthPending: null,
  };

  constructor(api: CubeApi, update: RegisterStateUpdater) {
    this.api = api;
    this.update = update;
  }

  private setState(partial: Partial<RegisterState>) {
    Object.assign(this.state, partial);
    this.update(partial);
  }

  async sendRegisterCode(username: string, channel: 'Sms' | 'Mail') {
    if (!username) {
      this.setState({ error: channel === 'Sms' ? '请输入手机号' : '请输入邮箱' });
      return false;
    }

    this.setState({ sending: true, error: '' });
    try {
      await this.api.user.sendCode({ channel, username, action: 'register' });
      this.setState({ sending: false });
      this._startCountdown();
      return true;
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
      this.setState({ sending: false, error: msg });
      return false;
    }
  }

  async loadOAuthPendingInfo(token: string) {
    if (!token) {
      this.setState({ error: 'oauthToken不能为空' });
      return null;
    }

    this.setState({ error: '' });
    try {
      const res = await this.api.user.getOAuthPendingInfo(token);
      const data = res.data ?? null;
      this.setState({ oauthPending: data });
      return data;
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '获取OAuth预填信息失败';
      this.setState({ error: msg, oauthPending: null });
      return null;
    }
  }

  async register(model: RegisterModel) {
    if (!model.password) {
      this.setState({ error: '请输入密码' });
      return false;
    }

    const confirmPassword = model.confirmPassword ?? model.password2;
    if (!confirmPassword) {
      this.setState({ error: '请输入确认密码' });
      return false;
    }
    if (model.password !== confirmPassword) {
      this.setState({ error: '两次密码输入不一致' });
      return false;
    }

    if ((model.registerCategory ?? RegisterCategory.Password) === RegisterCategory.Phone && !model.code) {
      this.setState({ error: '请输入手机验证码' });
      return false;
    }
    if ((model.registerCategory ?? RegisterCategory.Password) === RegisterCategory.Email && !model.code) {
      this.setState({ error: '请输入邮箱验证码' });
      return false;
    }
    if ((model.registerCategory ?? RegisterCategory.Password) === RegisterCategory.OAuthBind && !model.oauthToken) {
      this.setState({ error: 'oauthToken不能为空' });
      return false;
    }

    this.setState({ submitting: true, error: '' });
    try {
      const payload: RegisterModel = {
        ...model,
        confirmPassword,
      };
      const res = await this.api.user.register(payload);
      if (res.data?.accessToken) this.api.tokenManager.setToken(res.data.accessToken);
      this.setState({ submitting: false });
      return true;
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '注册失败，请稍后重试';
      this.setState({ submitting: false, error: msg });
      return false;
    }
  }

  reset() {
    this._clearCountdown();
    this.setState({ sending: false, submitting: false, countdown: 0, error: '', oauthPending: null });
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
