<template>
  <div class="dc-card" :class="{ 'dc-card--editing': !!canEdit }">
    <div class="dc-head">
      <span class="dc-title">{{ widget.title || '数据卡片' }}</span>
      <WidgetLinkBadge :widget="widget" />
      <a-tooltip content="更多">
        <a-button type="text" size="mini" class="dc-more" @click="openList">
          <icon-park type="share" :size="14" />
        </a-button>
      </a-tooltip>
    </div>
    <div class="dc-main">
      <a-spin :loading="!!loading" class="dc-spin">
        <div v-if="error" class="dc-err">{{ error }}</div>
        <a-empty v-else-if="!rows.length" description="暂无数据" />
        <div v-else class="dc-viewport">
          <div
            class="dc-track"
            :style="{
              gap: cardGap + 'px',
              transform: `translateX(-${trackOffset}px)`,
            }"
          >
            <div
              v-for="(row, idx) in rows"
              :key="rowKeyOf(row, idx)"
              class="dc-item"
              :style="{ width: cardWidth + 'px' }"
            >
              <RecordCard
                :record="row"
                :title="titleOf(row)"
                :image-url="imageOf(row)"
                :body-fields="bodyOf(row)"
                :can-view-detail="false"
                :enable-table-double-click="false"
                :can-edit="false"
                :can-delete="false"
                layout="standard"
                :body-columns="1"
                field-orientation="horizontal"
              />
            </div>
          </div>
        </div>
      </a-spin>
      <div
        v-if="showNav"
        class="dc-side dc-side--prev"
        role="button"
        tabindex="0"
        aria-label="上一张"
        @click.stop="prev"
        @keydown.enter.prevent="prev"
        @keydown.space.prevent="prev"
      >
        <span class="dc-side-btn">
          <icon-park type="left" :size="14" />
        </span>
      </div>
      <div
        v-if="showNav"
        class="dc-side dc-side--next"
        role="button"
        tabindex="0"
        aria-label="下一张"
        @click.stop="next"
        @keydown.enter.prevent="next"
        @keydown.space.prevent="next"
      >
        <span class="dc-side-btn">
          <icon-park type="right" :size="14" />
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import RecordCard from '@/features/views/RecordCard.vue';
import type { WidgetCardProps } from './context';
import WidgetLinkBadge from './WidgetLinkBadge.vue';
import { useDataCardWidget } from './useDataCardWidget';

const props = defineProps<WidgetCardProps>();
const {
  rows,
  titleOf,
  bodyOf,
  imageOf,
  rowKeyOf,
  prev,
  next,
  trackOffset,
  openList,
  cardWidth,
  cardGap,
} = useDataCardWidget(props);

const showNav = computed(() => rows.value.length > 1);
</script>

<style scoped>
.dc-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
  height: 100%;
  padding: 10px 12px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  overflow: hidden;
  box-sizing: border-box;
}
.dc-head {
  display: flex;
  align-items: center;
  gap: 6px;
  height: 22px;
  margin-bottom: 8px;
  padding-right: 0;
  flex-shrink: 0;
  box-sizing: border-box;
}
.dc-card--editing .dc-head {
  padding-right: 52px;
}
.dc-title {
  flex: 1;
  min-width: 0;
  font-size: var(--font-size-body-3, 14px);
  font-weight: 500;
  line-height: 22px;
  color: var(--color-text-2);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.dc-more {
  margin-left: auto;
  flex-shrink: 0;
  width: 22px !important;
  height: 22px !important;
  padding: 0 !important;
  color: var(--color-text-3) !important;
}
.dc-more:hover {
  color: rgb(var(--primary-6)) !important;
}
.dc-main {
  position: relative;
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.dc-spin {
  flex: 1;
  min-height: 0;
  overflow: hidden;
}
.dc-spin :deep(.arco-spin),
.dc-spin :deep(.arco-spin-children) {
  height: 100%;
  min-height: 0;
}
.dc-viewport {
  height: 100%;
  overflow: hidden;
}
.dc-track {
  display: flex;
  align-items: stretch;
  height: 100%;
  transition: transform 0.22s ease;
  will-change: transform;
}
.dc-item {
  flex: 0 0 auto;
  min-width: 0;
  height: 100%;
  overflow: hidden;
}
.dc-item :deep(.record-card) {
  height: 100%;
  min-height: 0;
  overflow: hidden;
}
.dc-item :deep(.record-card-body),
.dc-item :deep(.record-card-fields) {
  overflow: hidden;
}
/* 左右整条热区：垂直居中浮钮；平时半透明，悬停侧边可点并高亮 */
.dc-side {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 36px;
  z-index: 3;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  background: transparent;
  border: none;
  padding: 0;
}
.dc-side--prev {
  left: 0;
}
.dc-side--next {
  right: 0;
}
.dc-side-btn {
  width: 28px;
  height: 48px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  border: 1px solid var(--color-border-2);
  background: var(--color-bg-2);
  color: var(--color-text-3);
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  opacity: 0.4;
  transition: opacity 0.15s ease, color 0.15s ease, background 0.15s ease, border-color 0.15s ease;
  pointer-events: none;
}
.dc-side:hover .dc-side-btn {
  opacity: 1;
  color: rgb(var(--primary-6));
  background: var(--color-bg-1);
  border-color: rgb(var(--primary-3));
}
.dc-err {
  color: rgb(var(--danger-6));
  font-size: var(--font-size-body-1, 12px);
}
</style>
