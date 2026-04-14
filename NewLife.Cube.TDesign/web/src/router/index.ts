import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  { path: '/login', component: () => import('@/views/login/index.vue') },
  { path: '/forgot-password', component: () => import('@/views/login/forgot-password.vue') },
  {
    path: '/',
    component: () => import('@/layouts/default.vue'),
    children: [
      { path: '', component: () => import('@/views/home/index.vue') },
      { path: ':type(.*)', component: () => import('@/views/dynamic/DynamicPage.vue') },
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
