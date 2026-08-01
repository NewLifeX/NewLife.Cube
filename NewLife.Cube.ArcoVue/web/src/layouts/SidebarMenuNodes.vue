<template>
  <template v-for="item in items" :key="item.id">
    <a-sub-menu v-if="item.children?.length && visible(item)" :key="'sub-' + item.id">
      <template #title>{{ item.displayName || item.name }}</template>
      <SidebarMenuNodes :items="item.children" />
    </a-sub-menu>
    <a-menu-item v-else-if="item.url && visible(item)" :key="normalize(item.url)">
      {{ item.displayName || item.name }}
    </a-menu-item>
  </template>
</template>

<script setup lang="ts">
import type { MenuItem } from '@cube/api-core';
import { normalizeMenuUrl } from '@/core/utils/url';
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
