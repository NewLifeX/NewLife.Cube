/**
 * 全局 API 客户端（对齐 Vue 皮肤 composables/useCubeApi.ts）
 *
 * 基于 @cube/api-core 创建统一实例，提供：
 * - 实体/页面接口（page/client）→ 自动补 /api 前缀
 * - 服务接口（user/menu/config：/Auth /Cube /Sso）→ 同源不带 /api
 * - 统一错误处理：字段级错误 / 业务错误 / 401 未授权
 */
import { createCubeApi, type CubeApi } from '@cube/api-core';
import { getConfig } from '@/configure';
import { message } from 'antd';

// 同一响应若携带 fieldErrors，onFieldError 会弹出更精确的字段提示；
// 此处标记抑制紧随其后的 onBusinessError，避免"操作失败！xxx"与"xxx"重复弹出
let fieldErrorShown = false;

/**
 * 全局 CubeApi 实例
 */
export const api: CubeApi = createCubeApi({
  baseURL: getConfig().request.baseUrl,
  tokenStorage: getConfig().request.tokenStorage ?? 'localStorage',
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
    message.error(fieldErrors.map((e) => e.message).join('；'));
  },
  onBusinessError: (_code, msg) => {
    // 微任务中执行：若同一响应已通过 onFieldError 弹出字段级错误则跳过
    Promise.resolve()
      .then(() => {
        if (fieldErrorShown) return;
        message.error(msg);
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
