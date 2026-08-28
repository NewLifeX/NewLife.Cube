<template>
  <div class="mini-chart">
    <div class="mini-chart-head">
      <a-input
        v-if="titleEditing"
        ref="inputRef"
        v-model="titleDraft"
        size="mini"
        class="mini-chart-title-input"
        :max-length="40"
        @click.stop
        @blur="commitTitle"
        @keydown="onTitleKeydown"
      />
      <span
        v-else
        class="mini-chart-title"
        :class="{ editable: canEdit }"
        @click="startTitleEdit"
      >{{ widget.title || '未命名' }}</span>
      <WidgetLinkBadge :widget="widget" />
    </div>
    <div class="mini-chart-main">
      <div v-if="error" class="mini-chart-err">{{ error }}</div>
      <div v-else-if="empty" class="mini-chart-empty">暂无数据</div>
      <div
        v-show="!error && !empty"
        ref="chartEl"
        class="mini-chart-body"
        :class="{ 'mini-chart-body--drill': drillable }"
      />
      <div v-if="loading" class="mini-chart-mask">
        <a-spin />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import WidgetLinkBadge from './WidgetLinkBadge.vue';
import { useMiniChartWidget } from './useMiniChartWidget';
import { useWidgetTitleEdit } from './useWidgetTitleEdit';

const props = defineProps<WidgetCardProps>();
const { chartEl, empty, drillable } = useMiniChartWidget(props);
const {
  editing: titleEditing,
  draft: titleDraft,
  inputRef,
  startEdit: startTitleEdit,
  commit: commitTitle,
  onKeydown: onTitleKeydown,
} = useWidgetTitleEdit(props);
</script>

<style scoped>
.mini-chart {
  flex: 1;
  width: 100%;
  min-width: 0;
  min-height: 0;
  height: 100%;
  display: flex;
  flex-direction: column;
  padding: 8px 12px 4px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  overflow: hidden;
}
.mini-chart-head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 2px;
  padding-right: 52px;
  flex-shrink: 0;
  line-height: 22px;
}
.mini-chart-title {
  flex: 1;
  font-size: var(--font-size-body-3, 14px);
  font-weight: 500;
  color: var(--color-text-2);
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.mini-chart-title.editable {
  cursor: text;
}
.mini-chart-title.editable:hover {
  color: rgb(var(--primary-6));
  text-decoration: underline;
  text-underline-offset: 2px;
}
.mini-chart-title-input {
  flex: 1;
  min-width: 0;
}
.mini-chart-main {
  position: relative;
  width: 100%;
  min-width: 0;
  min-height: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.mini-chart-body {
  flex: 1;
  width: 100%;
  min-width: 0;
  min-height: 0;
  height: 100%;
}
.mini-chart-body--drill {
  cursor: pointer;
}
.mini-chart-err {
  color: rgb(var(--danger-6));
  font-size: 12px;
}
.mini-chart-empty {
  flex: 1;
  min-height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-3);
  font-size: 12px;
}
.mini-chart-mask {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--color-bg-2) 70%, transparent);
}
</style>
