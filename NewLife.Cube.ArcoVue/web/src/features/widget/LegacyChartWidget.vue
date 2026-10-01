<template>
  <div
    class="legacy-chart"
    :class="{
      'legacy-chart--fixed': fixed,
      'legacy-chart--tall': tall,
    }"
  >
    <div class="legacy-chart-head">
      <span class="legacy-chart-title">{{ widget.title || '未命名' }}</span>
    </div>
    <div class="legacy-chart-main">
      <div v-if="error" class="legacy-chart-err">{{ error }}</div>
      <div
        v-show="!error"
        ref="chartEl"
        class="legacy-chart-body"
      />
      <div v-if="loading" class="legacy-chart-mask">
        <a-spin />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { WidgetCardProps } from './context';
import { isFixedDeveloperChart } from '@/core/utils/chartOptions';
import { useLegacyChartWidget } from './useLegacyChartWidget';

const props = defineProps<WidgetCardProps>();
const { chartEl, loading, error } = useLegacyChartWidget(props);
const fixed = computed(() => isFixedDeveloperChart(props.widget.id));
const tall = computed(() => !!(props.widget as Record<string, unknown>).tall);
</script>

<style scoped>
/* 与洞察区迷你图表部件同壳：标题条 + 卡片边框，不接设置抽屉 */
.legacy-chart {
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
  box-sizing: border-box;
}
.legacy-chart-head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 2px;
  padding-right: calc(var(--widget-ops-w, 22px) + 7px);
  flex-shrink: 0;
  line-height: 22px;
}
.legacy-chart-title {
  flex: 1;
  font-size: var(--font-size-body-3, 14px);
  font-weight: 500;
  color: var(--color-text-2);
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.legacy-chart-main {
  position: relative;
  width: 100%;
  min-width: 0;
  min-height: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.legacy-chart-body {
  flex: 1;
  width: 100%;
  min-width: 0;
  min-height: 0;
  height: 100%;
}
/* 固定开发者图：整行略压；箱线/K 线略增高 */
.legacy-chart--fixed .legacy-chart-body {
  flex: none;
  height: 140px;
}
.legacy-chart--fixed.legacy-chart--tall .legacy-chart-body {
  height: 220px;
}
.legacy-chart-err {
  color: rgb(var(--danger-6));
  font-size: 12px;
}
.legacy-chart-mask {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--color-bg-2) 70%, transparent);
}
</style>
