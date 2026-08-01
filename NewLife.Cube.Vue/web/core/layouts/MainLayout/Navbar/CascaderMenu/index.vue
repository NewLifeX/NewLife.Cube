<script setup lang="ts">
import { computed } from 'vue';
import { type TreeMenuItem } from '@newlifex/cube-vue/core/stores/menu';
import SecondCascaderMenu from './components/SecondCascaderMenu.vue';
import { isChildMenu, hasChildren } from '@newlifex/cube-vue/core/utils/menuHelpers';

interface CascaderMenuProps {
  menu: TreeMenuItem;
  activeMenu?: TreeMenuItem;
  currentMenu?: TreeMenuItem;
  onMenuClick: (menu: TreeMenuItem) => void;
}

const props = defineProps<CascaderMenuProps>();
const menuItemColumns = 3;
const subMenuWidth = 225;
const subWidth = `${subMenuWidth * menuItemColumns}px`;
const navWidth = '220px';
const collapsed = false;

const children = computed(() => {
  const noChildrenList = props.menu.children?.filter((item) => !hasChildren(item));
  const childrenList: TreeMenuItem[] =
    noChildrenList && noChildrenList.length > 0
      ? [{ ...props.menu, children: noChildrenList }]
      : [];
  props.menu.children?.forEach((item) => {
    if (noChildrenList?.includes(item)) return;
    childrenList.push(item);
  });
  return childrenList;
});
</script>

<template>
  <div
    :class="[
      'menu-cascader',
      { 'menu-cascader--visible': currentMenu && isChildMenu(currentMenu, menu) },
    ]"
    :style="{ maxWidth: collapsed ? 'calc(100vw - 50px)' : `calc(100vw - ${navWidth})` }"
  >
    <SecondCascaderMenu
      v-for="second in children"
      :key="second.id"
      :menu="second"
      :width="subWidth"
      :menuItemColumns="menuItemColumns"
      :active-menu="activeMenu"
      :on-menu-click="onMenuClick"
    />
  </div>
</template>

<style lang="scss" scoped>
.menu-cascader {
  position: relative;
  box-sizing: border-box;
  display: none;
  height: 100%;
  width: 675px;
  max-width: calc(100vw - 220px);
  overflow: hidden auto;
  padding: 28px 20px 0;
  z-index: 1100;
  background: color-mix(in srgb, var(--el-bg-color-overlay) 95%, transparent);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border-left: 1px solid var(--el-border-color-light);
  border-radius: 0 14px 14px 0;
  box-shadow:
    4px 0 24px color-mix(in srgb, var(--el-text-color-primary) 5%, transparent),
    0 4px 24px color-mix(in srgb, var(--el-text-color-primary) 4%, transparent);

  scrollbar-width: none;
  -ms-overflow-style: none;
  &::-webkit-scrollbar {
    width: 0;
    height: 0;
    display: none;
  }
}

.menu-cascader--visible {
  display: block;
}
</style>
