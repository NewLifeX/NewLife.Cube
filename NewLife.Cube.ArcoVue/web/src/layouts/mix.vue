<template>
  <a-layout class="layout layout-mix">
    <a-layout-header class="layout-header">
      <div class="layout-header__brand">
        <ShellBrand />
      </div>
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

    <a-layout class="layout-mix__body">
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
              <icon-park v-if="!collapsed" type="menu-fold" />
              <icon-park v-else type="menu-unfold" />
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
import SidebarMenuNodes from './SidebarMenuNodes.vue';
import ShellBrand from './ShellBrand.vue';
import ShellToolbar from './ShellToolbar.vue';
import LayoutContent from './LayoutContent.vue';
import { useMixLayout } from './useMixLayout';

const {
  collapsed,
  siderWidth,
  topMenus,
  activeTopId,
  sideMenus,
  selectedKeys,
  onTopClick,
  onMenuClick,
  toggleCollapsed,
} = useMixLayout();
</script>

<style scoped>
.layout-mix {
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
.layout-mix__body {
  flex: 1;
  min-height: 0;
}
.layout-mix__main {
  flex: 1;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
}
/* 隐藏侧边栏底部折叠触发器（白色横条）；:trigger=null 在 Arco 下仍渲染，折叠功能由 sider 顶部按钮承担 */
.layout-mix :deep(.arco-layout-sider-trigger) {
  display: none;
}
/* 根因修复（OSC-0017 后续）：trigger 隐藏后 Arco 仍给 sider 保留 has-trigger 的 padding-bottom:48px，
   导致滚动容器 .arco-layout-sider-children 高度少 48px、滚动条无法到底（底部留空白）；置 0 恢复全高滚动 */
.layout-mix :deep(.arco-layout-sider-has-trigger) {
  padding-bottom: 0;
}
</style>
