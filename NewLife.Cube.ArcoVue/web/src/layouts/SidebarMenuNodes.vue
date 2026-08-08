<template>
  <template v-for="item in items" :key="item.id">
    <a-sub-menu v-if="item.children?.length && visible(item)" :key="'sub-' + item.id">
      <template #title>
        <icon-park :type="menuIcon(item)" class="menu-node-icon" />
        <span class="menu-node-label">{{ item.displayName || item.name }}</span>
      </template>
      <SidebarMenuNodes :items="item.children" />
    </a-sub-menu>
    <a-menu-item v-else-if="item.url && visible(item)" :key="normalize(item.url)">
      <icon-park :type="menuIcon(item)" class="menu-node-icon" />
      <span class="menu-node-label">{{ item.displayName || item.name }}</span>
    </a-menu-item>
  </template>
</template>

<script setup lang="ts">
import type { MenuItem } from '@cube/api-core';
import { normalizeMenuUrl } from '@/core/utils/url';
import { menuIcon } from '@/core/utils/iconRegistry';
// 递归自引用
import SidebarMenuNodes from './SidebarMenuNodes.vue';

defineProps<{ items: MenuItem[] }>();

function visible(item: MenuItem) {
  return item.visible !== false;
}
function normalize(url: string) {
  return normalizeMenuUrl(url, 'pascal');
}
</script>

<style scoped>
.menu-node-icon {
  display: inline-flex;
  align-items: center;
  margin-right: 8px;
  font-size: 14px;
  vertical-align: -2px;
}
</style>
