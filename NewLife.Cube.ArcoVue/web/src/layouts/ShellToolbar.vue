<template>
  <div class="shell-toolbar">
    <a-space>
      <a-tooltip :content="appearanceLabel">
        <a-button type="text" size="small" @click="cycleAppearance">
          <icon-park :type="APPEARANCE_ICONS[profileStore.theme.appearance]" />
        </a-button>
      </a-tooltip>
      <a-dropdown>
        <a-button type="text">
          <a-avatar :size="28">{{ userStore.displayName?.charAt(0) || 'U' }}</a-avatar>
          <span style="margin-left: 8px;">{{ userStore.displayName }}</span>
        </a-button>
        <template #content>
          <a-doption @click="goAppearance">外观设置</a-doption>
          <a-doption @click="handleLogout">退出登录</a-doption>
        </template>
      </a-dropdown>
    </a-space>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useAppStore } from '@/stores/app';
import { useTagsViewStore } from '@/stores/tagsView';
import { resetMenuRoutesFlag } from '@/router';
import { APPEARANCE_ICONS } from '@/core/utils/iconRegistry';
import type { Appearance } from '@/core/utils/userProfile';

const router = useRouter();
const userStore = useUserStore();
const profileStore = useUserProfileStore();
const appStore = useAppStore();
const tagsStore = useTagsViewStore();

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

async function handleLogout() {
  await userStore.logout();
  profileStore.resetSession();
  tagsStore.clearAll();
  resetMenuRoutesFlag();
  router.push('/login');
}
</script>

<style scoped>
.shell-toolbar {
  display: flex;
  align-items: center;
}
</style>
