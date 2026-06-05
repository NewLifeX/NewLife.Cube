<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { type TreeMenuItem } from 'cube-front/core/stores/menu';
import { isChildMenu, renderMenuTitle, hasChildren } from 'cube-front/core/utils/menuHelpers';
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
  <div class="side-menu-item">
    <!-- 菜单行 -->
    <div
      :class="[
        'menu-row',
        {
          'menu-row--active': isActive,
          'menu-row--ancestor': isAncestorOfActive && isExpanded,
        },
      ]"
      :style="{ paddingLeft: `${depth * 16 + 16}px` }"
      @click="handleClick"
    >
      <!-- 展开箭头 -->
      <span
        v-if="hasChildrenMenu"
        class="menu-row-arrow"
        :class="{ 'menu-row-arrow--expanded': isExpanded }"
      >
        <svg width="12" height="12" viewBox="0 0 12 12" fill="none" class="arrow-svg">
          <path
            d="M4.5 2.5L8 6L4.5 9.5"
            stroke="currentColor"
            stroke-width="1.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        </svg>
      </span>
      <span v-else class="menu-row-arrow-placeholder" />

      <!-- 图标 -->
      <span v-if="menu.icon" class="menu-row-icon">
        <i :class="menu.icon"></i>
      </span>

      <!-- 标题 -->
      <span class="menu-row-title">{{ renderMenuTitle(menu) }}</span>

      <!-- 子菜单计数 -->
      <span v-if="hasChildrenMenu" class="menu-row-badge">
        {{ menu.children?.length }}
      </span>
    </div>

    <!-- 子菜单容器 -->
    <div
      v-if="hasChildrenMenu"
      class="menu-children"
      :class="{ 'menu-children--expanded': isExpanded }"
    >
      <div class="menu-children-inner">
        <SideMenuItem
          v-for="child in menu.children"
          :key="child.id"
          :menu="child"
          :depth="depth + 1"
          :activeMenu="activeMenu"
        />
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.menu-row {
  display: flex;
  align-items: center;
  height: 40px;
  padding-right: 12px;
  cursor: pointer;
  user-select: none;
  position: relative;
  transition:
    background-color 0.15s ease,
    color 0.15s ease;
  border-radius: 0 10px 10px 0;
  margin: 1px 8px 1px 0;

  // 左侧激活指示条
  &::before {
    content: '';
    position: absolute;
    left: 0;
    top: 50%;
    transform: translateY(-50%);
    width: 3px;
    height: 0;
    background: linear-gradient(180deg, #1e40af 0%, #2563eb 100%);
    border-radius: 0 3px 3px 0;
    transition: height 0.22s cubic-bezier(0.4, 0, 0.2, 1);
  }

  &:hover {
    background: #f3f0ed;
  }

  // 激活态（叶子节点）
  &--active {
    background: linear-gradient(90deg, #eff6ff 0%, rgba(239, 246, 255, 0.4) 100%);
    color: #1e40af;
    font-weight: 600;

    &::before {
      height: 24px;
    }

    .menu-row-title {
      color: #1e40af;
    }
    .menu-row-icon {
      color: #1e40af;
    }
  }

  // 展开的祖先节点
  &--ancestor {
    .menu-row-title {
      color: #1e3a8a;
      font-weight: 600;
    }
  }
}

.menu-row-arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  margin-right: 2px;
  color: #94a3b8;
  transition: transform 0.22s cubic-bezier(0.4, 0, 0.2, 1);
  flex-shrink: 0;

  &--expanded {
    transform: rotate(90deg);
    color: #64748b;
  }

  .arrow-svg {
    display: block;
  }
}

.menu-row-arrow-placeholder {
  width: 20px;
  height: 20px;
  margin-right: 2px;
  flex-shrink: 0;
}

.menu-row-icon {
  display: flex;
  align-items: center;
  margin-right: 8px;
  font-size: 16px;
  color: #94a3b8;
  flex-shrink: 0;
  transition: color 0.15s ease;
}

.menu-row-title {
  font-size: 14px;
  color: #475569;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: 1.4;
  flex: 1;
  transition: color 0.15s ease;
}

.menu-row-badge {
  font-size: 11px;
  color: #64748b;
  background: #f1f5f9;
  padding: 1px 7px;
  border-radius: 10px;
  margin-left: 8px;
  flex-shrink: 0;
  font-weight: 500;
  transition:
    background 0.15s ease,
    color 0.15s ease;
}

// 子菜单展开动画
.menu-children {
  display: grid;
  grid-template-rows: 0fr;
  transition: grid-template-rows 0.28s cubic-bezier(0.4, 0, 0.2, 1);

  &--expanded {
    grid-template-rows: 1fr;
  }
}

.menu-children-inner {
  overflow: hidden;
}
</style>
