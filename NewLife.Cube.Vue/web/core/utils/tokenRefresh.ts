import axios from 'axios';
import { getConfig } from '../configure';
import { getCurrentLanguage } from '../i18n';
import { getAccessToken, getRefreshToken, setRefreshToken, updateAccessToken } from './token';
import { getRefreshGeneration, getRefreshUserName, isRefreshSessionEligible } from './tokenRefreshState';

export { getRememberLogin, setRememberLogin, rememberLoginSession } from './tokenRefreshState';

/** 可选刷新策略；独立客户端避免刷新请求递归进入认证重试。 */
export function createTokenRefreshPolicy() {
  return {
    isEnabled: () => getConfig().auth.tokenRefresh?.enabled === true && isRefreshSessionEligible() && !!getRefreshToken() && !!getRefreshUserName(),
    getSessionKey: getRefreshGeneration,
    async refresh(): Promise<void> {
      const cfg = getConfig();
      const generation = getRefreshGeneration();
      const previousToken = getAccessToken();
      const refreshToken = getRefreshToken();
      const userName = getRefreshUserName();
      if (!refreshToken || !userName || !isRefreshSessionEligible()) throw new Error('Refresh session unavailable');
      // 保留反向代理路径，仅移除末尾实体 /api 前缀。
      const base = cfg.request.baseUrl.replace(/\/+$/, '').replace(/\/api$/i, '');
      const extra = cfg.request.additionalRequestHeaders;
      const headers = typeof extra === 'function' ? extra() : extra;
      const response = await axios.post(`${base}/Auth/Refresh`, { refreshToken, userName }, {
        timeout: cfg.request.timeout || 10000,
        withCredentials: true,
        headers: { 'Accept-Language': getCurrentLanguage(), ...headers },
      });
      const body = response.data;
      const data = body?.data;
      const token = data?.accessToken ?? data?.access_token ?? data?.Token;
      const nextRefresh = data?.refreshToken ?? data?.refresh_token ?? data?.RefreshToken;
      if (!String(response.headers['content-type'] || '').toLowerCase().includes('json') || body?.code !== 0 || typeof token !== 'string' || !token) {
        throw new Error('Invalid refresh response');
      }
      // 退出、换用户、取消勾选后，迟到结果不能复活旧登录。
      if (generation !== getRefreshGeneration() || previousToken !== getAccessToken() || refreshToken !== getRefreshToken() || !isRefreshSessionEligible()) {
        throw new Error('Refresh session changed');
      }
      updateAccessToken(token);
      if (typeof nextRefresh === 'string' && nextRefresh) setRefreshToken(nextRefresh);
    },
  };
}
