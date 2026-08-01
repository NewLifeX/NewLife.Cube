import { useRouter } from 'vue-router';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';

/** 布局挂载时：登录配置 + 会话恢复 + UserProfile */
export function useShellAuth() {
  const router = useRouter();
  const appStore = useAppStore();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();

  profileStore.bootstrapLocal();
  appStore.fetchLoginConfig();

  const ensureProfile = () => {
    if (!profileStore.loaded) void profileStore.loadFromServer();
  };

  if (!userStore.isLoggedIn) {
    userStore.fetchUserInfo().then(() => {
      if (userStore.isLoggedIn) {
        userStore.fetchMenus();
        ensureProfile();
      } else {
        router.push('/login');
      }
    });
  } else {
    if (!userStore.menus?.length) userStore.fetchMenus();
    ensureProfile();
  }
}
