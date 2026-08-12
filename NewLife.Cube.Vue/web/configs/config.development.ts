import type { EnvConfig } from '../core/configure/types';

/**
 * 开发环境特定配置
 */
export const config: EnvConfig = {
  base: {
    env: 'dev',
    title: '魔方系统',
  },
  request: {
    // 所有相对路径统一拼接 baseUrl（API_HOST）；接口自身决定路径是否带 /api 前缀，请求层不再按前缀区分服务/实体。
    baseUrl: import.meta.env.VITE_API_URL || '',
  },
};
