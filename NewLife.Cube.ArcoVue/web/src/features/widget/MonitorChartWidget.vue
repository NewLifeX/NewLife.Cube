<template>
  <div class="mon-card">
    <div class="mon-title">{{ widget.title || '性能监控' }}</div>
    <a-spin :loading="loading" class="mon-spin">
      <div v-if="error" class="mon-err">{{ error }}</div>
      <div v-else ref="chartEl" class="mon-chart" />
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import { useMonitorChartWidget } from './useMonitorChartWidget';

const props = defineProps<WidgetCardProps>();
const { chartEl } = useMonitorChartWidget(props);
</script>

<style scoped>
.mon-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  /* 与迷你图表卡片上下边距一致 */
  padding: 8px 12px 4px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  min-width: 0;
  min-height: 0;
  height: 100%;
  overflow: hidden;
}
.mon-title {
  font-size: var(--font-size-body-3, 14px);
  font-weight: 500;
  color: var(--color-text-2);
  margin-bottom: 2px;
  flex-shrink: 0;
  line-height: 22px;
}
.mon-spin {
  flex: 1;
  min-height: 0;
  width: 100%;
  display: block;
  overflow: hidden;
}
.mon-spin :deep(.arco-spin) {
  display: flex;
  flex-direction: column;
  width: 100%;
  height: 100%;
  max-height: 100%;
}
.mon-spin :deep(.arco-spin-children) {
  flex: 1;
  min-height: 0;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.mon-chart {
  flex: 1;
  min-height: 0;
  width: 100%;
  height: 100%;
}
.mon-err {
  color: rgb(var(--danger-6));
  font-size: 12px;
}
</style>
