import { getConfig } from '../configure';
import { rememberLoginSession, revokeRefreshSession } from './tokenRefreshState';

export const NEW_ACCESS_TOKEN = 'token';
export const NEW_REFRESH_TOKEN = 'refresh_token';
export const REFRESH_TOKEN = 'refresh_token';

function getTokenName() {
  const config = getConfig();

  return config.auth.tokenKey || NEW_ACCESS_TOKEN;
}

/**
 * 从本地存储获取刷新令牌
 * @returns {string | null} 返回存储的刷新令牌，如果不存在则返回 null
 */
export const getRefreshToken = () => {
  return localStorage.getItem(NEW_REFRESH_TOKEN);
};

/**
 * 设置刷新令牌到本地存储
 * @param token 需要存储的刷新令牌字符串
 */
export function setRefreshToken(token: string) {
  localStorage.setItem(NEW_REFRESH_TOKEN, token);
}

/**
 * 从本地存储获取访问令牌
 * @returns {string | null} 返回存储的访问令牌，如果不存在则返回 null
 */
export const getAccessToken = () => {
  return localStorage.getItem(getTokenName());
};

/**
 * 从哈希中提取访问令牌
 * @returns 返回访问令牌字符串
 */
export const extractAccessTokenFromHash = () => {
  return getAccessToken();
};

/**
 * 抽取RefreshToken
 * @param {String} hash   hash值
 */
export function extractRefreshTokenFromHash(hash: string) {
  if (hash) {
    const ai = hash.indexOf(REFRESH_TOKEN);
    if (ai !== -1) {
      const refreshTokenReg = /#?refresh_token=[0-9a-zA-Z-]*/g;
      hash.match(refreshTokenReg);
      const centerReg = hash.match(refreshTokenReg)?.[0];
      const refreshToken = centerReg?.split('=')[1];
      return refreshToken;
    }
  }
  return null;
}

/**
 * 设置访问令牌到本地存储
 * @param token 需要存储的访问令牌字符串
 */
export function setAccessToken(token: string) {
  revokeRefreshSession();
  updateAccessToken(token);
}

/** 在当前刷新会话内更新访问令牌。调用前须校验会话仍有效。 */
export function updateAccessToken(token: string) {
  localStorage.setItem(getTokenName(), token);
}

/**
 * 移除本地存储中的访问令牌
 *
 * 从浏览器的 localStorage 中删除访问令牌。
 * 令牌名称从环境配置中获取，如果配置中未指定则使用默认值 NEW_ACCESS_TOKEN
 */
export function removeAccessToken() {
  revokeRefreshSession();
  localStorage.removeItem(getTokenName());
}

/** 移除旧刷新令牌，避免新登录继承上一账号的刷新凭据。 */
export function removeRefreshToken() {
  localStorage.removeItem(NEW_REFRESH_TOKEN);
}

/**
 * 从 URL 片段读取登录凭据，支持直接参数和路由后的认证片段。
 * @returns 访问令牌，未找到时返回 null
 */
export function getUrlHashToken() {
  const hash = window.location.hash;
  const fragment = hash.slice(hash.lastIndexOf('#') + 1);
  const query = fragment.includes('?') ? fragment.slice(fragment.indexOf('?') + 1) : fragment;
  const params = new URLSearchParams(query);
  const token = params.get(getTokenName());
  if (!token) return null;

  setAccessToken(token);
  const refreshToken = params.get(REFRESH_TOKEN);
  const userName = params.get('u');
  if (getConfig().auth.tokenRefresh?.enabled === true && refreshToken && userName) {
    setRefreshToken(refreshToken);
    rememberLoginSession(true, userName);
  } else {
    removeRefreshToken();
  }
  return token;
}
