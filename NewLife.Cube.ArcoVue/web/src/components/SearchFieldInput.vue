<template>
  <div class="search-field-input" style="min-width: 160px; width: 100%">
    <!-- 文本 -->
    <a-input
      v-if="searchType === 'text'"
      :model-value="strValue"
      :placeholder="`请输入${label}`"
      allow-clear
      @update:model-value="emitScalar"
      @press-enter="$emit('search')"
    />

    <!-- 数值范围 -->
    <a-space v-else-if="searchType === 'numberRange'" :size="4">
      <a-input-number
        :model-value="numOf(`${field.name}_min`)"
        placeholder="最小值"
        hide-button
        style="width: 90px"
        @update:model-value="(v: unknown) => emitKey(`${field.name}_min`, v)"
      />
      <span class="range-sep">~</span>
      <a-input-number
        :model-value="numOf(`${field.name}_max`)"
        placeholder="最大值"
        hide-button
        style="width: 90px"
        @update:model-value="(v: unknown) => emitKey(`${field.name}_max`, v)"
      />
    </a-space>

    <!-- 日期范围 -->
    <a-range-picker
      v-else-if="searchType === 'dateRange'"
      :model-value="rangeValue"
      value-format="YYYY-MM-DD"
      style="width: 100%"
      @update:model-value="emitScalar"
    />

    <!-- 日期时间范围 -->
    <a-range-picker
      v-else-if="searchType === 'datetimeRange'"
      show-time
      :model-value="rangeValue"
      value-format="YYYY-MM-DDTHH:mm:ss"
      style="width: 100%; min-width: 280px"
      @update:model-value="emitScalar"
    />

    <!-- 时间范围 -->
    <a-space v-else-if="searchType === 'timeRange'" :size="4">
      <a-time-picker
        :model-value="strOf(`${field.name}_min`)"
        placeholder="起"
        value-format="HH:mm:ss"
        style="width: 100px"
        @update:model-value="(v: unknown) => emitKey(`${field.name}_min`, v)"
      />
      <span class="range-sep">~</span>
      <a-time-picker
        :model-value="strOf(`${field.name}_max`)"
        placeholder="止"
        value-format="HH:mm:ss"
        style="width: 100px"
        @update:model-value="(v: unknown) => emitKey(`${field.name}_max`, v)"
      />
    </a-space>

    <!-- 布尔 / 附件存在性：下拉可读标签 -->
    <a-select
      v-else-if="searchType === 'switch' || searchType === 'fileExists'"
      :model-value="selectValue"
      :placeholder="'请选择'"
      allow-clear
      style="width: 100%"
      @update:model-value="onSelect"
    >
      <a-option
        v-for="opt in boolOptions"
        :key="opt.value === '' ? '_all' : opt.value"
        :value="opt.value"
        :label="opt.label"
      >
        {{ opt.label }}
      </a-option>
    </a-select>

    <!-- GetPage 已物化 dataSource：直接展示可读标签 -->
    <a-select
      v-else-if="searchType === 'select' || hasDataSource"
      :model-value="selectValue"
      :placeholder="`请选择${label}`"
      allow-clear
      style="width: 100%"
      @update:model-value="onSelect"
    >
      <a-option
        v-for="opt in dataSourceOptions"
        :key="opt.value"
        :value="opt.value"
        :label="opt.label"
      >
        {{ opt.label }}
      </a-option>
    </a-select>

    <!-- 地区 / 级联（User.AreaId 等） -->
    <CascaderField
      v-else-if="searchType === 'cascader'"
      :model-value="modelValue as string | number | null"
      :placeholder="`请选择${label}`"
      @update:model-value="emitScalar"
    />

    <!-- 值集（无本地 dataSource 时走 Lov） -->
    <LovSelect
      v-else-if="(searchType === 'lov' || searchType === 'lovMulti') && field.lovCode"
      :code="field.lovCode"
      :model-value="modelValue as any"
      :multiple="searchType === 'lovMulti'"
      :placeholder="`请选择${label}`"
      @update:model-value="emitScalar"
    />

    <!-- 兜底文本 -->
    <a-input
      v-else
      :model-value="strValue"
      :placeholder="`请输入${label}`"
      allow-clear
      @update:model-value="emitScalar"
      @press-enter="$emit('search')"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { resolveSearchControl } from '@/core/utils/fieldControl';
import { normalizeDataSource } from '@/core/utils/viewMapping';
import LovSelect from './LovSelect.vue';
import CascaderField from './CascaderField.vue';

const props = defineProps<{
  field: FieldMeta;
  modelValue?: unknown;
  /** 范围字段等需要读写相邻键时传入整个 searchForm */
  form?: Record<string, unknown>;
}>();

const emit = defineEmits<{
  'update:modelValue': [unknown];
  'update:key': [key: string, value: unknown];
  search: [];
}>();

const label = computed(() => props.field.displayName || props.field.name);
const searchType = computed(() => resolveSearchControl(props.field));
const hasDataSource = computed(
  () => !!(props.field.dataSource && Object.keys(props.field.dataSource).length),
);

const strValue = computed(() =>
  props.modelValue == null ? '' : String(props.modelValue),
);
const selectValue = computed(() =>
  props.modelValue == null || props.modelValue === ''
    ? undefined
    : String(props.modelValue),
);
const rangeValue = computed(() => {
  const v = props.modelValue;
  if (Array.isArray(v) && v.length === 2) return v as string[];
  return undefined;
});

const dataSourceOptions = computed(() =>
  hasDataSource.value ? normalizeDataSource(props.field.dataSource!).options : [],
);

const boolOptions = computed(() => {
  if (searchType.value === 'fileExists') {
    return [
      { value: '', label: '全部' },
      { value: 'true', label: '有' },
      { value: 'false', label: '无' },
    ];
  }
  if (hasDataSource.value) {
    return [{ value: '', label: '全部' }, ...dataSourceOptions.value];
  }
  return [
    { value: '', label: '全部' },
    { value: 'true', label: '是' },
    { value: 'false', label: '否' },
  ];
});

function strOf(key: string): string | undefined {
  const v = props.form?.[key];
  return v == null || v === '' ? undefined : String(v);
}
function numOf(key: string): number | undefined {
  const v = props.form?.[key];
  if (v == null || v === '') return undefined;
  const n = Number(v);
  return Number.isNaN(n) ? undefined : n;
}

function emitScalar(v: unknown) {
  emit('update:modelValue', v);
}
function emitKey(key: string, value: unknown) {
  emit('update:key', key, value);
}

function onSelect(v: unknown) {
  if (v == null || v === '') {
    emitScalar(undefined);
    return;
  }
  const s = String(v);
  const tn = props.field.typeName;
  if (tn === 'Boolean' || searchType.value === 'switch' || searchType.value === 'fileExists') {
    if (s === 'true' || s === '1') emitScalar(true);
    else if (s === 'false' || s === '0') emitScalar(false);
    else emitScalar(s);
    return;
  }
  if (tn === 'Int64' || tn === 'UInt64') {
    const n = Number(s);
    emitScalar(/^-?\d+$/.test(s.trim()) && Number.isSafeInteger(n) ? n : s);
    return;
  }
  if (tn === 'Int32' || tn === 'Decimal' || tn === 'Double' || tn === 'Single') {
    const n = Number(s);
    emitScalar(Number.isNaN(n) ? s : n);
    return;
  }
  emitScalar(s);
}
</script>

<style scoped>
.range-sep {
  color: var(--color-text-3);
  flex-shrink: 0;
}
</style>
