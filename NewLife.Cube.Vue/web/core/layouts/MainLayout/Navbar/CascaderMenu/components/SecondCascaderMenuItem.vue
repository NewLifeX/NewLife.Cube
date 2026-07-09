<script setup lang="ts">
import { type TreeMenuItem } from '@newlifex/cube-vue/core/stores/menu';
import { isChildMenu, renderMenuTitle } from '@newlifex/cube-vue/core/utils/menuHelpers';
import TextOverflow from '@newlifex/cube-vue/core/components/TextOverflow.vue';

interface SecondCascaderMenuItemProps {
  menu: TreeMenuItem;
  activeMenu?: TreeMenuItem;
  onMenuClick: (menu: TreeMenuItem) => void;
}

const props = defineProps<SecondCascaderMenuItemProps>();

const handleMenuClick = (event: MouseEvent) => {
  event.preventDefault();
  event.stopPropagation();
  props.onMenuClick?.(props.menu);
};
</script>

<template>
  <div
    :class="['cascader-item', { 'cascader-item--active': isChildMenu(activeMenu, menu) }]"
    @click="handleMenuClick"
  >
    <TextOverflow :text="renderMenuTitle(menu)" tooltip-placement="right" class="menu-title-text" />
  </div>
</template>

<style lang="scss" scoped>
.cascader-item {
  font-size: 13px;
  width: 100%;
  height: 34px;
  margin-left: 12px;
  padding-left: 16px;
  padding-right: 8px;
  color: #64748b;
  line-height: 34px;
  cursor: pointer;
  border-radius: 8px;
  box-sizing: border-box;
  transition:
    background-color 0.15s ease,
    color 0.15s ease;

  .menu-title-text {
    width: 100%;
    display: block;
  }

  &--active,
  &:hover {
    color: #1e40af;
    background: #eff6ff;
  }

  &--active {
    font-weight: 600;
  }
}
</style>
