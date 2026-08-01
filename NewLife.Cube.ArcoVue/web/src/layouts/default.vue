<template>
  <a-layout class="layout" style="min-height: 100vh">
    <a-layout-sider
      :collapsed="appStore.collapsed"
      collapsible
      :trigger="null"
      :width="220"
      :collapsed-width="48"
    >
      <div
        class="logo"
        style="height: 48px; display: flex; align-items: center; justify-content: center; color: #fff; font-weight: bold; font-size: 16px;"
      >
        {{ appStore.collapsed ? '魔' : (appStore.loginConfig?.name || '魔方管理平台') }}
      </div>
      <a-menu
        :selected-keys="selectedKeys"
        :auto-open-selected="true"
        @menu-item-click="onMenuClick"
      >
        <SidebarMenuNodes :items="visibleMenus" />
      </a-menu>
    </a-layout-sider>

    <a-layout>
      <a-layout-header
        style="background: var(--color-bg-2); padding: 0 16px; display: flex; align-items: center; justify-content: space-between;"
      >
        <div style="display: flex; align-items: center; gap: 12px;">
          <a-button type="text" @click="appStore.toggleCollapsed">
            <template #icon>
              <icon-menu-fold v-if="!appStore.collapsed" />
              <icon-menu-unfold v-else />
            </template>
          </a-button>
          <a-breadcrumb>
            <a-breadcrumb-item>
              <router-link to="/home">首页</router-link>
            </a-breadcrumb-item>
            <a-breadcrumb-item v-if="currentTitle">{{ currentTitle }}</a-breadcrumb-item>
          </a-breadcrumb>
        </div>

        <div style="display: flex; align-items: center; gap: 12px;">
          <a-switch :model-value="appStore.darkMode" @change="onDarkChange">
            <template #checked>暗</template>
            <template #unchecked>亮</template>
          </a-switch>
          <a-dropdown>
            <a-button type="text">
              <a-avatar :size="28">{{ userStore.displayName?.charAt(0) || 'U' }}</a-avatar>
              <span style="margin-left: 8px;">{{ userStore.displayName }}</span>
            </a-button>
            <template #content>
              <a-doption @click="handleLogout">退出登录</a-doption>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>

      <a-layout-content style="padding: 16px;">
        <router-view v-slot="{ Component }">
          <keep-alive>
            <component :is="Component" />
          </keep-alive>
        </router-view>
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { IconMenuFold, IconMenuUnfold } from '@arco-design/web-vue/es/icon';
import type { MenuItem } from '@cube/api-core';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { normalizeMenuUrl } from '@/core/utils/url';
import { resetMenuRoutesFlag } from '@/router';
import SidebarMenuNodes from './SidebarMenuNodes.vue';

const route = useRoute();
const router = useRouter();
const appStore = useAppStore();
const userStore = useUserStore();

appStore.fetchLoginConfig();
if (!userStore.isLoggedIn) {
  userStore.fetchUserInfo().then(() => {
    if (userStore.isLoggedIn) userStore.fetchMenus();
    else router.push('/login');
  });
} else if (!userStore.menus?.length) {
  userStore.fetchMenus();
}

const visibleMenus = computed(() =>
  (userStore.menus || []).filter((m: MenuItem) => m.visible !== false),
);

const selectedKeys = computed(() => [normalizeMenuUrl(route.path, 'pascal')]);
const currentTitle = computed(() => (route.meta.title as string) || '');

function onMenuClick(key: string) {
  if (key && !key.startsWith('sub-')) router.push(key);
}

function onDarkChange() {
  appStore.toggleDarkMode();
}

async function handleLogout() {
  await userStore.logout();
  resetMenuRoutesFlag();
  router.push('/login');
}
</script>
