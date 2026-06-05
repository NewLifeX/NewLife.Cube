import { type RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  // 首页
  {
    path: '/',
    name: 'home',
    component: () => import('../pages/home/index.vue'),
  },
  // Cube 相关路由 - 只保留接口文档中存在的接口
  {
    path: '/Cube/Info',
    name: 'cube-info',
    component: () => import('../pages/cube/info/index.vue'),
  },
  {
    path: '/Cube/Apis',
    name: 'cube-apis',
    component: () => import('../pages/cube/apis/index.vue'),
  },
  {
    path: '/Cube/UserSearch',
    name: 'cube-user-search',
    component: () => import('../pages/cube/user-search/index.vue'),
  },
  {
    path: '/Cube/DepartmentSearch',
    name: 'cube-department-search',
    component: () => import('../pages/cube/department-search/index.vue'),
  },
  {
    path: '/Cube/GetArea',
    name: 'cube-get-area',
    component: () => import('../pages/cube/get-area/index.vue'),
  },
  {
    path: '/Cube/AreaChilds',
    name: 'cube-area-childs',
    component: () => import('../pages/cube/area-childs/index.vue'),
  },
  {
    path: '/Cube/AreaParents',
    name: 'cube-area-parents',
    component: () => import('../pages/cube/area-parents/index.vue'),
  },
  {
    path: '/Cube/AreaAllParents',
    name: 'cube-area-all-parents',
    component: () => import('../pages/cube/area-all-parents/index.vue'),
  },
  {
    path: '/Cube/Avatar',
    name: 'cube-avatar',
    component: () => import('../pages/cube/avatar/index.vue'),
  },
  {
    path: '/Cube/Lookup',
    name: 'cube-lookup',
    component: () => import('../pages/cube/lookup/index.vue'),
  },
  {
    path: '/Cube/SaveLayout',
    name: 'cube-save-layout',
    component: () => import('../pages/cube/save-layout/index.vue'),
  },
  {
    path: '/Cube/GetPageConfig',
    name: 'cube-get-page-config',
    component: () => import('../pages/cube/get-page-config/index.vue'),
  },
  {
    path: '/Cube/SetPageConfig',
    name: 'cube-set-page-config',
    component: () => import('../pages/cube/set-page-config/index.vue'),
  },
  {
    path: '/Cube/Image',
    name: 'cube-image',
    component: () => import('../pages/cube/image/index.vue'),
  },
  {
    path: '/Cube/File',
    name: 'cube-file',
    component: () => import('../pages/cube/file/index.vue'),
  },

  // SSO 相关路由 - 只保留接口文档中存在的接口
  {
    path: '/Sso/Login',
    name: 'sso-login',
    component: () => import('../pages/sso/login/index.vue'),
  },
  {
    path: '/Sso/LoginInfo/:id?',
    name: 'sso-login-info',
    component: () => import('../pages/sso/login-info/index.vue'),
  },
  {
    path: '/Sso/Logout',
    name: 'sso-logout',
    component: () => import('../pages/sso/logout/index.vue'),
  },
  {
    path: '/Sso/Bind',
    name: 'sso-bind',
    component: () => import('../pages/sso/bind/index.vue'),
  },
  {
    path: '/Sso/UnBind',
    name: 'sso-unbind',
    component: () => import('../pages/sso/unbind/index.vue'),
  },
  {
    path: '/Sso/Authorize',
    name: 'sso-authorize',
    component: () => import('../pages/sso/authorize/index.vue'),
  },
  {
    path: '/Sso/Auth2',
    name: 'sso-auth2',
    component: () => import('../pages/sso/auth2/index.vue'),
  },
  {
    path: '/Sso/Access_Token',
    name: 'sso-access-token',
    component: () => import('../pages/sso/access-token/index.vue'),
  },
  {
    path: '/Sso/Token',
    name: 'sso-token',
    component: () => import('../pages/sso/token/index.vue'),
  },
  {
    path: '/Sso/PasswordToken',
    name: 'sso-password-token',
    component: () => import('../pages/sso/password-token/index.vue'),
  },
  {
    path: '/Sso/UserInfo',
    name: 'sso-user-info',
    component: () => import('../pages/sso/user-info/index.vue'),
  },
  {
    path: '/Sso/Refresh_Token',
    name: 'sso-refresh-token',
    component: () => import('../pages/sso/refresh-token/index.vue'),
  },
  {
    path: '/Sso/Auth',
    name: 'sso-auth',
    component: () => import('../pages/sso/auth/index.vue'),
  },
  {
    path: '/Sso/GetKey',
    name: 'sso-get-key',
    component: () => import('../pages/sso/get-key/index.vue'),
  },
  {
    path: '/Sso/Verify',
    name: 'sso-verify',
    component: () => import('../pages/sso/verify/index.vue'),
  },
  {
    path: '/Sso/UserAuth',
    name: 'sso-user-auth',
    component: () => import('../pages/sso/user-auth/index.vue'),
  },
  {
    path: '/Sso/Avatar',
    name: 'sso-avatar',
    component: () => import('../pages/sso/avatar/index.vue'),
  },
];

export default routes;
