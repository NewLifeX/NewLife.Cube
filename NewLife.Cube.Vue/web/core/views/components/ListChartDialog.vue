<script setup lang="ts">
/**
 * 图表弹窗（GetChartData 数据渲染）
 *
 * 后端 `GetChartData` 返回 ECharts 配置数组（每个元素含 title / option），
 * 本组件按需注册常用 ECharts 组件并逐个渲染。
 * 由列表页懒加载（defineAsyncComponent），仅在点击「图表」时载入 echarts。
 */
import VChart from 'vue-echarts';
import { use } from 'echarts/core';
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DataZoomComponent,
  ToolboxComponent,
} from 'echarts/components';
import { BarChart, LineChart, PieChart } from 'echarts/charts';
import { LabelLayout } from 'echarts/features';
import { CanvasRenderer } from 'echarts/renderers';

use([
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DataZoomComponent,
  ToolboxComponent,
  BarChart,
  LineChart,
  PieChart,
  LabelLayout,
  CanvasRenderer,
]);

interface ChartItem {
  title?: string;
  option?: Record<string, unknown>;
}

const props = withDefaults(
  defineProps<{
    visible?: boolean;
    charts?: ChartItem[];
  }>(),
  {
    visible: false,
    charts: () => [],
  },
);

const emit = defineEmits<{ (e: 'update:visible', value: boolean): void }>();

/** 关闭弹窗 */
function close() {
  emit('update:visible', false);
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="图表"
    width="880px"
    destroy-on-close
    @update:model-value="(v: boolean) => emit('update:visible', v)"
  >
    <div v-if="charts.length === 0" class="chart-empty">暂无图表数据</div>
    <div v-else class="chart-list">
      <div v-for="(item, idx) in charts" :key="idx" class="chart-item">
        <h3 v-if="item.title" class="chart-item__title">{{ item.title }}</h3>
        <VChart v-if="item.option" class="chart-item__canvas" :option="item.option" autoresize />
      </div>
    </div>
    <template #footer>
      <el-button @click="close">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.chart-empty {
  padding: 40px 0;
  text-align: center;
  color: var(--el-text-color-secondary);
  font-size: 14px;
}

.chart-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.chart-item {
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  padding: 12px 16px;
}

.chart-item__title {
  margin: 0 0 8px;
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.chart-item__canvas {
  height: 320px;
  width: 100%;
}
</style>
