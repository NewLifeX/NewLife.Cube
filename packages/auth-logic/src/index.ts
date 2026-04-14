/**
 * @cube/auth-logic — 魔方前端认证业务逻辑（框架无关核心）
 *
 * 将登录、登出、用户信息获取、菜单加载、权限查询等业务逻辑
 * 封装为框架无关的纯逻辑类，各框架通过适配器（Pinia/Zustand/Svelte）桥接。
 */

import type { CubeApi, UserInfo, MenuItem } from '@cube/api-core';
import { findMenu, getMenuPermission } from '@cube/page-utils';

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

  /** 密码登录 */
  async login(username: string, password: string) {
    const res = await this.api.user.login({ username, password });
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
export type { UserInfo, MenuItem, CubeApi } from '@cube/api-core';
export { findMenu, getMenuPermission, checkAuth, Auth } from '@cube/page-utils';
export type { AuthCode } from '@cube/page-utils';
