import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import DefaultLayout from '@/layouts/default.vue';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
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
    component: DefaultLayout,
    redirect: '/home',
    children: [
      {
        path: 'home',
        name: 'Home',
        component: () => import('@/views/home/index.vue'),
        meta: { title: '首页' },
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
    try {
      if (!userStore.menus?.length) {
        await userStore.fetchMenus();
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
