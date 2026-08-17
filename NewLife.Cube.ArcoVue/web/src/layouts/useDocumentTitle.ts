import { watchEffect } from 'vue';
import { useRoute } from 'vue-router';
import { useAppStore } from '@/stores/app';
import { formatDocumentTitle } from '@/core/utils/documentTitle';

/**
 * 同步 `document.title` 为「页面名 / 显示名称」。
 * 配置中心可通过 appStore.shellPageTitle / shellSysTitle 覆盖
 *（如左侧选中「系统设置」+ 表单 DisplayName，即使路由仍为 Admin/Core）。
 */
export function useDocumentTitle() {
  const route = useRoute();
  const appStore = useAppStore();

  watchEffect(() => {
    const page =
      (appStore.shellPageTitle && appStore.shellPageTitle.trim()) ||
      (route.meta?.title as string | undefined) ||
      '';
    const sys =
      (appStore.shellSysTitle && appStore.shellSysTitle.trim()) ||
      appStore.loginConfig?.name ||
      appStore.loginConfig?.displayName ||
      '';
    document.title = formatDocumentTitle(page, sys);
  });
}
