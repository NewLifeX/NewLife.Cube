<template>
  <a-layout class="layout layout-side" style="min-height: 100vh">
    <a-layout-sider
      :collapsed="collapsed"
      collapsible
      :trigger="null"
      :width="siderWidth"
      :collapsed-width="48"
    >
      <div class="logo">
        {{ collapsed ? '魔' : productName }}
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
      <a-layout-header class="layout-header">
        <div class="layout-header__left">
          <a-button type="text" @click="toggleCollapsed">
            <template #icon>
              <icon-menu-fold v-if="!collapsed" />
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
        <ShellToolbar />
      </a-layout-header>
      <LayoutContent />
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
import { useUserProfileStore } from '@/stores/userProfile';
import { normalizeMenuUrl } from '@/core/utils/url';
import SidebarMenuNodes from './SidebarMenuNodes.vue';
import ShellToolbar from './ShellToolbar.vue';
import LayoutContent from './LayoutContent.vue';

const route = useRoute();
const router = useRouter();
const appStore = useAppStore();
const userStore = useUserStore();
const profileStore = useUserProfileStore();

const productName = computed(() => appStore.loginConfig?.name || '魔方管理平台');
const collapsed = computed(() => profileStore.layout.siderCollapsed);
const siderWidth = computed(() => profileStore.layout.siderWidth || 220);

const visibleMenus = computed(() =>
  (userStore.menus || []).filter((m: MenuItem) => m.visible !== false),
);

const selectedKeys = computed(() => [normalizeMenuUrl(route.path, 'pascal')]);
const currentTitle = computed(() => (route.meta.title as string) || '');

function onMenuClick(key: string) {
  if (key && !key.startsWith('sub-')) router.push(key);
}

function toggleCollapsed() {
  profileStore.patchLayout({ siderCollapsed: !profileStore.layout.siderCollapsed });
}
</script>

<style scoped>
.logo {
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-weight: bold;
  font-size: 16px;
}
.layout-header {
  background: var(--color-bg-2);
  padding: 0 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.layout-header__left {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
