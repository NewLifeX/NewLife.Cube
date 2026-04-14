<template>
  <component :is="renderField" />
</template>

<script setup lang="ts">
import { computed, h } from 'vue';
import type { DataField } from '@cube/api-core';
import { resolveWidget } from '@cube/field-mapping';
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
    case 'switch':
      return h(VSwitch, {
        modelValue: !!val,
        'onUpdate:modelValue': onUpdate,
        label,
        color: 'primary',
        hideDetails: 'auto',
      });

    case 'select': {
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

    case 'number':
      return h(VTextField, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        type: 'number',
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto',
      });

    case 'datetime':
    case 'date':
      return h(VTextField, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        type: widgetType.value === 'datetime' ? 'datetime-local' : 'date',
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto',
      });

    case 'textarea':
    case 'html':
    case 'code':
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

    case 'password':
      return h(VTextField, {
        modelValue: val,
        'onUpdate:modelValue': onUpdate,
        label,
        type: 'password',
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto',
      });

    case 'checkbox':
      return h(VCheckbox, {
        modelValue: !!val,
        'onUpdate:modelValue': onUpdate,
        label,
        color: 'primary',
        hideDetails: 'auto',
      });

    case 'readonly':
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
  return Object.entries(field.dataSource).map(([value, label]) => ({ value, label }));
}
</script>
