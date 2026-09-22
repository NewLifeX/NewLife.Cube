/**
 * HTTP请求工具
 *
 * 底层复用 @newlifex/api-core 的 createCubeApi（内部 createApiClient），非 UI 的请求逻辑（host 拼接、
 * /api 前缀补全、Token 头注入、附加请求头、withCredentials、content-type 透传、traceId、204 处理、
 * 错误分类归一化、响应钩子 responseInterceptor）已全部迁移至 api-core，所有皮肤共享。
 *
 * 本文件保留 cube-vue 特有、与 UI 强相关的逻辑：
 *   1. 401 跳转 / 导航（handleUnauthorized、redirectToLogin）；
 *   2. 错误与字段/业务错误的弹窗展示（onFieldError / onBusinessError / onResponseError 回调）。
 * 其余配置（additionalRequestHeaders / requestInterceptor / responseInterceptor）均以回调形式接线，
 * 机制在 api-core，取值来自 cube-vue 配置系统。
 *
 * 统一解包模型：createCubeApi 使用 unwrapResponse:false，使 createRequest 封装(page/user/menu/config)
 * 返回统一业务体 ApiResponse，调用方只需访问 res.data，杜绝 res.data.data 重复解包。
 * 默认导出的 request 对 client 做薄封装（Proxy），request(config)/request.get/post 自动取 response.data，
 * 与旧 unwrap:true 行为一致，既有业务调用点零改动。
 *
 * 对外导出（request / cubeAxios / client / user / menu / page / config / tokenManager / toReLogin）
 * 保持兼容，业务文件无需改动。useCubeApi 复用本实例，不再单独创建 createCubeApi。
 */
import { createCubeApi, type TokenStorage, type ResponseErrorInfo } from '@newlifex/api-core';
import type { AxiosInstance, AxiosResponse } from 'axios';
import { ElMessage } from 'element-plus';
import queryString from 'query-string';
import { getSession, removeAllCookie, setSession } from './storage';
import { getAccessToken, removeAccessToken } from './token';
import { getConfig } from '../configure';
import { gotoPage } from './router';
import notification from '../components/Notification';
import { intl } from '../i18n';

const {
  request: { baseUrl: API_HOST },
  auth: { oauthUrl, reLoginParams },
} = getConfig();

const loginPageUrl = reLoginParams?.loginPageUrl || '/login';

// 常量定义
const BASE_PATH = '';
const INDEX_ROUTE_PATH = '/';

// 自定义 Token 存储：复用 cube-vue 的 token 模块（getAccessToken / removeAccessToken）
const cubeTokenStorage: TokenStorage = {
  getToken: () => getAccessToken(),
  setToken: () => { /* cube-vue 登录流程自行写入，此处不处理 */ },
  clearToken: () => removeAccessToken(),
};

/**
 * 重定向到登录页
 * @param options.loginPageUrl - 可选的登录页URL
 */
export function redirectToLogin({ loginPageUrl: loginPageUrl2 }: { loginPageUrl?: string; } = {}) {
  removeAccessToken();
  removeAllCookie();

  // oauthUrl 为 /Sso/Login 等无 /api 前缀的服务地址，直接使用
  const LOGIN_URL = loginPageUrl2 || loginPageUrl || oauthUrl || '/Sso/Login';
  console.log('redirectToLogin', LOGIN_URL);

  const sessionData = getSession('redirectUrl');
  let cacheLocation = sessionData;
  if (!cacheLocation) {
    cacheLocation = encodeURIComponent(`${window.location.origin}${BASE_PATH || '/'}`);
  }

  const loginPath = LOGIN_URL;

  // 构建重定向URL
  const redirectParams = getSession('templateParams') || '';
  if (loginPath.includes('?')) {
    gotoPage(`${loginPath}&redirect_uri=${cacheLocation}${redirectParams}`);
  } else {
    gotoPage(`${loginPath}?redirect_uri=${cacheLocation}${redirectParams}`);
  }
}

// 401错误标志，防止重复处理401
let isErrorFlag = false;

/**
 * 401 跳转处理（作为 api-core onUnauthorized 回调，接收当前请求 url）。
 * api-core 已清除 Token，此处仅负责跳转/防重/判断是否自身请求。
 */
function handleUnauthorized(url?: string) {
  // 避免重复处理401
  if (isErrorFlag) {
    return;
  }

  const setRedirectUrl = () => {
    let _cacheLocation = window.location.toString().replace('/unauthorized', '');
    const basePath = (window as unknown as { routerBase?: string; }).routerBase?.replace(/\/$/, '') || BASE_PATH;
    const url1 = new URL(_cacheLocation);
    let p = url1.pathname;
    if (basePath && p.startsWith(basePath)) {
      p = p.replace(basePath, '');
    }
    if (p === '/') {
      url1.pathname = `${basePath}${INDEX_ROUTE_PATH}`;
      _cacheLocation = url1.toString();
    }
    const cacheLocation = encodeURIComponent(_cacheLocation);
    const searchParams = queryString.parse(window.location.search)?.template;
    const templateParams = searchParams ? `&template=${searchParams}` : '';
    setSession('templateParams', templateParams);
    setSession('redirectUrl', cacheLocation);
  };

  isErrorFlag = true;
  const reqUrl = url ?? '';
  const {
    user: { getUserInfoAxiosConfig },
  } = getConfig();

  // 兜底默认值：正常由 getConfig().user.getUserInfoAxiosConfig() 动态覆盖。
  // 与 defaultConfig 保持一致指向外层统一用户接口 /Auth/Info，避免任何漏配场景回退到旧区域接口。
  let AUTH_SELF_URL = '/Auth/Info';
  if (typeof getUserInfoAxiosConfig === 'function') {
    const cfg = getUserInfoAxiosConfig();
    if (!(cfg instanceof Promise)) {
      AUTH_SELF_URL = cfg.url || AUTH_SELF_URL;
    }
  } else {
    AUTH_SELF_URL = getUserInfoAxiosConfig.url || AUTH_SELF_URL;
  }

  const isSelf401 = reqUrl.includes(AUTH_SELF_URL);

  if (isSelf401) {
    // 如果已经在登录页、loading 页或未授权页，不再重复跳转，避免 redirect_uri 嵌套增长
    const currentPath = window.location.pathname;
    if (currentPath === '/login' || currentPath === '/loading' || currentPath === '/unauthorized') {
      isErrorFlag = false;
      return;
    }
    setRedirectUrl();
    redirectToLogin();
    return;
  }

  const redirectToUnauthorized = (pageUrl = '/unauthorized') => {
    const language = intl.getLocale()?.replace('-', '_');
    if (!window.location.pathname.startsWith(`${BASE_PATH}${pageUrl.replace(/^\//, '')}`)) {
      setRedirectUrl();
    }
    gotoPage(`${pageUrl}?language=${language}${getSession('templateParams') || ''}`);
  };

  // 当位于/unauthorized页面时，不处理401
  const isInUnauthorizedPage = window.location.toString().indexOf('/unauthorized') !== -1;
  if (isInUnauthorizedPage) {
    return;
  }

  setTimeout(() => {
    // 如果当前在登录页、loading 页或未授权页，不跳转，避免循环跳转
    const currentPath = window.location.pathname;
    if (currentPath === '/login' || currentPath === '/loading' || currentPath === '/unauthorized') {
      return;
    }
    redirectToUnauthorized();
  }, 100);
}

/**
 * 错误弹窗（作为 api-core onResponseError 回调）。
 * api-core 已把网络/4xx/5xx 错误归一化为 ResponseErrorInfo（纯数据、无文案），
 * 此处负责本地化与展示——网络错误用中文提示，其余交给通知组件。
 */
function showErrorNotification(info: ResponseErrorInfo) {
  if (info.isNetwork) {
    notification.error({ message: intl.get('notification.network.typeError').d('网络请求异常') });
    return;
  }
  notification.autoNotification('error', info.message, info.description || undefined);
}

// 字段级校验错误去重标志：同一响应先经 onFieldError 弹出字段提示，则抑制紧随的 onBusinessError，避免重复弹窗
let fieldErrorShown = false;
const cubeConfig = getConfig();

// 创建统一的魔方 API 客户端（底层来自 @newlifex/api-core 的 createCubeApi）
// 内部派生两个客户端：entityClient(=client) 走 baseURL(API_HOST)；serviceClient(去掉 /api 前缀) 供 user/menu/config 使用。
// unwrapResponse:false：createRequest 封装(page/user/menu/config)返回统一业务体 ApiResponse，调用方只需访问 res.data。
// cube-vue 仅以回调接线 UI 行为（弹窗 / 401 跳转）与配置（附加头 / 请求钩子 / 响应钩子），其余下沉至 api-core。
const api = createCubeApi({
  baseURL: API_HOST,
  tokenStorage: cubeTokenStorage,
  tokenHeaderPrefix: 'bearer ',
  // 非 UI 逻辑（下沉至 api-core）：
  withCredentials: true,
  unwrapResponse: false,
  additionalRequestHeaders: () => {
    const cfg = cubeConfig.request.additionalRequestHeaders;
    if (!cfg) return {};
    return typeof cfg === 'function' ? cfg() : cfg;
  },
  onRequestHook: (config) => {
    const ri = cubeConfig.request.requestInterceptor;
    return ri ? ri(config) : config;
  },
  onResponseHook: (response) => {
    const ri = cubeConfig.request.responseInterceptor;
    if (ri) ri(response);
  },
  onUnauthorized: handleUnauthorized,
  // 字段级验证错误：统一 toast（原 useCubeApi 逻辑合并至 request，页面无需各自处理）
  onFieldError: (fieldErrors) => {
    fieldErrorShown = true;
    ElMessage.error(fieldErrors.map(e => e.message).join('；'));
  },
  // 业务错误：统一 toast；若同一响应已弹字段级错误或消息为空则跳过，微任务后重置标志
  onBusinessError: (_code, message) => {
    Promise.resolve()
      .then(() => {
        if (fieldErrorShown || !message) return;
        ElMessage.error(message);
      })
      .finally(() => {
        fieldErrorShown = false;
      });
  },
  onResponseError: showErrorNotification,
});

// 原始实体客户端（unwrapResponse:false，.get/.post 返回 AxiosResponse，供需要原生响应的场景使用）
const client = api.client;

// 默认导出的 request 沿用原语义：对 client 做薄封装（Proxy），request(config) / request.get/post 等
// 自动解包取 response.data，统一返回业务体 ApiResponse，与 page/user 语义一致，既有调用点零改动。
type LooseCallable = (...args: unknown[]) => Promise<AxiosResponse>;
const request = new Proxy(client, {
  apply: (target, thisArg, args) =>
    (target as unknown as LooseCallable).apply(thisArg, args).then((r) => r?.data),
  get: (target, prop, receiver) => {
    const value = Reflect.get(target, prop, receiver);
    if (typeof value === 'function' && prop !== 'then' && prop !== 'catch') {
      return (...args: unknown[]) =>
        (value as unknown as LooseCallable).apply(target, args).then((r) => r?.data);
    }
    return value;
  },
}) as AxiosInstance;

export default request;
export { request, client };
export const cubeAxios = client;
// user(认证) / menu(菜单) / page(通用 CRUD) / config(配置) / tokenManager(令牌)
export const { user, menu, page, config, tokenManager } = api;

// 替换原来导出的toReLogin
export { redirectToLogin as toReLogin };
