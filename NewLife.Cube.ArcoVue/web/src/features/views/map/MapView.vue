<template>
  <div class="map-view" :style="height ? { height: height + 'px' } : undefined">
    <div ref="canvasRef" class="map-view__canvas" />
    <div class="map-view__toolbar">
      <slot name="toolbar" />
    </div>
    <!-- 比例尺（定位按钮左侧）：当前缩放对应的地图标尺 -->
    <div v-if="ready && scaleText" class="map-view__scale">
      <span class="map-view__scale-bar" :style="{ width: scaleWidth + 'px' }" />
      <span class="map-view__scale-text">{{ scaleText }}</span>
    </div>
    <!-- 定位：点击回到自定义配置的地图中心；悬浮显示绘制/加载/层级统计 -->
    <a-tooltip v-if="ready" position="left" :content-style="{ maxWidth: '280px' }">
      <button type="button" class="map-view__locate" @click="onLocate">
        <icon-park type="local" />
      </button>
      <template #content>
        <div class="map-view__stats">
          <div>已绘制 {{ drawnCount }} / 已加载 {{ loadedCount }}<template v-if="total"> / 共 {{ total }}</template></div>
          <div v-if="levelField">
            层级 L1<template v-if="activeLevel > 1">–L{{ activeLevel }}</template> · 无效点 {{ skippedCount }}
          </div>
          <div v-else>无效点 {{ skippedCount }}</div>
          <div v-if="loadingMore || levelBusy">加载中…</div>
        </div>
      </template>
    </a-tooltip>
    <MapHoverCard
      v-if="hover && ready"
      :row="hover.point.row"
      :fields="fields"
      :columns="columns"
      :titles="titles"
      :title-field="mapping?.titleField"
      :row-key="rowKey"
      :x="hover.x"
      :y="hover.y"
      :container-width="size.w"
      :container-height="size.h"
    />
    <div v-if="initializing" class="map-view__notice">
      <a-spin dot />
    </div>
    <div v-else-if="error" class="map-view__notice">
      <a-empty :description="error">
        <a-button type="primary" size="small" @click="refresh">重试</a-button>
      </a-empty>
    </div>
    <div v-else-if="!ready" class="map-view__notice">
      <a-empty description="地图服务未配置" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import type { FieldMeta } from '@/core/types/field';
import type { MapMapping } from '@/core/utils/viewMapping';
import type { ColumnPref, ViewFormatRule } from '@/core/utils/viewProfile';
import MapHoverCard from './MapHoverCard.vue';
import { useMapView, type MapViewPersistState } from './useMapView';

const props = defineProps<{
  /** 已加载行（父级首批数据；续页由 loadPage 提供） */
  records: readonly Record<string, unknown>[];
  fields: readonly FieldMeta[];
  /** 字段配置的可见列（悬停卡片内容 = 此处可见列） */
  columns: readonly ColumnPref[];
  titles?: Record<string, string>;
  mapping: MapMapping | null;
  rowKey: string;
  formatRules?: readonly ViewFormatRule[];
  /** 后端总行数 */
  total?: number;
  /** 强制桩模式（E2E 注入） */
  stub?: boolean;
  /** 续页加载（pageIndex 为 1 基；null=无更多） */
  loadPage?: (pageIndex: number) => Promise<readonly Record<string, unknown>[] | null>;
  /** 分层加载（地区实体）：按层级字段逐层拉取（level 从 1=省/直辖市/港澳台 起） */
  loadLevelPage?: (
    levelField: string,
    level: number,
    pageIndex: number,
  ) => Promise<readonly Record<string, unknown>[] | null>;
  /** 父级列表测高（px；>0 时优先，保证与列表视图同款且不出现竖直滚动条） */
  height?: number;
  /** 定位查询（地图中心）：按标题字段搜索一行 */
  loadLocateRow?: (field: string, value: string) => Promise<Record<string, unknown> | null>;
}>();

const emit = defineEmits<{
  detail: [row: Record<string, unknown>];
  'viewport-persist': [state: MapViewPersistState];
}>();

const canvasRef = ref<HTMLElement | null>(null);

// 生命周期与数据监听在 useMapView 内注册（SFC 构薄门禁）
const {
  ready,
  error,
  initializing,
  zoom,
  drawnCount,
  loadedCount,
  skippedCount,
  loadingMore,
  hover,
  size,
  height,
  levelField,
  activeLevel,
  levelBusy,
  refresh,
  zoomIn,
  zoomOut,
  locate,
  focusRow,
  satellite,
  toggleSatellite,
  scaleWidth,
  scaleText,
} = useMapView({
  container: canvasRef,
  records: () => props.records,
  fields: () => props.fields,
  mapping: () => props.mapping,
  rowKey: () => props.rowKey,
  formatRules: () => props.formatRules ?? [],
  total: () => props.total ?? 0,
  loadPage: (i) => props.loadPage?.(i) ?? Promise.resolve(null),
  loadLevelPage: (f, l, i) => props.loadLevelPage?.(f, l, i) ?? Promise.resolve(null),
  height: () => props.height ?? 0,
  loadLocateRow: (f, v) => props.loadLocateRow?.(f, v) ?? Promise.resolve(null),
  stub: () => props.stub ?? false,
  onDetail: (row) => emit('detail', row),
  onViewPersist: (state) => emit('viewport-persist', state),
});

/** 定位失败提示（未配置中心点或未找到对应对象） */
async function onLocate() {
  const ok = await locate();
  if (!ok) Message.info('未设置地图中心点，或未找到对应对象；请在「自定义配置 → 地图区」设置');
}

defineExpose({ ready, zoom, zoomIn, zoomOut, refresh, locate, focusRow, satellite, toggleSatellite });
</script>

<style>
/* 标记 DOM 在 SDK 覆盖物/桩层中渲染，需全局样式 */
.cube-map-marker {
  display: flex;
  align-items: center;
  justify-content: center;
  filter: drop-shadow(0 1px 3px rgba(0, 0, 0, 0.35));
  cursor: pointer;
  transition: transform 0.12s;
}
.cube-map-marker:hover {
  transform: scale(1.12);
}
.cube-map-marker svg {
  display: block;
}
.cube-map-stub {
  background:
    repeating-linear-gradient(45deg, rgba(22, 93, 255, 0.05) 0 12px, transparent 12px 24px),
    radial-gradient(circle at 30% 20%, rgba(22, 93, 255, 0.08), transparent 60%);
}
/* 桩模式：深色主题 / 卫星底图标记（E2E 断言用） */
.cube-map-stub--dark {
  background-color: #15181e;
}
.cube-map-stub--satellite {
  background-color: #1b2a20;
}
/* 定位按钮悬浮统计卡片（tooltip 内容渲染到 body，需全局样式） */
.map-view__stats {
  display: flex;
  flex-direction: column;
  gap: 2px;
  font-size: 12px;
  line-height: 18px;
}
/* 比例尺暗色主题覆盖：白字黑晕，深色/卫星底图上清晰可读（须全局样式：scoped 内 :global 在本工程不生效） */
body[arco-theme='dark'] .map-view__scale {
  color: rgba(255, 255, 255, 0.95);
  filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.9));
}
</style>

<style scoped>
.map-view {
  position: relative;
  width: 100%;
  height: clamp(460px, calc(100vh - 340px), 1200px);
  min-height: 420px;
  border-radius: 8px;
  overflow: hidden;
  background: var(--color-bg-2);
}
.map-view__canvas {
  position: absolute;
  inset: 0;
}
.map-view__locate {
  position: absolute;
  /* 与右上工具栏右缘对齐（.map-view__toolbar 同为 right:12） */
  right: 12px;
  bottom: 8px;
  z-index: 15;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  padding: 0;
  border: 0;
  border-radius: 50%;
  color: #fff;
  background: rgba(0, 0, 0, 0.55);
  -webkit-backdrop-filter: blur(8px);
  backdrop-filter: blur(8px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.18);
  cursor: pointer;
  transition: background 0.15s;
}
.map-view__locate:hover {
  background: rgba(0, 0, 0, 0.72);
}
.map-view__locate :deep(svg) {
  display: block;
}
/* 比例尺：定位按钮左侧同排（右缘 12+36+8），仅标尺线 + 文字，无容器背景不遮挡地图 */
.map-view__scale {
  position: absolute;
  right: 56px;
  bottom: 19px;
  z-index: 15;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  color: rgba(0, 0, 0, 0.68);
  font-size: 11px;
  font-weight: 400;
  line-height: 1;
  pointer-events: none;
  /* 浅色底图：黑字白晕，深浅底图上均可读 */
  filter: drop-shadow(0 1px 2px rgba(255, 255, 255, 0.9));
}
.map-view__scale-bar {
  display: inline-block;
  height: 6px;
  border: 1px solid currentColor;
  border-top: none;
  box-sizing: border-box;
}
.map-view__scale-text {
  white-space: nowrap;
}
.map-view__toolbar {
  position: absolute;
  top: 12px;
  right: 12px;
  z-index: 15;
}
.map-view__notice {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
