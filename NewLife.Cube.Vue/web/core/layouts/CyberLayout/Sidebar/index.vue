<script setup lang="ts">
import { ref, computed, provide, readonly, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { getConfig } from '@newlifex/cube-vue/core/configure';
import { useMenuStore, type TreeMenuItem } from '@newlifex/cube-vue/core/stores/menu';
import MenuItem from '@newlifex/cube-vue/core/components/MenuItem.vue';
import UserProfile from '@newlifex/cube-vue/core/components/UserProfile.vue';
import ModeSwitcher from '@newlifex/cube-vue/core/components/ModeSwitcher.vue';
import ThemeSwitcher from '@newlifex/cube-vue/core/components/ThemeSwitcher.vue';
import LayoutSwitcher from '@newlifex/cube-vue/core/components/LayoutSwitcher.vue';
import NotificationBell from '@newlifex/cube-vue/core/components/NotificationBell.vue';
import SearchBar from '@newlifex/cube-vue/core/components/SearchBar.vue';
import LogoBrand from '@newlifex/cube-vue/core/components/LogoBrand.vue';
import { renderMenuTitle } from '@newlifex/cube-vue/core/utils/menuHelpers';
import { openMenuTab } from '@newlifex/cube-vue/core/utils/menuTab';

const config = getConfig();
const menuStore = useMenuStore();
const { treeMenus, activeMenu } = storeToRefs(menuStore);

// 菜单分组（包含 source 以便折叠模式读取图标）
type MenuGroup = { source: TreeMenuItem; title: string; menus: TreeMenuItem[] };
const menuGroups = computed((): MenuGroup[] => {
  if (!treeMenus.value?.length) return [];
  return treeMenus.value.map((menu) => ({
    source: menu,
    title: menu.title || menu.name,
    menus: menu.children || [],
  }));
});

// 侧边栏整体折叠（220px ↔ 64px）
const collapsed = ref(false);
const toggleCollapse = () => {
  collapsed.value = !collapsed.value;
};

// 各分组独立折叠（默认全部折叠）
const groupCollapsed = ref<Record<string, boolean>>({});
watch(
  menuGroups,
  (groups) => {
    groups.forEach((g) => {
      if (groupCollapsed.value[g.title] === undefined) {
        groupCollapsed.value[g.title] = true; // 默认收起
      }
    });
  },
  { immediate: true },
);
const toggleGroup = (title: string) => {
  groupCollapsed.value[title] = !groupCollapsed.value[title];
};

provide('sidebarCollapsed', readonly(collapsed));

// ─── 折叠模式：飞出面板 ───
const flyoutGroup = ref<MenuGroup | null>(null);
const flyoutL1 = ref<TreeMenuItem | null>(null); // 当前展开 L2 的 L1 项
let _ftTimer: ReturnType<typeof setTimeout> | null = null;

function startFlyout(group: MenuGroup) {
  if (_ftTimer) {
    clearTimeout(_ftTimer);
    _ftTimer = null;
  }
  if (flyoutGroup.value?.title !== group.title) flyoutL1.value = null;
  flyoutGroup.value = group;
}
function keepFlyout() {
  if (_ftTimer) {
    clearTimeout(_ftTimer);
    _ftTimer = null;
  }
}
function endFlyout() {
  _ftTimer = setTimeout(() => {
    flyoutGroup.value = null;
    flyoutL1.value = null;
  }, 200);
}

function onFlyoutL1Enter(item: TreeMenuItem) {
  if (item.children?.length) flyoutL1.value = item;
  else flyoutL1.value = null;
}
function handleFlyoutL1Click(item: TreeMenuItem) {
  if (item.children?.length) {
    flyoutL1.value = flyoutL1.value?.id === item.id ? null : item;
  } else {
    openMenuTab({ url: item.path, title: item.name });
    menuStore.setActiveMenu(item);
    flyoutGroup.value = null;
    flyoutL1.value = null;
  }
}
function handleFlyoutL2Click(item: TreeMenuItem) {
  if (!item.children?.length) {
    openMenuTab({ url: item.path, title: item.name });
    menuStore.setActiveMenu(item);
    flyoutGroup.value = null;
    flyoutL1.value = null;
  }
}

// 判断某 L0 分组是否含有当前激活项
function isGroupActive(group: MenuGroup): boolean {
  if (!activeMenu.value) return false;
  function check(m: TreeMenuItem): boolean {
    return m.id === activeMenu.value?.id || (m.children?.some(check) ?? false);
  }
  return group.menus.some(check);
}
// 判断某菜单项（或其后代）是否激活
function isAncestorActive(menu: TreeMenuItem): boolean {
  if (!activeMenu.value) return false;
  function check(m: TreeMenuItem): boolean {
    return m.id === activeMenu.value?.id || (m.children?.some(check) ?? false);
  }
  return check(menu);
}
</script>

<template>
  <aside class="cyber-sidebar" :class="{ collapsed }">
    <!-- 侧边发光线 -->
    <div class="sidebar-glow" />

    <!-- Logo 区域 -->
    <div class="sidebar-logo">
      <LogoBrand :collapsed="collapsed" />
    </div>

    <!-- 搜索框：折叠时隐藏 -->
    <div v-show="!collapsed" class="sidebar-search">
      <SearchBar mode="box" placeholder="搜索菜单..." />
    </div>

    <!-- 导航菜单 -->
    <nav class="sidebar-nav">
      <!-- ── 折叠模式：每组一个图标按钮 + 飞出面板 ── -->
      <template v-if="collapsed">
        <div
          v-for="group in menuGroups"
          :key="group.title"
          class="ci-group"
          :class="{ active: isGroupActive(group), open: flyoutGroup?.title === group.title }"
          @mouseenter="startFlyout(group)"
          @mouseleave="endFlyout()"
        >
          <!-- 图标按钮 -->
          <div class="ci-btn" :title="group.title">
            <!-- 已知图标 -->
            <svg
              v-if="group.source.icon === 'dashboard'"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <rect x="3" y="3" width="7" height="7" rx="1" />
              <rect x="14" y="3" width="7" height="7" rx="1" />
              <rect x="14" y="14" width="7" height="7" rx="1" />
              <rect x="3" y="14" width="7" height="7" rx="1" />
            </svg>
            <svg
              v-else-if="group.source.icon === 'device'"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <rect x="4" y="4" width="16" height="16" rx="2" />
              <circle cx="12" cy="12" r="3" />
              <path d="M12 2v2M12 20v2M2 12h2M20 12h2" />
            </svg>
            <svg
              v-else-if="group.source.icon === 'layers'"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M12 2L2 7l10 5 10-5-10-5z" />
              <path d="M2 17l10 5 10-5" />
              <path d="M2 12l10 5 10-5" />
            </svg>
            <svg
              v-else-if="group.source.icon === 'settings'"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <circle cx="12" cy="12" r="3" />
              <path
                d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"
              />
            </svg>
            <svg
              v-else-if="group.source.icon === 'users'"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M23 21v-2a4 4 0 00-3-3.87" />
              <path d="M16 3.13a4 4 0 010 7.75" />
            </svg>
            <svg
              v-else-if="group.source.icon === 'bell'"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9" />
              <path d="M13.73 21a2 2 0 01-3.46 0" />
            </svg>
            <svg
              v-else-if="group.source.icon === 'activity'"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <polyline points="22,12 18,12 15,21 9,3 6,12 2,12" />
            </svg>
            <!-- 无图标：首字符 -->
            <span v-else class="ci-char">{{ group.title.charAt(0) }}</span>
          </div>

          <!-- 飞出面板 -->
          <div
            v-if="flyoutGroup?.title === group.title"
            class="flyout-panel"
            @mouseenter="keepFlyout()"
            @mouseleave="endFlyout()"
          >
            <div class="flyout-header">{{ group.title }}</div>
            <template v-for="item in group.menus" :key="item.id">
              <div
                class="flyout-item"
                :class="{
                  active: activeMenu?.id === item.id || isAncestorActive(item),
                  'has-sub': item.children?.length,
                  open: flyoutL1?.id === item.id,
                }"
                @mouseenter="onFlyoutL1Enter(item)"
                @click="handleFlyoutL1Click(item)"
              >
                <span class="fi-text">{{ renderMenuTitle(item) }}</span>
                <svg
                  v-if="item.children?.length"
                  class="fi-arrow"
                  width="10"
                  height="10"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2.5"
                >
                  <path d="M9 18l6-6-6-6" />
                </svg>
              </div>
              <!-- L2 展开（inline） -->
              <template v-if="flyoutL1?.id === item.id && item.children?.length">
                <div
                  v-for="child in item.children"
                  :key="child.id"
                  class="flyout-item flyout-item--l2"
                  :class="{ active: activeMenu?.id === child.id }"
                  @click.stop="handleFlyoutL2Click(child)"
                >
                  {{ renderMenuTitle(child) }}
                </div>
              </template>
            </template>
          </div>
        </div>
      </template>

      <!-- ── 展开模式：分组 + MenuItem ── -->
      <template v-else>
        <template v-for="group in menuGroups" :key="group.title">
          <div v-if="group.menus.length > 0" class="nav-group">
            <!-- 分组标签：可点击展开/收起 -->
            <div class="nav-label" @click="toggleGroup(group.title)">
              <span class="nav-label-text">{{ group.title }}</span>
              <svg
                class="nav-label-arrow"
                :class="{ closed: groupCollapsed[group.title] }"
                width="10"
                height="10"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.5"
              >
                <path d="M6 9l6 6 6-6" />
              </svg>
            </div>
            <div v-show="!groupCollapsed[group.title]" class="nav-group-items">
              <MenuItem
                v-for="menu in group.menus"
                :key="menu.id"
                :menu="menu"
                :activeMenu="activeMenu"
              />
            </div>
          </div>
        </template>
      </template>
    </nav>

    <!-- 底部用户区域 -->
    <div class="sidebar-user" :class="{ collapsed }">
      <!-- 侧边栏整体折叠切换按钮 -->
      <button
        class="sidebar-collapse-btn"
        :class="{ collapsed }"
        :title="collapsed ? '展开侧边栏' : '折叠侧边栏'"
        @click="toggleCollapse"
      >
        <svg
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
        >
          <path d="M15 18l-6-6 6-6" />
        </svg>
        <span v-if="!collapsed" class="collapse-label">折叠侧边栏</span>
      </button>

      <div class="user-profile-wrap">
        <UserProfile variant="sidebar" dropup>
          <template #extra-options>
            <div class="tool-row">
              <ModeSwitcher />
              <ThemeSwitcher />
              <LayoutSwitcher />
              <NotificationBell />
            </div>
          </template>
        </UserProfile>
      </div>
    </div>
  </aside>
</template>

<style lang="scss" scoped>
/* —— 侧边栏容器 —— */
.cyber-sidebar {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100vh;
  width: 220px;
  flex-shrink: 0;
  overflow: visible; // 不裁剪搜索结果下拉
  background: var(--cube-layout-sidebar-bg);
  border-right: 1px solid var(--cube-layout-sidebar-border-color);
  transition: width 0.25s cubic-bezier(0.4, 0, 0.2, 1);

  &.collapsed {
    width: 64px;
  }
}

/* —— 发光线 —— */
.sidebar-glow {
  position: absolute;
  top: 0;
  right: 0;
  width: 1px;
  height: 100%;
  background: linear-gradient(
    180deg,
    var(--el-color-primary),
    transparent 50%,
    var(--accent-secondary)
  );
  opacity: 0.3;
  pointer-events: none;
}

/* —— Logo 区域 —— */
.sidebar-logo {
  display: flex;
  align-items: center;
  padding: 12px;
  border-bottom: 1px solid var(--cube-layout-sidebar-border-color);
  height: var(--layout-nav-height, 64px);
  flex-shrink: 0;
  overflow: hidden;
}

/* —— 搜索框区域 —— */
.sidebar-search {
  padding: 8px 10px;
  border-bottom: 1px solid var(--cube-layout-sidebar-border-color);
  flex-shrink: 0;
  overflow: visible;

  // 覆盖 SearchBar 的输入框样式，适配侧边栏主题
  :deep(.sb-box) {
    background: var(--el-fill-color-light);
    border-color: var(--cube-layout-sidebar-border-color);

    &.focused,
    &:focus-within {
      border-color: var(--el-color-primary);
      background: var(--el-fill-color-lighter);
    }
  }
}

/* —— 导航菜单 —— */
.sidebar-nav {
  flex: 1;
  padding: 16px 8px;
  overflow-y: auto;
  overflow-x: hidden;
  scrollbar-width: thin;

  // 折叠模式：允许飞出面板超出侧边栏范围
  .cyber-sidebar.collapsed & {
    overflow: visible;
    padding: 8px 0;
  }
  scrollbar-color: var(--el-border-color-light) transparent;

  &::-webkit-scrollbar {
    width: 4px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: var(--el-border-color-light);
    border-radius: 4px;
  }
}

.nav-group {
  margin-bottom: 20px;
}

.nav-label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 12px;
  font-weight: 500;
  letter-spacing: 0.3px;
  color: var(--el-text-color-regular);
  padding: 0 12px;
  margin-bottom: 6px;
  white-space: nowrap;
  overflow: hidden;
  cursor: pointer;
  user-select: none;
  border-radius: 4px;
  transition: color 0.15s;

  &:hover {
    color: var(--el-text-color-primary);
  }
}

.nav-label-text {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
}

.nav-label-arrow {
  flex-shrink: 0;
  color: var(--el-text-color-secondary);
  transition: transform 0.2s;
  transform: rotate(0deg); // 默认展开（v 向下）

  &.closed {
    transform: rotate(-90deg); // 收起（> 向右）
  }
}

/* —— 折叠模式：组图标 + 飞出面板 —— */
.ci-group {
  position: relative;
  display: flex;
  justify-content: center;
  padding: 2px 8px;

  &.active .ci-btn,
  &.open .ci-btn {
    background: var(--cube-layout-menu-item-active-bg);
    color: var(--el-color-primary);
  }
}

.ci-btn {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-sm);
  cursor: pointer;
  color: var(--el-text-color-regular);
  transition:
    background 0.15s,
    color 0.15s;

  &:hover {
    background: var(--cube-layout-menu-item-hover-bg);
    color: var(--el-text-color-primary);
  }
}

.ci-char {
  font-size: 14px;
  font-weight: 700;
  line-height: 1;
}

/* 飞出面板 */
.flyout-panel {
  position: absolute;
  left: calc(100% + 4px);
  top: 0;
  min-width: 180px;
  max-width: 240px;
  max-height: 70vh;
  overflow-y: auto;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  z-index: 500;
  padding: 4px;
  scrollbar-width: thin;
  scrollbar-color: var(--el-border-color-light) transparent;

  &::-webkit-scrollbar {
    width: 4px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: var(--el-border-color-light);
    border-radius: 4px;
  }
}

.flyout-header {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  color: var(--el-text-color-secondary);
  padding: 6px 10px 4px;
  white-space: nowrap;
}

.flyout-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 7px 10px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  color: var(--el-text-color-regular);
  font-size: 13px;
  font-weight: 500;
  transition:
    background 0.12s,
    color 0.12s;
  white-space: nowrap;

  &:hover {
    background: var(--cube-layout-menu-item-hover-bg);
    color: var(--el-text-color-primary);
  }

  &.active {
    color: var(--el-color-primary);
    font-weight: 600;
  }
}

.fi-text {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
}

.fi-arrow {
  flex-shrink: 0;
  transition: transform 0.2s;
  color: var(--el-text-color-secondary);
  margin-left: 4px;
}

.flyout-item.open .fi-arrow {
  transform: rotate(90deg);
}

/* L2 缩进子项 */
.flyout-item--l2 {
  padding-left: 22px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  font-weight: 400;
  justify-content: flex-start;

  &:hover {
    color: var(--el-text-color-primary);
  }

  &.active {
    color: var(--el-color-primary);
    font-weight: 500;
  }
}

/* —— 用户信息区域 —— */
.sidebar-user {
  border-top: 1px solid var(--cube-layout-sidebar-border-color);
  flex-shrink: 0;
  overflow: visible;

  // UserProfile CSS 变量覆盖——适配侧边栏主题
  --navbar-text: var(--el-text-color-primary);
  --navbar-text-hover: var(--el-color-primary);
  --navbar-hover-bg: var(--el-fill-color-light);

  // 折叠时隐藏用户名和箭头
  &.collapsed {
    :deep(.profile-username),
    :deep(.profile-chevron) {
      display: none;
    }

    :deep(.profile-trigger) {
      justify-content: center;
      padding: 8px;
    }

    .sidebar-collapse-btn {
      justify-content: center;
      padding: 8px 0;
    }

    .user-profile-wrap {
      padding: 4px;
    }
  }

  :deep(.tool-row) {
    display: flex;
    align-items: center;
    justify-content: space-around;
    padding: 4px 0;
  }
}

/* UserProfile 内边距包装层 */
.user-profile-wrap {
  padding: 6px 8px 8px;
}

/* 侧边栏整体折叠/展开按钮 */
.sidebar-collapse-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px 16px;
  background: transparent;
  border: none;
  border-bottom: 1px solid var(--cube-layout-sidebar-border-color);
  cursor: pointer;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  font-family: inherit;
  transition:
    background 0.15s,
    color 0.15s;

  svg {
    flex-shrink: 0;
    transition: transform 0.25s;
  }

  &.collapsed svg {
    transform: rotate(180deg);
  }

  &:hover {
    background: var(--navbar-hover-bg, var(--el-fill-color-light));
    color: var(--el-text-color-primary);
  }

  .collapse-label {
    font-size: 12px;
    white-space: nowrap;
  }
}
</style>
