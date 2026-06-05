import { type RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  // 应用管理
  {
    path: '/Cube/App',
    name: 'cube-app',
    component: () => import('../views/cube/app/index.vue'),
  },
  // 应用日志
  {
    path: '/Cube/AppLog',
    name: 'cube-app-log',
    component: () => import('../views/cube/app-log/index.vue'),
  },
  // 地区管理
  {
    path: '/Cube/Area',
    name: 'cube-area',
    component: () => import('../views/cube/area/index.vue'),
  },
  // 附件管理
  {
    path: '/Cube/Attachment',
    name: 'cube-attachment',
    component: () => import('../views/cube/attachment/index.vue'),
  },
  // 定时任务
  {
    path: '/Cube/CronJob',
    name: 'cube-cron-job',
    component: () => import('../views/cube/cron-job/index.vue'),
  },
  // 订单管理
  {
    path: '/Cube/OrderManager',
    name: 'cube-order-manager',
    component: () => import('../views/cube/order-manager/index.vue'),
  },
  // 主体代理
  {
    path: '/Cube/PrincipalAgent',
    name: 'cube-principal-agent',
    component: () => import('../views/cube/principal-agent/index.vue'),
  },
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
