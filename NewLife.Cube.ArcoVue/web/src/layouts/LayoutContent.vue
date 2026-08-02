<template>
  <div class="layout-content">
    <TagsView />
    <!-- 滚动 + 外边距在外层，避免子内容撑宽后裁掉右侧 padding -->
    <div class="layout-content__scroll">
      <div class="layout-content__body" :class="contentWidthClass">
        <router-view v-slot="{ Component, route: r }">
          <keep-alive :include="tagsStore.cached">
            <component :is="Component" :key="r.path" />
          </keep-alive>
        </router-view>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import TagsView from '@/components/TagsView.vue';
import { useTagsViewStore } from '@/stores/tagsView';
import { useUserProfileStore } from '@/stores/userProfile';
import type { ContentWidth } from '@/core/utils/userProfile';

const props = defineProps<{
  /** 可选覆盖；默认读 profile */
  width?: ContentWidth;
}>();

const profileStore = useUserProfileStore();
const tagsStore = useTagsViewStore();

const contentWidth = computed<ContentWidth>(
  () => props.width || profileStore.layout.contentWidth,
);

const contentWidthClass = computed(() => {
  const w = contentWidth.value;
  if (w === 'standard') return 'layout-content__body--standard';
  if (w === 'wide') return 'layout-content__body--wide';
  return 'layout-content__body--fluid';
});
</script>

<style scoped>
.layout-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  /* 混合导航时与 sider 并排：必须允许横向收缩，否则宽表会撑开整页 */
  min-width: 0;
  min-height: 0;
  overflow: hidden;
  background: var(--color-fill-2);
}
.layout-content__scroll {
  flex: 1;
  min-width: 0;
  min-height: 0;
  overflow: auto;
  /* 水平 gutter 固定在滚动层，宽/流式时也不会被内部溢出吃掉 */
  padding: 16px;
  box-sizing: border-box;
}
.layout-content__body {
  min-width: 0;
  width: 100%;
  margin: 0 auto;
  box-sizing: border-box;
}
/* 标准：常见笔记本；较宽：大屏；流式：占满可用宽度（仍保留外层 16px gutter） */
.layout-content__body--standard {
  max-width: 1200px;
}
.layout-content__body--wide {
  max-width: 1600px;
}
.layout-content__body--fluid {
  max-width: none;
}
</style>
