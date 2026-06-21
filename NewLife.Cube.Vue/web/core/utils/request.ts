/**
 * HTTP请求工具
 * 封装axios，处理请求拦截、响应拦截、错误处理和401授权问题
 */
import axios, {
  type AxiosError,
  type AxiosRequestHeaders,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios';
import queryString from 'query-string';
import { getSession, removeAllCookie, setSession } from './storage';
import { getAccessToken, removeAccessToken } from './token';
import { getConfig } from '../configure';
import { gotoPage } from './router';
import { getResponse } from './common';
import { type ApiResponse } from './response';
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

/**
 * 重定向到登录页
 * @param {Object} options - 配置选项
 * @param {string} options.loginPageUrl - 可选的登录页URL
 */
export function redirectToLogin({ loginPageUrl: loginPageUrl2 }: { loginPageUrl?: string; } = {}) {
  removeAccessToken();
  removeAllCookie();


  const LOGIN_URL = loginPageUrl2 || loginPageUrl || `${API_HOST}${oauthUrl}`;
  console.log('redirectToLogin', LOGIN_URL);

  const sessionData = getSession('redirectUrl');
  let cacheLocation = sessionData;
  if (!cacheLocation) {
    cacheLocation = encodeURIComponent(`${window.location.origin}${BASE_PATH || '/'}`);
  }

  //   });
  // }

  const loginPath = LOGIN_URL;

  // 构建重定向URL
  const redirectParams = getSession('templateParams') || '';
  if (loginPath.includes('?')) {
    gotoPage(`${loginPath}&redirect_uri=${cacheLocation}${redirectParams}`);
  } else {
    gotoPage(`${loginPath}?redirect_uri=${cacheLocation}${redirectParams}`);
  }
}

// 创建带token的axios实例
const cubeAxios = axios.create();

// 创建不带token的axios实例
const notWithTokenAxios = axios.create();
notWithTokenAxios.interceptors.request.use((config) => {
  const {
    request: { baseUrl: API_HOST },
  } = getConfig();
  let { url = '' } = config;
  if (url.indexOf('://') === -1 && !url.startsWith('/_api')) {
    url = `${API_HOST}${url}`;
  }
  return {
    ...config,
    url,
  };
});

// 401错误标志，防止重复处理401
let isErrorFlag = false;

/**
 * 鉴权拦截处理
 * @param {number} status - HTTP状态码
 * @param {InternalAxiosRequestConfig} config - 请求配置
 * @returns {boolean} - 是否继续处理响应
 */
function authIntercept(status: number, config: InternalAxiosRequestConfig) {
  if (status === 401) {
    // 避免重复处理401
    if (isErrorFlag) {
      return false;
    }

    /**
     * 设置重定向URL到会话存储
     */
    const setRedirectUrl = () => {
      let _cacheLocation = window.location.toString().replace('/unauthorized', '');
      // @ts-expect-error 全局窗口对象可能包含routerBase属性
      const basePath = (window.routerBase || BASE_PATH)?.replace(/\/$/, '');
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
    const { url = '' } = config;
    const {
      user: { getUserInfoAxiosConfig },
    } = getConfig();

    let AUTH_SELF_URL = '/Admin/User/Info';
    if (typeof getUserInfoAxiosConfig === 'function') {
      const config = getUserInfoAxiosConfig();
      if (!(config instanceof Promise)) {
        AUTH_SELF_URL = config.url || AUTH_SELF_URL;
      }
    } else {
      AUTH_SELF_URL = getUserInfoAxiosConfig.url || AUTH_SELF_URL;
    }

    const isSelf401 = url.includes(AUTH_SELF_URL);

    if (isSelf401) {
      setRedirectUrl();
      redirectToLogin();
      return false;
    }

    /**
     * 跳转到未授权页面
     * @param {string} pageUrl - 未授权页面路径
     */
    const redirectToUnauthorized = (pageUrl = '/unauthorized') => {
      const language = intl.getLocale()?.replace('-', '_');

      // 登录后需要跳回的界面，放到session中
      if (!window.location.pathname.startsWith(`${BASE_PATH}${pageUrl.replace(/^\//, '')}`)) {
        setRedirectUrl();
      }

      // token失效，跳转到token失效页面
      gotoPage(`${pageUrl}?language=${language}${getSession('templateParams') || ''}`);
    };

    // 当位于/unauthorized页面时，不处理401
    const isInUnauthorizedPage = window.location.toString().indexOf('/unauthorized') !== -1;
    if (isInUnauthorizedPage) {
      return false;
    }

    setTimeout(() => {
      redirectToUnauthorized();
    }, 100);
  }
  return true;
}

/**
 * 响应错误处理
 * @param {Object} error - 错误对象
 * @returns {Promise} - 处理后的Promise
 */
function handleResponseError(error: AxiosError) {
  // 移除debugger语句，优化错误日志记录
  console.error('Request error:', error.message, error.config?.url);
  const { response, code } = error;

  // 如果response为空，直接返回
  if (!response) {
    if (code === 'ERR_NETWORK') {
      notification.error({ message: intl.get('notification.network.typeError').d('网络请求异常') });
      return;
    }
    return Promise.reject(error);
  }

  const envConfig = getConfig();

  // 判断是否鉴权问题
  if (response && response.status && !authIntercept(response.status, response.config)) {
    return Promise.reject(false);
  }

  // 状态204当做成功处理
  if (response?.status === 204) {
    return undefined;
  }

  // 响应拦截，请求时设置
  const responseIntercept = envConfig.request.responseIntercept;
  if (responseIntercept && typeof responseIntercept === 'function') {
    responseIntercept(error);
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const data = response?.data as any;
  const errorObj = {
    type: undefined,
    message: error.message,
    description: `${data?.description || data?.content || response?.requestMessage || ''}`,
  };

  if (data && data.code > 200) {
    errorObj.message =
      data.message || data.code || data.type || intl.get('cube.notification.failed').d('操作失败');
    // errorObj.type === data.type;
  }

  if (data && data.detailsMessage) {
    errorObj.description += `\n ${data.detailsMessage}`;
  }

  if (errorObj.message === 'Network Error') {
    errorObj.message = intl.get('notification.network.typeError').d('网络请求异常');
    errorObj.description = intl.get('notification.typeError.description').d('请稍后重试');
  }

  if (response.status === 200) {
    getResponse(data);
  } else if (response.status) {
    notification.autoNotification(
      errorObj?.type || 'error',
      errorObj?.message,
      errorObj?.description,
    );
  }

  throw error;
}

/**
 * 处理成功响应
 * @param {Object} response - 响应对象
 * @returns {any} - 处理后的响应数据，直接返回data部分
 */
function handleResponseSuccess(response: AxiosResponse) {
  const { data, config, headers } = response;
  const contentType = headers['content-type'] || '';

  // 处理文件下载等二进制数据响应
  if (contentType === 'application/octet-stream' || contentType === 'arraybuffer') {
    return data;
  }

  if (contentType === 'text' && typeof data !== 'string') {
    return JSON.stringify(data);
  }

  // 响应拦截
  const responseIntercept = getConfig().request.responseIntercept;
  if (responseIntercept && typeof responseIntercept === 'function') {
    responseIntercept(response);
  }

  // 处理标准API响应格式
  if (data && typeof data === 'object' && 'code' in data) {
    const apiResponse = data as ApiResponse;

    // 输出 traceId 到控制台
    if (apiResponse.traceId) {
      console.log('TraceId:', apiResponse.traceId);
    }

    // 判断API调用是否成功
    const isSuccess = apiResponse.code === 0 || apiResponse.code === 200;

    if (isSuccess) {
      // 保持统一，返回原始数据，固定的响应结构，避免每个接口返回结构不一样
      return apiResponse;

      // // 如果有分页信息，返回包含data和page的对象
      // if (apiResponse.page) {
      //   return {
      //     data: apiResponse.data,
      //     page: apiResponse.page,
      //     stat: apiResponse.stat,
      //   };
      // }

      // // 无分页信息，直接返回data部分
      // return apiResponse.data;
    } else {
      // 失败：自动显示错误提示
      let errorMessage: string;
      if (apiResponse.message) {
        errorMessage = apiResponse.message;
      } else if (apiResponse.data && typeof apiResponse.data === 'string') {
        errorMessage = apiResponse.data;
      } else {
        errorMessage = '操作失败';
      }

      notification.error({ message: errorMessage });

      // 抛出错误，让业务代码能够在catch中处理
      throw new Error(errorMessage);
    }
  }

  // 非标准API响应，直接返回原始data
  return data;
}

cubeAxios.interceptors.response.use(handleResponseSuccess, handleResponseError);

/**
 * 请求拦截器
 * @param {Object} config - 请求配置
 * @returns {Object} - 处理后的请求配置
 */
function handleRequestConfig(config: InternalAxiosRequestConfig) {
  const {
    request: { baseUrl: API_HOST },
  } = getConfig();
  let { url = '' } = config || {};

  if (url.indexOf('://') === -1 && !url.startsWith('/_api')) {
    url = `${API_HOST}${url}`;
  }

  // 添加额外的请求头
  const additionalRequestHeaderConfig = getConfig().request.additionalRequestHeader;
  let additionalRequestHeader: Record<string, string> = {};
  if (additionalRequestHeaderConfig) {
    additionalRequestHeader =
      typeof additionalRequestHeaderConfig === 'function'
        ? additionalRequestHeaderConfig()
        : additionalRequestHeaderConfig;
  }

  const newOptions: InternalAxiosRequestConfig = {
    ...config,
    url,
    withCredentials: true,
    headers: {
      Authorization: `bearer ${getAccessToken()}`,
      ...additionalRequestHeader,
      ...config?.headers,
    } as AxiosRequestHeaders,
  };

  return newOptions;
}

/**
 * 请求错误处理
 * @param {Object} error - 错误对象
 * @returns {Promise} - 处理后的Promise
 */
async function handleRequestError(error: AxiosError) {
  console.error('Request processing error:', error.message);

  return Promise.reject(error);
}

cubeAxios.interceptors.request.use(handleRequestConfig, handleRequestError);

// 导出配置好的axios实例
export const request = cubeAxios;
export default request;
export { cubeAxios };

// 替换原来导出的toReLogin
export { redirectToLogin as toReLogin };
