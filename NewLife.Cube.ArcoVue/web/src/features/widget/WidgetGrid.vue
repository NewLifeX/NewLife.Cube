<template>
  <div ref="rootRef" class="widget-grid">
    <div
      v-for="item in items"
      :key="item.widget.id"
      class="widget-grid-cell"
      :style="{ gridColumn: `span ${item.span}`, minHeight: item.minHeight + 'px' }"
    >
      <slot :widget="item.widget" :min-height="item.minHeight" />
    </div>
  </div>
</template>

<script setup lang="ts">
import type { WidgetInstance } from '@cube/api-core';
import { useWidgetGrid } from './useWidgetGrid';

const props = defineProps<{ widgets: WidgetInstance[] }>();
const { rootRef, items } = useWidgetGrid(props);
</script>

<style scoped>
.widget-grid {
  display: grid;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  column-gap: 12px;
  row-gap: 12px;
  min-width: 0;
}
.widget-grid-cell {
  min-width: 0;
  display: flex;
}
</style>
