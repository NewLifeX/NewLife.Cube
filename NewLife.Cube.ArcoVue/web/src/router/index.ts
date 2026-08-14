import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import RootLayout from '@/layouts/RootLayout.vue';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useAppStore } from '@/stores/app';
import { registerLeafRoutes } from '@/core/utils/menuRoutes';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: { public: true },
  },
  {
    path: '/forgot-password',
    name: 'ForgotPassword',
    component: () => import('@/views/login/forgot-password.vue'),
    meta: { public: true },
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/login/register.vue'),
    meta: { public: true },
  },
  {
    path: '/',
    name: 'Layout',
    component: RootLayout,
    redirect: '/home',
    children: [
      {
        path: 'home',
        name: 'Home',
        component: () => import('@/views/home/index.vue'),
        meta: { title: '首页' },
      },
      {
        /** 工作台首页（菜单 visible=false 不注册动态路由，此处静态兜底）；经 pageKind=home 复用 DefaultHome */
        path: 'Admin/Index',
        name: 'AdminIndex',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '首页' },
      },
      {
        /** 数据库管理（菜单可能不注册动态路由时兜底）；经 pageKind=custom 挂专用页 */
        path: 'Admin/Db',
        name: 'AdminDb',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '数据库' },
      },
      {
        /** 文件管理（菜单 visible=false）；经 pageKind=custom 挂专用页 */
        path: 'Admin/File',
        name: 'AdminFile',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '文件' },
      },
      {
        /** 星尘设置（菜单 visible=false）；经探测落入 DefaultObject */
        path: 'Admin/Star',
        name: 'AdminStar',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '星尘设置' },
      },
      {
        /** 魔方设置（ConfigController<CubeSetting>）；经探测落入 DefaultObject */
        path: 'Admin/Cube',
        name: 'AdminCube',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '魔方设置' },
      },
      {
        /** 系统设置（有控制器则渲染，无则 unknown 空状态） */
        path: 'Admin/Sys',
        name: 'AdminSys',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '系统设置' },
      },
      {
        /** 核心设置（有控制器则渲染，无则 unknown 空状态） */
        path: 'Admin/Core',
        name: 'AdminCore',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '核心设置' },
      },
      {
        /** XCode 设置（有控制器则渲染，无则 unknown 空状态） */
        path: 'Admin/XCode',
        name: 'AdminXCode',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: 'XCode 设置' },
      },
      {
        /** 兼容旧链接：打开外观抽屉并回首页，不占用内容页签 */
        path: 'settings/appearance',
        name: 'AppearanceSettings',
        component: { render: () => null },
        beforeEnter: (_to, _from, next) => {
          useAppStore().openAppearanceDrawer();
          next({ path: '/home', replace: true });
        },
      },
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

let routesLoaded = false;

export function resetMenuRoutesFlag() {
  routesLoaded = false;
}

router.beforeEach(async (to, _from, next) => {
  const isPublic = !!to.meta.public;
  const token = cubeApi.tokenManager.getToken();

  if (!token && !isPublic) {
    next({ path: '/login', query: { redirect: to.fullPath } });
    return;
  }

  if (token && !routesLoaded && !isPublic) {
    const userStore = useUserStore();
    const profileStore = useUserProfileStore();
    try {
      if (!userStore.menus?.length) {
        await userStore.fetchMenus();
      }
      if (!profileStore.loaded) {
        profileStore.bootstrapLocal();
        await profileStore.loadFromServer();
      }
      const { currentPathNeedsRefresh } = registerLeafRoutes(
        router,
        userStore.menus || [],
        to.path,
      );
      routesLoaded = true;
      if (currentPathNeedsRefresh) {
        next({ ...to, replace: true });
        return;
      }
    } catch {
      routesLoaded = true;
    }
  }

  next();
});

export default router;
