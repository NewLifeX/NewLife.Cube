<template>
  <a-layout class="layout layout-side">
    <a-layout-sider
      :collapsed="collapsed"
      collapsible
      :trigger="null"
      :width="siderWidth"
      :collapsed-width="48"
    >
      <div class="logo">
        <ShellBrand :collapsed="collapsed" />
      </div>
      <a-menu
        :selected-keys="selectedKeys"
        :auto-open-selected="true"
        @menu-item-click="onMenuClick"
      >
        <SidebarMenuNodes :items="visibleMenus" />
      </a-menu>
    </a-layout-sider>

    <a-layout class="layout-side__body">
      <a-layout-header class="layout-header">
        <div class="layout-header__left">
          <a-button type="text" @click="toggleCollapsed">
            <template #icon>
              <icon-park v-if="!collapsed" type="menu-fold" />
              <icon-park v-else type="menu-unfold" />
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
import type { MenuItem } from '@cube/api-core';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { normalizeMenuUrl } from '@/core/utils/url';
import SidebarMenuNodes from './SidebarMenuNodes.vue';
import ShellBrand from './ShellBrand.vue';
import ShellToolbar from './ShellToolbar.vue';
import LayoutContent from './LayoutContent.vue';

const route = useRoute();
const router = useRouter();
const userStore = useUserStore();
const profileStore = useUserProfileStore();

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
.layout-side {
  height: 100%;
}
.logo {
  height: 48px;
  box-sizing: border-box;
}
.layout-header {
  height: 60px;
  background: var(--color-bg-2);
  padding: 0 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  box-sizing: border-box;
  min-width: 0;
  border-bottom: 1px solid var(--color-border);
}
.layout-header__left {
  display: flex;
  align-items: center;
  gap: 12px;
}
.layout-side__body {
  min-height: 0;
  overflow: hidden;
}
/* 隐藏侧边栏底部折叠触发器（白色横条）；:trigger=null 在 Arco 下仍渲染，折叠功能由 header 按钮承担 */
.layout-side :deep(.arco-layout-sider-trigger) {
  display: none;
}
/* 根因修复（OSC-0017 后续）：trigger 隐藏后 Arco 仍给 sider 保留 has-trigger 的 padding-bottom:48px，
   导致滚动容器 .arco-layout-sider-children 高度少 48px、滚动条无法到底（底部留空白）；置 0 恢复全高滚动 */
.layout-side :deep(.arco-layout-sider-has-trigger) {
  padding-bottom: 0;
}
</style>
