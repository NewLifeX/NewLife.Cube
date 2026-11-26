// 选择跨会话保留，资格只由本次成功登录授予；清令牌时立即撤销。
const preferenceKey = 'cube.rememberLogin';
const eligibleKey = 'cube.tokenRefreshEligible';
const userKey = 'cube.tokenRefreshUser';
let generation = 0;

export const getRememberLogin = (): boolean => localStorage.getItem(preferenceKey) === 'true';
export function setRememberLogin(remember: boolean): void {
  localStorage.setItem(preferenceKey, String(remember));
  if (!remember) revokeRefreshSession();
}
export function revokeRefreshSession(): void {
  generation++;
  localStorage.removeItem(eligibleKey);
  localStorage.removeItem(userKey);
}
export function rememberLoginSession(remember: boolean, userName?: string): void {
  setRememberLogin(remember);
  if (remember) {
    localStorage.setItem(eligibleKey, 'true');
    if (userName) localStorage.setItem(userKey, userName);
  }
}
export const getRefreshUserName = (): string | null => localStorage.getItem(userKey);
export const isRefreshSessionEligible = (): boolean => getRememberLogin() && localStorage.getItem(eligibleKey) === 'true';
export const getRefreshGeneration = (): number => generation;
