/**
 * HTTP请求工具
 *
 * 底层复用 @cube/api-core 的 createApiClient，非 UI 的请求逻辑（host 拼接、/api 前缀补全、
 * Token 头注入、附加请求头、withCredentials、content-type 透传、traceId、204 处理、
 * 错误分类归一化、响应钩子 responseIntercept）已全部迁移至 api-core，所有皮肤共享。
 *
 * 本文件仅保留 cube-vue 特有、与 UI 强相关的逻辑：
 *   1. 401 跳转 / 导航（handleUnauthorized、redirectToLogin）；
 *   2. 错误与业务错误的弹窗展示（onBusinessError、onResponseError 回调）。
 * 其余配置（additionalRequestHeaders / requestInterceptor / responseIntercept）均以回调形式接线，
 * 机制在 api-core，取值来自 cube-vue 配置系统。
 *
 * 对外导出（request / cubeAxios / redirectToLogin / toReLogin）保持兼容，业务文件无需改动。
 */
import { createApiClient, TokenManager, type TokenStorage, type ResponseErrorInfo } from '@cube/api-core';
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

  let AUTH_SELF_URL = '/Admin/User/Info';
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

// 创建统一的 axios 实例（底层来自 api-core，非 UI 逻辑已由 api-core 承担）
// cube-vue 仅以回调接线 UI 行为（弹窗 / 401 跳转）与配置（附加头 / 请求钩子 / 响应钩子）。
const cubeAxios = createApiClient({
  baseURL: API_HOST,
  tokenHeaderPrefix: 'bearer ',
  tokenManager: new TokenManager(cubeTokenStorage),
  // 非 UI 逻辑（下沉至 api-core）：
  withCredentials: true,
  additionalRequestHeaders: () => {
    const cfg = getConfig().request.additionalRequestHeaders;
    if (!cfg) return {};
    return typeof cfg === 'function' ? cfg() : cfg;
  },
  onRequestHook: (config) => {
    const ri = getConfig().request.requestInterceptor;
    return ri ? ri(config) : config;
  },
  onResponseHook: (response) => {
    const ri = getConfig().request.responseIntercept;
    if (ri) ri(response);
  },
  unwrapResponse: true,
  // UI 强相关（保留在 cube-vue）：
  onUnauthorized: handleUnauthorized,
  onBusinessError: (code, message) => {
    if (message) notification.error({ message });
  },
  onResponseError: showErrorNotification,
});

// 导出配置好的axios实例
export const request = cubeAxios;
export default request;
export { cubeAxios };

// 替换原来导出的toReLogin
export { redirectToLogin as toReLogin };
