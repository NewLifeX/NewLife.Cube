<template>
  <a-layout class="layout layout-mix" style="min-height: 100vh">
    <a-layout-header class="layout-header">
      <div class="layout-header__brand">{{ productName }}</div>
      <a-menu
        mode="horizontal"
        :selected-keys="[String(activeTopId)]"
        class="layout-mix__top-menu"
        @menu-item-click="onTopClick"
      >
        <a-menu-item v-for="item in topMenus" :key="String(item.id)">
          {{ item.displayName || item.name }}
        </a-menu-item>
      </a-menu>
      <ShellToolbar />
    </a-layout-header>

    <a-layout>
      <a-layout-sider
        v-if="sideMenus.length"
        :collapsed="collapsed"
        collapsible
        :trigger="null"
        :width="siderWidth"
        :collapsed-width="48"
      >
        <div class="sider-toggle">
          <a-button type="text" @click="toggleCollapsed">
            <template #icon>
              <icon-menu-fold v-if="!collapsed" />
              <icon-menu-unfold v-else />
            </template>
          </a-button>
        </div>
        <a-menu
          :selected-keys="selectedKeys"
          :auto-open-selected="true"
          @menu-item-click="onMenuClick"
        >
          <SidebarMenuNodes :items="sideMenus" />
        </a-menu>
      </a-layout-sider>
      <!-- 包一层 a-layout，命中 Arco has-sider > .arco-layout 的 overflow-x: hidden -->
      <a-layout class="layout-mix__main">
        <LayoutContent />
      </a-layout>
    </a-layout>
  </a-layout>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
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

const topMenus = computed(() =>
  (userStore.menus || []).filter((m: MenuItem) => m.visible !== false),
);

const activeTopId = ref<number>(0);

function menuContainsPath(item: MenuItem, path: string): boolean {
  if (item.url && normalizeMenuUrl(item.url, 'pascal') === path) return true;
  return (item.children || []).some((c) => menuContainsPath(c, path));
}

function syncActiveTop() {
  const path = normalizeMenuUrl(route.path, 'pascal');
  const hit = topMenus.value.find((m) => menuContainsPath(m, path));
  if (hit) {
    activeTopId.value = hit.id;
    return;
  }
  if (!activeTopId.value && topMenus.value.length) {
    activeTopId.value = topMenus.value[0].id;
  }
}

watch([() => route.path, topMenus], syncActiveTop, { immediate: true });

const sideMenus = computed(() => {
  const top = topMenus.value.find((m) => m.id === activeTopId.value);
  if (!top) return [];
  if (top.children?.length) {
    return top.children.filter((m) => m.visible !== false);
  }
  // 一级本身是叶子：侧栏显示自身
  return top.url ? [top] : [];
});

const selectedKeys = computed(() => [normalizeMenuUrl(route.path, 'pascal')]);

function onTopClick(key: string) {
  const id = Number(key);
  activeTopId.value = id;
  const top = topMenus.value.find((m) => m.id === id);
  if (!top) return;
  const firstLeaf = findFirstLeaf(top);
  if (firstLeaf?.url) router.push(normalizeMenuUrl(firstLeaf.url, 'pascal'));
}

function findFirstLeaf(item: MenuItem): MenuItem | null {
  if (item.url && (!item.children || !item.children.length)) return item;
  for (const c of item.children || []) {
    if (c.visible === false) continue;
    const leaf = findFirstLeaf(c);
    if (leaf) return leaf;
  }
  return item.url ? item : null;
}

function onMenuClick(key: string) {
  if (key && !key.startsWith('sub-')) router.push(key);
}

function toggleCollapsed() {
  profileStore.patchLayout({ siderCollapsed: !profileStore.layout.siderCollapsed });
}
</script>

<style scoped>
.layout-header {
  background: var(--color-bg-2);
  padding: 0 16px;
  display: flex;
  align-items: center;
  gap: 16px;
  border-bottom: 1px solid var(--color-border);
}
.layout-header__brand {
  font-weight: 700;
  white-space: nowrap;
  flex-shrink: 0;
}
.layout-mix__top-menu {
  flex: 1;
  min-width: 0;
  background: transparent;
}
.sider-toggle {
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.layout-mix__main {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}
</style>
