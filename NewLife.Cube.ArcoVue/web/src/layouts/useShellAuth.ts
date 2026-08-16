import { useRouter } from 'vue-router';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useTenantStore } from '@/stores/tenant';

/** 布局挂载时：登录配置 + 会话恢复 + UserProfile + 租户 */
export function useShellAuth() {
  const router = useRouter();
  const appStore = useAppStore();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();
  const tenantStore = useTenantStore();

  profileStore.bootstrapLocal();
  appStore.fetchLoginConfig();

  const ensureProfile = () => {
    if (!profileStore.loaded) void profileStore.loadFromServer();
  };

  const afterLogin = () => {
    userStore.fetchMenus();
    ensureProfile();
    void tenantStore.load();
  };

  if (!userStore.isLoggedIn) {
    userStore.fetchUserInfo().then(() => {
      if (userStore.isLoggedIn) {
        afterLogin();
      } else {
        router.push('/login');
      }
    });
  } else {
    if (!userStore.menus?.length) userStore.fetchMenus();
    ensureProfile();
    void tenantStore.load();
  }
}
