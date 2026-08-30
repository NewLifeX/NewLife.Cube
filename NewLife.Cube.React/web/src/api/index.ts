/**
 * 全局 API 客户端（对齐 Vue 皮肤 composables/useCubeApi.ts）
 *
 * 基于 @cube/api-core 创建统一实例，提供：
 * - 实体/页面接口（page/client）→ 自动补 /api 前缀
 * - 服务接口（user/menu/config：/Auth /Cube /Sso）→ 同源不带 /api
 * - 统一错误处理：字段级错误 / 业务错误 / 401 未授权
 */
import { createCubeApi, clearPageMetaCache, type CubeApi, type TokenStorage } from '@cube/api-core';
import { getConfig } from '@/configure';
import { getMessage } from '@/utils/antdApp';

/** Token 变更事件（登录/登出时派发，供响应式登录态使用） */
export const CUBE_TOKEN_EVENT = 'cube:token-change';

/** 从 Cookie 读取指定键（localStorage 丢失时回退） */
function readCookie(key: string): string | null {
  const match = document.cookie.match(new RegExp(`(?:^|;\\s*)${key}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

/**
 * 双通道 Token 存储：localStorage 优先 + Cookie 兜底
 *
 * 「保存密码」登录后 token 同时写入持久 Cookie（对齐 MVC 版写 365 天 Cookie 的行为），
 * 当 localStorage 被浏览器清理但 Cookie 保留时，仍可从 Cookie 恢复登录态免登录。
 */
const tokenStorage: TokenStorage = {
  getToken: () => localStorage.getItem('token') ?? readCookie('token'),
  setToken: (t, expireIn) => {
    localStorage.setItem('token', t);
    // 有有效期（保存密码=365 天）写持久 Cookie，否则会话级
    const exp = expireIn && expireIn > 0 ? `; max-age=${expireIn}` : '';
    document.cookie = `token=${encodeURIComponent(t)}; path=/; SameSite=Lax${exp}`;
    window.dispatchEvent(new Event(CUBE_TOKEN_EVENT));
  },
  clearToken: () => {
    localStorage.removeItem('token');
    document.cookie = 'token=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT';
    window.dispatchEvent(new Event(CUBE_TOKEN_EVENT));
  },
};

// Token 变更（登录/登出/切换账号）时清空页面元数据缓存，避免串用上一账号的页面配置
window.addEventListener(CUBE_TOKEN_EVENT, () => clearPageMetaCache());

// 同一响应若携带 fieldErrors，onFieldError 会弹出更精确的字段提示；
// 此处标记抑制紧随其后的 onBusinessError，避免"操作失败！xxx"与"xxx"重复弹出
let fieldErrorShown = false;

/**
 * 全局 CubeApi 实例
 */
export const api: CubeApi = createCubeApi({
  baseURL: getConfig().request.baseUrl,
  tokenStorage,
  tokenHeaderPrefix: getConfig().request.tokenHeaderPrefix ?? 'Bearer ',
  timeout: getConfig().request.timeout,
  // 接入附加请求头（如多租户 X-Tenant），实体/服务两条链均生效
  additionalRequestHeaders: () => {
    const extra = getConfig().request.additionalRequestHeaders;
    if (!extra) return {};
    return typeof extra === 'function' ? extra() : extra;
  },
  onFieldError: (fieldErrors) => {
    // 统一展示字段级验证错误（如"编码不可以为空！"）
    fieldErrorShown = true;
    getMessage().error(fieldErrors.map((e) => e.message).join('；'));
  },
  onBusinessError: (_code, msg) => {
    // 微任务中执行：若同一响应已通过 onFieldError 弹出字段级错误则跳过
    Promise.resolve()
      .then(() => {
        if (fieldErrorShown) return;
        getMessage().error(msg);
      })
      .finally(() => {
        fieldErrorShown = false;
      });
  },
  onUnauthorized: () => {
    // 401 → 清除 token 并跳转登录页（避免在登录页重复跳转）
    const { pathname } = window.location;
    const { loginPageUrl } = getConfig().auth;
    if (!pathname.startsWith(loginPageUrl)) {
      window.location.href = `${loginPageUrl}?r=${encodeURIComponent(pathname + window.location.search)}`;
    }
  },
});

export default api;
