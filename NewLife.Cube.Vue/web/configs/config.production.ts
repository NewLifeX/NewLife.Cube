import type { EnvConfig } from '../core/configure/types';

/**
 * 生产环境特定配置
 */
export const config: EnvConfig = {
  base: {
    env: 'production',
    title: '魔方系统',
  },
  request: {
    baseUrl: 'https://cube.newlifex.com',
  },
};
