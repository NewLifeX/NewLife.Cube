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
  request: {
    // 多租户：注入租户编码请求头。开启多租户后所有请求携带 X-Tenant，
    // 后端按租户编码定位租户并校验归属；未开启多租户时可不配置
    additionalRequestHeaders: () => ({ 'X-Tenant': '<租户编码>' }),
  },
};
