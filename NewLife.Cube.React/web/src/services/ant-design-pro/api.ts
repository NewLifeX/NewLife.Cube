// @ts-ignore
/* eslint-disable */
/**
 * 认证及用户相关 API — 薄包装层（委托到 @cube/api-core cubeApi）
 *
 * 保持原有函数签名不变，方便存量页面零改动迁移。
 */
import cubeApi from '@/services/cubeApi';

export async function currentUser() {
  return cubeApi.user.info();
}

export async function outLogin() {
  return cubeApi.user.logout();
}

export async function login(body: { username: string; password: string; challengeId?: string }) {
  return cubeApi.user.login(body);
}

export async function queryMenus() {
  return cubeApi.menu.getMenuTree();
}

export async function getLoginConfig() {
  return cubeApi.user.getLoginConfig();
}

export async function getSiteInfo() {
  return cubeApi.user.getSiteInfo();
}

export async function sendCode(body: { channel: string; username: string; action?: string }) {
  return cubeApi.user.sendCode(body);
}

export async function loginByCode(body: { username: string; password: string; loginCategory: any }) {
  return cubeApi.user.loginByCode(body);
}

export async function register(body: any) {
  return cubeApi.user.register(body);
}

export async function resetPassword(body: any) {
  return cubeApi.user.resetPassword(body);
}

export async function getOAuthPendingInfo(token: string) {
  return cubeApi.user.getOAuthPendingInfo(token);
}

export async function getChallenge() {
  return cubeApi.user.getChallenge();
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function queryIndex(_options?: any) { return {}; }

// ——— 以下为 Ant Design Pro 模板残留，保留签名避免编译报错 ———

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function getNotices(_options?: any) { return { data: [] }; }
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function rule(_params?: any, _options?: any) { return { data: { list: [] } }; }
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function updateRule(_options?: any) { return {}; }
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function addRule(_options?: any) { return {}; }
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function removeRule(_options?: any) { return {}; }
