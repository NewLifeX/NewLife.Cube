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
    baseUrl: import.meta.env.VITE_API_URL, //魔方后台API地址
  },
};
