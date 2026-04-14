import type { AxiosInstance } from 'axios';
import { createApiClient, createRequest, type ApiClientOptions } from './client';
import { TokenManager, type TokenStorage } from './token';
import { createUserApi, createMenuApi, createPageApi, createConfigApi } from './api';

export interface CubeApiOptions extends ApiClientOptions {
  /** Token 存储方式，默认 cookie */
  tokenStorage?: TokenStorage | 'cookie' | 'localStorage';
}

export interface CubeApi {
  /** 底层 axios 实例 */
  client: AxiosInstance;
  /** Token 管理器 */
  tokenManager: TokenManager;
  /** 用户认证 API */
  user: ReturnType<typeof createUserApi>;
  /** 菜单 API */
  menu: ReturnType<typeof createMenuApi>;
  /** 通用 CRUD 及数据操作 API */
  page: ReturnType<typeof createPageApi>;
  /** 系统配置 API */
  config: ReturnType<typeof createConfigApi>;
}

/**
 * 一键创建完整的魔方 API 客户端
 *
 * @example
 * ```ts
 * const api = createCubeApi({ baseURL: '/base-api' });
 * const { data } = await api.user.login({ username: 'admin', password: '123' });
 * api.tokenManager.setToken(data.token);
 * ```
 */
export function createCubeApi(options: CubeApiOptions = {}): CubeApi {
  const { tokenStorage, ...clientOpts } = options;
  const tokenManager = new TokenManager(tokenStorage);
  const client = createApiClient({ ...clientOpts, tokenManager });
  const request = createRequest(client);

  return {
    client,
    tokenManager,
    user: createUserApi(request),
    menu: createMenuApi(request),
    page: createPageApi(request, options.baseURL),
    config: createConfigApi(request),
  };
}
