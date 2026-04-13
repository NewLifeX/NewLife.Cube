<template>
  <t-switch v-if="wt === WidgetType.Switch" :value="!!modelValue" @change="(v: boolean) => emit('update:modelValue', v)" />
  <t-select v-else-if="wt === WidgetType.Select || wt === WidgetType.TagList"
    :value="modelValue" @change="(v: any) => emit('update:modelValue', v)"
    :placeholder="'请选择' + (field.displayName || field.name)" clearable style="width: 100%">
    <t-option v-for="opt in options" :key="opt.value" :value="opt.value" :label="opt.label" />
  </t-select>
  <t-input-number v-else-if="wt === WidgetType.Number"
    :value="modelValue" @change="(v: number) => emit('update:modelValue', v)"
    :placeholder="'请输入' + (field.displayName || field.name)" theme="normal" style="width: 100%" />
  <t-input-number v-else-if="wt === WidgetType.Decimal"
    :value="modelValue" @change="(v: number) => emit('update:modelValue', v)"
    :decimal-places="field.precision || 2"
    :placeholder="'请输入' + (field.displayName || field.name)" theme="normal" style="width: 100%" />
  <t-date-picker v-else-if="wt === WidgetType.DateTime"
    :value="modelValue" @change="(v: any) => emit('update:modelValue', v)"
    enable-time-picker :placeholder="'请选择' + (field.displayName || field.name)" style="width: 100%" />
  <t-date-picker v-else-if="wt === WidgetType.Date"
    :value="modelValue" @change="(v: any) => emit('update:modelValue', v)"
    :placeholder="'请选择' + (field.displayName || field.name)" style="width: 100%" />
  <t-textarea v-else-if="wt === WidgetType.TextArea || wt === WidgetType.RichText"
    :value="modelValue" @change="(v: string) => emit('update:modelValue', v)"
    :placeholder="'请输入' + (field.displayName || field.name)" :autosize="{ minRows: 3 }" />
  <t-input v-else-if="wt === WidgetType.Password" type="password"
    :value="modelValue" @change="(v: string) => emit('update:modelValue', v)"
    :placeholder="'请输入' + (field.displayName || field.name)" />
  <span v-else-if="wt === WidgetType.ReadOnly" style="color: var(--td-text-color-secondary)">{{ modelValue ?? '-' }}</span>
  <t-input v-else
    :value="modelValue" @change="(v: string) => emit('update:modelValue', v)"
    :placeholder="'请输入' + (field.displayName || field.name)" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { DataField } from '@cube/api-core';
import { resolveWidget, type WidgetType } from '@cube/field-mapping';

const props = defineProps<{ field: DataField; modelValue: any }>();
const emit = defineEmits<{ 'update:modelValue': [v: any] }>();

const wt = computed(() => resolveWidget(props.field).widget);

const options = computed(() => {
  if (!props.field.dataSource) return [];
  try {
    const map = JSON.parse(props.field.dataSource) as Record<string, string>;
    return Object.entries(map).map(([value, label]) => ({ value, label }));
  } catch {
    return [];
  }
});
</script>
