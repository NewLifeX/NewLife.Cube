import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useAppStore } from '@/stores/app';
import { useTagsViewStore } from '@/stores/tagsView';
import { useTenantStore } from '@/stores/tenant';
import { resetMenuRoutesFlag } from '@/router';
import { APPEARANCE_ICONS } from '@/core/utils/iconRegistry';
import type { Appearance } from '@/core/utils/userProfile';
import { clearSession } from '@/views/login/sessionTokens';

/** 顶栏：外观、租户切换、账号菜单 */
export function useShellToolbar() {
  const router = useRouter();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();
  const appStore = useAppStore();
  const tagsStore = useTagsViewStore();
  const tenantStore = useTenantStore();

  onMounted(() => {
    if (userStore.isLoggedIn) void tenantStore.load();
  });

  const appearanceLabel = computed(() => {
    const map: Record<Appearance, string> = { light: '亮色', dark: '暗色', system: '跟随系统' };
    return map[profileStore.theme.appearance];
  });

  function cycleAppearance() {
    const order: Appearance[] = ['light', 'dark', 'system'];
    const i = order.indexOf(profileStore.theme.appearance);
    profileStore.patchTheme({ appearance: order[(i + 1) % order.length] });
  }

  function goAppearance() {
    appStore.openAppearanceDrawer();
  }

  function goSecurity() {
    router.push('/account/security');
  }

  async function onSwitchTenant(id: number) {
    try {
      await tenantStore.switchTo(id);
      resetMenuRoutesFlag();
      await userStore.fetchMenus();
      Message.success('已切换租户');
      router.go(0);
    } catch (e: unknown) {
      Message.error((e as { message?: string })?.message || '切换失败');
    }
  }

  async function handleLogout() {
    await userStore.logout();
    clearSession();
    tenantStore.clear();
    profileStore.resetSession();
    tagsStore.clearAll();
    resetMenuRoutesFlag();
    router.push('/login');
  }

  return {
    tenantStore,
    userStore,
    profileStore,
    appearanceLabel,
    APPEARANCE_ICONS,
    cycleAppearance,
    goAppearance,
    goSecurity,
    onSwitchTenant,
    handleLogout,
  };
}
