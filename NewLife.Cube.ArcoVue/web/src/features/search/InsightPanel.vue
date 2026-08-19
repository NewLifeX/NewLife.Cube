<template>
  <!-- 默认隐藏：统计/图表开关均关时不渲染面板；任一开启（ViewConfigDrawer 统计标签/固定图表开关）后显示 -->
  <div v-if="showStat || showChart" class="query-insight-panel list-panel">
    <!-- 简易看板/图表展示区（OSC-0012 / OSC-260819e483 P5）：不含搜索表单——查询统一走工具栏「搜索」抽屉 SearchDrawer -->
    <div class="qip-result">
      <div v-if="showStat" class="qip-stat">
        <div v-if="statEntries.length" class="list-dist">
          <div v-for="e in statEntries" :key="e.key" class="list-dist-item">
            <div class="list-dist-label">{{ statLabels[e.key] || e.key }}</div>
            <div class="list-dist-value">{{ e.value }}</div>
          </div>
        </div>
        <a-empty v-else description="暂无统计" />
      </div>

      <div v-if="showChart" class="qip-chart">
        <a-spin :loading="chartLoading" style="width: 100%">
          <div v-if="chartError" class="qip-chart-error">
            <a-typography-text type="warning">{{ chartError }}</a-typography-text>
          </div>
          <div
            v-else-if="chartData.length"
            v-show="!chartLoading"
            class="qip-chart-body"
          >
            <div
              v-for="(_, idx) in chartData"
              :key="idx"
              :ref="(el) => setChartRef(el as HTMLElement, idx)"
              class="qip-chart-item"
            />
          </div>
          <!-- 空态：无开发者图且无用户 option 时提供配置入口（OSC-260819e483 P5） -->
          <div v-else-if="!chartLoading" class="qip-chart-empty">
            <a-empty description="暂无图表数据" />
            <a-button size="small" @click="openChartConfig">配置图表</a-button>
          </div>
        </a-spin>
        <!-- 有用户 option 时提供修改入口 -->
        <a-button
          v-if="showChart && !chartLoading && !chartError && chartData.length"
          class="qip-chart-config"
          size="small"
          @click="openChartConfig"
        >
          配置图表
        </a-button>
        <ChartOptionEditor
          :visible="chartConfigVisible"
          :chart-option="chartOption"
          :rows="chartRows ?? []"
          @update:visible="(v: boolean) => (chartConfigVisible = v)"
          @save="onChartConfigSave"
          @clear="onChartConfigClear"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import ChartOptionEditor from './ChartOptionEditor.vue';
import { useInsightPanel } from './useInsightPanel';

defineOptions({ name: 'InsightPanel' });

const props = defineProps<{
  /** 洞察开关：统计标签 */
  showStat: boolean;
  /** 洞察开关：固定图表 */
  showChart: boolean;
  /** GetList 响应 stat（与列表同源） */
  statData: Record<string, unknown> | null;
  /** 统计标签显示名映射（按 listFields 构造；缺省回落字段名） */
  statLabels?: Record<string, string>;
  /** 图表数据：开发者 GetChartData 非空数组，或用户 chartOption applyChartData 后的单元素数组（OSC-260819e483 P5） */
  chartData: unknown[];
  chartLoading: boolean;
  chartError: string;
  /** 用户配置的 ECharts option（OSC-260819e483 P5）；无则 undefined */
  chartOption?: unknown;
  /** 当前列表行（图表配置预览用） */
  chartRows?: Record<string, unknown>[];
}>();

const emit = defineEmits<{
  /** 图表配置保存/清除（OSC-260819e483 P5）：父级走 updateInsight */
  chartOptionChange: [option: unknown];
}>();

const {
  statLabels,
  statEntries,
  setChartRef,
  chartConfigVisible,
  openChartConfig,
  onChartConfigSave,
  onChartConfigClear,
} = useInsightPanel(props, emit);
</script>

<style scoped>
.query-insight-panel {
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
  padding: 16px;
  background: var(--color-bg-2);
  border: none;
  border-radius: 8px;
  /* 允许内部横向滚动；勿用 overflow:hidden 把右侧 gutter 连带裁掉 */
  overflow-x: auto;
  overflow-y: visible;
}
.qip-result {
  margin-top: 4px;
  padding-top: 12px;
  border-top: 1px solid var(--color-border-2);
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
.qip-stat {
  min-width: 0;
}
.list-dist {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}
.list-dist-item {
  min-width: 120px;
  padding: 10px 14px;
  border-radius: 6px;
  background: var(--color-fill-2);
  border: 1px solid var(--color-border-2);
}
.list-dist-label {
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-3);
  margin-bottom: 4px;
}
.list-dist-value {
  font-size: var(--cube-font-size-title);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
}
.qip-chart {
  min-width: 0;
  max-width: 100%;
  position: relative;
}
.qip-chart-body {
  width: 100%;
}
.qip-chart-item {
  width: 100%;
  height: 300px;
}
.qip-chart-error {
  padding: 8px 0;
}
/* 空态 + 配置入口（OSC-260819e483 P5） */
.qip-chart-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 8px 0;
}
.qip-chart-config {
  position: absolute;
  top: 0;
  right: 0;
  z-index: 1;
}
</style>
