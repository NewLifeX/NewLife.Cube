import { createRouter, createWebHashHistory, type RouteRecordRaw } from 'vue-router';
import DefaultLayout from '@/layouts/default.vue';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
  },
  {
    path: '/',
    component: DefaultLayout,
    redirect: '/home',
    children: [
      {
        path: 'home',
        name: 'Home',
        component: () => import('@/views/home/index.vue'),
        meta: { title: '首页' },
      },
      {
        path: ':type+',
        name: 'DynamicPage',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '动态页面' },
      },
    ],
  },
];

const router = createRouter({
  history: createWebHashHistory(),
  routes,
});

export default router;
