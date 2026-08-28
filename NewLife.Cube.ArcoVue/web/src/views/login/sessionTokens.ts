import cubeApi from '@/api';
import { clearObjectKindCache } from '@/core/utils/objectKindCache';
import { clearPageMetaCache } from '@/core/utils/pageMetaCache';

const REFRESH_KEY = 'cube.refreshToken';
const USER_KEY = 'cube.tokenUserName';

/** 持久化登录会话（access cookie + refresh/userName localStorage） */
export function persistSession(accessToken: string, refreshToken?: string, userName?: string) {
  cubeApi.tokenManager.setToken(accessToken);
  if (refreshToken) localStorage.setItem(REFRESH_KEY, refreshToken);
  else localStorage.removeItem(REFRESH_KEY);
  if (userName) localStorage.setItem(USER_KEY, userName);
}

export function clearSession() {
  cubeApi.tokenManager.clearToken();
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(USER_KEY);
  // 探测缓存按菜单指纹绑定；登出清掉避免同标签切换账号残留
  clearObjectKindCache();
  clearPageMetaCache();
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_KEY);
}

export function getTokenUserName(): string | null {
  return localStorage.getItem(USER_KEY);
}
