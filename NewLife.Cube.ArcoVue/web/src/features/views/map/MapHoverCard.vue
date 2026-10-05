/** 地图悬停卡片（OSC-261004d7f4）：字段配置的可见列内容 + 光标跟随（含边缘翻转） */
<template>
  <div class="map-hover-card" :style="cardStyle">
    <div class="map-hover-card__title">{{ title }}</div>
    <div class="map-hover-card__body">
      <div v-for="e in entries" :key="e.label" class="map-hover-card__row">
        <span class="map-hover-card__label">{{ e.label }}</span>
        <span class="map-hover-card__value">{{ e.value }}</span>
      </div>
      <div v-if="more" class="map-hover-card__more">+{{ more }} 项</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import { buildHoverEntries, resolveHoverTitle } from './mapHoverEntries';

const props = defineProps<{
  row: Record<string, unknown>;
  fields: readonly FieldMeta[];
  columns: readonly ColumnPref[];
  titles?: Record<string, string>;
  titleField?: string;
  rowKey: string;
  x: number;
  y: number;
  containerWidth: number;
  containerHeight: number;
}>();

const MAX_ROWS = 10;
const CARD_WIDTH = 280;
const CARD_ROW_HEIGHT = 26;
const CARD_BASE_HEIGHT = 64;
const OFFSET = 14;

const title = computed(() => resolveHoverTitle(props.row, props.fields, props.titleField, props.rowKey));

/** 字段配置的可见列（剔除标题字段本身），最多 10 行 */
const visibleColumns = computed(() =>
  props.columns.filter((c) => c.visible && c.key !== props.titleField),
);

const entries = computed(() =>
  buildHoverEntries(props.row, props.fields, props.columns, props.titles, props.titleField, MAX_ROWS),
);

const more = computed(() => Math.max(0, visibleColumns.value.length - MAX_ROWS));

const cardStyle = computed(() => {
  let left = props.x + OFFSET;
  if (props.containerWidth && left + CARD_WIDTH > props.containerWidth) {
    left = Math.max(0, props.x - OFFSET - CARD_WIDTH);
  }
  const estHeight = CARD_BASE_HEIGHT + entries.value.length * CARD_ROW_HEIGHT + (more.value ? 22 : 0);
  let top = props.y + OFFSET;
  if (props.containerHeight && top + estHeight > props.containerHeight) {
    top = Math.max(0, props.y - OFFSET - estHeight);
  }
  return { left: `${left}px`, top: `${top}px` };
});
</script>

<style scoped>
.map-hover-card {
  position: absolute;
  z-index: 20;
  width: 280px;
  padding: 0;
  border-radius: 8px;
  overflow: hidden;
  /* 半透明玻璃：透出底图同时保证字段可读 */
  background: rgba(255, 255, 255, 0.76);
  -webkit-backdrop-filter: blur(10px) saturate(140%);
  backdrop-filter: blur(10px) saturate(140%);
  border: 1px solid rgba(0, 0, 0, 0.08);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  pointer-events: none;
  font-size: 12px;
  line-height: 20px;
}
:global(body[arco-theme='dark']) .map-hover-card {
  background: rgba(35, 38, 44, 0.8);
  border-color: rgba(255, 255, 255, 0.1);
}
/* 标题栏：参考卡片视图（RecordCard）标题栏样式（半透明） */
.map-hover-card__title {
  padding: 8px 12px;
  font-weight: var(--cube-font-weight-medium, 600);
  font-size: 13px;
  color: var(--color-text-1);
  background: rgba(255, 255, 255, 0.4);
  border-bottom: 1px solid rgba(0, 0, 0, 0.06);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
:global(body[arco-theme='dark']) .map-hover-card__title {
  background: rgba(255, 255, 255, 0.06);
  border-bottom-color: rgba(255, 255, 255, 0.08);
}
.map-hover-card__body {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 8px 12px;
}
.map-hover-card__row {
  display: flex;
  gap: 8px;
}
.map-hover-card__label {
  flex: 0 0 84px;
  color: var(--color-text-3);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.map-hover-card__value {
  flex: 1 1 auto;
  color: var(--color-text-2);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.map-hover-card__more {
  color: var(--color-text-3);
}
</style>
