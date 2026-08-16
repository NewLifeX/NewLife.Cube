import { createCubeApi } from '@cube/api-core';
import { clearLocalProfile } from '@/core/utils/userProfile';
import { resolveTenantHeader } from '@/stores/tenantHeader';
import { clearSession, getRefreshToken, getTokenUserName, persistSession } from '@/views/login/sessionTokens';

async function tryRefreshAccessToken(): Promise<string | null> {
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
  tryRefreshToken: tryRefreshAccessToken,
  onUnauthorized() {
    clearSession();
    clearLocalProfile();
    if (!window.location.pathname.startsWith('/login')) {
      window.location.href = '/login';
    }
  },
  // 业务/字段错误由调用方 formatApiError 展示，避免与页面重复 toast
});

export default cubeApi;
