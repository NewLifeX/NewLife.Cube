/**
 * 魔方 API 统一入口（基于 @cube/api-core，桥接 Vue 皮肤 Session 存储）
 *
 * 替代原有分散在 user.ts/page.ts/menu.ts 中的重复封装，
 * 皮肤层只保留 UI 壳与路由，业务逻辑上提到共享包。
 */

import { createCubeApi, type TokenStorage } from '@cube/api-core';
import { Session } from '/@/utils/storage';

/** 桥接 Vue 皮肤 sessionStorage Token 存储 */
const sessionTokenStorage: TokenStorage = {
  getToken(): string | null {
    return Session.get('token') as string | null;
  },
  setToken(token: string): void {
    Session.set('token', token);
  },
  clearToken(): void {
    Session.remove('token');
  },
};

/** 全局 CubeApi 实例（与皮肤 Session 共享 token） */
const cubeApi = createCubeApi({
  baseURL: import.meta.env.DEV ? '/base-api' : (import.meta.env.VITE_API_URL ?? ''),
  tokenStorage: sessionTokenStorage,
  onUnauthorized: () => {
    Session.clear();
    window.location.href = '/';
  },
});

export default cubeApi;
