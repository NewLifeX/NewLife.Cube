<template>
  <div v-if="show" class="tags-view">
    <a-tag
      v-for="tag in tagsStore.visited"
      :key="tag.path"
      :checked="tag.path === route.path"
      checkable
      :closable="tag.path !== '/home'"
      class="tags-view__item"
      @check="() => go(tag.path)"
      @close="(e: Event) => close(e, tag.path)"
    >
      {{ tag.title }}
    </a-tag>
  </div>
</template>

<script setup lang="ts">
import { useTagsView } from './useTagsView';

const {
  show,
  tagsStore,
  route,
  go,
  close,
} = useTagsView();
</script>

<style scoped>
.tags-view {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 8px 16px;
  background: var(--color-bg-2);
  border-bottom: 1px solid var(--color-border);
}
.tags-view__item {
  cursor: pointer;
}
/* 当前打开的页签（系统多页签）：文字/边框/底纹跟随当前主题主色（--cube-primary，外观设置可换） */
.tags-view__item.arco-tag-checked,
.tags-view__item :deep(.arco-tag-checked) {
  color: var(--cube-primary);
  border-color: var(--cube-primary);
  background-color: color-mix(in srgb, var(--cube-primary) 10%, #fff);
}
</style>
