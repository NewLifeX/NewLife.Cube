<template>
  <a-tooltip v-if="show" content="未联动：跨实体部件未配置字段映射，不会随列表筛选变化">
    <span class="link-badge" aria-label="未联动" @click.stop>
      <icon-park type="unlink" :size="12" />
      <span class="link-badge__text">未联动</span>
    </span>
  </a-tooltip>
</template>

<script setup lang="ts">
import { computed, inject, toRef } from 'vue';
import type { WidgetInstance } from '@cube/api-core';
import { WIDGET_SURFACE_KEY } from './context';
import { isUnlinkedWidget } from './legacy';

const props = defineProps<{
  widget: WidgetInstance;
  /** 可选覆盖；默认取 surface.hostTypePath */
  hostTypePath?: string;
}>();

const surface = inject(WIDGET_SURFACE_KEY, null);
const widgetRef = toRef(props, 'widget');

const show = computed(() => {
  const host = props.hostTypePath || surface?.hostTypePath;
  return isUnlinkedWidget(widgetRef.value, host);
});
</script>

<style scoped>
.link-badge {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  gap: 2px;
  height: 18px;
  padding: 0 5px;
  border-radius: 4px;
  cursor: default;
  color: rgb(var(--warning-6));
  background: color-mix(in srgb, rgb(var(--warning-6)) 14%, transparent);
  font-size: 11px;
  line-height: 1;
  position: relative;
  z-index: 3;
}
.link-badge__text {
  white-space: nowrap;
}
</style>
