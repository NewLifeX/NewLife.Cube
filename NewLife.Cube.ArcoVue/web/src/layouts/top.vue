<template>
  <a-layout class="layout layout-top">
    <a-layout-header class="layout-header">
      <div class="layout-header__brand">
        <ShellBrand />
      </div>
      <a-menu
        mode="horizontal"
        :selected-keys="selectedKeys"
        class="layout-top__menu"
        @menu-item-click="onMenuClick"
      >
        <SidebarMenuNodes :items="visibleMenus" />
      </a-menu>
      <ShellToolbar />
    </a-layout-header>
    <LayoutContent />
  </a-layout>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { MenuItem } from '@cube/api-core';
import { useUserStore } from '@/stores/user';
import { normalizeMenuUrl } from '@/core/utils/url';
import SidebarMenuNodes from './SidebarMenuNodes.vue';
import ShellBrand from './ShellBrand.vue';
import ShellToolbar from './ShellToolbar.vue';
import LayoutContent from './LayoutContent.vue';

const route = useRoute();
const router = useRouter();
const userStore = useUserStore();

const visibleMenus = computed(() =>
  (userStore.menus || []).filter((m: MenuItem) => m.visible !== false),
);
const selectedKeys = computed(() => [normalizeMenuUrl(route.path, 'pascal')]);

function onMenuClick(key: string) {
  if (key && !key.startsWith('sub-')) router.push(key);
}
</script>

<style scoped>
.layout-top {
  height: 100%;
}
.layout-header {
  background: var(--color-bg-2);
  padding: 0 16px;
  display: flex;
  align-items: center;
  gap: 16px;
  border-bottom: 1px solid var(--color-border);
}
.layout-header__brand {
  flex-shrink: 0;
  max-width: 220px;
  height: 48px;
}
.layout-top__menu {
  flex: 1;
  min-width: 0;
  background: transparent;
}
</style>
