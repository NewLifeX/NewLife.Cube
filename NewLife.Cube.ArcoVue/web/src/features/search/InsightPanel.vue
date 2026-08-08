<template>
  <div class="query-insight-panel list-panel">
    <!-- 上部：搜索字段与操作（OSC-0012 单一查询面板，不复刻第二份搜索状态） -->
    <div class="qip-search">
      <!-- 主行：左侧流式条件区（前 N 字段 + 主时间 + Q，横向连贯流式排布、不固定位置）+ 右侧固定查询按钮（永远停靠第一行右侧） -->
      <div class="qip-main-row">
        <!-- 左侧流式区：字段/主时间/Q 同一 a-form inline 连贯排布，溢出裁剪、多余字段进第二行 -->
        <div class="qip-fields-clip">
          <a-form :model="model" layout="inline" @submit.prevent="$emit('search')">
            <a-form-item
              v-for="field in mainFields"
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
            <a-form-item v-if="masterTimeName" :label="masterTimeDisplayName || '时间范围'">
              <a-range-picker
                :model-value="masterTimeRange"
                show-time
                value-format="YYYY-MM-DDTHH:mm:ss"
                style="width: 280px"
                @update:model-value="onMasterTimeChange"
              />
            </a-form-item>
            <a-form-item v-if="enableKey !== false" label="关键字">
              <a-input
                :model-value="String(model.Q ?? '')"
                placeholder="全字段模糊搜索"
                allow-clear
                style="width: 180px"
                @update:model-value="(v: unknown) => (model.Q = v)"
                @press-enter="$emit('search')"
              />
            </a-form-item>
          </a-form>
        </div>
        <!-- 右侧固定区：仅查询按钮，flex-shrink:0 永远停靠第一行右侧 -->
        <div class="qip-tail">
          <QueryComboButton
            :queries="queries"
            :active-query-id="activeQueryId"
            :params-dirty="paramsDirty"
            :can-save="canSaveQuery"
            :has-more-fields="extraFields.length > 0"
            :more-field-count="extraFields.length"
            :expanded="expanded"
            @search="$emit('search')"
            @reset="$emit('reset')"
            @toggle-expand="expanded = !expanded"
            @apply="(id: string) => $emit('apply', id)"
            @save="(name: string) => $emit('saveQuery', name)"
            @rename="(id: string, name: string) => $emit('renameQuery', id, name)"
            @delete="(id: string) => $emit('deleteQuery', id)"
          />
        </div>
      </div>
      <!-- 第二行：其余（多余）查询条件，默认收起、展开时显示 -->
      <div class="qip-extra-row" :class="{ 'qip-extra-open': expanded }">
        <a-form :model="model" layout="inline">
          <a-form-item
            v-for="field in extraFields"
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
        </a-form>
      </div>
      <!-- 测量容器（不可见）：与主行同布局（字段区 flex:1 + 右侧固定），计算第一行可容纳字段数 N -->
      <div ref="measureRef" class="qip-measure" aria-hidden="true">
        <div class="qip-fields-clip">
          <a-form :model="model" layout="inline">
            <a-form-item
              v-for="field in fields"
              :key="`m-${field.name}`"
              class="qip-measure-field"
              :label="field.displayName || field.name"
            >
              <div class="qip-measure-box" />
            </a-form-item>
            <a-form-item
              v-if="masterTimeName"
              class="qip-measure-tail"
              :label="masterTimeDisplayName || '时间范围'"
            >
              <div class="qip-measure-box qip-measure-wide" />
            </a-form-item>
            <a-form-item v-if="enableKey !== false" class="qip-measure-tail" label="关键字">
              <div class="qip-measure-box qip-measure-key" />
            </a-form-item>
          </a-form>
        </div>
        <div class="qip-tail">
          <div class="qip-measure-btn" />
        </div>
      </div>
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
import { computed, markRaw, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import * as echarts from 'echarts';

defineOptions({ name: 'InsightPanel' });
import SearchFieldInput from '@/components/SearchFieldInput.vue';
import QueryComboButton from './QueryComboButton.vue';
import { resolveStatEntries } from '@/core/utils/searchFilters';
import type { FieldMeta } from '@/core/types/field';
import type { SavedQuery } from '@/core/utils/viewProfile';

const props = defineProps<{
  /** search 分区字段（用于搜索表单渲染） */
  fields: FieldMeta[];
  /** 搜索表单对象（父组件 reactive，直接读写其属性，保持既有 SearchFieldInput 语义） */
  model: Record<string, unknown>;
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
  /** 主时间字段名（OSC-0016）；无 MasterTime 时不渲染主时间范围 */
  masterTimeName?: string | null;
  /** 主时间字段显示名（OSC-0016） */
  masterTimeDisplayName?: string | null;
  /** 关键字 Q 是否启用（OSC-0016）；false 时不渲染关键字框 */
  enableKey?: boolean;
  /** 预定义查询列表（OSC-0016） */
  queries: SavedQuery[];
  /** 当前应用的预定义查询 id（会话内存） */
  activeQueryId: string | null;
  /** 当前参数与 activeQuery 是否不一致（条目 ✓ 标记控制） */
  paramsDirty: boolean;
  /** 当前参数是否可保存（非空） */
  canSaveQuery: boolean;
}>();

defineEmits<{
  search: [];
  reset: [];
  /** 应用预定义查询（OSC-0016） */
  apply: [id: string];
  /** 保存当前查询为预定义（OSC-0016） */
  saveQuery: [name: string];
  /** 重命名当前查询（OSC-0016） */
  renameQuery: [id: string, name: string];
  /** 删除预定义查询（OSC-0016） */
  deleteQuery: [id: string];
}>();

/** 主时间范围值：dtStart/dtEnd 两键映射 [start, end] */
const masterTimeRange = computed(() => {
  const s = props.model?.dtStart;
  const e = props.model?.dtEnd;
  return s && e ? [String(s), String(e)] : undefined;
});

/** 主时间范围变更：写 dtStart/dtEnd，清空时删除两键 */
function onMasterTimeChange(val: unknown) {
  const arr = Array.isArray(val) ? val : [];
  if (arr.length >= 2) {
    props.model.dtStart = arr[0] ?? '';
    props.model.dtEnd = arr[1] ?? '';
  } else {
    delete props.model.dtStart;
    delete props.model.dtEnd;
  }
}


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

// ---------- 两行布局（面板重构）：第一行 = 前 N 字段 + 主时间/Q（流式）+ 右侧查询按钮，第二行 = 其余字段（默认收起） ----------
const measureRef = ref<HTMLElement | null>(null);

/** 面板是否展开（显示第二行；默认收起仅第一行） */
const expanded = ref(false);
/** 第一行可容纳的字段数 N（测量得出） */
const firstRowCount = ref(0);

/** 第一行字段（前 N 个） */
const mainFields = computed(() => props.fields.slice(0, firstRowCount.value));
/** 第二行字段（其余，默认收起） */
const extraFields = computed(() => props.fields.slice(firstRowCount.value));

/**
 * 测量第一行可容纳字段数 N：主时间/Q 作为流式条件排在字段之后，测量时为其预留宽度；
 * 左侧字段区（flex:1）可用宽度内按字段宽度累加，放不下则进第二行。查询按钮固定右侧不占字段区。
 */
function measureMainRow() {
  const m = measureRef.value;
  if (!m) return;
  const clip = m.querySelector('.qip-fields-clip');
  if (!clip) return;
  const fieldItems = Array.from(clip.querySelectorAll<HTMLElement>('.qip-measure-field'));
  // 主时间/Q 为流式条件（排在字段之后），预留其宽度保证与查询按钮同在第一行
  const tailItems = Array.from(clip.querySelectorAll<HTMLElement>('.qip-measure-tail'));
  if (!fieldItems.length) return;
  const gap = 8;
  const avail = clip.clientWidth;
  const tailW = tailItems.reduce((s, el) => s + el.offsetWidth + gap, 0);
  let used = 0;
  let n = 0;
  for (const el of fieldItems) {
    const w = el.offsetWidth + gap;
    if (used + w > avail - tailW) break;
    used += w;
    n++;
  }
  firstRowCount.value = Math.min(n, props.fields.length);
}

/** 容器尺寸变化时重测（面板宽度随窗口/侧栏调整） */
let resizeObserver: ResizeObserver | null = null;

onMounted(() => {
  nextTick(() => {
    measureMainRow();
    resizeObserver = new ResizeObserver(() => nextTick(measureMainRow));
    if (measureRef.value?.parentElement) resizeObserver.observe(measureRef.value.parentElement);
  });
});

watch(
  () => props.fields,
  () => nextTick(measureMainRow),
  { deep: true },
);

onBeforeUnmount(() => {
  disposeCharts();
  resizeObserver?.disconnect();
});
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
  position: relative;
}
/* 主行：左侧字段区 flex:1 + 右侧固定区（flex-shrink:0），查询按钮永远停靠第一行右侧 */
.qip-main-row {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  min-width: 0;
  max-width: 100%;
}
/* 左侧流式区：占剩余宽度，字段/主时间/Q 同一 a-form inline 连贯流式排布、不固定位置；溢出裁剪（收起不显示滚动条） */
.qip-fields-clip {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}
/* 右侧固定区：仅查询按钮，不收缩、永远停靠第一行右侧 */
.qip-tail {
  flex-shrink: 0;
}
/* 第二行：多余条件，默认收起（max-height 0），展开时平滑增高；a-form inline 内部横向 */
.qip-extra-row {
  max-height: 0;
  overflow: hidden;
  transition: max-height 0.25s ease, opacity 0.2s ease;
  opacity: 0;
}
.qip-extra-row.qip-extra-open {
  max-height: 1200px;
  opacity: 1;
}
.qip-search :deep(.arco-form-item) {
  margin-right: 8px;
  margin-bottom: 12px;
}
/* 测量容器：不可见，与主行同布局（字段区 flex:1 + 右侧固定区），仅用于计算第一行可容纳字段数 N */
.qip-measure {
  position: absolute;
  left: -9999px;
  top: 0;
  width: 100%;
  visibility: hidden;
  pointer-events: none;
  display: flex;
  align-items: flex-start;
  gap: 8px;
}
.qip-measure-box {
  width: 160px;
  height: 32px;
}
.qip-measure-wide {
  width: 280px;
}
.qip-measure-key {
  width: 180px;
}
.qip-measure-btn {
  width: 90px;
  height: 32px;
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
