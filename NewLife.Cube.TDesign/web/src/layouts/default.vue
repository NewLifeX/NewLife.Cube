<template>
  <t-layout>
    <!-- Sidebar -->
    <t-aside :width="appStore.collapsed ? '64px' : '232px'" style="transition: width .3s">
      <div style="height: 56px; display: flex; align-items: center; padding: 0 16px; font-weight: bold; font-size: 16px; color: var(--td-text-color-primary)">
        <span v-if="!appStore.collapsed">{{ appStore.siteTitle }}</span>
      </div>
      <t-menu :collapsed="appStore.collapsed" :value="$route.path" @change="(v: string) => $router.push(v)" expandMutex>
        <template v-for="item in userStore.menus" :key="item.name">
          <t-submenu v-if="item.children?.length" :value="item.url || item.name" :title="item.displayName || item.name">
            <t-menu-item v-for="child in item.children" :key="child.name" :value="buildUrl(child)">
              {{ child.displayName || child.name }}
            </t-menu-item>
          </t-submenu>
          <t-menu-item v-else :value="buildUrl(item)">
            {{ item.displayName || item.name }}
          </t-menu-item>
        </template>
      </t-menu>
    </t-aside>

    <t-layout>
      <!-- Header -->
      <t-header style="height: 56px; display: flex; align-items: center; padding: 0 16px; border-bottom: 1px solid var(--td-component-border)">
        <t-button theme="default" variant="text" shape="square" @click="appStore.collapsed = !appStore.collapsed">
          <template #icon><view-list-icon /></template>
        </t-button>

        <t-breadcrumb style="margin-left: 12px">
          <t-breadcrumb-item to="/">首页</t-breadcrumb-item>
          <t-breadcrumb-item v-if="$route.path !== '/'">{{ $route.path }}</t-breadcrumb-item>
        </t-breadcrumb>

        <div style="flex: 1" />

        <t-button theme="default" variant="text" shape="square" @click="appStore.toggleDark">
          <template #icon>
            <sunny-icon v-if="appStore.darkMode" />
            <moon-icon v-else />
          </template>
        </t-button>

        <t-dropdown :options="userDropdown" @click="handleUserAction" trigger="click" style="margin-left: 8px">
          <t-button theme="default" variant="text">{{ userStore.displayName }}</t-button>
        </t-dropdown>
      </t-header>

      <!-- Content -->
      <t-content style="padding: 16px; overflow: auto; background: var(--td-bg-color-page)">
        <router-view />
      </t-content>
    </t-layout>
  </t-layout>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ViewListIcon, SunnyIcon, MoonIcon } from 'tdesign-icons-vue-next';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { api } from '@/api';
import type { MenuItem } from '@cube/api-core';

const appStore = useAppStore();
const userStore = useUserStore();
const router = useRouter();

const userDropdown = [{ content: '退出登录', value: 'logout' }];

function buildUrl(item: MenuItem): string {
  return item.url?.startsWith('/') ? item.url : `/${item.url ?? ''}`;
}

function handleUserAction(data: { value: string }) {
  if (data.value === 'logout') {
    userStore.logout();
    router.push('/login');
  }
}

onMounted(async () => {
  try {
    const res = await api.config.getSiteInfo();
    if (res?.data) appStore.siteInfo = res.data;
  } catch { /* ignore */ }

  await userStore.fetchUserInfo();
  if (!userStore.isLoggedIn) { router.push('/login'); return; }
  await userStore.fetchMenus();
});
</script>

<style scoped>
.t-layout {
  height: 100vh;
}
.t-layout .t-aside {
  border-right: 1px solid var(--td-component-border);
}
</style>
