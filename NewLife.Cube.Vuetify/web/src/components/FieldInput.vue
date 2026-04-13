<template>
  <component :is="renderField" />
</template>

<script setup lang="ts">
import { computed, h } from 'vue';
import type { DataField } from '@cube/api-core';
import { resolveWidget, type WidgetType } from '@cube/field-mapping';
import {
  VTextField,
  VTextarea,
  VSwitch,
  VSelect,
  VCheckbox,
} from 'vuetify/components';

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
  const label = props.field.displayName || props.field.name;

  switch (widgetType.value) {
    case WidgetType.Switch:
      return h(VSwitch, {
        modelValue: !!val,
        'onUpdate:modelValue': onUpdate,
        label,
        color: 'primary',
        hideDetails: 'auto',
      });

    case WidgetType.Select:
    case WidgetType.TagList: {
      const items = parseOptions(props.field);
      return h(VSelect, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        items,
        itemTitle: 'label',
        itemValue: 'value',
        variant: 'outlined',
        density: 'comfortable',
        clearable: true,
        hideDetails: 'auto',
      });
    }

    case WidgetType.Number:
    case WidgetType.Decimal:
      return h(VTextField, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        type: 'number',
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto',
      });

    case WidgetType.DateTime:
    case WidgetType.Date:
      return h(VTextField, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        type: widgetType.value === WidgetType.DateTime ? 'datetime-local' : 'date',
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto',
      });

    case WidgetType.TextArea:
    case WidgetType.RichText:
    case WidgetType.Html:
    case WidgetType.Markdown:
      return h(VTextarea, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        variant: 'outlined',
        density: 'comfortable',
        rows: 3,
        autoGrow: true,
        hideDetails: 'auto',
      });

    case WidgetType.Password:
      return h(VTextField, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        type: 'password',
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto',
      });

    case WidgetType.Checkbox:
      return h(VCheckbox, {
        modelValue: !!val,
        'onUpdate:modelValue': onUpdate,
        label,
        color: 'primary',
        hideDetails: 'auto',
      });

    case WidgetType.ReadOnly:
      return h('span', { class: 'text-body-1' }, val ?? '-');

    default:
      return h(VTextField, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        variant: 'outlined',
        density: 'comfortable',
        clearable: true,
        hideDetails: 'auto',
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
