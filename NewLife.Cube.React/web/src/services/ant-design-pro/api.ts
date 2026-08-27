// @ts-ignore
/* eslint-disable */
/**
 * 认证及用户相关 API — 薄包装层（委托到 @cube/api-core cubeApi）
 *
 * 保持原有函数签名不变，方便存量页面零改动迁移。
 */
import cubeApi from '@/services/cubeApi';
import { encryptPassword, type AuthCategory } from '@cube/api-core';

export async function currentUser() {
  return cubeApi.user.info();
}

export async function outLogin() {
  return cubeApi.user.logout();
}

export async function login(body: {
  username: string;
  password: string;
  type?: string;
  category?: AuthCategory;
  challengeId?: string;
  captchaId?: string;
  captchaCode?: string;
}) {
  // 密码登录时，提交前动态获取 RSA 公钥加密密码（与 @cube/auth-logic 语义一致，避免明文传输）
  // 手机/邮箱验证码登录传的是验证码，无需加密
  const category = body.category || body.type || 'account';
  if (category === 'account') {
    try {
      const cr = await cubeApi.user.getChallenge();
      if (cr.data?.publicKey) {
        const encrypted = await encryptPassword(body.password, cr.data.publicKey);
        if (encrypted) {
          body = { ...body, password: encrypted, challengeId: cr.data.challengeId };
        }
      }
    } catch {
      // Challenge 接口不可达或加密失败，降级明文（需服务端 AllowPlainPassword=true）
    }
  }
  return cubeApi.user.login(body);
}

export async function queryMenus() {
  return cubeApi.menu.getMenuTree();
}

export async function getLoginConfig() {
  return cubeApi.user.getLoginConfig();
}

export async function sendCode(body: { channel: string; username: string; action?: string }) {
  return cubeApi.user.sendCode(body);
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
