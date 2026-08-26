<template>
  <div
    class="record-card"
    :class="[
      layoutClass,
      orientationClass,
      { 'record-card--no-image': !imageUrl, 'record-card--side': !!sideFormatColor },
    ]"
    :style="cardShellStyle"
    @dblclick="onDblClick"
  >
    <div class="record-card-header" :style="headerFormatStyle">
      <div class="record-card-title" :style="titleTextStyle" :title="title">{{ title }}</div>
    </div>
    <div class="record-card-body">
      <div v-if="imageUrl" class="record-card-image">
        <!-- 懒加载 + 异步解码：千条卡片时避免图片并发加载/解码阻塞首屏渲染 -->
        <img :src="imageUrl" alt="" loading="lazy" decoding="async" />
      </div>
      <div class="record-card-fields">
        <div
          v-for="item in bodyFields"
          :key="item.key"
          class="record-card-field"
          :class="{
            'record-card-field--full': item.fullRow,
            'record-card-field--badge': !!item.badge,
          }"
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
    </div>
    <div v-if="hasActions" ref="opsRef" class="record-card-actions record-card-ops">
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
      <button
        v-for="link in inlineOpsLinks"
        :key="link.name"
        type="button"
        class="record-card-btn record-card-btn--link"
        @click.stop="$emit('opsLink', link, record)"
      >
        {{ link.label }}
      </button>
      <a-dropdown v-if="overflowOpsLinks.length" trigger="click" @click.stop>
        <button type="button" class="record-card-btn record-card-btn--link" @click.stop>
          更多
        </button>
        <template #content>
          <a-doption
            v-for="link in overflowOpsLinks"
            :key="link.name"
            @click="$emit('opsLink', link, record)"
          >
            {{ link.label }}
          </a-doption>
        </template>
      </a-dropdown>
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
import type { OpsCustomLink } from '@/core/utils/opsAction';
import type { CardBodyField } from './cardHelpers';
import { useRecordCard } from './useRecordCard';

const props = withDefaults(
  defineProps<{
    record: Record<string, unknown>;
    title: string;
    imageUrl?: string;
    bodyFields: CardBodyField[];
    canViewDetail: boolean;
    /** 魔方设置 EnableTableDoubleClick；false 时禁用双击进详情 */
    enableTableDoubleClick?: boolean;
    canEdit: boolean;
    canDelete: boolean;
    opsCustomLinks?: OpsCustomLink[];
    layout?: CardLayout;
    bodyColumns?: CardBodyColumns;
    fieldOrientation?: CardFieldOrientation;
    /** 等高：所有卡片统一最小高度（由 CardList 取最高卡片下发） */
    minHeight?: number;
    titleFormatColor?: string;
    titleFormatBold?: boolean;
    sideFormatColor?: string;
  }>(),
  {
    layout: 'standard',
    bodyColumns: 2,
    fieldOrientation: 'vertical',
    minHeight: 0,
    enableTableDoubleClick: true,
    opsCustomLinks: () => [],
  },
);

const emit = defineEmits<{
  detail: [row: Record<string, unknown>];
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
  toggleEnable: [row: Record<string, unknown>, field: string];
  opsLink: [link: OpsCustomLink, row: Record<string, unknown>];
}>();

function onDblClick() {
  if (props.canViewDetail && props.enableTableDoubleClick !== false) {
    emit('detail', props.record);
  }
}

const headerFormatStyle = computed(() => {
  if (!props.titleFormatColor) return undefined;
  return { backgroundColor: props.titleFormatColor };
});

const titleTextStyle = computed(() => {
  if (!props.titleFormatBold) return undefined;
  return { fontWeight: '700' };
});

const {
  badgeStyle,
  layoutClass,
  orientationClass,
  cardShellStyle,
  hasActions,
  opsRef,
  inlineOpsLinks,
  overflowOpsLinks,
} = useRecordCard(props);
</script>

<style scoped>
/* 结构对齐 Arco Card：标题栏 / 内容区 / 脚注栏，栏与内容用分隔线切开。
   不用 <a-card>：千条卡片实例化成本过高，视觉与 Admin/Db hoverable 同源。 */
.record-card {
  position: relative;
  border: 1px solid var(--color-border-2);
  border-radius: var(--border-radius-medium, 8px);
  padding: 0;
  overflow: hidden;
  background: var(--color-bg-2);
  display: grid;
  gap: 0;
  min-width: 0;
  grid-template-areas:
    'header'
    'body'
    'actions';
  grid-template-rows: auto 1fr auto;
  transition: box-shadow 0.2s cubic-bezier(0, 0, 1, 1);
}
.record-card:hover {
  z-index: 1;
  box-shadow: 0 4px 10px rgb(var(--gray-2));
}
:global(body[arco-theme='dark']) .record-card:hover {
  box-shadow: 0 4px 10px rgba(var(--gray-1), 40%);
}
.record-card-header {
  grid-area: header;
  display: flex;
  align-items: center;
  min-width: 0;
  padding: 10px 16px;
  border-bottom: 1px solid var(--color-border-2);
  box-sizing: border-box;
}
.record-card-title {
  flex: 1;
  min-width: 0;
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
.record-card-body {
  grid-area: body;
  display: grid;
  gap: 8px;
  min-width: 0;
  min-height: 0;
  padding: 12px 16px;
  box-sizing: border-box;
}
.record-card-image {
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
.record-card--orient-horizontal .record-card-field--badge {
  /* 标签与状态徽标同一水平中线，避免徽标 padding 相对基线下沉/上浮 */
  align-items: center;
}
.record-card--orient-horizontal .record-card-field .label {
  flex-shrink: 0;
  min-width: 3em;
}
.record-card--orient-horizontal .record-card-field--badge .label {
  display: inline-flex;
  align-items: center;
  line-height: 1.6;
}
.record-card--orient-horizontal .record-card-field .label::after {
  content: '：';
}
.record-card--orient-horizontal .record-card-field .value {
  flex: 1;
  min-width: 0;
}
.record-card--orient-horizontal .record-card-field--badge .record-card-badge {
  align-self: center;
  display: inline-flex;
  align-items: center;
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
.record-card-actions {
  grid-area: actions;
  display: flex;
  flex-wrap: nowrap;
  justify-content: flex-start;
  align-items: center;
  gap: 6px;
  min-width: 0;
  overflow: hidden;
  padding: 8px 16px;
  border-top: 1px solid var(--color-border-2);
  box-sizing: border-box;
}

/* 操作按钮：原生 button 替代 Arco a-button——组件实例化/卸载成本高，千条卡片翻页/懒加载重建时显著拖慢性能；
   样式用 Arco 语义变量模拟 secondary mini 按钮（fill-2 底 + text-1 字），视觉保持一致 */
.record-card-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  height: 24px;
  padding: 0 12px;
  border: none;
  border-radius: 4px;
  background: var(--color-fill-2);
  color: var(--color-text-1);
  font-size: var(--cube-font-size-meta);
  line-height: 1;
  white-space: nowrap;
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
.record-card-btn--link {
  color: rgb(var(--link-6));
}
.record-card-btn--link:hover {
  color: rgb(var(--link-5));
  background: rgba(var(--link-6), 0.08);
}

.record-card--row:not(.record-card--no-image) .record-card-body {
  grid-template-columns: 180px 1fr;
  align-items: start;
  gap: 12px;
}
.record-card--row .record-card-image img {
  width: 180px;
  height: 180px;
}

@media (max-width: 639px) {
  .record-card--row:not(.record-card--no-image) .record-card-body {
    grid-template-columns: 1fr;
  }
  .record-card--row .record-card-image img {
    width: 100%;
    height: 140px;
  }
}
</style>
