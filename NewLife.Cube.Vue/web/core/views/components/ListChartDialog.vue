<script setup lang="ts">
/**
 * 图表弹窗（charts 数据渲染）
 *
 * 后端 `charts` 返回 ECharts 配置数组（每个元素含 title / option），
 * 本组件按需注册常用 ECharts 组件并逐个渲染。
 * 由列表页懒加载（defineAsyncComponent），仅在点击「图表」时载入 echarts。
 */
import VChart from 'vue-echarts';
import { useI18n } from 'vue-i18n';
import { use } from 'echarts/core';
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DataZoomComponent,
  ToolboxComponent,
  MarkLineComponent,
  MarkPointComponent,
  MarkAreaComponent,
} from 'echarts/components';
import { BarChart, LineChart, PieChart, BoxplotChart, CandlestickChart, ScatterChart } from 'echarts/charts';
import { LabelLayout } from 'echarts/features';
import { CanvasRenderer } from 'echarts/renderers';

// 后端 charts 用到的图型都要注册，否则对应图表渲染成空白（MVC 用完整包，这里按需注册）
use([
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DataZoomComponent,
  ToolboxComponent,
  MarkLineComponent,
  MarkPointComponent,
  MarkAreaComponent,
  BarChart,
  LineChart,
  PieChart,
  BoxplotChart,
  CandlestickChart,
  ScatterChart,
  LabelLayout,
  CanvasRenderer,
]);

/**
 * 图表项：后端 `charts` 返回的就是 ECharts option 本身（`title` / `tooltip` / `xAxis` / `series` …），
 * 不是 `{ title, option }` 包装体。
 */
type ChartItem = Record<string, unknown> & {
  /** ECharts 标题允许字符串、`{ text }` 对象或数组三种写法 */
  title?: string | { text?: string } | Array<string | { text?: string }>;
};

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

const { t } = useI18n();
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="t('listChart.title')"
    width="880px"
    destroy-on-close
    @update:model-value="(v: boolean) => emit('update:visible', v)"
  >
      <div v-if="charts.length === 0" class="chart-empty">{{ t('listChart.empty') }}</div>
    <div v-else class="chart-list">
      <!-- 每项就是一个完整 ECharts option（标题由 ECharts 自己画在画布里） -->
      <div v-for="(item, idx) in charts" :key="idx" class="chart-item">
        <VChart class="chart-item__canvas" :option="item" autoresize />
      </div>
    </div>
    <template #footer>
        <el-button @click="close">{{ t('button.close') }}</el-button>
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

.chart-item__canvas {
  height: 320px;
  width: 100%;
}
</style>
