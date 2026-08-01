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
import { computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useTagsViewStore } from '@/stores/tagsView';
import { useUserProfileStore } from '@/stores/userProfile';

const route = useRoute();
const router = useRouter();
const tagsStore = useTagsViewStore();
const profileStore = useUserProfileStore();

const show = computed(() => profileStore.layout.showTabs);

watch(
  () => route.fullPath,
  () => {
    // 始终登记缓存名，供 keep-alive prune；showTabs 只控制 UI 显隐
    tagsStore.addView(route);
  },
  { immediate: true },
);

function go(path: string) {
  if (path !== route.path) router.push(path);
}

function close(e: Event, path: string) {
  e.preventDefault?.();
  e.stopPropagation?.();
  const next = tagsStore.removeView(path);
  if (path === route.path) {
    router.push(next?.path || '/home');
  }
}
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
