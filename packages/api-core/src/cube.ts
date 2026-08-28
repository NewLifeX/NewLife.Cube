import type { AxiosInstance } from 'axios';
import { createApiClient, createRequest, type ApiClientOptions } from './client';
import { TokenManager, type TokenStorage } from './token';
import { createUserApi, createMenuApi, createPageApi, createConfigApi, createProfileApi, createCommentApi, createAutomationApi, createWidgetApi, createWorkbenchApi } from './api';
import { getServiceBaseUrl } from './service-path';

export interface CubeApiOptions extends ApiClientOptions {
  /** Token 存储方式，默认 cookie */
  tokenStorage?: TokenStorage | 'cookie' | 'localStorage';
}

export interface CubeApi {
  /** 底层 axios 实例（实体/页面接口，带 baseURL 如 /api） */
  client: AxiosInstance;
  /** Token 管理器 */
  tokenManager: TokenManager;
  /** 用户认证 API（/Auth/*，后端无 /api 前缀） */
  user: ReturnType<typeof createUserApi>;
  /** 菜单 API（/Cube/* 服务，后端无 /api 前缀） */
  menu: ReturnType<typeof createMenuApi>;
  /** 通用 CRUD 及数据操作 API（实体/页面，带 /api 前缀） */
  page: ReturnType<typeof createPageApi>;
  /** 系统配置 API（/Auth /Cube 服务，后端无 /api 前缀） */
  config: ReturnType<typeof createConfigApi>;
  /** 用户呈现配置 API */
  profile: ReturnType<typeof createProfileApi>;
  /** 实体评论 API */
  comment: ReturnType<typeof createCommentApi>;
  /** 实体自动化流程 API */
  automation: ReturnType<typeof createAutomationApi>;
  /** 页面仪表盘 Widget API（OSC-2608280e9e） */
  widget: ReturnType<typeof createWidgetApi>;
  /** 首页工作台 API（OSC-26082815a1） */
  workbench: ReturnType<typeof createWorkbenchApi>;
}

/**
 * 一键创建完整的魔方 API 客户端
 *
 * 实体/页面接口（page、client）使用 baseURL（如 /api）拼接 /api 前缀，
 * 服务接口（user/menu/config：/Auth、/Cube、/Sso 等）走同源、不带 /api 前缀。
 *
 * @example
 * ```ts
 * const api = createCubeApi({ baseURL: '/api' });
 * const { data } = await api.user.login({ username: 'admin', password: '123' });
 * api.tokenManager.setToken(data.token);
 * ```
 */
export function createCubeApi(options: CubeApiOptions = {}): CubeApi {
  const { tokenStorage, ...clientOpts } = options;
  const tokenManager = new TokenManager(tokenStorage);

  // 实体/页面接口：带 baseURL（http://host 或 http://host/api，对应后端实体路由 /api/{area}/...）
  const entityClient = createApiClient({ ...clientOpts, tokenManager });
  // 服务接口（Auth/Cube/Sso/Mfa，后端路由不带 /api）：由实体 baseURL 去掉 /api 路径前缀派生。
  // 跨域部署 http://host/api → http://host；同域部署 /api 或 '' → ''
  const serviceClient = createApiClient({
    ...clientOpts,
    baseURL: getServiceBaseUrl(options.baseURL),
    tokenManager,
  });

  const request = createRequest(entityClient);
  const serviceRequest = createRequest(serviceClient);

  return {
    client: entityClient,
    tokenManager,
    user: createUserApi(serviceRequest),
    menu: createMenuApi(serviceRequest),
    // 导出下载 URL 不走 axios，需直接拼完整地址；createPageApi 内部经 resolveRequestUrl 统一补 /api
    page: createPageApi(request, options.baseURL),
    config: createConfigApi(serviceRequest),
    // UserProfile / ViewProfile / EntityComment 挂在 CubeController（无 /api）
    profile: createProfileApi(serviceRequest),
    comment: createCommentApi(serviceRequest),
    automation: createAutomationApi(serviceRequest),
    widget: createWidgetApi(serviceRequest),
    workbench: createWorkbenchApi(serviceRequest),
  };
}
