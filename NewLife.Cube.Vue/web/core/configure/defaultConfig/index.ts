import type { CubeFrontConfig } from '../types';
import type { AxiosRequestConfig } from 'axios';

export const defaultConfig: CubeFrontConfig = {
  base: {
    title: '魔方系统',
    logo: '/logo.png',
    footer: '版权所有',
    env: 'dev',
  },
  menu: {
    getMenuAxiosConfig: () => {
      return {
        method: 'GET',
        url: '/Admin/Menu',
      };
    },
    isMenuTree: false,
    dataKey: 'data',
    idField: 'id',
    parentField: 'parentID',
    nameField: 'title',
    pathField: 'url',
    titleField: 'displayName',
    iconField: 'icon',
    sortField: 'sort',
    childrenField: 'children',
  },
  user: {
    getUserInfoAxiosConfig: (): AxiosRequestConfig => {
      return {
        method: 'GET',
        url: '/Admin/User/Info',
      };
    },
  },
  ui: {
    layout: {
      header: {
        show: true,
        fixed: true,
        theme: 'light',
      },
      sider: {
        show: true,
        collapsible: true,
        defaultCollapsed: false,
        width: 200,
        collapsedWidth: 80,
        theme: 'light',
      },
      footer: {
        show: true,
        fixed: false,
      },
    },
    theme: {
      primaryColor: '#1890ff',
      linkColor: '#1890ff',
      successColor: '#52c41a',
      warningColor: '#faad14',
      errorColor: '#f5222d',
      font: {
        baseSize: 14,
        family:
          '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
      },
    },
  },
  request: {
    baseUrl: '',
    timeout: 10000,
    responseIntercept: (response) => response,
  },
  auth: {
    tokenKey: 'token',
    // 后面会自动拼接重定向地址即可，后台需要配置SsoSafeDomains，跳转地址允许白名单，否则不能正常重定向
    oauthUrl: '/Sso/Login?name=NewLife&source=front-end&redirect_uri=',
    redirectUrl: '/login',
    pageTitle: '登录',
    background: '',
    logoutAxiosConfig: (): AxiosRequestConfig => {
      return {
        method: 'GET',
        url: '/Admin/User/Logout',
      };
    },
    reLoginParams: {
      // 默认的重新登录参数
    },
  },
};

export default defaultConfig;
