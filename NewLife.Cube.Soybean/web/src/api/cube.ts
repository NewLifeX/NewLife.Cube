import { createCubeApi } from '@newlifex/api-core';
import type { UserInfo } from '@newlifex/api-core';
import { localStg } from '@/utils/storage';

/**
 * 魔方 API 客户端
 *
 * 复用 @newlifex/api-core：实体/页面接口带 `/api` 前缀，认证与菜单等
 * 服务接口（/Auth、/Cube、/Sso 等）直接走同源根路径。
 * Token 统一存放在 soybean 的 localStg 中（key: token / refreshToken）。
 */
export const cubeApi = createCubeApi({
  baseURL: '/api',
  // 魔方后端要求 bearer 前缀；Cookie 与 Authorization 双通道，/Cube/MenuTree 等 MVC 动作依赖登录 Cookie
  tokenHeaderPrefix: 'bearer ',
  withCredentials: true,
  tokenStorage: {
    getToken: () => localStg.get('token') || null,
    setToken: token => localStg.set('token', token),
    clearToken: () => localStg.remove('token')
  },
  additionalRequestHeaders: () => ({ 'Accept-Language': localStg.get('lang') || 'zh-CN' }),
  // 字段元数据含本地化标题，切换语言后需重新拉取
  metaCacheScope: () => `${localStg.get('token') || ''}|${localStg.get('lang') || 'zh-CN'}`,
  onUnauthorized: async () => {
    const { useAuthStore } = await import('@/store/modules/auth');
    await useAuthStore().resetStore();
  },
  onBusinessError: (_code, message) => {
    window.$message?.error(message);
  },
  onFieldError: fieldErrors => {
    window.$message?.error(fieldErrors.map(e => e.message).join('；'));
  },
  onResponseError: info => {
    if (info.status === 401) return;
    const message = info.description ? `${info.message}\n${info.description}` : info.message;
    window.$message?.error(message);
  }
});

/** 当前登录用户信息（魔方 /Auth/Info 原始结构） */
export type CubeUserInfo = UserInfo;
