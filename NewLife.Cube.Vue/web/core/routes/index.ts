import type { ConfigRoute } from '../typings';

const routes: ConfigRoute[] = [
  {
    path: '/',
    name: 'home',
    meta: {
      title: '首页',
    },
    component: () => import('../pages/PageHome.vue'),
  },
  {
    path: '/login',
    name: 'login',
    meta: {
      title: '登录',
      auth: false,
      layout: false,
    },
    component: () => import('../pages/PageLogin'),
  },
  {
    path: '/unauthorized',
    name: 'unauthorized',
    meta: {
      title: '未授权',
      layout: false,
      auth: false,
    },
    component: () => import('../pages/PageUnauthorized.vue'),
  },
  {
    path: '/loading',
    name: 'Loading',
    component: () => import('../views/Loading.vue'),
    meta: {
      title: '加载中',
      layout: false,
      auth: false, // 不需要认证
    },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'DefaultEntity',
    component: () => import('../pages/DefaultEntity.vue'),
    meta: {
      title: '默认页面',
      auth: true,
    },
  },
];

export default routes;
