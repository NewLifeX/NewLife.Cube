<template>
  <component :is="renderField" />
</template>

<script setup lang="ts">
import { computed, h } from 'vue';
import type { DataField } from '@cube/api-core';
import { resolveWidget } from '@cube/field-mapping';
import {
  Input as AInput,
  InputNumber as AInputNumber,
  InputPassword as AInputPassword,
  Select as ASelect,
  Switch as ASwitch,
  Textarea as ATextarea,
  DatePicker as ADatePicker,
} from '@arco-design/web-vue';

const props = defineProps<{
  field: DataField;
  value?: any;
}>();

const emit = defineEmits<{
  (e: 'update:value', v: any): void;
}>();

const widgetType = computed(() => resolveWidget(props.field).widget);

const renderField = computed(() => {
  const val = props.value;
  const onUpdate = (v: any) => emit('update:value', v);

  switch (widgetType.value) {
    case 'switch':
      return h(ASwitch, { modelValue: !!val, 'onUpdate:modelValue': onUpdate });

    case 'select': {
      const options = parseOptions(props.field);
      return h(ASelect, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请选择${props.field.displayName || props.field.name}`,
        allowClear: true,
        options,
      });
    }

    case 'number':
      return h(AInputNumber, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入${props.field.displayName || props.field.name}`,
        precision: 0,
      });

    case 'datetime':
    case 'date':
      return h(ADatePicker, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        showTime: widgetType.value === 'datetime',
        placeholder: `请选择${props.field.displayName || props.field.name}`,
        allowClear: true,
        style: 'width: 100%',
      });

    case 'textarea':
    case 'html':
    case 'code':
      return h(ATextarea, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入${props.field.displayName || props.field.name}`,
        autoSize: { minRows: 3 },
      });

    case 'password':
      return h(AInputPassword, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入${props.field.displayName || props.field.name}`,
      });

    case 'file':
    case 'image':
      return h(AInput, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入文件URL`,
      });

    case 'readonly':
      return h('span', {}, val ?? '-');

    default:
      return h(AInput, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入${props.field.displayName || props.field.name}`,
        allowClear: true,
      });
  }
});

function parseOptions(field: DataField) {
  if (!field.dataSource) return [];
  return Object.entries(field.dataSource).map(([value, label]) => ({ value, label }));
}
</script>
