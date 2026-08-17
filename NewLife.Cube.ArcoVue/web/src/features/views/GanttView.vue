<template>
  <div class="gantt-view">
    <div
      v-if="!mapping?.plannedStartField || !mapping?.plannedEndField"
      class="view-empty-wrap"
      :style="{ minHeight: height + 'px' }"
    >
      <a-alert type="warning">请在自定义配置中设置计划开始/结束日期字段</a-alert>
    </div>
    <div
      v-else-if="!records.length"
      class="view-empty-wrap"
      :style="{ minHeight: height + 'px' }"
    >
      <a-empty description="暂无数据" />
    </div>
    <template v-else>
      <div ref="host" class="gantt-host" :style="{ height: height + 'px' }" />
      <!-- 等级切换/时间轴重绘等待遮罩：等级改变重绘 canvas 时给用户反馈（列数差异大时重绘可能耗时） -->
      <div v-if="zooming" class="gantt-zoom-mask">
        <a-spin />
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { GanttMapping } from '@/core/utils/viewMapping';
import { useGanttView } from './useGanttView';

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    fields: FieldMeta[];
    mapping?: GanttMapping | null;
    rowKey: string;
    height?: number;
    /** 缩放级别（zoomScale.levels 下标 0~4：年/年月/月·日/周·日/日·时），由父级工具栏控制，默认月·日 */
    zoomLevel?: number;
  }>(),
  { height: 520, zoomLevel: 2 },
);

const emit = defineEmits<{
  detail: [row: Record<string, unknown>];
  /** 拖拽左侧表格宽度后上报新 mapping（tableWidth 随 mapping 持久化，OSC-0019） */
  'mapping-change': [mapping: GanttMapping];
}>();

const { host, zooming } = useGanttView(props, emit);
</script>

<style scoped>
.gantt-view {
  position: relative;
  width: 100%;
}
/* 等级切换/时间轴重绘等待遮罩：半透明覆盖甘特图区域，居中等待图标 */
.gantt-zoom-mask {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 20;
  background: color-mix(in srgb, var(--color-bg-2) 55%, transparent);
  border-radius: 6px;
  pointer-events: none;
}
.gantt-host {
  /* 定位上下文：VTable 分割线（verticalSplitResizeLine）为 absolute，宿主须 relative
     否则分割线相对页面定位（被页头遮挡），左侧表格宽度无法拖拽（OSC-0019） */
  position: relative;
  width: 100%;
  min-height: 240px;
}
</style>
