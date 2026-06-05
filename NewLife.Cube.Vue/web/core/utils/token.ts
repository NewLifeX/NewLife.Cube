import { getConfig } from '../configure';

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
  localStorage.setItem(getTokenName(), token);
}

/**
 * 移除本地存储中的访问令牌
 *
 * 从浏览器的 localStorage 中删除访问令牌。
 * 令牌名称从环境配置中获取，如果配置中未指定则使用默认值 NEW_ACCESS_TOKEN
 */
export function removeAccessToken() {
  localStorage.removeItem(getTokenName());
}

/**
 * 从 URL hash 中获取 token 参数
 *
 * 遍历 URL hash 中的所有参数，查找包含 'token' 的键
 * 如果找到 token，将其存储到 localStorage 中，并清空 URL hash
 *
 * @returns {string|null} 返回找到的 token，如果未找到则返回 null
 */
/**
 * 从 URL hash 中获取 token 参数
 *
 * 处理形如 #token=xxx 或 #/path#token=xxx 的哈希值
 * 如果找到 token，将其存储到 localStorage 中，并清空 URL hash
 *
 * @returns {string|null} 返回找到的 token，如果未找到则返回 null
 */
export function getUrlHashToken() {
  const tokenName = getTokenName();

  // 获取哈希值（不含#符号）
  let hash = window.location.hash;
  if (hash.startsWith('#')) {
    hash = hash.substring(1);
  }

  // 处理直接在哈希中的token（如 #token=xxx）
  const tokenRegex = new RegExp(`${tokenName}=([^&]+)`);
  const match = hash.match(tokenRegex);

  if (match && match[1]) {
    const token = match[1];
    localStorage.setItem(tokenName, token);
    return token;
  }

  return null;
}
