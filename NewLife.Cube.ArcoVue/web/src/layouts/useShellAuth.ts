import { useRouter } from 'vue-router';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useTenantStore } from '@/stores/tenant';
import { setStarWebResolver } from '@/core/utils/apiError';
import { ensureEchartsTheme } from '@/core/utils/echartsTheme';
import { isEmbedMode } from '@/core/utils/embedMode';

/** 布局挂载时：登录配置 + 会话恢复 + UserProfile + 租户 */
export function useShellAuth() {
  const router = useRouter();
  const appStore = useAppStore();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();
  const tenantStore = useTenantStore();
  const embed = isEmbedMode();

  profileStore.bootstrapLocal();
  setStarWebResolver(() => appStore.loginConfig?.starWeb);
  appStore.fetchLoginConfig().then(() => {
    tenantStore.applyFeatureFlag(appStore.loginConfig?.enableTenant);
    void ensureEchartsTheme(appStore.loginConfig?.echartsTheme);
  });

  const ensureProfile = () => {
    if (!profileStore.loaded) void profileStore.loadFromServer();
  };

  const afterLogin = () => {
    userStore.fetchMenus();
    ensureProfile();
    if (!embed) void tenantStore.load();
  };

  if (!userStore.isLoggedIn) {
    userStore.fetchUserInfo().then(() => {
      if (userStore.isLoggedIn) {
        afterLogin();
      } else if (!embed) {
        router.push('/login');
      } else {
        // 分享令牌：仍尝试拉菜单以注册动态路由
        void userStore.fetchMenus();
        ensureProfile();
      }
    });
  } else {
    if (!userStore.menus?.length) userStore.fetchMenus();
    ensureProfile();
    if (!embed) void tenantStore.load();
  }
}
