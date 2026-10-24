import type { ConfigRoute } from '../typings';
import i18n from '../i18n';

/** 路由标题跟随语言：用 getter，读 meta.title 时才取当前 locale 的文案 */
const titleOf = (key: string) => ({
  get title() {
    return i18n.global.t(key);
  },
});

const routes: ConfigRoute[] = [
  {
    path: '/',
    name: 'home',
    meta: titleOf('route.home'),
    component: () => import('../pages/PageHome.vue'),
  },
  {
    path: '/login',
    name: 'login',
    meta: {
      ...titleOf('route.login'),
      auth: false,
      layout: false,
    },
    component: () => import('../pages/PageLogin'),
  },
  {
    path: '/activate',
    name: 'activate',
    meta: {
      ...titleOf('route.activate'),
      auth: false,
      layout: false,
    },
    component: () => import('../pages/ActivatePage.vue'),
  },
  {
    path: '/profile/security',
    name: 'profile-security',
    meta: {
      ...titleOf('route.security'),
      auth: true,
    },
    component: () => import('../pages/ProfileSecurity.vue'),
  },
  {
    path: '/unauthorized',
    name: 'unauthorized',
    meta: {
      ...titleOf('route.unauthorized'),
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
      ...titleOf('route.loading'),
      layout: false,
      auth: false, // 不需要认证
    },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'DefaultEntity',
    component: () => import('../pages/DefaultEntity.vue'),
    meta: {
      ...titleOf('route.default'),
      auth: true,
    },
  },
];

export default routes;
