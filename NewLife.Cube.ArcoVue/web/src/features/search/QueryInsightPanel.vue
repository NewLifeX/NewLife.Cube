<template>
  <div class="query-insight-panel list-panel">
    <!-- 上部：搜索字段与操作（OSC-0012 单一查询面板，不复刻第二份搜索状态） -->
    <div class="qip-search">
      <a-form :model="model" layout="inline" @submit.prevent="$emit('search')">
        <a-form-item
          v-for="field in fields"
          :key="field.name"
          :label="field.displayName || field.name"
        >
          <SearchFieldInput
            :field="field"
            :model-value="model[field.name]"
            :form="model"
            @update:model-value="(v) => (model[field.name] = v)"
            @update:key="(k, v) => (model[k] = v)"
            @search="$emit('search')"
          />
        </a-form-item>
        <a-form-item>
          <a-space :size="8" wrap>
            <a-button type="primary" html-type="submit">搜索</a-button>
            <a-button @click="$emit('reset')">重置</a-button>
            <a-tooltip :content="canSave ? '将当前条件保存为命名视图默认筛选' : '当前无命名视图'">
              <a-button :disabled="!canSave" @click="$emit('save')">保存到此视图</a-button>
            </a-tooltip>
            <a-tooltip content="清除当前命名视图的已保存默认筛选">
              <a-button :disabled="!canSave" @click="$emit('clear')">清除默认筛选</a-button>
            </a-tooltip>
          </a-space>
        </a-form-item>
        <a-form-item v-if="sourceLabel" class="qip-source-item">
          <a-typography-text type="secondary" class="qip-source">
            {{ sourceLabel }}
          </a-typography-text>
        </a-form-item>
      </a-form>
    </div>

    <!-- 下部：可选结果区（统计标签 + 一张固定图表，均与列表同源） -->
    <div v-if="showStat || showChart" class="qip-result">
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
          <a-empty v-else-if="!chartLoading && !chartError" description="暂无图表数据" />
        </a-spin>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, markRaw, nextTick, onBeforeUnmount, watch } from 'vue';
import * as echarts from 'echarts';
import SearchFieldInput from '@/components/SearchFieldInput.vue';
import { resolveStatEntries } from '@/core/utils/searchFilters';
import type { FieldMeta } from '@/core/types/field';

const props = defineProps<{
  /** search 分区字段（用于搜索表单渲染） */
  fields: FieldMeta[];
  /** 搜索表单对象（父组件 reactive，直接读写其属性，保持既有 SearchFieldInput 语义） */
  model: Record<string, unknown>;
  /** 条件来源：URL > 已保存 > 空 */
  source: 'url' | 'saved' | 'none';
  /** 来源提示文案（不显示内部 JSON 或字段值） */
  sourceLabel?: string;
  /** 是否有可保存的命名视图（无 active view 时禁用保存/清除） */
  canSave: boolean;
  /** 洞察开关：统计标签 */
  showStat: boolean;
  /** 洞察开关：固定图表 */
  showChart: boolean;
  /** GetList 响应 stat（与列表同源） */
  statData: Record<string, unknown> | null;
  /** 统计标签显示名映射（按 listFields 构造；缺省回落字段名） */
  statLabels?: Record<string, string>;
  /** GetChartData 返回的 ECharts option 数组 */
  chartData: unknown[];
  chartLoading: boolean;
  chartError: string;
}>();

defineEmits<{
  search: [];
  reset: [];
  save: [];
  clear: [];
}>();

/** 统计标签显示名映射（父组件按 listFields 构造；缺省回落字段名） */
const statLabels = computed<Record<string, string>>(() => props.statLabels ?? {});

/** 仅展示 stat 中非 null 的条目；无 stat 时展示「暂无统计」而非编造 0 */
const statEntries = computed(() => resolveStatEntries(props.statData));

const chartInstances: echarts.ECharts[] = [];

function setChartRef(el: HTMLElement | null, idx: number) {
  if (!el) return;
  nextTick(() => {
    if (chartInstances[idx]) chartInstances[idx].dispose();
    const inst = markRaw(echarts.init(el));
    const option = props.chartData[idx];
    if (option && typeof option === 'object') {
      inst.setOption(option as echarts.EChartsOption);
    }
    chartInstances[idx] = inst;
  });
}

function disposeCharts() {
  for (const i of chartInstances) i?.dispose();
  chartInstances.length = 0;
}

watch(
  () => props.chartData,
  () => {
    nextTick(() => {
      disposeCharts();
      // 重绘由 v-for ref 回调触发
    });
  },
  { deep: true },
);

onBeforeUnmount(disposeCharts);
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
.qip-search {
  min-width: 0;
  max-width: 100%;
}
.qip-search :deep(.arco-form-item) {
  margin-bottom: 12px;
}
.qip-source-item {
  margin-left: auto;
  margin-bottom: 12px !important;
  align-self: flex-end;
}
.qip-source {
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
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
</style>
