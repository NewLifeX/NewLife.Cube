<script setup lang="ts">
import { type TreeMenuItem } from 'cube-front/core/stores/menu';
import { renderMenuTitle } from 'cube-front/core/utils/menuHelpers';
import SecondCascaderMenuItem from './SecondCascaderMenuItem.vue';
import { computed } from 'vue';

interface SecondCascaderMenuProps {
  menu: TreeMenuItem;
  width: string;
  menuItemColumns: number;
  activeMenu?: TreeMenuItem;
  onMenuClick?: (menu: TreeMenuItem) => void;
}

const props = defineProps<SecondCascaderMenuProps>();
const menuItemColumns = computed(() => props.menuItemColumns);
</script>

<template>
  <div class="cascader-second">
    <div class="cascader-second-title" :style="{ width }">
      <span class="title-text">{{ renderMenuTitle(menu) }}</span>
    </div>
    <div class="cascader-second-items" :style="{ width }">
      <SecondCascaderMenuItem
        v-for="leaf in menu.children || []"
        :key="leaf.id"
        :menu="leaf"
        :active-menu="activeMenu"
        :on-menu-click="onMenuClick || ((menu) => {})"
      />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.cascader-second {
  padding-bottom: 20px;

  .cascader-second-title {
    display: flex;
    align-items: center;
    height: 36px;
    margin-left: 12px;
    padding-left: 16px;
    border-bottom: 1px solid #f0ece6;
    overflow: hidden;
  }

  .title-text {
    font-size: 14px;
    font-weight: 700;
    color: #1e293b;
    letter-spacing: -0.01em;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .cascader-second-items {
    margin-top: 6px;
    display: grid;
    grid-template-columns: repeat(v-bind(menuItemColumns), 1fr);
    width: 100%;
    gap: 0;
  }
}
</style>
