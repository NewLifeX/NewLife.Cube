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
        // 统一菜单接口：CubeController 服务动作（/Cube/MenuTree），
        // 后端返回 MenuItem 树（camelCase），按当前登录用户角色过滤
        url: '/Cube/MenuTree',
      };
    },
    // 字段映射对齐 @newlifex/api-core MenuItem（/Cube/MenuTree 返回结构，camelCase）
    isMenuTree: true,
    dataKey: 'data',
    idField: 'id',
    parentField: 'parentID',
    nameField: 'name',
    pathField: 'url',
    titleField: 'displayName',
    iconField: 'icon',
    // MenuItem 无 sort 字段，保留以符合 MenuConfig 必填约束，运行时取值为 undefined（不影响树构建）
    sortField: 'sort',
    childrenField: 'children',
    visibleField: 'visible',
  },
  user: {
    getUserInfoAxiosConfig: (): AxiosRequestConfig => {
      return {
        method: 'GET',
        // 统一用户接口：外层 AuthController 服务动作（/Auth/Info），无需再经 Admin 区域实体控制器
        url: '/Auth/Info',
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
    requestInterceptor: (config) => config,
  },
  auth: {
    tokenKey: 'token',
    refreshTokenKey: 'refresh_token',
    // 后面会自动拼接重定向地址即可，后台需要配置SsoSafeDomains，跳转地址允许白名单，否则不能正常重定向
    oauthUrl: '/Sso/Login?name=NewLife&source=front-end&redirect_uri=',
    redirectUrl: '/login',
    pageTitle: '登录',
    background: '',
    logoutAxiosConfig: (): AxiosRequestConfig => {
      return {
        // AuthController.Logout 声明 [HttpPost]，必须用 POST，GET 会返回 405
        method: 'POST',
        url: '/Auth/Logout',
      };
    },
    // 默认的重新登录参数
    reLoginParams: {
      loginPageUrl: '/login'
    },
  },
  router: {
    routeNamingStyle: 'pascal',
  },
};

export default defaultConfig;
