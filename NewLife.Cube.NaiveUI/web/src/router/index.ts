import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import api from '@/api';

/** 静态路由 */
const staticRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: { title: '登录', requiresAuth: false },
  },
  {
    path: '/forgot-password',
    name: 'ForgotPassword',
    component: () => import('@/views/login/forgot-password.vue'),
    meta: { title: '忘记密码', requiresAuth: false },
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/login/register.vue'),
    meta: { title: '注册', requiresAuth: false },
  },
];

/** 基础布局路由（子路由动态注入） */
const layoutRoute: RouteRecordRaw = {
  path: '/',
  name: 'Layout',
  component: () => import('@/layouts/default.vue'),
  redirect: '/home',
  children: [
    {
      path: 'home',
      name: 'Home',
      component: () => import('@/views/home/index.vue'),
      meta: { title: '首页', icon: 'HomeOutline', affix: true },
    },
  ],
};

const router = createRouter({
  history: createWebHistory(),
  routes: [...staticRoutes, layoutRoute],
});

/** 是否已加载后端菜单路由 */
let hasLoadedRoutes = false;

router.beforeEach(async (to, _from, next) => {
  const token = api.tokenManager.getToken();

  if (to.meta.requiresAuth === false) {
    return next();
  }

  if (!token) {
    return next({ path: '/login', query: { redirect: to.fullPath } });
  }

  if (!hasLoadedRoutes) {
    try {
      const res = await api.menu.getMenuTree();
      const dynamicRoutes = buildRoutes(res.data ?? []);
      for (const r of dynamicRoutes) {
        layoutRoute.children!.push(r);
        router.addRoute('Layout', r);
      }
      hasLoadedRoutes = true;
      return next({ ...to, replace: true });
    } catch {
      api.tokenManager.clearToken();
      return next({ path: '/login' });
    }
  }

  next();
});

/** 根据后端菜单树构建动态路由 */
function buildRoutes(menus: Array<Record<string, any>>): RouteRecordRaw[] {
  return menus.map((item) => {
    const route: RouteRecordRaw = {
      path: item.url?.startsWith('/') ? item.url : `/${item.url}`,
      name: item.name,
      component: () => import('@/views/dynamic/DynamicPage.vue'),
      props: { type: item.url, authId: item.id },
      meta: {
        title: item.displayName,
        icon: item.icon,
        hidden: !item.visible,
      },
      children: item.children?.length ? buildRoutes(item.children) : [],
    };
    return route;
  });
}

export default router;
