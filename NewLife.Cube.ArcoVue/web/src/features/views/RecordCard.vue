<template>
  <div
    class="record-card"
    :class="[
      layoutClass,
      orientationClass,
      { 'record-card--no-image': !imageUrl },
    ]"
    :style="cardCssVars"
    @dblclick="$emit('detail', record)"
  >
    <div class="record-card-title">{{ title }}</div>
    <div v-if="imageUrl" class="record-card-image">
      <!-- 懒加载 + 异步解码：千条卡片时避免图片并发加载/解码阻塞首屏渲染 -->
      <img :src="imageUrl" alt="" loading="lazy" decoding="async" />
    </div>
    <div class="record-card-fields">
      <div
        v-for="item in bodyFields"
        :key="item.key"
        class="record-card-field"
        :class="{ 'record-card-field--full': item.fullRow }"
      >
        <span class="label">{{ item.label }}</span>
        <span
          v-if="item.badge"
          class="record-card-badge"
          :class="{ 'record-card-badge--toggle': item.enableToggle }"
          :style="badgeStyle(item)"
          @click.stop="item.enableToggle && $emit('toggleEnable', record, item.key)"
        >
          {{ item.badge.label }}
        </span>
        <span v-else class="value">{{ item.value }}</span>
      </div>
    </div>
    <div class="record-card-ops">
      <button
        v-if="canViewDetail"
        type="button"
        class="record-card-btn"
        @click.stop="$emit('detail', record)"
      >
        详情
      </button>
      <button v-if="canEdit" type="button" class="record-card-btn" @click.stop="$emit('edit', record)">
        编辑
      </button>
      <button
        v-if="canDelete"
        type="button"
        class="record-card-btn record-card-btn--danger"
        @click.stop="$emit('delete', record)"
      >
        删除
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type {
  CardBodyColumns,
  CardFieldOrientation,
  CardLayout,
} from '@/core/utils/viewMapping';
import type { CardBodyField } from './cardHelpers';

const props = withDefaults(
  defineProps<{
    record: Record<string, unknown>;
    title: string;
    imageUrl?: string;
    bodyFields: CardBodyField[];
    canViewDetail: boolean;
    canEdit: boolean;
    canDelete: boolean;
    layout?: CardLayout;
    bodyColumns?: CardBodyColumns;
    fieldOrientation?: CardFieldOrientation;
    /** 等高：所有卡片统一最小高度（由 CardList 取最高卡片下发） */
    minHeight?: number;
  }>(),
  {
    layout: 'standard',
    bodyColumns: 2,
    fieldOrientation: 'vertical',
    minHeight: 0,
  },
);

/** 徽标样式：浅底 + 同色文字（与列表徽章一致） */
function badgeStyle(item: CardBodyField): Record<string, string> | undefined {
  const b = item.badge;
  if (!b) return undefined;
  return {
    backgroundColor: b.buttonColor,
    borderColor: b.buttonBorderColor,
    color: b.textColor,
  };
}

defineEmits<{
  detail: [row: Record<string, unknown>];
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
  toggleEnable: [row: Record<string, unknown>, field: string];
}>();

const cols = computed(() => {
  const n = props.bodyColumns;
  return n === 1 || n === 3 ? n : 2;
});

const layoutClass = computed(() => `record-card--${props.layout || 'standard'}`);
const orientationClass = computed(
  () => `record-card--orient-${props.fieldOrientation === 'horizontal' ? 'horizontal' : 'vertical'}`,
);

/** 用 CSS 变量驱动列数，避免动态 class 未命中时样式不生效；同时下发等高 min-height */
const cardCssVars = computed(() => ({
  '--record-card-cols': String(cols.value),
  ...(props.minHeight && props.minHeight > 0
    ? { minHeight: `${props.minHeight}px` }
    : {}),
}));
</script>

<style scoped>
.record-card {
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  padding: 12px;
  background: var(--color-bg-2);
  display: grid;
  /* 紧凑排版：收紧标题/图片/字段/操作区之间的间隙 */
  gap: 4px;
  min-width: 0;
  grid-template-areas:
    'title'
    'image'
    'fields'
    'ops';
}
.record-card-title {
  grid-area: title;
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
  word-break: break-all;
}
.record-card-image {
  grid-area: image;
  width: 100%;
  overflow: hidden;
  border-radius: 6px;
  background: var(--color-fill-1);
}
.record-card-image img {
  width: 100%;
  height: 140px;
  object-fit: cover;
  display: block;
}
.record-card--large .record-card-image img {
  height: 180px;
}
.record-card-fields {
  grid-area: fields;
  display: grid;
  grid-template-columns: repeat(var(--record-card-cols, 2), minmax(0, 1fr));
  gap: 6px 12px;
  font-size: var(--cube-font-size-meta);
  align-content: start;
  min-width: 0;
}
.record-card-field--full {
  grid-column: 1 / -1;
}
.record-card-field {
  min-width: 0;
  display: flex;
  gap: 2px;
}
.record-card--orient-vertical .record-card-field {
  flex-direction: column;
}
.record-card--orient-horizontal .record-card-field {
  flex-direction: row;
  align-items: baseline;
  gap: 8px;
}
.record-card--orient-horizontal .record-card-field .label {
  flex-shrink: 0;
  min-width: 3em;
}
.record-card--orient-horizontal .record-card-field .label::after {
  content: '：';
}
.record-card--orient-horizontal .record-card-field .value {
  flex: 1;
  min-width: 0;
}
.record-card--orient-horizontal .record-card-field .record-card-badge {
  /* 横向排版：徽标与前方标签垂直居中对齐，不随文本基线下沉 */
  align-self: center;
}
.record-card-field .label {
  color: var(--color-text-3);
}
.record-card-field .value {
  color: var(--color-text-1);
  word-break: break-all;
}
/* 状态/枚举/值集徽标（与列表徽章一致：浅底 + 同色文字）；Enable 徽标可点击 */
/* align-self:flex-start 防止 vertical 布局下被 flex 交叉轴拉伸，宽度严格按文案自适应 */
.record-card-badge {
  display: inline-block;
  align-self: flex-start;
  max-width: 100%;
  box-sizing: border-box;
  padding: 2px 8px;
  border: 1px solid transparent;
  border-radius: 4px;
  font-size: var(--cube-font-size-meta);
  line-height: 1.6;
  white-space: nowrap;
  cursor: default;
  user-select: none;
}
.record-card-badge--toggle {
  cursor: pointer;
}
.record-card-ops {
  grid-area: ops;
  display: flex;
  justify-content: flex-start;
  gap: 6px;
  margin-top: auto;
  padding-top: 2px;
}

/* 操作按钮：原生 button 替代 Arco a-button——组件实例化/卸载成本高，千条卡片翻页/懒加载重建时显著拖慢性能；
   样式用 Arco 语义变量模拟 secondary mini 按钮（fill-2 底 + text-1 字），视觉保持一致 */
.record-card-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 24px;
  padding: 0 12px;
  border: none;
  border-radius: 4px;
  background: var(--color-fill-2);
  color: var(--color-text-1);
  font-size: var(--cube-font-size-meta);
  line-height: 1;
  cursor: pointer;
  transition: color 0.2s, background-color 0.2s;
}
.record-card-btn:hover {
  background: var(--color-fill-3);
}
.record-card-btn--danger {
  color: rgb(var(--danger-6));
}
.record-card-btn--danger:hover {
  color: rgb(var(--danger-6));
  background: rgba(var(--danger-6), 0.08);
}

.record-card--row:not(.record-card--no-image) {
  grid-template-columns: 180px 1fr;
  grid-template-areas:
    'image title'
    'image fields'
    'image ops';
  align-items: start;
}
.record-card--row .record-card-image img {
  width: 180px;
  height: 180px;
}

@media (max-width: 639px) {
  .record-card--row:not(.record-card--no-image) {
    grid-template-columns: 1fr;
    grid-template-areas:
      'title'
      'image'
      'fields'
      'ops';
  }
  .record-card--row .record-card-image img {
    width: 100%;
    height: 140px;
  }
}
</style>
