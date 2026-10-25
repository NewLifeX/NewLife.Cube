import { type RouteRecordRaw } from 'vue-router';

/**
 * cube-admin 的静态路由表只承载「需要显式接管」的页面。
 *
 * 其余业务页不写在这里：后端菜单会驱动 `menuRoutes.registerMenuRoutes` 注册路由，
 * `resolvePageComponent` 命中 views 目录下的 index.vue 就用自定义页，否则回落 `AutoPage`（/meta 动态页）。
 * 因此新增/删除一个页面，通常只需增删视图文件，不需要动这张表。
 */
const routes: RouteRecordRaw[] = [
  // 数据库管理（四视图诊断，元数据表达不了）
  {
    path: '/Admin/Db',
    name: 'admin-db',
    component: () => import('../views/admin/db/index.vue'),
  },
  // 部门管理（部门树 + 主管选择）
  {
    path: '/Admin/Department',
    name: 'admin-department',
    component: () => import('../views/admin/department/index.vue'),
  },
  // 文件管理（上传/下载/预览）
  {
    path: '/Admin/File',
    name: 'admin-file',
    component: () => import('../views/admin/file/index.vue'),
  },
  // 系统首页（监控看板）
  {
    path: '/Admin/Index',
    name: 'admin-index',
    component: () => import('../views/admin/index/index.vue'),
  },
  // 菜单管理（菜单树 + 拖拽排序 + 图标选择）
  {
    path: '/Admin/Menu',
    name: 'admin-menu',
    component: () => import('../views/admin/menu/index.vue'),
  },
  // OAuth配置管理（多 Tab 分组 + scope 多选）
  {
    path: '/Admin/OAuthConfig',
    name: 'admin-oauth-config',
    component: () => import('../views/admin/oauth-config/index.vue'),
  },
  // 角色管理（菜单权限树）
  {
    path: '/Admin/Role',
    name: 'admin-role',
    component: () => import('../views/admin/role/index.vue'),
  },
  // 租户用户管理（角色分配 + 主角色）
  {
    path: '/Admin/TenantUser',
    name: 'admin-tenant-user',
    component: () => import('../views/admin/tenant-user/index.vue'),
  },
  // 用户管理（角色/部门/地区关联选择）
  {
    path: '/Admin/User',
    name: 'admin-user',
    component: () => import('../views/admin/user/index.vue'),
  },
  // 用户统计（图表）
  {
    path: '/Admin/UserStat',
    name: 'admin-user-stat',
    component: () => import('../views/admin/user-stat/index.vue'),
  },
  // 值集管理（值集明细配置入口）
  {
    path: '/Admin/Lov',
    name: 'admin-lov',
    component: () => import('../views/admin/lov/index.vue'),
  },
];

export default routes;
