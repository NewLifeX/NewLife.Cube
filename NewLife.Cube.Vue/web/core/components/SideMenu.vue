<template>
  <aside class="cube-side-menu" :class="{ 'cube-side-menu--collapsed': collapsed }">
    <!-- 品牌区：由消费方通过 #brand 插槽注入（如各业务系统的 Logo + 名称） -->
    <div class="cube-side-menu-logo" @click="handleLogoClick">
      <slot name="brand" />
    </div>

    <!-- 搜索框 -->
    <div v-show="!collapsed" class="cube-side-menu-search">
      <el-input
        v-model="searchText"
        placeholder="搜索菜单..."
        :prefix-icon="Search"
        size="small"
        clearable
      />
    </div>

    <!-- 展开态：导航菜单 — 使用 Element Plus el-menu 组件渲染整棵菜单树 -->
    <div v-if="!collapsed" class="cube-side-menu-menu-wrapper">
      <el-menu
        :default-active="activeMenuId"
        :unique-opened="true"
        :router="false"
        @select="handleMenuSelect"
      >
        <!-- 菜单组：遍历 menuStore 的 treeMenus 渲染 -->
        <template v-for="group in filteredGroups" :key="group.id">
          <!-- 有子菜单的组 → el-sub-menu -->
          <el-sub-menu
            v-if="hasVisibleChildren(group)"
            :index="String(group.id)"
          >
            <template #title>
              <el-icon :size="18">
                <component :is="resolveGroupIcon(group.icon)" />
              </el-icon>
              <span>{{ group.title || group.name }}</span>
            </template>
            <!-- 子菜单项 -->
            <template v-for="child in group.children" :key="child.id">
              <!-- 孙子菜单：二级分组（极少使用，但保留支持） -->
              <el-sub-menu
                v-if="hasVisibleChildren(child)"
                :index="String(child.id)"
              >
                <template #title>
                  <span>{{ child.title || child.name }}</span>
                </template>
                <el-menu-item
                  v-for="leaf in child.children"
                  :key="leaf.id"
                  :index="String(leaf.id)"
                >
                  <el-icon :size="16">
                    <component :is="resolveLeafIcon(leaf.icon)" />
                  </el-icon>
                  <span>{{ leaf.title || leaf.name }}</span>
                </el-menu-item>
              </el-sub-menu>
              <!-- 叶子菜单项 -->
              <el-menu-item v-else :index="String(child.id)">
                <el-icon :size="16">
                  <component :is="resolveLeafIcon(child.icon)" />
                </el-icon>
                <span>{{ child.title || child.name }}</span>
              </el-menu-item>
            </template>
          </el-sub-menu>

          <!-- 顶层叶子菜单 → el-menu-item -->
          <el-menu-item v-else :index="String(group.id)">
            <el-icon :size="18">
              <component :is="resolveGroupIcon(group.icon)" />
            </el-icon>
            <span>{{ group.title || group.name }}</span>
          </el-menu-item>
        </template>
      </el-menu>
    </div>

    <!-- 折叠态：只显示大类图标（悬停显示名称，点击右侧弹出子菜单） -->
    <div v-else class="cube-side-menu-rail">
      <div
        v-for="group in menuGroups"
        :key="group.id"
        class="cube-side-menu-rail-item"
      >
        <el-tooltip
          :content="group.title || group.name"
          placement="right"
          :show-after="150"
        >
          <div
            class="cube-side-menu-rail-btn"
            :class="{ 'is-active': isGroupActive(group) }"
            @click="handleRailClick(group, $event)"
          >
            <el-icon :size="20">
              <component :is="resolveGroupIcon(group.icon)" />
            </el-icon>
          </div>
        </el-tooltip>
      </div>
    </div>

    <!-- 底部：折叠 / 展开按钮 -->
    <div class="cube-side-menu-footer">
      <button
        class="cube-side-menu-collapse-btn"
        :title="collapsed ? '展开菜单' : '收起菜单'"
        @click="$emit('toggle')"
      >
        <el-icon :size="16">
          <component :is="collapsed ? Expand : Fold" />
        </el-icon>
      </button>
    </div>
  </aside>

  <!-- 折叠态：点击大类图标弹出的子菜单面板（Teleport 到 body，避免被侧边栏裁剪） -->
  <Teleport to="body">
    <template v-if="collapsed && flyoutGroup">
      <!-- 点击遮罩关闭面板 -->
      <div class="cube-side-menu-flyout-mask" @click="closeFlyout"></div>
      <!-- 子菜单面板：垂直对齐被点击的大类图标 -->
      <div class="cube-side-menu-flyout" :style="{ top: flyoutTop + 'px' }">
        <div class="cube-side-menu-flyout-title">
          {{ flyoutGroup.title || flyoutGroup.name }}
        </div>
        <div class="cube-side-menu-flyout-items">
          <template v-for="child in flyoutGroup.children" :key="child.id">
            <!-- 二级分组 -->
            <div
              v-if="hasVisibleChildren(child)"
              class="cube-side-menu-flyout-subgroup"
            >
              <div class="cube-side-menu-flyout-subgroup-title">
                {{ child.title || child.name }}
              </div>
              <div
                v-for="leaf in child.children"
                :key="leaf.id"
                class="cube-side-menu-flyout-item"
                :class="{ 'is-active': isLeafActive(leaf) }"
                @click="handleFlyoutSelect(leaf)"
              >
                <el-icon :size="16">
                  <component :is="resolveLeafIcon(leaf.icon)" />
                </el-icon>
                <span>{{ leaf.title || leaf.name }}</span>
              </div>
            </div>
            <!-- 叶子子菜单项 -->
            <div
              v-else
              class="cube-side-menu-flyout-item"
              :class="{ 'is-active': isLeafActive(child) }"
              @click="handleFlyoutSelect(child)"
            >
              <el-icon :size="16">
                <component :is="resolveLeafIcon(child.icon)" />
              </el-icon>
              <span>{{ child.title || child.name }}</span>
            </div>
          </template>
        </div>
      </div>
    </template>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { type Component } from "vue";
import {
  useMenuStore,
  type TreeMenuItem,
} from "@newlifex/cube-vue/core/stores/menu";
import { storeToRefs } from "pinia";
import { openMenuTab } from "@newlifex/cube-vue/core/utils/menuTab";
import {
  renderMenuTitle,
  hasChildren,
  isChildMenu,
} from "@newlifex/cube-vue/core/utils/menuHelpers";
import * as ElementPlusIconsVue from "@element-plus/icons-vue";
import { Search, Expand, Fold } from "@element-plus/icons-vue";

const props = defineProps<{ collapsed?: boolean }>();
defineEmits<{ (e: "toggle"): void }>();

const menuStore = useMenuStore();
const { treeMenus, activeMenu, loading, hasMenus } = storeToRefs(menuStore);
const searchText = ref("");

// ======== 图标解析（复用 MenuItem.vue 的 resolveEpIcon 思路）========

/** 将图标名解析为 Element Plus 图标组件（兼容各种命名格式） */
function resolveIcon(iconName?: string): Component | null {
  if (!iconName) return null;
  // 直接匹配
  const icons = ElementPlusIconsVue as Record<string, Component>;
  if (icons[iconName]) return icons[iconName];
  // PascalCase 匹配
  const pascal = iconName
    .replace(/[-_]/g, " ")
    .replace(/\b\w/g, (c) => c.toUpperCase())
    .replace(/\s/g, "");
  if (icons[pascal]) return icons[pascal];
  return null;
}

/** 组图标（带 fallback: Folder） */
function resolveGroupIcon(icon?: string): Component {
  return (
    resolveIcon(icon) ||
    resolveIcon("Folder") ||
    ElementPlusIconsVue.FolderOpened
  );
}

/** 叶子菜单图标（带 fallback: Menu） */
function resolveLeafIcon(icon?: string): Component {
  return (
    resolveIcon(icon) || resolveIcon("Menu") || ElementPlusIconsVue.Document
  );
}

// ======== 菜单数据处理 ========

/** 过滤隐藏菜单 */
const filterVisibleMenus = (menus: TreeMenuItem[]): TreeMenuItem[] => {
  return menus
    .filter((m) => (m as { visible?: boolean }).visible !== false)
    .map((m) => ({
      ...m,
      children: m.children ? filterVisibleMenus(m.children) : undefined,
    }));
};

const menuGroups = computed(() => filterVisibleMenus(treeMenus.value ?? []));

/** 是否有可见子菜单 */
function hasVisibleChildren(menu: TreeMenuItem): boolean {
  if (!menu.children || menu.children.length === 0) return false;
  return menu.children.some(
    (c) => (c as { visible?: boolean }).visible !== false,
  );
}

/** 当前激活的菜单 ID（用于 el-menu default-active） */
const activeMenuId = computed(() => String(activeMenu.value?.id ?? ""));

/** 搜索过滤后的菜单 */
const filteredGroups = computed(() => {
  const q = searchText.value.trim().toLowerCase();
  if (!q) return menuGroups.value;
  return menuGroups.value
    .map((g) => {
      const filteredChildren = g.children?.filter((c) =>
        (c.title || c.name || "").toLowerCase().includes(q),
      );
      const groupMatch = (g.title || g.name || "").toLowerCase().includes(q);
      if (groupMatch) return g;
      if (filteredChildren && filteredChildren.length > 0) {
        return { ...g, children: filteredChildren };
      }
      return null;
    })
    .filter(Boolean) as TreeMenuItem[];
});

// ======== 菜单点击处理（复用 MenuItem.vue 的 handleClick 核心逻辑）========

/** 根据菜单 ID 在树中查找菜单项 */
function findMenuById(
  menus: TreeMenuItem[],
  id: string,
): TreeMenuItem | undefined {
  for (const m of menus) {
    if (String(m.id) === id) return m;
    if (m.children) {
      const found = findMenuById(m.children, id);
      if (found) return found;
    }
  }
  return undefined;
}

/** el-menu select 回调处理 */
function handleMenuSelect(index: string) {
  const menu = findMenuById(menuGroups.value, index);
  if (!menu) return;

  if (hasChildren(menu)) {
    // 有子菜单的组 — 由 el-sub-menu 自行处理展开/折叠，不做路由跳转
    return;
  }

  // 叶子菜单 — 复用 MenuItem.vue 的核心逻辑
  openMenuTab({ url: menu.path, title: renderMenuTitle(menu) });
  menuStore.setActiveMenu(menu);
}

/** Logo 点击：跳转到首页 / 工作台 */
function handleLogoClick() {
  // 如果有 工作台 / dashboard 菜单则跳转，否则跳根路径
  const dashboard = menuGroups.value.find(
    (m) => m.path === "/" || (m.title || m.name) === "工作台",
  );
  if (dashboard) {
    openMenuTab({ url: dashboard.path, title: renderMenuTitle(dashboard) });
    menuStore.setActiveMenu(dashboard);
  }
}

// ======== 折叠态：大类图标栏 + 右侧弹出子菜单面板 ========

/** 当前弹出子菜单的大类（null 表示未弹出） */
const flyoutGroup = ref<TreeMenuItem | null>(null);

/** 弹出面板的 top 坐标（与点击的大类图标垂直对齐） */
const flyoutTop = ref(0);

/** 展开侧边栏时关闭弹出面板 */
watch(() => props.collapsed, (val) => {
  if (!val) closeFlyout();
});

/** 打开大类子菜单面板：与图标垂直对齐，防止超出视口底部 */
function openFlyout(group: TreeMenuItem, trigger: HTMLElement) {
  const rect = trigger.getBoundingClientRect();
  const top = Math.min(Math.max(8, rect.top), window.innerHeight - 360);
  flyoutTop.value = top;
  flyoutGroup.value = group;
}

/** 关闭大类子菜单面板 */
function closeFlyout() {
  flyoutGroup.value = null;
}

/** 跳转到叶子菜单并记录激活态 */
function handleLeafSelect(menu: TreeMenuItem) {
  openMenuTab({ url: menu.path, title: renderMenuTitle(menu) });
  menuStore.setActiveMenu(menu);
}

/** 折叠态点击大类图标：有子菜单 → 右侧弹出子菜单；叶子 → 直接跳转 */
function handleRailClick(group: TreeMenuItem, e: MouseEvent) {
  if (hasVisibleChildren(group)) {
    openFlyout(group, e.currentTarget as HTMLElement);
  } else {
    handleLeafSelect(group);
  }
}

/** 点击弹出面板中的子菜单项：跳转并关闭面板 */
function handleFlyoutSelect(menu: TreeMenuItem) {
  handleLeafSelect(menu);
  closeFlyout();
}

/** 大类是否包含当前激活菜单（用于图标高亮） */
function isGroupActive(group: TreeMenuItem): boolean {
  const current = activeMenu.value;
  if (!current) return false;
  return isChildMenu(current, group);
}

/** 叶子菜单是否为当前激活菜单 */
function isLeafActive(leaf: TreeMenuItem): boolean {
  return activeMenu.value?.id === leaf.id;
}
</script>

<style scoped lang="scss">
.cube-side-menu {
  // 自带布局变量回退：脱离业务系统的 variables.css 也能正常渲染
  --cube-layout-sidebar-width: 220px;
  --cube-layout-sidebar-collapsed-width: 64px;
  --cube-layout-sidebar-bg: var(--el-bg-color);
  --cube-layout-sidebar-border-color: var(--el-border-color-light);
  --cube-layout-z-sidebar: 100;
  --cube-layout-transition-duration: 0.25s;
  --cube-layout-menu-item-hover-bg: var(--el-fill-color-light);
  --cube-layout-menu-item-active-bg: var(--el-color-primary-light-9);
  --cube-layout-menu-item-active-color: var(--el-color-primary);
  --cube-layout-menu-item-active-border-color: var(--el-color-primary);
  --cube-layout-menu-icon-active-color: var(--el-color-primary);

  position: fixed;
  left: 0;
  top: 0;
  width: var(--cube-layout-sidebar-width, 220px);
  min-width: var(--cube-layout-sidebar-width, 220px);
  height: 100vh;
  background: var(--cube-layout-sidebar-bg);
  border-right: 1px solid var(--cube-layout-sidebar-border-color);
  z-index: var(--cube-layout-z-sidebar, 100);
  display: flex;
  flex-direction: column;
  transition: width var(--cube-layout-transition-duration, 0.25s) ease;
  overflow: hidden;

  &--collapsed {
    width: var(--cube-layout-sidebar-collapsed-width, 64px);
    min-width: var(--cube-layout-sidebar-collapsed-width, 64px);

    .cube-side-menu-logo {
      padding: 16px 0;
      justify-content: center;
    }
  }
}

// ---- 品牌区 ----
.cube-side-menu-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 16px 20px;
  border-bottom: 1px solid var(--cube-layout-sidebar-border-color);
  cursor: pointer;
  user-select: none;
}

// ---- 搜索框 ----
.cube-side-menu-search {
  padding: 8px 12px;
  border-bottom: 1px solid var(--el-border-color-light);

  :deep(.el-input) {
    --el-input-bg-color: var(--el-bg-color-page);
  }
}

// ---- 菜单区域 ----
.cube-side-menu-menu-wrapper {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 8px 0;

  /* 自定义 el-menu 样式 */
  :deep(.el-menu) {
    border-right: none;
    background: transparent;

    /* 子菜单标题 */
    .el-sub-menu__title {
      display: flex;
      align-items: center;
      gap: 8px;
      height: 40px;
      line-height: 40px;
      padding: 0 16px !important;
      font-size: 13px;
      color: var(--el-text-color-regular);
      border-left: 3px solid transparent;
      transition: all 0.2s ease;

      &:hover {
        background: var(--cube-layout-menu-item-hover-bg);
        color: var(--el-text-color-primary);
      }

      .el-icon {
        color: var(--el-text-color-secondary);
        flex-shrink: 0;
      }
    }

    /* 菜单项 */
    .el-menu-item {
      display: flex;
      align-items: center;
      gap: 8px;
      height: 36px;
      line-height: 36px;
      padding: 0 16px 0 28px !important;
      font-size: 13px;
      color: var(--el-text-color-regular);
      border-left: 3px solid transparent;
      transition: all 0.2s ease;

      &:hover {
        background: var(--cube-layout-menu-item-hover-bg);
        color: var(--el-text-color-primary);
      }

      &.is-active {
        background: var(--cube-layout-menu-item-active-bg);
        color: var(--cube-layout-menu-item-active-color);
        border-left-color: var(--cube-layout-menu-item-active-border-color);
        font-weight: 500;

        .el-icon {
          color: var(--cube-layout-menu-icon-active-color);
        }
      }

      .el-icon {
        color: var(--el-text-color-secondary);
        flex-shrink: 0;
      }
    }

    /* 展开箭头覆写 */
    .el-sub-menu__icon-arrow {
      font-size: 12px;
      transition: transform 0.2s;
    }
  }
}

// ---- 底部折叠 / 展开按钮 ----
.cube-side-menu-footer {
  flex-shrink: 0;
  padding: 8px 0;
  border-top: 1px solid var(--cube-layout-sidebar-border-color);
  display: flex;
  justify-content: center;
}

.cube-side-menu-collapse-btn {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--el-text-color-secondary);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--cube-layout-menu-item-hover-bg);
    color: var(--el-color-primary);
  }
}

// ---- 折叠态：大类图标栏 ----
.cube-side-menu-rail {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 12px 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.cube-side-menu-rail-item {
  flex-shrink: 0;
}

.cube-side-menu-rail-btn {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--el-text-color-secondary);
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--cube-layout-menu-item-hover-bg);
    color: var(--el-color-primary);
  }

  &.is-active {
    background: var(--cube-layout-menu-item-active-bg);
    color: var(--cube-layout-menu-icon-active-color);
  }
}

// ---- 折叠态：弹出面板（Teleport 到 body，点击遮罩关闭） ----
.cube-side-menu-flyout-mask {
  position: fixed;
  inset: 0;
  z-index: calc(var(--cube-layout-z-sidebar, 100) + 1);
  background: var(--el-mask-color-extra-light);
}

.cube-side-menu-flyout {
  position: fixed;
  left: calc(var(--cube-layout-sidebar-collapsed-width, 64px) + 8px);
  min-width: 180px;
  max-width: 240px;
  max-height: 380px;
  overflow-y: auto;
  z-index: calc(var(--cube-layout-z-sidebar, 100) + 2);
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--cube-layout-sidebar-border-color);
  border-radius: 10px;
  box-shadow: var(--el-box-shadow-light);
  padding: 6px;

  .cube-side-menu-flyout-title {
    padding: 8px 12px;
    font-size: 13px;
    font-weight: 600;
    color: var(--el-text-color-primary);
    border-bottom: 1px solid var(--el-border-color-light);
  }

  .cube-side-menu-flyout-items {
    padding: 4px 0;
  }

  .cube-side-menu-flyout-item {
    display: flex;
    align-items: center;
    gap: 8px;
    height: 36px;
    line-height: 36px;
    padding: 0 12px;
    font-size: 13px;
    color: var(--el-text-color-regular);
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.2s;

    &:hover {
      background: var(--cube-layout-menu-item-hover-bg);
      color: var(--el-text-color-primary);
    }

    &.is-active {
      background: var(--cube-layout-menu-item-active-bg);
      color: var(--cube-layout-menu-item-active-color);
      font-weight: 500;
    }

    .el-icon {
      color: var(--el-text-color-secondary);
      flex-shrink: 0;
    }
  }

  .cube-side-menu-flyout-subgroup {
    .cube-side-menu-flyout-subgroup-title {
      padding: 8px 12px 4px;
      font-size: 11px;
      color: var(--el-text-color-secondary);
      letter-spacing: 1px;
      text-transform: uppercase;
    }
  }
}
</style>
