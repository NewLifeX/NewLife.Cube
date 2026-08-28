/**
 * 根布局（对齐 Vue 皮肤 layouts/RootLayout.vue）
 *
 * 职责：
 * - 认证守卫：需登录路由无 token → 重定向登录页（带回跳）
 * - 初始化：登录态下加载用户信息与菜单
 * - 布局分发：公开页（layout=false）直接渲染；主布局页用 MainLayout 包裹
 */
import { useEffect, type ReactNode } from 'react';
import { Navigate, Outlet, useLocation, useMatches } from 'react-router-dom';
import { Spin } from 'antd';
import MainLayout from '@/layouts/MainLayout';
import { useUserStore, useIsLoggedIn } from '@/stores/user';
import { useMenuStore } from '@/stores/menu';
import { getConfig } from '@/configure';
import type { RouteMeta } from '@/router';

export default function RootLayout({ children }: { children?: ReactNode }) {
  const matches = useMatches();
  const location = useLocation();

  const meta = (matches[matches.length - 1]?.handle ?? {}) as RouteMeta;
  const needAuth = meta.auth !== false;
  const useMainLayout = meta.layout !== false;

  const userInfo = useUserStore((s) => s.userInfo);
  const menus = useUserStore((s) => s.menus);
  const setFlatMenus = useMenuStore((s) => s.setFlatMenus);
  const isLoggedIn = useIsLoggedIn();

  const { loginPageUrl, redirectKey } = getConfig().auth;

  // 初始化：登录态下加载用户信息与菜单（isLoggedIn 变化时触发，登录后自动加载）
  useEffect(() => {
    if (!isLoggedIn) return;
    const store = useUserStore.getState();
    if (!store.userInfo) {
      store.fetchUserInfo().catch(() => {});
    }
    if (!store.menus.length) {
      store
        .fetchMenus()
        .then((list) => setFlatMenus(list))
        .catch(() => {});
    } else if (!useMenuStore.getState().flatMenus.length) {
      setFlatMenus(store.menus);
    }
  }, [isLoggedIn, setFlatMenus]);

  // 认证守卫
  if (needAuth && !isLoggedIn) {
    const r = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`${loginPageUrl}?${redirectKey}=${r}`} replace />;
  }

  // 公开页（无主布局）
  if (!useMainLayout) {
    return <>{children ?? <Outlet />}</>;
  }

  // 主布局页：等用户/菜单就绪
  if (!userInfo) {
    return (
      <div style={{ display: 'flex', height: '100%', alignItems: 'center', justifyContent: 'center' }}>
        <Spin size="large" />
      </div>
    );
  }

  return <MainLayout>{children ?? <Outlet />}</MainLayout>;
}
