<template>
  <div class="locked-widget">
    <icon-park type="lock" />
    <div>
      <div>无查看权限</div>
      <div class="locked-path">{{ widget.source?.typePath }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';

defineProps<Pick<WidgetCardProps, 'widget'>>();
</script>

<style scoped>
.locked-widget {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: var(--cube-space-md, 16px);
  background: var(--color-fill-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  color: var(--color-text-3);
  font-size: 13px;
}
.locked-path {
  font-size: 12px;
  margin-top: 4px;
}
</style>
