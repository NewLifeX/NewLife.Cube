import { type RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  // 访问规则管理
  {
    path: '/Admin/AccessRule',
    name: 'admin-access-rule',
    component: () => import('../views/admin/access-rule/index.vue'),
  },
  // 基本设置
  {
    path: '/Admin/Core',
    name: 'admin-core',
    component: () => import('../views/admin/core/index.vue'),
  },
  // 魔方设置
  {
    path: '/Admin/Cube',
    name: 'admin-cube',
    component: () => import('../views/admin/cube/index.vue'),
  },
  // 数据库管理
  {
    path: '/Admin/Db',
    name: 'admin-db',
    component: () => import('../views/admin/db/index.vue'),
  },
  // 部门管理
  {
    path: '/Admin/Department',
    name: 'admin-department',
    component: () => import('../views/admin/department/index.vue'),
  },
  // 文件管理
  {
    path: '/Admin/File',
    name: 'admin-file',
    component: () => import('../views/admin/file/index.vue'),
  },
  // 系统首页
  {
    path: '/Admin/Index',
    name: 'admin-index',
    component: () => import('../views/admin/index/index.vue'),
  },
  // 审计日志
  {
    path: '/Admin/Log',
    name: 'admin-log',
    component: () => import('../views/admin/log/index.vue'),
  },
  // 菜单管理
  {
    path: '/Admin/Menu',
    name: 'admin-menu',
    component: () => import('../views/admin/menu/index.vue'),
  },
  // OAuth配置管理
  {
    path: '/Admin/OAuthConfig',
    name: 'admin-oauth-config',
    component: () => import('../views/admin/oauth-config/index.vue'),
  },
  // OAuth日志
  {
    path: '/Admin/OAuthLog',
    name: 'admin-oauth-log',
    component: () => import('../views/admin/oauth-log/index.vue'),
  },
  // 参数管理
  {
    path: '/Admin/Parameter',
    name: 'admin-parameter',
    component: () => import('../views/admin/parameter/index.vue'),
  },
  // 角色管理
  {
    path: '/Admin/Role',
    name: 'admin-role',
    component: () => import('../views/admin/role/index.vue'),
  },
  // 系统设置
  {
    path: '/Admin/Sys',
    name: 'admin-sys',
    component: () => import('../views/admin/sys/index.vue'),
  },
  // 租户管理
  {
    path: '/Admin/Tenant',
    name: 'admin-tenant',
    component: () => import('../views/admin/tenant/index.vue'),
  },
  // 用户管理
  {
    path: '/Admin/User',
    name: 'admin-user',
    component: () => import('../views/admin/user/index.vue'),
  },
  // 用户连接管理
  {
    path: '/Admin/UserConnect',
    name: 'admin-user-connect',
    component: () => import('../views/admin/user-connect/index.vue'),
  },
  // 租户用户管理
  {
    path: '/Admin/TenantUser',
    name: 'admin-tenant-user',
    component: () => import('../views/admin/tenant-user/index.vue'),
  },
  // 在线用户管理
  {
    path: '/Admin/UserOnline',
    name: 'admin-user-online',
    component: () => import('../views/admin/user-online/index.vue'),
  },
  // 用户统计
  {
    path: '/Admin/UserStat',
    name: 'admin-user-stat',
    component: () => import('../views/admin/user-stat/index.vue'),
  },
  // 用户令牌管理
  {
    path: '/Admin/UserToken',
    name: 'admin-user-token',
    component: () => import('../views/admin/user-token/index.vue'),
  },
  // XCode设置
  {
    path: '/Admin/XCode',
    name: 'admin-xcode',
    component: () => import('../views/admin/xcode/index.vue'),
  },
];

export default routes;
