import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () => import('./layouts/layout.component').then(m => m.LayoutComponent),
    children: [
      {
        path: 'home',
        loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent),
        data: { title: '首页' },
      },
      {
        path: '**',
        loadComponent: () => import('./pages/dynamic/dynamic-page.component').then(m => m.DynamicPageComponent),
        data: { title: '动态页面' },
      },
    ],
  },
];
