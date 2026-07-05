<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { useMenuStore, type TreeMenuItem } from 'cube-front/core/stores/menu';
import { openMenuTab } from 'cube-front/core/utils/menuTab';

const menuStore = useMenuStore();
const { treeMenus, topLevelActiveMenu } = storeToRefs(menuStore);

const isActive = (menu: TreeMenuItem) => topLevelActiveMenu.value?.id === menu.id;

const findFirstLeaf = (m: TreeMenuItem): TreeMenuItem => {
  if (!m.children || m.children.length === 0) return m;
  return findFirstLeaf(m.children[0]);
};

const handleClick = (menu: TreeMenuItem) => {
  const target = findFirstLeaf(menu);
  openMenuTab({ url: target.path, title: target.name });
  menuStore.setActiveMenu(target);
};
</script>

<template>
  <aside class="icon-rail">
    <template v-for="menu in treeMenus" :key="menu.id">
      <button
        class="ir-btn"
        :class="{ on: isActive(menu) }"
        :title="menu.title || menu.name"
        @click="handleClick(menu)"
      >
        <span class="ir-tip">{{ menu.title || menu.name }}</span>
        <span class="ir-letter">{{ (menu.title || menu.name || '?').charAt(0) }}</span>
      </button>
    </template>
  </aside>
</template>

<style lang="scss" scoped>
.icon-rail {
  width: 52px;
  background: var(--el-bg-color-overlay);
  border-right: 1px solid var(--el-border-color-light);
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 14px 0;
  gap: 4px;
  flex-shrink: 0;
  overflow-y: auto;

  &::-webkit-scrollbar {
    width: 0;
  }
}

.ir-btn {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--el-text-color-placeholder);
  cursor: pointer;
  border: none;
  background: transparent;
  transition:
    background 0.14s,
    color 0.14s;
  position: relative;
  flex-shrink: 0;

  &:hover {
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);

    .ir-tip {
      opacity: 1;
    }
  }

  &.on {
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
  }
}

.ir-tip {
  position: absolute;
  left: 44px;
  top: 50%;
  transform: translateY(-50%);
  background: var(--el-color-info);
  color: var(--el-color-white);
  font-size: 11px;
  padding: 3px 8px;
  border-radius: 4px;
  white-space: nowrap;
  pointer-events: none;
  opacity: 0;
  transition: opacity 0.1s;
  z-index: 300;
}

.ir-letter {
  font-size: 13px;
  font-weight: 600;
  line-height: 1;
}
</style>
