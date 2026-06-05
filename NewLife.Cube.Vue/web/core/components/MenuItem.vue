<script setup lang="ts">
/**
 * MenuItem - 可复用的菜单项组件
 *
 * 支持：
 * - 递归渲染子菜单
 * - 激活状态高亮
 * - 点击跳转/展开
 */
import { ref, computed, watch, inject } from 'vue';
import { type TreeMenuItem } from 'cube-front/core/stores/menu';
import { isChildMenu, hasChildren, renderMenuTitle } from 'cube-front/core/utils/menuHelpers';
import { openMenuTab } from 'cube-front/core/utils/menuTab';
import { useMenuStore } from 'cube-front/core/stores/menu';

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

        <!-- 图标 -->
        <span v-if="menu.icon" class="menu-icon">
          <svg
            v-if="menu.icon === 'dashboard'"
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
            v-else-if="menu.icon === 'device'"
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
            v-else-if="menu.icon === 'layers'"
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
            v-else-if="menu.icon === 'lock'"
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <rect x="3" y="11" width="18" height="11" rx="2" />
            <path d="M7 11V7a5 5 0 0110 0v4" />
          </svg>
          <svg
            v-else-if="menu.icon === 'bell'"
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
            v-else-if="menu.icon === 'activity'"
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <polyline points="22,12 18,12 15,21 9,3 6,12 2,12" />
          </svg>
          <svg
            v-else-if="menu.icon === 'settings'"
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
            v-else-if="menu.icon === 'users'"
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
            v-else
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <circle cx="12" cy="12" r="10" />
          </svg>
        </span>

        <!-- 标题 -->
        <span class="menu-title">{{ renderMenuTitle(menu) }}</span>

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
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: background 0.15s;
  position: relative;

  &:hover {
    background: var(--sidebar-item-hover);
  }

  &.active {
    background: var(--sidebar-item-active);

    .menu-abbr {
      color: var(--accent);
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
  color: var(--text-secondary);
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
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all 0.2s;
  position: relative;
  color: var(--text-secondary);
  font-size: 13px;
  font-weight: 500;

  &:hover {
    background: var(--sidebar-item-hover);
    color: var(--text-primary);
  }

  &.active {
    background: var(--sidebar-item-active);
    color: var(--accent);

    &::before {
      content: '';
      position: absolute;
      left: 0;
      top: 50%;
      transform: translateY(-50%);
      width: 3px;
      height: 24px;
      background: var(--accent);
      border-radius: 0 2px 2px 0;
    }

    .menu-icon {
      color: var(--accent);
    }
  }

  &.ancestor-active {
    .menu-title {
      color: var(--accent);
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
}

.menu-title {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.menu-badge {
  background: var(--accent-secondary);
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
