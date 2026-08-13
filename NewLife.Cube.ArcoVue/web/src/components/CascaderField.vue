<template>
  <a-cascader
    :model-value="pathValue"
    :options="options"
    :field-names="{ value: 'value', label: 'label', children: 'children' }"
    :loading="loading"
    :disabled="disabled"
    :placeholder="placeholder"
    allow-search
    expand-trigger="click"
    :path-mode="false"
    allow-clear
    style="width: 100%"
    @update:model-value="onChange"
  />
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
  onChange,
} = useCascaderField(props, emit);
</script>
