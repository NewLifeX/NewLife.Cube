import type { EnvConfig } from '../core/configure/types';

/**
 * 通用配置（所有环境共享）
 */
export const config: EnvConfig = {
  base: {
    title: '魔方系统',
    footer: '版权所有 © 2025',
    logo: 'https://sso.newlifex.com/favicon.ico',
  },
  ui: {
    theme: {
      primaryColor: '#1890ff',
    },
  },
};
