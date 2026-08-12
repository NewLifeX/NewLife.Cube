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
    // 多租户：可在此注入租户编码请求头。开启多租户后所有请求携带 X-Tenant，
    // 后端按租户编码定位租户并校验归属；未配置时请求不带租户头（多租户开启时注册等接口会要求显式传租户）
    // 示例：additionalRequestHeaders: () => ({ 'X-Tenant': localStorage.getItem('tenantCode') ?? '' })
  },
};
