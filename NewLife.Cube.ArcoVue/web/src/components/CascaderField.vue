<template>
  <a-cascader
    :model-value="pathValue"
    :popup-visible="popupVisible"
    @popup-visible-change="onPopupVisibleChange"
    :options="options"
    :field-names="{ value: 'value', label: 'label', children: 'children' }"
    :loading="loading"
    :disabled="disabled"
    :placeholder="placeholder"
    allow-search
    expand-trigger="click"
    :path-mode="true"
    :load-more="loadMore"
    :fallback="fallbackLabel"
    allow-clear
    style="width: 100%"
    @update:model-value="onChange"
  >
    <template #option="{ data }">
      <span
        class="cascader-option-label"
        :title="'双击选定 ' + (data.label || '')"
        @dblclick.stop="onOptionDblClick(data)"
      >
        {{ data.label }}
      </span>
    </template>
  </a-cascader>
</template>

<script setup lang="ts">
import { useCascaderField } from './useCascaderField';

const props = withDefaults(
  defineProps<{
    modelValue?: string | number | null;
    disabled?: boolean;
    placeholder?: string;
  }>(),
  { placeholder: '请选择地区', disabled: false },
);

const emit = defineEmits<{ 'update:modelValue': [value: number | string | undefined] }>();

const {
  options,
  pathValue,
  loading,
  popupVisible,
  onChange,
  fallbackLabel,
  onPopupVisibleChange,
  onOptionDblClick,
  loadMore,
} = useCascaderField(props, emit);
</script>
