import { createCubeApi } from '@cube/api-core';
import { clearLocalProfile } from '@/core/utils/userProfile';
import { isEmbedMode } from '@/core/utils/embedMode';
import { clearTenantSession, resolveTenantHeader } from '@/stores/tenantHeader';
import { clearSession, getRefreshToken, getTokenUserName, persistSession } from '@/views/login/sessionTokens';

async function tryRefreshAccessToken(): Promise<string | null> {
  // 分享短令牌无 refreshToken，禁止走 Refresh 以免误清会话
  if (isEmbedMode()) return null;
  const refreshToken = getRefreshToken();
  const userName = getTokenUserName() || undefined;
  if (!refreshToken) return null;
  try {
    // 避免经拦截器递归：用原生 fetch 调 Refresh
    const res = await fetch('/Auth/Refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken, userName }),
    });
    const json = await res.json();
    if (!res.ok || (json.code && json.code !== 0)) return null;
    const data = json.data || {};
    const access =
      data.accessToken || data.AccessToken || data.Token || data.token;
    const refresh = data.refreshToken || data.RefreshToken;
    if (!access) return null;
    persistSession(access, refresh || refreshToken, userName);
    return access as string;
  } catch {
    return null;
  }
}

const cubeApi = createCubeApi({
  // WebAPI 版实体/后台接口固定 /api 前缀（如 /api/Admin/User/GetPage）；
  // /Auth /Cube 服务动作由 api-core 去掉此前缀
  baseURL: '/api',
  additionalRequestHeaders: () => {
    try {
      return resolveTenantHeader(sessionStorage.getItem('cube.tenant.code'));
    } catch {
      return {};
    }
  },
  onRequestHook(config) {
    // 实体写请求（POST/PUT/PATCH，带 /api 前缀）附带字段校验头（OSC-260819e483 P1）；
    // 读请求（GET）、服务接口（/Auth /Cube 评论/Profile 等，无 /api）不加，无头行为与今日一致
    const method = (config.method ?? '').toLowerCase();
    if ((method === 'post' || method === 'put' || method === 'patch') && (config.url ?? '').startsWith('/api/')) {
      const headers = config.headers as { set?: (k: string, v: string) => void } & Record<string, string>;
      if (typeof headers?.set === 'function') headers.set('X-Cube-Field-Validation', '1');
      else headers['X-Cube-Field-Validation'] = '1';
    }
    return config;
  },
  tryRefreshToken: tryRefreshAccessToken,
  onUnauthorized() {
    // 分享页：保留短令牌与 URL，避免 401 误跳登录导致白屏/丢参
    if (isEmbedMode()) return;
    clearSession();
    clearLocalProfile();
    clearTenantSession();
    try {
      sessionStorage.removeItem('cube.shareEmbed');
    } catch {
      /* ignore */
    }
    if (!window.location.pathname.startsWith('/login')) {
      window.location.href = '/login';
    }
  },
  // 业务/字段错误由调用方 formatApiError 展示，避免与页面重复 toast
});

export default cubeApi;
