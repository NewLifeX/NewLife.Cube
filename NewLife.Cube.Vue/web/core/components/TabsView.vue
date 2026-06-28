<script setup lang="ts">
/**
 * TabsView - 多标签页栏组件
 *
 * 显示已打开的页面标签，支持关闭、右键菜单。
 * 标签数据由 tabs store 管理，随路由自动增删。
 */
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useTabsStore, type TabItem } from '../stores/tabs';
import { useMenuStore } from '../stores/menu';
import * as ElementPlusIcons from '@element-plus/icons-vue';

const ElIconClose = ElementPlusIcons.Close;

const router = useRouter();
const tabsStore = useTabsStore();
const menuStore = useMenuStore();

const scrollContainer = ref<HTMLDivElement>();

/** 将 icon 字符串解析为 Element Plus 图标组件 */
function resolveEpIcon(iconName?: string) {
  if (!iconName || iconName.startsWith('fa')) return null;
  const key = iconName as keyof typeof ElementPlusIcons;
  if (ElementPlusIcons[key]) return ElementPlusIcons[key];
  const pascal = iconName
    .replace(/[-_]/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase())
    .replace(/\s/g, '');
  const key2 = pascal as keyof typeof ElementPlusIcons;
  if (ElementPlusIcons[key2]) return ElementPlusIcons[key2];
  return null;
}

/** 查找菜单项获取标题 */
const menuTitle = computed(() => {
  return (path: string): string => {
    const menu = menuStore.flatMenus?.find((m) => m.path === path);
    return menu?.title || menu?.name || '';
  };
});

function handleTabClick(tab: TabItem) {
  tabsStore.setActive(tab.id);
}

function handleCloseTab(tab: TabItem, e: Event) {
  e.stopPropagation();
  const nextId = tabsStore.closeTab(tab.id);
  if (nextId) {
    const next = tabsStore.tabs.find((t) => t.id === nextId);
    if (next) {
      router.push(next.fullPath);
    }
  }
}

/** 右键菜单 */
const contextMenuVisible = ref(false);
const contextMenuStyle = ref<Record<string, string>>({});
const contextMenuTarget = ref<TabItem | null>(null);

function handleContextMenu(e: MouseEvent, tab: TabItem) {
  e.preventDefault();
  contextMenuTarget.value = tab;
  contextMenuStyle.value = {
    left: `${e.clientX}px`,
    top: `${e.clientY}px`,
  };
  contextMenuVisible.value = true;
}

function closeContextMenu() {
  contextMenuVisible.value = false;
  contextMenuTarget.value = null;
}

function handleContextClose() {
  if (!contextMenuTarget.value) return;
  const nextId = tabsStore.closeTab(contextMenuTarget.value.id);
  closeContextMenu();
  if (nextId) {
    const next = tabsStore.tabs.find((t) => t.id === nextId);
    if (next) router.push(next.fullPath);
  }
}

function handleContextCloseOthers() {
  if (!contextMenuTarget.value) return;
  tabsStore.closeOthers(contextMenuTarget.value.id);
  const target = contextMenuTarget.value;
  closeContextMenu();
  router.push(target.fullPath);
}

function handleContextCloseAll() {
  tabsStore.closeAll();
  closeContextMenu();
  const home = tabsStore.homeTab;
  if (home) router.push(home.fullPath);
}

function handleContextRefresh() {
  if (!contextMenuTarget.value) return;
  closeContextMenu();
  router.push({ path: contextMenuTarget.value.fullPath, force: true } as any);
}

// 点击页面任意位置关闭右键菜单
if (typeof document !== 'undefined') {
  document.addEventListener('click', closeContextMenu);
}
</script>

<template>
  <div class="tabs-view" v-if="tabsStore.tabs.length > 0">
    <div ref="scrollContainer" class="tabs-scroll">
      <div
        v-for="tab in tabsStore.tabs"
        :key="tab.id"
        :class="['tabs-item', { active: tab.id === tabsStore.activeTabId }]"
        @click="handleTabClick(tab)"
        @contextmenu="handleContextMenu($event, tab)"
      >
        <!-- 图标 -->
        <span v-if="tab.icon" class="tabs-item-icon">
          <component :is="resolveEpIcon(tab.icon)" />
        </span>

        <!-- 标题 -->
        <span class="tabs-item-title">
          {{ tab.title || menuTitle(tab.path) || tab.path }}
        </span>

        <!-- 关闭按钮 -->
        <span
          v-if="tab.closable"
          class="tabs-item-close"
          @click.stop="handleCloseTab(tab, $event)"
        >
          <ElIconClose />
        </span>
      </div>
    </div>

    <!-- 右键菜单 -->
    <Teleport to="body">
      <div
        v-if="contextMenuVisible"
        class="tabs-context-menu"
        :style="contextMenuStyle"
      >
        <div
          v-if="contextMenuTarget?.closable"
          class="tabs-context-item"
          @click="handleContextClose"
        >
          关闭
        </div>
        <div class="tabs-context-item" @click="handleContextCloseOthers">
          关闭其他
        </div>
        <div class="tabs-context-item" @click="handleContextCloseAll">
          关闭全部
        </div>
        <div class="tabs-context-item" @click="handleContextRefresh">
          刷新当前
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped lang="scss">
.tabs-view {
  display: flex;
  align-items: flex-end;
  height: 27px;
  background: var(--cube-layout-tabsview-bg, var(--el-bg-color-overlay));
  padding: 0 12px;
  flex-shrink: 0;
  position: relative;
  margin-bottom: 4px;
  /* 标签栏底线 — 所有标签坐在这条线上 */
  border-bottom: 1px solid var(--cube-layout-tabsview-border-color, var(--el-border-color-light));
}

.tabs-scroll {
  display: flex;
  align-items: flex-end;
  gap: 2px;
  overflow-x: auto;
  overflow-y: hidden;
  flex: 1;
  scrollbar-width: none;

  &::-webkit-scrollbar {
    display: none;
  }
}

.tabs-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 0 12px;
  height: 28px;
  font-size: var(--cube-layout-tabsview-item-font-size, 12px);
  color: var(--cube-layout-tabsview-item-color, var(--el-text-color-regular));
  background: var(--cube-layout-tabsview-item-bg, var(--el-fill-color-light));
  cursor: pointer;
  white-space: nowrap;
  flex-shrink: 0;
  transition: all 0.2s ease;
  user-select: none;

  /* 标签形状：仅上方和两侧有边框，无底边以"坐"在底线上 */
  border: 1px solid var(--cube-layout-tabsview-item-border-color, var(--el-border-color-light));
  border-bottom: none;
  border-radius: 4px 4px 0 0;
  margin-bottom: -1px;

  &:hover {
    background: var(--cube-layout-tabsview-item-hover-bg, var(--el-bg-color-overlay));
    color: var(--el-text-color-primary);
    z-index: 1;
    position: relative;
  }

  &.active {
    color: var(--cube-layout-tabsview-item-active-color, var(--el-color-primary));
    background: var(--cube-layout-tabsview-item-active-bg, var(--el-bg-color-overlay));

    /* 活跃标签用白色底边遮盖容器底线，产生"浮出"感 */
    border-bottom: 1px solid var(--cube-layout-tabsview-item-active-bg, var(--el-bg-color-overlay));
    margin-bottom: -1px;
    z-index: 2;
    position: relative;

    .tabs-item-close {
      opacity: 1;
    }
  }

  /* 非活跃标签 hover 时也显示关闭按钮 */
  &:hover .tabs-item-close {
    opacity: 1;
  }
}

.tabs-item-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 14px;
  height: 14px;

  :deep(svg) {
    width: 14px;
    height: 14px;
  }
}

.tabs-item-title {
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tabs-item-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  border-radius: 50%;
  opacity: 0;
  transition: opacity 0.15s, background 0.15s;
  flex-shrink: 0;

  :deep(svg) {
    width: 10px;
    height: 10px;
  }

  &:hover {
    background: rgba(0, 0, 0, 0.1);
  }
}

.tabs-context-menu {
  position: fixed;
  z-index: 9999;
  background: var(--cube-layout-tabsview-bg, var(--el-bg-color-overlay));
  border: 1px solid var(--cube-layout-tabsview-border-color, var(--el-border-color-light));
  border-radius: 6px;
  padding: 4px 0;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
  min-width: 120px;
}

.tabs-context-item {
  padding: 6px 16px;
  font-size: 13px;
  color: var(--el-text-color-primary);
  cursor: pointer;
  transition: background 0.15s;

  &:hover {
    background: var(--cube-layout-menu-item-hover-bg, var(--el-fill-color-light));
  }
}
</style>
