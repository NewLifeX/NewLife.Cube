<template>
  <a-layout class="layout layout-embed">
    <LayoutContent width="fluid" />
  </a-layout>
</template>

<script setup lang="ts">
import LayoutContent from './LayoutContent.vue';
</script>

<style scoped>
/* 与 side/mix 一致：根布局锁视口高度，才能让 LayoutContent__scroll 出现纵向滚动条，
   底部分页器才进得了可视/可滚区域（仅 min-height 时内容撑高后被 #app height:100% 裁切） */
.layout-embed {
  height: 100%;
  max-height: 100%;
  overflow: hidden;
  background: var(--color-fill-2);
}
.layout-embed :deep(.layout-content) {
  flex: 1;
  min-height: 0;
}
</style>
