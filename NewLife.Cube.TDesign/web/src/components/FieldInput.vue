<template>
  <t-switch v-if="wt === 'switch'" :value="!!modelValue" @change="(v: boolean) => emit('update:modelValue', v)" />
  <t-select v-else-if="wt === 'select'"
    :value="modelValue" @change="(v: any) => emit('update:modelValue', v)"
    :placeholder="'请选择' + (field.displayName || field.name)" clearable style="width: 100%">
    <t-option v-for="opt in options" :key="opt.value" :value="opt.value" :label="opt.label" />
  </t-select>
  <t-input-number v-else-if="wt === 'number'"
    :value="modelValue" @change="(v: number) => emit('update:modelValue', v)"
    :placeholder="'请输入' + (field.displayName || field.name)" theme="normal" style="width: 100%" />
  <t-date-picker v-else-if="wt === 'datetime'"
    :value="modelValue" @change="(v: any) => emit('update:modelValue', v)"
    enable-time-picker :placeholder="'请选择' + (field.displayName || field.name)" style="width: 100%" />
  <t-date-picker v-else-if="wt === 'date'"
    :value="modelValue" @change="(v: any) => emit('update:modelValue', v)"
    :placeholder="'请选择' + (field.displayName || field.name)" style="width: 100%" />
  <t-textarea v-else-if="wt === 'textarea' || wt === 'html'"
    :value="modelValue" @change="(v: string) => emit('update:modelValue', v)"
    :placeholder="'请输入' + (field.displayName || field.name)" :autosize="{ minRows: 3 }" />
  <t-input v-else-if="wt === 'password'" type="password"
    :value="modelValue" @change="(v: string) => emit('update:modelValue', v)"
    :placeholder="'请输入' + (field.displayName || field.name)" />
  <span v-else-if="wt === 'readonly'" style="color: var(--td-text-color-secondary)">{{ modelValue ?? '-' }}</span>
  <t-input v-else
    :value="modelValue" @change="(v: string) => emit('update:modelValue', v)"
    :placeholder="'请输入' + (field.displayName || field.name)" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { DataField } from '@cube/api-core';
import { resolveWidget } from '@cube/field-mapping';

const props = defineProps<{ field: DataField; modelValue: any }>();
const emit = defineEmits<{ 'update:modelValue': [v: any] }>();

const wt = computed(() => resolveWidget(props.field).widget);

const options = computed(() => {
  if (!props.field.dataSource) return [];
  return Object.entries(props.field.dataSource).map(([value, label]) => ({ value, label }));
});
</script>
