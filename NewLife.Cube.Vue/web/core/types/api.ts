import type { ApiResponse, ListResponse } from './common';

/** HTTP方法类型 */
export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';

/** 请求配置 */
export interface RequestConfig {
  url: string;
  method?: HttpMethod;
  params?: Record<string, unknown>;
  data?: Record<string, unknown>;
  headers?: Record<string, string>;
}

/** API操作回调类型 */
export type ApiCallback<T = unknown> = () => Promise<ApiResponse<T>>;

/** 列表API回调类型 */
export type ListApiCallback<T = unknown> = () => Promise<ApiResponse<ListResponse<T>>>;
