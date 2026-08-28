<template>
  <div class="legacy-chart">
    <div class="legacy-chart-head">
      <span>{{ widget.title || '来自旧图表' }}</span>
      <a-tag size="small" color="orangered">来自旧图表</a-tag>
    </div>
    <a-spin :loading="loading">
      <div v-if="error" class="legacy-chart-err">{{ error }}</div>
      <div v-else ref="chartEl" class="legacy-chart-body" />
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import { useLegacyChartWidget } from './useLegacyChartWidget';

const props = defineProps<WidgetCardProps>();
const { chartEl, loading, error } = useLegacyChartWidget(props);
</script>

<style scoped>
.legacy-chart {
  flex: 1;
  padding: var(--cube-space-md, 16px);
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  min-width: 0;
}
.legacy-chart-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
  font-size: 13px;
  color: var(--color-text-2);
}
.legacy-chart-body {
  width: 100%;
  height: 280px;
}
.legacy-chart-err {
  color: rgb(var(--danger-6));
  font-size: 12px;
}
</style>
