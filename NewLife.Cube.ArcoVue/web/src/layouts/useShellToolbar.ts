import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import { storeToRefs } from 'pinia';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useAppStore } from '@/stores/app';
import { useTagsViewStore } from '@/stores/tagsView';
import { useTenantStore } from '@/stores/tenant';
import { resetMenuRoutesFlag } from '@/router';
import { APPEARANCE_ICONS } from '@/core/utils/iconRegistry';
import type { Appearance } from '@/core/utils/userProfile';
import { formatInboxBadgeCount } from '@/core/utils/inboxBadge';
import { resolveSsoAccountUrl } from '@/core/utils/accountCenter';
import { clearSession } from '@/views/login/sessionTokens';

/** 顶栏：外观、租户切换、站内通知、账号菜单 */
export function useShellToolbar() {
  const router = useRouter();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();
  const appStore = useAppStore();
  const tagsStore = useTagsViewStore();
  const tenantStore = useTenantStore();
  const { inboxUnreadCount } = storeToRefs(appStore);
  const inboxBadgeCount = computed(() => formatInboxBadgeCount(inboxUnreadCount.value));

  onMounted(() => {
    if (userStore.isLoggedIn) {
      void tenantStore.load();
      void appStore.refreshInboxUnread();
    }
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

  function goInbox() {
    appStore.openInboxDrawer();
  }

  function goProfile() {
    const url = resolveSsoAccountUrl(appStore.loginConfig, 'profile');
    if (url) {
      window.location.assign(url);
      return;
    }
    router.push('/account?tab=profile');
  }

  function goSecurity() {
    router.push('/account/security');
  }

  function tenantOptionLabel(t: { id: number; name?: string | null; code?: string | null }) {
    return t.name || t.code || (t.id === 0 ? '平台' : String(t.id));
  }

  async function onSwitchTenant(id: number) {
    if (id === tenantStore.currentId) return;
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
    inboxUnreadCount,
    inboxBadgeCount,
    cycleAppearance,
    goAppearance,
    goInbox,
    goProfile,
    goSecurity,
    tenantOptionLabel,
    onSwitchTenant,
    handleLogout,
  };
}
