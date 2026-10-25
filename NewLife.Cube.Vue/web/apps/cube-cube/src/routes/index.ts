import { type RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  // 测试页面
  {
    path: '/Cube/Test',
    name: 'cube-test',
    component: () => import('../views/cube/test/index.vue'),
  },
  // 调试页面
  {
    path: '/Cube/Debug',
    name: 'cube-debug',
    component: () => import('../views/cube/debug/index.vue'),
  },
];

export default routes;
