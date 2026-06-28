<script lang="ts">
/**
 * MenuItem - 可复用的菜单项组件
 *
 * 支持：
 * - 递归渲染子菜单
 * - 激活状态高亮
 * - 点击跳转/展开
 *
 * == 扩展点 ==
 * 1. 插槽（slot）：
 *    - `#icon="{ menu, resolvedIcon }"` — 自定义图标渲染
 *    - `#title="{ menu }"` — 自定义标题渲染
 * 2. Provide/Inject：
 *    - `MENU_ITEM_ICON_RESOLVER` — 注入自定义图标解析函数 `(iconName?: string) => Component | null`
 *      优先于内置的 EP 图标解析，返回 null 回退到默认逻辑
 *    - `sidebarCollapsed` — 侧边栏折叠状态（由 Layout 提供）
 *
 * == 外部导入 ==
 *   import { MENU_ITEM_ICON_RESOLVER } from 'cube-front/core/components/MenuItem.vue';
 *   provide(MENU_ITEM_ICON_RESOLVER, (name) => name === 'x' ? MyIcon : null);
 */
import { type Component } from 'vue';

/** 注入自定义图标解析器的 Symbol Key */
export const MENU_ITEM_ICON_RESOLVER: unique symbol = Symbol('menuItemIconResolver');
export type MenuIconResolver = (iconName?: string) => Component | null;
</script>

<script setup lang="ts">
import { ref, computed, watch, inject, markRaw, type Component } from 'vue';
import { type TreeMenuItem } from 'cube-front/core/stores/menu';
import { isChildMenu, hasChildren, renderMenuTitle } from 'cube-front/core/utils/menuHelpers';
import { openMenuTab } from 'cube-front/core/utils/menuTab';
import { useMenuStore } from 'cube-front/core/stores/menu';
import * as ElementPlusIcons from '@element-plus/icons-vue';
const ElIconMenu = markRaw(ElementPlusIcons.Menu);

/** 将 icon 字符串解析为 Element Plus 图标组件 */
function resolveEpIcon(iconName?: string): Component | null {
  if (!iconName || iconName.startsWith('fa')) return null;
  // 直接匹配
  const key = iconName as keyof typeof ElementPlusIcons;
  if (ElementPlusIcons[key]) return markRaw(ElementPlusIcons[key]);
  // 转 PascalCase 匹配
  const pascal = iconName
    .replace(/[-_]/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase())
    .replace(/\s/g, '');
  const key2 = pascal as keyof typeof ElementPlusIcons;
  if (ElementPlusIcons[key2]) return markRaw(ElementPlusIcons[key2]);
  return null;
}

const props = withDefaults(
  defineProps<{
    menu: TreeMenuItem;
    depth?: number;
    activeMenu?: TreeMenuItem;
  }>(),
  { depth: 0 },
);

const menuStore = useMenuStore();
const isExpanded = ref(false);
const sidebarCollapsed = inject('sidebarCollapsed', ref(false));

const hasChildrenMenu = computed(() => hasChildren(props.menu));

const isActive = computed(
  () => !hasChildrenMenu.value && isChildMenu(props.activeMenu, props.menu),
);

const isAncestorOfActive = computed(() => {
  if (!props.activeMenu || !hasChildrenMenu.value) return false;
  let current: TreeMenuItem | undefined = props.activeMenu.parentMenu;
  while (current) {
    if (current.id === props.menu.id) return true;
    current = current.parentMenu;
  }
  return false;
});

// ── 图标解析扩展点：优先使用外部注入的解析器，回退到内置 EP 图标 ──
const iconResolver = inject<MenuIconResolver | null>(MENU_ITEM_ICON_RESOLVER, null);
const resolvedIcon = computed<Component | null>(() => {
  if (iconResolver) {
    const custom = iconResolver(props.menu.icon);
    if (custom) return custom;
  }
  return resolveEpIcon(props.menu.icon);
});

// 自动展开 activeMenu 的祖先链路
watch(
  () => props.activeMenu,
  (newActive) => {
    if (newActive && hasChildrenMenu.value) {
      let current: TreeMenuItem | undefined = newActive.parentMenu;
      while (current) {
        if (current.id === props.menu.id) {
          isExpanded.value = true;
          return;
        }
        current = current.parentMenu;
      }
    }
  },
  { immediate: true },
);

const handleClick = () => {
  if (hasChildrenMenu.value) {
    isExpanded.value = !isExpanded.value;
  } else {
    openMenuTab({ url: props.menu.path, title: props.menu.name });
    menuStore.setActiveMenu(props.menu);
  }
};
</script>

<template>
  <div class="menu-item">
    <!-- 折叠模式：仅显示 2 字缩写 badge -->
    <div
      v-if="sidebarCollapsed"
      :class="['menu-row', 'menu-row--collapsed', { active: isActive }]"
      :title="renderMenuTitle(menu)"
      @click="handleClick"
    >
      <span class="menu-abbr">{{ renderMenuTitle(menu).slice(0, 2) }}</span>
    </div>

    <!-- 展开模式：完整渲染 -->
    <template v-else>
      <!-- 菜单行 -->
      <div
        :class="[
          'menu-row',
          { active: isActive, 'ancestor-active': isAncestorOfActive && isExpanded },
        ]"
        :style="{ paddingLeft: `${depth * 16 + 16}px` }"
        @click="handleClick"
      >
        <!-- 展开箭头 -->
        <span v-if="hasChildrenMenu" class="menu-arrow" :class="{ expanded: isExpanded }">
          <svg
            width="10"
            height="10"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
          >
            <path d="M9 18l6-6-6-6" />
          </svg>
        </span>
        <span v-else class="menu-arrow-placeholder" />

        <!-- 图标（可插槽定制）：通过 inject 或 slot 自定义图标渲染 -->
        <span class="menu-icon">
          <slot name="icon" :menu="menu" :resolvedIcon="resolvedIcon">
            <component :is="resolvedIcon" v-if="resolvedIcon" />
            <ElIconMenu v-else style="width: 16px; height: 16px" />
          </slot>
        </span>

        <!-- 标题（可插槽定制） -->
        <span class="menu-title">
          <slot name="title" :menu="menu">
            {{ renderMenuTitle(menu) }}
          </slot>
        </span>

        <!-- 子菜单计数 -->
        <span v-if="hasChildrenMenu" class="menu-badge">{{ menu.children?.length }}</span>
      </div>

      <!-- 子菜单 -->
      <div v-if="hasChildrenMenu" class="menu-children" :class="{ expanded: isExpanded }">
        <div class="menu-children-inner">
          <MenuItem
            v-for="child in menu.children"
            :key="child.id"
            :menu="child"
            :depth="depth + 1"
            :activeMenu="activeMenu"
          />
        </div>
      </div>
    </template>
  </div>
</template>

<style lang="scss" scoped>
.menu-item {
  user-select: none;
}

/* 折叠模式行 */
.menu-row--collapsed {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 6px 4px;
  border-radius: var(--el-border-radius-small);
  cursor: pointer;
  transition: background 0.15s;
  position: relative;

  &:hover {
    background: var(--cube-layout-menu-item-hover-bg);
  }

  &.active {
    background: var(--cube-layout-menu-item-active-bg);

    .menu-abbr {
      color: var(--el-color-primary);
    }
  }
}

/* 折叠时的 2 字缩写 */
.menu-abbr {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 700;
  color: var(--cube-layout-menu-item-color);
  background: rgba(255, 255, 255, 0.06);
  transition:
    background 0.15s,
    color 0.15s;
}

.menu-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-radius: var(--el-border-radius-small);
  cursor: pointer;
  transition: all 0.2s;
  position: relative;
  color: var(--cube-layout-menu-item-color);
  font-size: 13px;
  font-weight: 500;

  &:hover {
    background: var(--cube-layout-menu-item-hover-bg);
    color: var(--el-text-color-primary);
  }

  &.active {
    background: var(--cube-layout-menu-item-active-bg);
    color: var(--el-color-primary);

    &::before {
      content: '';
      position: absolute;
      left: 0;
      top: 50%;
      transform: translateY(-50%);
      width: 3px;
      height: 24px;
      background: var(--el-color-primary);
      border-radius: 0 2px 2px 0;
    }

    .menu-icon {
      color: var(--el-color-primary);
    }
  }

  &.ancestor-active {
    .menu-title {
      color: var(--el-color-primary);
      font-weight: 600;
    }
  }
}

.menu-arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  transition: transform 0.2s;
  flex-shrink: 0;

  &.expanded {
    transform: rotate(90deg);
  }
}

.menu-arrow-placeholder {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.menu-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  :deep(svg) {
    width: 16px;
    height: 16px;
  }
}

.menu-title {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.menu-badge {
  background: var(--cube-layout-menu-icon-active-color);
  color: var(--text-inverse);
  font-size: 11px;
  font-weight: 600;
  padding: 2px 6px;
  border-radius: 10px;
  flex-shrink: 0;
}

.menu-children {
  display: grid;
  grid-template-rows: 0fr;
  transition: grid-template-rows 0.25s ease;

  &.expanded {
    grid-template-rows: 1fr;
  }
}

.menu-children-inner {
  overflow: hidden;
}
</style>
