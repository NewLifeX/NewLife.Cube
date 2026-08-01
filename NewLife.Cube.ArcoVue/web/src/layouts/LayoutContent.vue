<template>
  <div class="layout-content" :class="{ 'layout-content--fixed': contentWidth === 'fixed' }">
    <TagsView />
    <div class="layout-content__body">
      <router-view v-slot="{ Component, route: r }">
        <keep-alive :include="tagsStore.cached">
          <component :is="Component" :key="r.path" />
        </keep-alive>
      </router-view>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import TagsView from '@/components/TagsView.vue';
import { useTagsViewStore } from '@/stores/tagsView';
import { useUserProfileStore } from '@/stores/userProfile';

const props = defineProps<{
  /** 可选覆盖；默认读 profile */
  width?: 'fluid' | 'fixed';
}>();

const profileStore = useUserProfileStore();
const tagsStore = useTagsViewStore();

const contentWidth = computed(() => props.width || profileStore.layout.contentWidth);
</script>

<style scoped>
.layout-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  /* 混合导航时与 sider 并排：必须允许横向收缩，否则宽表会撑开整页 */
  min-width: 0;
  min-height: 0;
  overflow-x: hidden;
  background: var(--color-fill-2);
}
.layout-content__body {
  flex: 1;
  min-width: 0;
  padding: 16px;
  overflow: auto;
}
.layout-content--fixed .layout-content__body {
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
}
</style>
