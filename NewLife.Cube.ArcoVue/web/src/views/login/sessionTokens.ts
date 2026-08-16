import cubeApi from '@/api';

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
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_KEY);
}

export function getTokenUserName(): string | null {
  return localStorage.getItem(USER_KEY);
}
