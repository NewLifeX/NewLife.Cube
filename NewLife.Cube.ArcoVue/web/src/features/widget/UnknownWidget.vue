<template>
  <div class="unknown-widget">
    <icon-park type="info" />
    <span>未安装该部件（{{ widget.kind }}）</span>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';

defineProps<Pick<WidgetCardProps, 'widget'>>();
</script>

<style scoped>
.unknown-widget {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: var(--cube-space-md, 16px);
  background: var(--color-fill-2);
  border: 1px dashed var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  color: var(--color-text-3);
  font-size: 13px;
}
</style>
