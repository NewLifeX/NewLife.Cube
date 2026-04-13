<template>
  <component :is="renderField" />
</template>

<script setup lang="ts">
import { computed, h } from 'vue';
import type { DataField } from '@cube/api-core';
import { resolveWidget, type WidgetType } from '@cube/field-mapping';
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
    case WidgetType.Switch:
      return h(ASwitch, { modelValue: !!val, 'onUpdate:modelValue': onUpdate });

    case WidgetType.Select:
    case WidgetType.TagList: {
      const options = parseOptions(props.field);
      return h(ASelect, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请选择${props.field.displayName || props.field.name}`,
        allowClear: true,
        options,
      });
    }

    case WidgetType.Number:
    case WidgetType.Decimal:
      return h(AInputNumber, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入${props.field.displayName || props.field.name}`,
        precision: widgetType.value === WidgetType.Decimal ? (props.field.precision || 2) : 0,
      });

    case WidgetType.DateTime:
    case WidgetType.Date:
      return h(ADatePicker, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        showTime: widgetType.value === WidgetType.DateTime,
        placeholder: `请选择${props.field.displayName || props.field.name}`,
        allowClear: true,
        style: 'width: 100%',
      });

    case WidgetType.TextArea:
    case WidgetType.RichText:
    case WidgetType.Html:
    case WidgetType.Markdown:
      return h(ATextarea, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入${props.field.displayName || props.field.name}`,
        autoSize: { minRows: 3 },
      });

    case WidgetType.Password:
      return h(AInputPassword, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入${props.field.displayName || props.field.name}`,
      });

    case WidgetType.File:
    case WidgetType.Image:
      return h(AInput, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        placeholder: `请输入文件URL`,
      });

    case WidgetType.ReadOnly:
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
  try {
    const map = JSON.parse(field.dataSource) as Record<string, string>;
    return Object.entries(map).map(([value, label]) => ({ value, label }));
  } catch {
    return [];
  }
}
</script>
