<template>
  <a-drawer
    class="widget-config-drawer"
    :visible="visible"
    :width="420"
    unmount-on-close
    placement="right"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <template #title>配置部件</template>
    <div v-if="step === 'named'" class="wd-step">
      <a-tabs v-model:active-key="wbTab">
        <a-tab-pane key="named" title="平台部件">
          <div class="wd-named-grid">
            <a-empty v-if="!namedList.length" description="暂无可用平台部件" />
            <button
              v-for="n in namedList"
              :key="n.name"
              type="button"
              class="wd-named"
              @click="pickNamed(n)"
            >
              <icon-park :type="resolveWorkbenchIcon(n.icon, n.name)" class="wd-named-ico" />
              <div class="wd-named-title">{{ n.title }}</div>
              <a-tag v-if="n.adminOnly" size="small" color="orangered">Admin</a-tag>
            </button>
          </div>
        </a-tab-pane>
        <a-tab-pane key="entity" title="实体部件">
          <div class="wd-named-grid">
            <button
              v-for="k in platformKinds"
              :key="k.kind"
              type="button"
              class="wd-named"
              @click="pickKind(k.kind)"
            >
              <icon-park :type="k.icon" class="wd-named-ico" />
              <div class="wd-named-title">{{ k.title }}</div>
              <div class="wd-named-hint">{{ k.hint }}</div>
            </button>
          </div>
        </a-tab-pane>
      </a-tabs>
    </div>
    <div v-else-if="step === 'kind'" class="wd-step">
      <div class="wd-named-grid">
        <button
          v-for="k in platformKinds"
          :key="k.kind"
          type="button"
          class="wd-named"
          :class="{ 'wd-named--active': draft.kind === k.kind }"
          @click="pickKind(k.kind)"
        >
          <icon-park :type="k.icon" class="wd-named-ico" />
          <div class="wd-named-title">{{ k.title }}</div>
          <div class="wd-named-hint">{{ k.hint }}</div>
        </button>
      </div>
    </div>
    <div v-else-if="step === 'source'" class="wd-step wd-step--source">
      <div class="wd-label">数据源</div>
      <a-input-search v-model="sourceQ" placeholder="搜索中文名 / 路径 / 类型名" allow-clear />
      <div class="wd-src-list">
        <a-empty v-if="!filteredSources.length" description="暂无可用数据源" />
        <button
          v-for="s in filteredSources"
          :key="s.typePath"
          type="button"
          class="wd-src"
          :class="{ current: isCurrent(s.typePath) }"
          @click="pickSource(s.typePath)"
        >
          <div class="wd-src-row">
            <span class="wd-src-name">{{ sourceLabel(s) }}</span>
            <a-tag v-if="isCurrent(s.typePath)" size="small">当前实体</a-tag>
          </div>
          <span class="wd-src-path">{{ s.typePath }}</span>
        </button>
      </div>
    </div>
    <div v-else class="wd-step">
      <a-form layout="vertical">
        <!-- 分组（OSC-260921）：数据源 / 图表配置 / 图表布局；仅组间画分隔线，不加分组标签 -->
        <a-form-item label="标题">
          <a-input v-model="draft.title" :max-length="40" />
        </a-form-item>
        <a-form-item v-if="!isNamed" label="数据源">
          <a-select v-model="draft.typePath" allow-search @change="onSourceChange">
            <a-option v-for="s in sourceOptions" :key="s.typePath" :value="s.typePath">
              {{ sourceLabel(s) }}
            </a-option>
          </a-select>
        </a-form-item>
        <WidgetScopeFilter
          v-if="!isNamed"
          :label="draft.kind === 'miniChart' ? '数据范围' : '查询条件'"
          :count="filterCondCount"
          :model-value="filterModel"
          :fields="filterCandidates"
          :host-fields="hostEditorFields"
          :visible="filterEditorVisible"
          :hint="filterHint"
          @update:visible="(v: boolean) => (filterEditorVisible = v)"
          @apply="onFilterApply"
          @clear="clearWidgetFilter"
        />
        <a-form-item v-if="!isNamed && showFetchLimit" label="拉取数量">
          <a-select v-model="draft.limit">
            <a-option v-for="n in limitOptions" :key="n" :value="n">
              {{ formatDataListLimitLabel(n) }}
            </a-option>
          </a-select>
          <template #extra>
            <span v-if="fetchAllSelected" class="wd-hint wd-hint--warn">
              「全部」会拉取匹配的全部记录，数据量大时可能影响性能，请谨慎使用。
            </span>
            <span v-else class="wd-hint">
              从后端一次拉取的记录数，用于列表滚动 / 看板卡片 / 卡片轮播
            </span>
          </template>
        </a-form-item>
        <a-form-item v-if="!isNamed && isCross && !isWorkbench" label="跨实体联动">
          <div class="wd-link-block">
            <div class="wd-hint wd-hint--lead">
              当前数据源与本页实体不同。填写一对「相等」字段后，列表筛选会传到该部件；不填则独立统计并显示未联动图标。
            </div>
            <a-space direction="vertical" fill>
              <div class="wd-link-row">
                <span class="wd-link-label">本页字段</span>
                <a-select
                  v-model="draft.hostField"
                  allow-search
                  allow-clear
                  placeholder="例如：Id / RoleId"
                >
                  <a-option v-for="f in hostFieldOptions" :key="f.name" :value="f.name">
                    {{ f.displayName || f.name }}
                  </a-option>
                </a-select>
              </div>
              <div class="wd-link-row">
                <span class="wd-link-label">数据源字段</span>
                <a-select
                  v-model="draft.sourceField"
                  allow-search
                  allow-clear
                  placeholder="例如：RoleId / Id"
                >
                  <a-option v-for="f in sourceFields" :key="f.name" :value="f.name">
                    {{ f.displayName || f.name }}
                  </a-option>
                </a-select>
              </div>
            </a-space>
            <div class="wd-hint">
              示例：角色页挂「用户」统计时，本页字段填 <code>Id</code>，数据源字段填 <code>RoleId</code>（用户.角色 = 当前角色）。
            </div>
          </div>
        </a-form-item>
        <a-form-item v-if="isNamed">
          <div class="wd-hint">平台部件由服务端提供数据，只需调整标题与宽度。</div>
        </a-form-item>

        <!-- 图表配置（OSC-260921）：图表形态与字段 -->
        <template v-if="!isNamed">
          <a-divider class="wd-group" />
          <a-form-item v-if="draft.kind === 'metricCard'" label="标签">
            <a-input
              v-model="draft.badge"
              :max-length="12"
              placeholder="显示在指标数字下方，如「注册用户」"
              allow-clear
            />
          </a-form-item>
          <a-form-item v-if="draft.kind === 'metricCard'" label="度量">
            <a-select v-model="draft.measureFn">
              <a-option value="count">计数</a-option>
              <a-option value="sum">求和</a-option>
              <a-option value="avg">均值</a-option>
              <a-option value="min">最小</a-option>
              <a-option value="max">最大</a-option>
            </a-select>
          </a-form-item>
          <a-form-item v-if="draft.kind === 'metricCard' && draft.measureFn !== 'count'" label="数值字段">
            <a-select v-model="draft.measureField" allow-search>
              <a-option v-for="f in numericFields" :key="f.name" :value="f.name">
                {{ f.displayName || f.name }}
              </a-option>
            </a-select>
          </a-form-item>
          <a-form-item v-if="draft.kind === 'miniChart'" label="图表模板">
            <a-select v-model="draft.chartType">
              <a-option v-for="opt in chartTypeOptions" :key="opt.value" :value="opt.value">
                <span class="wd-chart-opt">
                  <icon-park :type="opt.icon" class="wd-chart-ico" />
                  {{ opt.label }}
                </span>
              </a-option>
            </a-select>
          </a-form-item>
          <a-form-item v-if="draft.kind === 'miniChart'" label="图表选项">
            <div class="wd-chartopts">
              <a-checkbox-group v-model="chartOptionKeys">
                <a-checkbox value="legend">图例</a-checkbox>
                <a-checkbox value="dataLabel">数据标签</a-checkbox>
                <a-checkbox v-if="showAxisOptions" value="axis">坐标轴</a-checkbox>
                <a-checkbox v-if="showAxisOptions" value="gridLine">网格线</a-checkbox>
              </a-checkbox-group>
            </div>
          </a-form-item>
          <a-form-item
            v-if="
              draft.kind === 'miniChart' &&
              (draft.chartType === 'bar' || draft.chartType === 'hbar' || draft.chartType === 'pie')
            "
            label="横轴（类别）"
          >
            <a-select v-model="draft.groupBy" allow-search>
              <a-option v-for="f in sourceFields" :key="f.name" :value="f.name">
                {{ f.displayName || f.name }}
              </a-option>
            </a-select>
          </a-form-item>
          <a-form-item v-if="draft.kind === 'miniChart' && showChartSort" label="排序依据">
            <a-radio-group v-model="draft.sortBy" type="button" size="small">
              <a-radio value="x">横轴值</a-radio>
              <a-radio value="y">纵轴值</a-radio>
              <a-radio value="record">记录顺序</a-radio>
            </a-radio-group>
          </a-form-item>
          <a-form-item v-if="draft.kind === 'miniChart' && showChartSort" label="排序规则">
            <a-radio-group v-model="draft.sortOrder" type="button" size="small">
              <a-radio value="asc">正序</a-radio>
              <a-radio value="desc">倒序</a-radio>
            </a-radio-group>
            <template #extra>
              <span class="wd-hint">排序对分组聚合结果生效；「记录顺序」按数据库返回顺序，不额外排序。</span>
            </template>
          </a-form-item>
          <a-form-item
            v-if="draft.kind === 'miniChart' && (draft.chartType === 'line' || draft.chartType === 'sparkline')"
            label="时间字段"
          >
            <a-select v-model="draft.timeField" allow-search>
              <a-option v-for="f in dateFields" :key="f.name" :value="f.name">
                {{ f.displayName || f.name }}
              </a-option>
            </a-select>
          </a-form-item>
          <!-- 纵轴（字段，OSC-260920）：统计记录总数（仅计数）/ 统计字段数值（计数/求和/最大/最小/平均） -->
          <a-form-item v-if="draft.kind === 'miniChart'" label="纵轴（字段）">
            <a-radio-group v-model="draft.measureMode" type="button" size="small">
              <a-radio value="count">统计记录总数</a-radio>
              <a-radio value="field">统计字段数值</a-radio>
            </a-radio-group>
          </a-form-item>
          <a-form-item v-if="draft.kind === 'miniChart'" label="选择字段">
            <div class="wd-measure-list">
              <div v-for="(m, i) in draft.measures" :key="i" class="wd-measure-item">
                <!-- 组合输入框（OSC-260921）：字段选择 / 统计方式 / 删除按钮 拼为一体 -->
                <a-input-group>
                  <a-select
                    v-model="m.field"
                    class="wd-measure-field"
                    allow-search
                    allow-clear
                    size="small"
                    placeholder="选择字段"
                    @change="onMeasureFieldChange(i)"
                  >
                    <a-option v-for="f in sourceFields" :key="f.name" :value="f.name">
                      {{ f.displayName || f.name }}
                    </a-option>
                  </a-select>
                  <!-- 统计方式按字段类型限定（OSC-260921）：数值字段 5 种，文本/日期/时间/编号类仅计数 -->
                  <a-select v-if="draft.measureMode === 'field'" v-model="m.fn" class="wd-measure-fn" size="small">
                    <a-option v-for="fn in measureFnOptions(m.field)" :key="fn" :value="fn">
                      {{ MEASURE_FN_LABELS[fn] }}
                    </a-option>
                  </a-select>
                  <!-- 统计记录总数：仅计数，仍置于组合框内以保持视觉一致 -->
                  <a-select v-else v-model="m.fn" class="wd-measure-fn" size="small">
                    <a-option value="count">计数</a-option>
                  </a-select>
                  <a-button size="small" status="danger" @click="removeMeasureRow(i)">
                    <icon-park type="delete" />
                  </a-button>
                </a-input-group>
              </div>
              <a-button size="mini" :disabled="!canAddMeasure" @click="addMeasureRow">
                <icon-park type="plus" />
                添加字段
              </a-button>
            </div>
            <template #extra>
              <span class="wd-hint">
                {{
                  draft.measureMode === 'count'
                    ? '统计记录总数：仅按计数（Count）统计；不添加字段时统计全部记录数，最多 5 个字段。'
                    : '统计字段数值：数值字段可按计数 / 求和 / 最大值 / 最小值 / 平均值统计；文本、日期、时间、编号等字段仅支持计数，最多 5 个字段。'
                }}
              </span>
            </template>
          </a-form-item>
          <template v-if="draft.kind === 'miniKanban'">
            <a-form-item label="分组字段">
              <a-select v-model="draft.groupField" allow-search>
                <a-option v-for="f in sourceFields" :key="f.name" :value="f.name">
                  {{ f.displayName || f.name }}
                </a-option>
              </a-select>
            </a-form-item>
            <a-form-item label="标题字段">
              <a-select v-model="draft.titleField" allow-search>
                <a-option v-for="f in sourceFields" :key="f.name" :value="f.name">
                  {{ f.displayName || f.name }}
                </a-option>
              </a-select>
            </a-form-item>
            <a-form-item label="显示字段">
              <a-select
                v-model="draft.displayFields"
                multiple
                allow-search
                allow-clear
                :max-tag-count="3"
                placeholder="留空则按列表列自动展示，最多 8 个"
              >
                <a-option
                  v-for="f in sourceFields"
                  :key="f.name"
                  :value="f.name"
                  :disabled="
                    f.name === draft.groupField ||
                    f.name === draft.titleField ||
                    f.name === draft.imageField
                  "
                >
                  {{ f.displayName || f.name }}
                </a-option>
              </a-select>
            </a-form-item>
          </template>
          <template v-if="draft.kind === 'dataCard'">
            <a-form-item label="标题字段">
              <a-select v-model="draft.titleField" allow-search>
                <a-option v-for="f in sourceFields" :key="f.name" :value="f.name">
                  {{ f.displayName || f.name }}
                </a-option>
              </a-select>
            </a-form-item>
            <a-form-item label="显示字段">
              <a-select
                v-model="draft.displayFields"
                multiple
                allow-search
                allow-clear
                :max-tag-count="3"
                placeholder="留空则按列表列自动展示，最多 8 个"
              >
                <a-option
                  v-for="f in sourceFields"
                  :key="f.name"
                  :value="f.name"
                  :disabled="f.name === draft.titleField || f.name === draft.imageField"
                >
                  {{ f.displayName || f.name }}
                </a-option>
              </a-select>
            </a-form-item>
          </template>
          <a-form-item v-if="draft.kind === 'dataList'" label="显示字段">
            <a-select
              v-model="draft.displayFields"
              multiple
              allow-search
              allow-clear
              :max-tag-count="3"
              placeholder="留空则按列表列自动展示，最多 8 个"
            >
              <a-option v-for="f in sourceFields" :key="f.name" :value="f.name">
                {{ f.displayName || f.name }}
              </a-option>
            </a-select>
          </a-form-item>
        </template>

        <!-- 图表布局（OSC-260921）：宽度 -->
        <a-divider class="wd-group" />
        <a-form-item label="宽度">
          <a-radio-group v-model="draft.w" type="button" size="small">
            <a-radio v-if="isWorkbench" :value="2">1/6</a-radio>
            <a-radio :value="3">1/4</a-radio>
            <a-radio :value="4">1/3</a-radio>
            <a-radio :value="6">1/2</a-radio>
            <a-radio v-if="isWorkbench" :value="8">2/3</a-radio>
            <a-radio :value="12">整行</a-radio>
          </a-radio-group>
        </a-form-item>
      </a-form>
      <div class="wd-preview">
        预览：{{ draft.title || '未命名' }} · {{ draft.kind }} ·
        {{ isNamed ? `平台:${draft.widgetName}` : draft.typePath }}
      </div>
    </div>
    <template #footer>
      <a-space>
        <a-button
          v-if="(step === 'fields' || step === 'source') && !isNamed"
          @click="step = step === 'fields' ? 'source' : (isWorkbench ? 'named' : 'kind')"
        >
          上一步
        </a-button>
        <a-button @click="cancel">取消</a-button>
        <a-button v-if="step === 'fields'" type="primary" @click="save">保存</a-button>
      </a-space>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { WidgetInstance, WidgetSourceItem } from '@newlifex/api-core';
import WidgetScopeFilter from './WidgetScopeFilter.vue';
import { CHART_TYPE_OPTIONS, MEASURE_FN_LABELS } from './chartTemplates';
import { useWidgetConfigDrawer, type WidgetConfigDrawerProps } from './useWidgetConfigDrawer';
import { normalizeTypePath } from './legacy';

const props = defineProps<WidgetConfigDrawerProps>();
const emit = defineEmits<{
  'update:visible': [boolean];
  save: [widget: WidgetInstance];
}>();

const {
  step,
  draft,
  platformKinds,
  sourceOptions,
  isCross,
  isWorkbench,
  isNamed,
  namedList,
  wbTab,
  numericFields,
  dateFields,
  measureFnOptions,
  onMeasureFieldChange,
  canAddMeasure,
  showAxisOptions,
  showChartSort,
  addMeasureRow,
  removeMeasureRow,
  sourceFields,
  filterCandidates,
  hostEditorFields,
  filterModel,
  filterCondCount,
  filterEditorVisible,
  onFilterApply,
  clearWidgetFilter,
  showFetchLimit,
  limitOptions,
  fetchAllSelected,
  formatDataListLimitLabel,
  pickKind,
  pickNamed,
  pickSource,
  save,
  cancel,
  resolveWorkbenchIcon,
} = useWidgetConfigDrawer(props, emit as (e: 'update:visible' | 'save', ...args: unknown[]) => void);

const chartTypeOptions = CHART_TYPE_OPTIONS;
/** 图表选项勾选值 ↔ draft.chartOptions 映射；轴/网格线对饼图、迷你折线不适用 */
const chartOptionKeys = computed<string[]>({
  get: () => {
    const co = draft.chartOptions;
    const keys: string[] = [];
    if (co.legend) keys.push('legend');
    if (co.dataLabel) keys.push('dataLabel');
    if (co.axis) keys.push('axis');
    if (co.gridLine) keys.push('gridLine');
    return keys;
  },
  set: (keys) => {
    draft.chartOptions.legend = keys.includes('legend');
    draft.chartOptions.dataLabel = keys.includes('dataLabel');
    if (showAxisOptions.value) {
      draft.chartOptions.axis = keys.includes('axis');
      draft.chartOptions.gridLine = keys.includes('gridLine');
    }
  },
});
/** 条件编辑器底部提示（是否可引用宿主字段） */
const filterHint = computed(() =>
  hostEditorFields.value.length
    ? '按条件过滤源数据；条件值可切「宿主」引用当前页面筛选字段，随宿主联动。'
    : '按条件过滤源实体数据，作用于本部件（工作台无宿主字段引用）。',
);
const hostFieldOptions = computed(() => props.hostFields ?? []);
const sourceQ = ref('');
const filteredSources = computed(() => {
  const q = sourceQ.value.trim().toLowerCase();
  if (!q) return sourceOptions.value;
  return sourceOptions.value.filter((s) => {
    const hay = `${s.displayName || ''}\0${s.typePath || ''}\0${s.name || ''}`.toLowerCase();
    return hay.includes(q);
  });
});
function isCurrent(typePath: string) {
  return normalizeTypePath(typePath) === normalizeTypePath(props.hostTypePath);
}
/** 优先中文 DisplayName；与类型名相同时退回 name/path */
function sourceLabel(s: WidgetSourceItem) {
  const dn = (s.displayName || '').trim();
  const name = (s.name || '').trim();
  if (dn && dn.toLowerCase() !== name.toLowerCase()) return dn;
  if (dn) return dn;
  return name || s.typePath;
}
/**
 * 配置界面内切换数据源（OSC-260921）：旧实体字段引用对新实体多已失效，先重置再重载字段/条件候选，
 * 否则保存或取数会因「未知分组字段/未知度量字段」被后端 400。展示类配置（图表模板/选项/排序）与源无关，保留。
 */
function onSourceChange(value: unknown) {
  const typePath = normalizeTypePath(String(value ?? ''));
  if (!typePath) return;
  draft.groupBy = '';
  draft.timeField = '';
  draft.measures = [];
  draft.measureMode = 'count';
  draft.measureField = '';
  draft.groupField = '';
  draft.titleField = '';
  draft.imageField = '';
  draft.displayFields = [];
  draft.hostField = '';
  draft.sourceField = '';
  draft.extraFilter = null;
  void pickSource(typePath);
}
</script>

<style scoped>
.widget-config-drawer :deep(.arco-drawer-body) {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  padding-bottom: 0;
}
.wd-step {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.wd-step--source {
  flex: 1;
  min-height: 0;
}
.wd-label {
  font-weight: 500;
  flex-shrink: 0;
}
.wd-filter-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
}
.wd-kind,
.wd-src {
  text-align: left;
  padding: 12px;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  background: var(--color-bg-2);
  cursor: pointer;
}
.wd-kind.active,
.wd-src.current {
  border-color: rgb(var(--primary-6));
}
.wd-named-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
  padding: 8px 0 12px;
}
.wd-named {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  text-align: left;
  padding: 12px;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  background: var(--color-bg-2);
  cursor: pointer;
}
.wd-named:hover,
.wd-named--active {
  border-color: rgb(var(--primary-6));
}
.wd-named-ico {
  color: rgb(var(--primary-6));
}
.wd-named-title {
  font-weight: 500;
}
.wd-named-hint {
  font-size: 12px;
  color: var(--color-text-3);
  line-height: 1.3;
}
.wd-kind-hint,
.wd-src-path,
.wd-hint,
.wd-preview {
  font-size: 12px;
  color: var(--color-text-3);
  margin-top: 4px;
}
.wd-src-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.wd-src-name {
  font-weight: 500;
  color: var(--color-text-1);
}
.wd-src-list {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
  overflow: auto;
  padding-bottom: 12px;
}
.wd-chart-opt {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}
/* 组间分隔线（无文字；首个分组上方不画线） */
.wd-group {
  margin: 12px 0;
}
/* 图表选项：4 项同排；项多时自动换行（外层普通 div，保证 scoped 样式命中） */
.wd-chartopts {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 16px;
}
.wd-measure-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}
.wd-measure-item {
  width: 100%;
}
/* 组合框占满整行；内部元素是 Arco 组件（根元素不带 scoped 属性），用 :deep() 定位 */
.wd-measure-item > :first-child {
  width: 100%;
}
.wd-measure-item :deep(.wd-measure-field) {
  flex: 1;
  min-width: 0;
}
.wd-measure-item :deep(.wd-measure-fn) {
  width: 84px;
  flex-shrink: 0;
}
.wd-chart-ico {
  color: rgb(var(--primary-6));
}
.wd-link-block {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}
.wd-link-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.wd-link-label {
  font-size: 12px;
  color: var(--color-text-2);
}
.wd-hint--lead {
  margin-bottom: 2px;
}
.wd-hint--warn {
  margin-top: 0;
  color: rgb(var(--warning-6));
}
.widget-config-drawer :deep(.arco-form-item-extra) .wd-hint {
  margin-top: 0;
  display: inline-block;
  line-height: 1.5;
}
.wd-hint code {
  padding: 0 4px;
  border-radius: 3px;
  background: var(--color-fill-2);
  font-size: 12px;
}
</style>
