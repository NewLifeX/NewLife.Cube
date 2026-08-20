<template>
  <!-- 批量修改单行值控件（OSC-260819e483）：按字段 typeName 自适应；状态/枚举/值集 → 下拉（元数据填充） -->
  <a-select
    v-if="isSelect"
    :model-value="modelValue"
    :options="options"
    :loading="optionsLoading"
    allow-clear
    placeholder="请选择要设置的字段值"
    @update:model-value="onSelect"
  />
  <a-input-number
    v-else-if="controlType === 'inputNumber'"
    :model-value="numValue"
    style="width: 100%"
    placeholder="请输入数值"
    @update:model-value="onNumber"
  />
  <a-date-picker
    v-else-if="controlType === 'datePicker'"
    :model-value="modelValue || undefined"
    style="width: 100%"
    value-format="YYYY-MM-DD HH:mm:ss"
    placeholder="请选择日期"
    @update:model-value="(v: string) => emit('update:modelValue', v ?? '')"
  />
  <a-time-picker
    v-else-if="controlType === 'timePicker'"
    :model-value="modelValue || undefined"
    style="width: 100%"
    placeholder="请选择时间"
    @update:model-value="(v: string) => emit('update:modelValue', v ?? '')"
  />
  <a-textarea
    v-else-if="controlType === 'textarea'"
    :model-value="modelValue"
    :auto-size="{ minRows: 2, maxRows: 4 }"
    placeholder="请输入要设置的字段值"
    @update:model-value="(v: string) => emit('update:modelValue', v)"
  />
  <a-input
    v-else
    :model-value="modelValue"
    placeholder="请输入要设置的字段值"
    @update:model-value="(v: string) => emit('update:modelValue', v)"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { ControlType, FieldOption } from '@/core/types/field';

defineOptions({ name: 'BatchEditValueInput' });

const props = defineProps<{
  controlType: ControlType;
  isSelect: boolean;
  options: FieldOption[];
  optionsLoading: boolean;
  modelValue: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: string];
}>();

const numValue = computed(() => {
  const t = props.modelValue.trim();
  return t === '' ? undefined : Number(t);
});

function onSelect(v: unknown) {
  emit('update:modelValue', v == null ? '' : String(v));
}

function onNumber(v: number | undefined) {
  emit('update:modelValue', v === undefined ? '' : String(v));
}
</script>
