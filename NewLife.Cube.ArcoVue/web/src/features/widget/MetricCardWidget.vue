<template>
  <div class="metric-card" @click="onCardClick">
    <div class="metric-head">
      <icon-park :type="icon" class="metric-ico" />
      <a-input
        v-if="titleEditing"
        ref="inputRef"
        v-model="titleDraft"
        size="mini"
        class="metric-title-input"
        :max-length="40"
        @click.stop
        @blur="commitTitle"
        @keydown="onTitleKeydown"
      />
      <span
        v-else
        class="metric-title"
        :class="{ editable: canEdit }"
        @click="startTitleEdit"
      >{{ widget.title || '未命名' }}</span>
      <WidgetLinkBadge :widget="widget" />
    </div>
    <a-spin :loading="loading" class="metric-spin">
      <div v-if="error" class="metric-err">{{ error }}</div>
      <div v-else class="metric-body">
        <div class="metric-value" :style="{ color }">{{ valueText }}</div>
        <div v-if="labelText" class="metric-label">{{ labelText }}</div>
      </div>
    </a-spin>
    <div v-if="sparkOption" ref="sparkEl" class="metric-spark" />
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import WidgetLinkBadge from './WidgetLinkBadge.vue';
import { useMetricCardWidget } from './useMetricCardWidget';
import { useWidgetTitleEdit } from './useWidgetTitleEdit';

const props = defineProps<WidgetCardProps>();
const { valueText, labelText, sparkOption, color, icon, sparkEl, onClick } = useMetricCardWidget(props);
const {
  editing: titleEditing,
  draft: titleDraft,
  inputRef,
  startEdit: startTitleEdit,
  commit: commitTitle,
  onKeydown: onTitleKeydown,
} = useWidgetTitleEdit(props);

function onCardClick() {
  if (titleEditing.value) return;
  onClick();
}
</script>

<style scoped>
.metric-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 10px 12px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  cursor: pointer;
  min-width: 0;
  min-height: 0;
}
.metric-head {
  display: flex;
  align-items: center;
  gap: 6px;
  padding-right: 52px;
  flex-shrink: 0;
}
.metric-ico {
  color: rgb(var(--primary-6));
}
.metric-title {
  flex: 1;
  font-size: var(--font-size-body-3, 14px);
  font-weight: 500;
  color: var(--color-text-2);
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.metric-title.editable:hover {
  color: rgb(var(--primary-6));
  text-decoration: underline;
  text-underline-offset: 2px;
}
.metric-title-input {
  flex: 1;
  min-width: 0;
}
.metric-spin {
  flex: 1;
  width: 100%;
  min-height: 0;
  display: flex;
}
.metric-spin :deep(.arco-spin) {
  flex: 1;
  width: 100%;
  display: flex;
}
.metric-spin :deep(.arco-spin-children) {
  flex: 1;
  width: 100%;
  display: flex;
  min-height: 0;
}
.metric-body {
  flex: 1;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 36px;
  gap: 4px;
}
.metric-label {
  font-size: var(--font-size-body-1, 12px);
  color: var(--color-text-3);
  line-height: 1.2;
  text-align: center;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.metric-value {
  font-size: 32px;
  font-weight: 700;
  line-height: 1.1;
  text-align: center;
}
.metric-err {
  color: rgb(var(--danger-6));
  font-size: 12px;
  margin: auto;
}
.metric-spark {
  height: 24px;
  margin-top: 4px;
  flex-shrink: 0;
}
</style>
