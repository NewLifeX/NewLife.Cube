import axios, {
  type AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios';
import { TokenManager } from './token';
import type { ApiResponse, LoginResult } from './types';
import { ApiError } from './types';
import { isServiceApiPath, resolveRequestUrl } from './service-path';
import qs from 'qs';

/**
 * 将后端可能返回的字段名归一化为小驼峰（accessToken）。
 * 后端版本差异较大，需同时兼容三种命名：
 * - snake_case：access_token / refresh_token / expire_in（Demo/新版常见）
 * - PascalCase：Token / RefreshToken / ExpireIn（C# 属性名原样）
 * - camelCase：accessToken / refreshToken / expireIn（标准）
 *
 * 注意：必须通过 raw 读取原始字段，不能直接用 data.accessToken，
 * 否则当后端仅返回 access_token 时（data 上无 accessToken 键）会丢失令牌。
 */
function normalizeLoginResult(data: LoginResult): LoginResult {
  const raw = data as unknown as Record<string, unknown>;
  return {
    accessToken:
      (raw.accessToken as string) ??
      (raw.access_token as string) ??
      (raw.Token as string) ??
      '',
    refreshToken:
      (raw.refreshToken as string) ??
      (raw.refresh_token as string) ??
      (raw.RefreshToken as string),
    expireIn:
      (raw.expireIn as number) ??
      (raw.expire_in as number) ??
      (raw.ExpireIn as number),
  };
}

/** 需要归一化登录结果的 URL 路径后缀 */
const LOGIN_RESULT_PATHS = ['/Auth/Login', '/Auth/Register', '/Auth/Refresh', '/Sso/WxMiniLogin', '/Sso/WxAppLogin'];

/**
 * 归一化后的错误描述（非 UI，纯数据）。由皮肤自行决定如何展示（弹窗/文案/本地化）。
 */
export interface ResponseErrorInfo {
  /** HTTP 状态码；网络错误（无响应）时为 undefined */
  status?: number;
  /** 是否为网络层错误（无响应 / ERR_NETWORK / Network Error） */
  isNetwork: boolean;
  /** 错误消息（优先取后端 data.message，否则 axios 原始消息） */
  message: string;
  /** 错误描述（来自 data.description / content / requestMessage + detailsMessage） */
  description: string;
  /** 原始响应体 */
  data?: unknown;
}

/**
 * 从 axios 错误中提取结构化错误描述（非 UI，纯逻辑）。
 * 具体文案与本地化由各皮肤的 onResponseError 回调负责。
 */
function buildResponseErrorInfo(error: AxiosError): ResponseErrorInfo {
  const { response } = error;

  // 网络层错误：无响应、ERR_NETWORK、或 axios 标记为 Network Error
  if (!response || error.code === 'ERR_NETWORK' || error.message === 'Network Error') {
    return {
      isNetwork: true,
      status: response?.status,
      message: error.message || 'Network Error',
      description: '',
      data: response?.data,
    };
  }

  const data = response.data as Record<string, unknown> | undefined;
  const info: ResponseErrorInfo = {
    isNetwork: false,
    status: response.status,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    message: error.message,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    description: `${data?.description || data?.content || (response as any)?.requestMessage || ''}`,
    data: response.data,
  };

  // 后端以 HTTP 200 但业务 code>200 返回错误时，取后端消息
  if (data && typeof data.code === 'number' && data.code > 200) {
    info.message = (data.message || data.code || data.type || '操作失败') as string;
  }
  if (data && typeof data.detailsMessage === 'string') {
    info.description += `\n ${data.detailsMessage}`;
  }
  return info;
}

export interface ApiClientOptions {
  /**
   * API 基础地址（含协议、主机、可选端口，可选带 /api 前缀），默认 ''（同源）。
   * 请求层按 resolveRequestUrl 规则拼接最终地址：
   * - 含 /api 前缀（如 http://host:5000/api）：实体请求保留 /api、服务接口去掉 /api、url 自带 /api 则去重；
   * - 不含 /api 前缀（如 http://host:5000，cube-vue 推荐传纯主机）：服务接口不加、非服务接口缺 /api 则补。
   */
  baseURL?: string;
  /** Authorization 头 token 前缀，默认 ''（如 'bearer '）。cube-vue 后端要求 'bearer '，其他皮肤可留空 */
  tokenHeaderPrefix?: string;
  /** 响应成功时是否解包为 response.data（即 ApiResponse 对象）而非完整响应，默认 false（向后兼容） */
  unwrapResponse?: boolean;
  /** 请求超时毫秒数，默认 50000 */
  timeout?: number;
  /** 自定义 Token 管理器 */
  tokenManager?: TokenManager;
  /** 是否携带跨域凭证（withCredentials），默认 false */
  withCredentials?: boolean;
  /** 附加请求头（静态对象或返回对象的函数），请求时合并进 headers */
  additionalRequestHeaders?: Record<string, string> | (() => Record<string, string>);
  /** 请求钩子：皮肤可注入自定义请求处理逻辑（在 token 注入与地址解析之后执行）。允许返回 Promise（同 axios 拦截器） */
  onRequestHook?: (config: InternalAxiosRequestConfig) => InternalAxiosRequestConfig | Promise<InternalAxiosRequestConfig>;
  /** 响应钩子（responseIntercept）：成功与失败响应均触发，传入完整 axios 响应 */
  onResponseHook?: (response: AxiosResponse) => void;
  /** 401 时的回调（默认触发 cube:unauthorized 事件）。url 为当前请求路径，便于上层判断是否为自身请求 */
  onUnauthorized?: (url?: string) => void;
  /** 业务错误回调（code 非 0） */
  onBusinessError?: (code: number, message: string) => void;
  /** 字段级验证错误回调（fieldErrors 非空时触发，用于统一 toast 提示） */
  onFieldError?: (fieldErrors: { field: string; message: string }[]) => void;
  /** 非 401 的响应错误回调（网络/4xx/5xx）。传入已归一化的错误描述，由皮肤决定如何展示 */
  onResponseError?: (info: ResponseErrorInfo) => void;
}

/**
 * 创建统一的 API 客户端实例
 * @param options 配置选项
 * @returns axios 实例
 */
export function createApiClient(options: ApiClientOptions = {}): AxiosInstance {
  const {
    baseURL = '',
    tokenHeaderPrefix = '',
    unwrapResponse = false,
    timeout = 50000,
    tokenManager,
    withCredentials = false,
    additionalRequestHeaders,
    onRequestHook,
    onResponseHook,
    onUnauthorized,
    onBusinessError,
    onFieldError,
    onResponseError,
  } = options;
  const tm = tokenManager ?? new TokenManager();

  const client = axios.create({
    baseURL,
    timeout,
    headers: { 'Content-Type': 'application/json' },
    paramsSerializer: { serialize(params) { return qs.stringify(params, { allowDots: true }); } },
  });

  // 请求拦截：注入 Token + 由 resolveRequestUrl 统一拼接 host 与 /api 前缀 + 附加头 + 钩子
  client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    // 实体 client 的 baseURL 多为 /api；Lookup/UserProfile 等 Cube 服务动作后端无此前缀
    if (config.url && isServiceApiPath(config.url)) {
      config.baseURL = '';
    }
    const token = tm.getToken();
    if (token) {
      config.headers.set('Authorization', `${tokenHeaderPrefix}${token}`);
    }
    // 统一解析最终地址：合并 baseURL 与 url，按 baseURL 是否含 /api 与 url 是否服务接口去重/补 /api
    config.url = resolveRequestUrl(config.baseURL ?? '', config.url ?? '');
    config.baseURL = '';

    if (withCredentials) {
      config.withCredentials = true;
    }
    // 合并附加请求头（config 既有头优先，附加头仅补充缺失键）
    if (additionalRequestHeaders) {
      const extra = typeof additionalRequestHeaders === 'function'
        ? additionalRequestHeaders()
        : additionalRequestHeaders;
      config.headers = { ...extra, ...(config.headers as Record<string, string>) } as InternalAxiosRequestConfig['headers'];
    }
    // 皮肤自定义请求钩子
    if (onRequestHook) {
      return onRequestHook(config);
    }
    return config;
  });

  // 响应拦截：统一错误处理 + 登录结果字段归一化 + content-type 透传 + traceId + 钩子
  client.interceptors.response.use(
    (response) => {
      const res = response.data as ApiResponse;
      if (res.code && res.code !== 0) {
        if (res.code === 401 || res.code === 4001) {
          tm.clearToken();
          if (onUnauthorized) {
            onUnauthorized(response.config?.url);
          } else {
            window.dispatchEvent(new CustomEvent('cube:unauthorized'));
          }
        } else {
          if (res.message && onBusinessError) {
            onBusinessError(res.code, res.message);
          }
          // 字段级验证错误：通过 onFieldError 回调统一 toast 提示
          if (res.fieldErrors && res.fieldErrors.length > 0 && onFieldError) {
            onFieldError(res.fieldErrors);
          }
          // 非 401 业务错误同样触发响应钩子（与下方错误路径一致），便于皮肤观察/统计响应
          if (onResponseHook) onResponseHook(response);
        }
        // 使用 ApiError 保留完整响应（含 fieldErrors），以便页面进行额外处理
        return Promise.reject(new ApiError(res as ApiResponse));
      }

      // 204 无内容：按成功返回 undefined（兼容文件下载/删除等场景）
      if (response.status === 204) {
        return undefined;
      }

      // 归一化登录结果字段名（兼容后端大写 Token/RefreshToken/ExpireIn）
      const url = response.config?.url ?? '';
      if (LOGIN_RESULT_PATHS.some(p => url.endsWith(p)) && res.data && typeof res.data === 'object') {
        res.data = normalizeLoginResult(res.data as LoginResult);
      }

      // content-type 透传：二进制与纯文本特殊处理后直接返回原始 data
      const contentType = (response.headers['content-type'] || '') as string;
      if (contentType === 'application/octet-stream' || contentType === 'arraybuffer') {
        return response.data;
      }
      if (contentType === 'text' && typeof response.data !== 'string') {
        return JSON.stringify(response.data);
      }

      // 跟踪编号日志（原 cube-vue 逻辑）
      if (res.traceId) {
        console.log('TraceId:', res.traceId);
      }

      // 响应钩子（responseIntercept）
      if (onResponseHook) {
        onResponseHook(response);
      }

      // 解包：直接返回 ApiResponse（response.data），与期望 response.data 的消费方兼容
      if (unwrapResponse) return response.data;
      return response;
    },
    (error) => {
      // 业务错误（ApiError，code 非 0）已由成功拦截器经 onBusinessError 处理并 reject，
      // 此处直接透传，避免重复弹窗或将 ApiResponse 误当 axios 响应传入 onResponseHook
      if (error instanceof ApiError) {
        return Promise.reject(error);
      }

      // 403=已登录但无权限，不应清除 Token 或跳转登录页，仅需提示无权限
      // 只有 401（未登录/登录过期）才清除 Token 并触发重新登录
      const { response } = error;
      if (response?.status === 401) {
        tm.clearToken();
        if (onUnauthorized) {
          onUnauthorized(error.config?.url);
        } else {
          window.dispatchEvent(new CustomEvent('cube:unauthorized'));
        }
      } else {
        // 响应钩子（responseIntercept）在错误路径同样触发
        if (response && onResponseHook) {
          onResponseHook(response);
        }
        // 归一化错误描述（非 UI 逻辑），交由皮肤做展示
        if (onResponseError) {
          onResponseError(buildResponseErrorInfo(error as AxiosError));
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
    return client.request<ApiResponse<T>>(config).then(res => res?.data);
  };
}
