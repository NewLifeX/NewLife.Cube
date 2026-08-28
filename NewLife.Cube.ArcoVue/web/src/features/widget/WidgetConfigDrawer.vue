<template>
  <a-drawer
    class="widget-config-drawer"
    :visible="visible"
    :width="480"
    unmount-on-close
    placement="right"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <template #title>配置部件</template>
    <div v-if="step === 'kind'" class="wd-step">
      <div class="wd-label">类型</div>
      <button
        v-for="k in platformKinds"
        :key="k.kind"
        type="button"
        class="wd-kind"
        :class="{ active: draft.kind === k.kind }"
        @click="pickKind(k.kind)"
      >
        <div class="wd-kind-title">{{ k.title }}</div>
        <div class="wd-kind-hint">{{ k.hint }}</div>
      </button>
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
      <div class="wd-label">字段与样式</div>
      <a-form layout="vertical">
        <a-form-item label="标题">
          <a-input v-model="draft.title" :max-length="40" />
        </a-form-item>
        <a-form-item v-if="draft.kind === 'metricCard'" label="角标">
          <a-input v-model="draft.badge" :max-length="12" placeholder="可选，如「本月」「全站」" allow-clear />
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
        <a-form-item
          v-if="
            draft.kind === 'miniChart' &&
            (draft.chartType === 'bar' || draft.chartType === 'hbar' || draft.chartType === 'pie')
          "
          label="分组字段"
        >
          <a-select v-model="draft.groupBy" allow-search>
            <a-option v-for="f in sourceFields" :key="f.name" :value="f.name">
              {{ f.displayName || f.name }}
            </a-option>
          </a-select>
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
        </template>
        <a-form-item v-if="isCross" label="跨实体联动">
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
        <a-form-item label="宽度">
          <a-radio-group v-model="draft.w" type="button" size="small">
            <a-radio :value="3">1/4</a-radio>
            <a-radio :value="4">1/3</a-radio>
            <a-radio :value="6">1/2</a-radio>
            <a-radio :value="12">整行</a-radio>
          </a-radio-group>
        </a-form-item>
      </a-form>
      <div class="wd-preview">预览：{{ draft.title || '未命名' }} · {{ draft.kind }} · {{ draft.typePath }}</div>
    </div>
    <template #footer>
      <a-space>
        <a-button v-if="step !== 'kind'" @click="step = step === 'fields' ? 'source' : 'kind'">上一步</a-button>
        <a-button @click="cancel">取消</a-button>
        <a-button v-if="step === 'fields'" type="primary" @click="save">保存</a-button>
      </a-space>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { WidgetInstance, WidgetSourceItem } from '@cube/api-core';
import { CHART_TYPE_OPTIONS } from './chartTemplates';
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
  numericFields,
  dateFields,
  sourceFields,
  pickKind,
  pickSource,
  save,
  cancel,
} = useWidgetConfigDrawer(props, emit as (e: 'update:visible' | 'save', ...args: unknown[]) => void);

const chartTypeOptions = CHART_TYPE_OPTIONS;
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
.wd-kind-title {
  font-weight: 500;
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
.wd-hint code {
  padding: 0 4px;
  border-radius: 3px;
  background: var(--color-fill-2);
  font-size: 12px;
}
</style>
