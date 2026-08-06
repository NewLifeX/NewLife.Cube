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
    // 实体/页面接口带 /api 前缀；/Auth /Sso /Cube 等服务接口由请求层自动去掉 /api
    baseUrl: import.meta.env.VITE_API_URL || '/api',
  },
};
