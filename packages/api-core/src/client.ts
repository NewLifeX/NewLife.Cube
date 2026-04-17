import axios, { type AxiosInstance, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios';
import { TokenManager } from './token';
import type { ApiResponse, LoginResult } from './types';
import qs from 'qs';

/**
 * 将后端可能返回的大写字段名（Token/RefreshToken/ExpireIn）归一化为小驼峰。
 * 部分版本后端直接返回 C# 属性名，需在客户端兼容。
 */
function normalizeLoginResult(data: LoginResult): LoginResult {
  return {
    accessToken: data.accessToken ?? data.Token ?? '',
    refreshToken: data.refreshToken ?? data.RefreshToken,
    expireIn: data.expireIn ?? data.ExpireIn,
  };
}

/** 需要归一化登录结果的 URL 路径后缀 */
const LOGIN_RESULT_PATHS = ['/Auth/Login', '/Auth/LoginByCode', '/Auth/Register', '/Auth/Refresh'];

export interface ApiClientOptions {
  /** API 基础路径，默认 '' （同源） */
  baseURL?: string;
  /** 请求超时毫秒数，默认 50000 */
  timeout?: number;
  /** 自定义 Token 管理器 */
  tokenManager?: TokenManager;
  /** 401 时的回调（默认触发 cube:unauthorized 事件） */
  onUnauthorized?: () => void;
  /** 业务错误回调（code 非 0） */
  onBusinessError?: (code: number, message: string) => void;
}

/**
 * 创建统一的 API 客户端实例
 * @param options 配置选项
 * @returns axios 实例
 */
export function createApiClient(options: ApiClientOptions = {}): AxiosInstance {
  const {
    baseURL = '',
    timeout = 50000,
    tokenManager,
    onUnauthorized,
    onBusinessError,
  } = options;
  const tm = tokenManager ?? new TokenManager();

  const client = axios.create({
    baseURL,
    timeout,
    headers: { 'Content-Type': 'application/json' },
    paramsSerializer: { serialize(params) { return qs.stringify(params, { allowDots: true }); } },
  });

  // 请求拦截：注入 Token
  client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const token = tm.getToken();
    if (token) {
      config.headers.set('Authorization', token);
    }
    return config;
  });

  // 响应拦截：统一错误处理 + 登录结果字段归一化
  client.interceptors.response.use(
    (response) => {
      const res = response.data as ApiResponse;
      if (res.code && res.code !== 0) {
        if (res.code === 401 || res.code === 4001) {
          tm.clearToken();
          if (onUnauthorized) {
            onUnauthorized();
          } else {
            window.dispatchEvent(new CustomEvent('cube:unauthorized'));
          }
        } else if (res.message && onBusinessError) {
          onBusinessError(res.code, res.message);
        }
        return Promise.reject(new Error(res.message ?? `API error: ${res.code}`));
      }
      // 归一化登录结果字段名（兼容后端大写 Token/RefreshToken/ExpireIn）
      const url = response.config?.url ?? '';
      if (LOGIN_RESULT_PATHS.some(p => url.endsWith(p)) && res.data && typeof res.data === 'object') {
        res.data = normalizeLoginResult(res.data as LoginResult);
      }
      return response;
    },
    (error) => {
      if (error.response?.status === 401 || error.response?.status === 403) {
        tm.clearToken();
        if (onUnauthorized) {
          onUnauthorized();
        } else {
          window.dispatchEvent(new CustomEvent('cube:unauthorized'));
        }
      }
      return Promise.reject(error);
    }
  );

  return client;
}

/**
 * 创建类型安全的请求函数
 * @param client axios 实例
 * @returns 泛型请求函数
 */
export function createRequest(client: AxiosInstance) {
  return <T = unknown>(config: AxiosRequestConfig): Promise<ApiResponse<T>> => {
    return client.request<ApiResponse<T>>(config).then(res => res.data);
  };
}
