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
</style>
