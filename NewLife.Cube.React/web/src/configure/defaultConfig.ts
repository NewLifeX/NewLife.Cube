/**
 * 默认配置（对齐 Vue 皮肤 configure/defaultConfig，按 React 皮肤需要精简）
 */
import type { CubeConfig } from './types';

export const defaultConfig: CubeConfig = {
  base: {
    title: '魔方系统',
    logo: '',
    footer: '版权所有',
    env: 'dev',
  },
  request: {
    // .env 中 VITE_API_URL 注入；dev 为 /api（走 Vite 代理），prod 为空（同源）
    baseUrl: (import.meta.env.VITE_API_URL as string) ?? '',
    timeout: 50000,
    tokenStorage: 'localStorage',
    tokenHeaderPrefix: 'Bearer ',
  },
  auth: {
    loginPageUrl: '/login',
    redirectKey: 'r',
  },
  ui: {
    layout: {
      header: {
        show: true,
        fixed: true,
        theme: 'light',
        height: 56,
      },
      sider: {
        show: true,
        collapsible: true,
        defaultCollapsed: false,
        width: 220,
        collapsedWidth: 64,
        theme: 'light',
      },
      footer: {
        show: false,
        fixed: false,
      },
    },
  },
  menu: {
    idField: 'id',
    parentField: 'parentID',
    nameField: 'name',
    pathField: 'url',
    titleField: 'displayName',
    iconField: 'icon',
    sortField: 'sort',
    childrenField: 'children',
    visibleField: 'visible',
  },
  theme: {
    defaultMode: 'dark',
    primaryColor: '#1677ff',
  },
};

export default defaultConfig;
