/* eslint-disable */
/* prettier-ignore */
// 魔方皮肤：仅保留内置常量路由，业务路由由 /Cube/MenuTree 动态下发（见 src/router/cube-routes.ts）

import type { GeneratedRoute } from '@elegant-router/types';

export const generatedRoutes: GeneratedRoute[] = [
  {
    name: '403',
    path: '/403',
    component: 'layout.blank$view.403',
    meta: {
      title: '403',
      i18nKey: 'route.403',
      constant: true,
      hideInMenu: true
    }
  },
  {
    name: '404',
    path: '/404',
    component: 'layout.blank$view.404',
    meta: {
      title: '404',
      i18nKey: 'route.404',
      constant: true,
      hideInMenu: true
    }
  },
  {
    name: '500',
    path: '/500',
    component: 'layout.blank$view.500',
    meta: {
      title: '500',
      i18nKey: 'route.500',
      constant: true,
      hideInMenu: true
    }
  },
  {
    name: 'dynamic',
    path: '/dynamic',
    component: 'layout.base$view.dynamic',
    meta: {
      title: 'dynamic',
      i18nKey: 'route.dynamic'
    }
  },
  {
    name: 'iframe-page',
    path: '/iframe-page/:url',
    component: 'layout.base$view.iframe-page',
    props: true,
    meta: {
      title: 'iframe-page',
      i18nKey: 'route.iframe-page'
    }
  },
  {
    name: 'login',
    path: '/login/:module(pwd-login|code-login|register|reset-pwd|bind-wechat)?',
    component: 'layout.blank$view.login',
    meta: {
      title: 'login',
      i18nKey: 'route.login',
      constant: true,
      hideInMenu: true
    },
    props: true
  },
  {
    name: 'workbench',
    path: '/workbench',
    component: 'layout.base$view.workbench',
    meta: {
      title: 'workbench',
      i18nKey: 'route.workbench'
    }
  }
];
