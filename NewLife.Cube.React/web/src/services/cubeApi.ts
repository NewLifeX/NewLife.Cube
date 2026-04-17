/**
 * 魔方 API 统一入口（基于 @cube/api-core，桥接 React 皮肤 localStorage Token 存储）
 */
import { createCubeApi, type TokenStorage } from '@cube/api-core';
import { getToken, setToken, removeToken } from '@/utils/token';

const reactTokenStorage: TokenStorage = {
  getToken,
  setToken,
  clearToken: removeToken,
};

const cubeApi = createCubeApi({
  baseURL: API_URL,
  tokenStorage: reactTokenStorage,
  onUnauthorized: () => {
    removeToken();
    window.location.href = '/user/login';
  },
});

export default cubeApi;
