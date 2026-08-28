/**
 * 用户认证 Store（对齐 Vue 皮肤 stores/user.ts）
 *
 * 基于 @cube/auth-logic/zustand 适配器创建，提供：
 * - 登录/登出、用户信息、菜单、权限
 * - 登出时联动清理菜单/标签/重定向
 */
import { createZustandAuthStore } from '@cube/auth-logic/zustand';
import { api } from '@/api';
import { useMenuStore } from './menu';
import { useTabsStore } from './tabs';
import { getConfig } from '@/configure';

export const useUserStore = createZustandAuthStore(api);

// 便捷选择器
export const useUserInfo = () => useUserStore((s) => s.userInfo);
export const useMenus = () => useUserStore((s) => s.menus);
export const useIsLoggedIn = () => useUserStore((s) => !!api.tokenManager.getToken());

/**
 * 登出（联动清理：清菜单/标签 → 清 token → 跳登录页）
 */
export async function logoutAndRedirect(): Promise<void> {
  try {
    await useUserStore.getState().logout();
  } catch {
    // 忽略登出接口失败，仍执行本地清理
  }
  useMenuStore.getState().reset();
  useTabsStore.getState().reset();
  const { loginPageUrl } = getConfig().auth;
  if (!window.location.pathname.startsWith(loginPageUrl)) {
    window.location.href = loginPageUrl;
  }
}

export default useUserStore;
