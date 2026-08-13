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
import SearchFieldInput from '@/components/SearchFieldInput.vue';
import QueryComboButton from './QueryComboButton.vue';
import type { FieldMeta } from '@/core/types/field';
import type { SavedQuery } from '@/core/utils/viewProfile';
import { useInsightPanel } from './useInsightPanel';

defineOptions({ name: 'InsightPanel' });

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

const {
  mainFields,
  extraFields,
  masterTimeRange,
  onMasterTimeChange,
  statLabels,
  statEntries,
  setChartRef,
  measureRef,
  expanded,
} = useInsightPanel(props);
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
