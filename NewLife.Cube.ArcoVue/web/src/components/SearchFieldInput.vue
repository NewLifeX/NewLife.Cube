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

    <!-- 数值（单值等值，OSC-0016） -->
    <a-input-number
      v-else-if="searchType === 'number'"
      :model-value="numOfField"
      placeholder="请输入数值"
      hide-button
      style="width: 100%"
      @update:model-value="emitScalar"
    />

    <!-- 日期（单值等值） -->
    <a-date-picker
      v-else-if="searchType === 'date'"
      :model-value="strOrUndef(modelValue)"
      value-format="YYYY-MM-DD"
      style="width: 100%"
      @update:model-value="emitScalar"
    />

    <!-- 日期时间（单值等值） -->
    <a-date-picker
      v-else-if="searchType === 'datetime'"
      show-time
      :model-value="strOrUndef(modelValue)"
      value-format="YYYY-MM-DDTHH:mm:ss"
      style="width: 100%; min-width: 200px"
      @update:model-value="emitScalar"
    />

    <!-- 时间（单值等值） -->
    <a-time-picker
      v-else-if="searchType === 'time'"
      :model-value="strOrUndef(modelValue)"
      placeholder="请选择时间"
      value-format="HH:mm:ss"
      style="width: 100%"
      @update:model-value="emitScalar"
    />

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
import type { FieldMeta } from '@/core/types/field';
import LovSelect from './LovSelect.vue';
import CascaderField from './CascaderField.vue';
import { useSearchFieldInput } from './useSearchFieldInput';

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

const {
  searchType,
  strValue,
  label,
  emitScalar,
  numOfField,
  strOrUndef,
  selectValue,
  boolOptions,
  onSelect,
  dataSourceOptions,
  hasDataSource,
} = useSearchFieldInput(props, emit);
</script>

<style scoped>
.range-sep {
  color: var(--color-text-3);
  flex-shrink: 0;
}
</style>
