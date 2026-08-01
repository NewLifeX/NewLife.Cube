<template>
  <div class="field-input" style="width: 100%">
    <template v-if="control === 'readonly'">
      <a-input :model-value="displayText" readonly disabled />
    </template>
    <a-textarea
      v-else-if="control === 'textarea'"
      :model-value="strValue"
      :disabled="disabled"
      :auto-size="{ minRows: 2, maxRows: 6 }"
      @update:model-value="emitValue"
    />
    <a-input-number
      v-else-if="control === 'inputNumber'"
      :model-value="numValue"
      :disabled="disabled"
      :precision="precision"
      :step="step"
      style="width: 100%"
      @update:model-value="emitValue"
    />
    <a-switch
      v-else-if="control === 'switch'"
      :model-value="!!modelValue"
      :disabled="disabled"
      @update:model-value="emitValue"
    />
    <a-date-picker
      v-else-if="control === 'datePicker'"
      :model-value="strValue || undefined"
      show-time
      value-format="YYYY-MM-DDTHH:mm:ss"
      style="width: 100%"
      :disabled="disabled"
      @update:model-value="emitValue"
    />
    <a-time-picker
      v-else-if="control === 'timePicker'"
      :model-value="strValue || undefined"
      value-format="HH:mm:ss"
      style="width: 100%"
      :disabled="disabled"
      @update:model-value="emitValue"
    />
    <a-select
      v-else-if="control === 'select'"
      :model-value="selectValue"
      :disabled="disabled"
      allow-clear
      style="width: 100%"
      @update:model-value="onSelect"
    >
      <a-option
        v-for="(label, key) in field.dataSource || {}"
        :key="key"
        :value="String(key)"
      >
        {{ label }}
      </a-option>
    </a-select>
    <LovSelect
      v-else-if="control === 'lov' || control === 'lovMulti'"
      :code="field.lovCode || ''"
      :model-value="modelValue as any"
      :multiple="control === 'lovMulti'"
      :disabled="disabled"
      @update:model-value="emitValue"
    />
    <a-upload
      v-else-if="control === 'upload' || control === 'image'"
      :custom-request="onUpload"
      :show-file-list="false"
      :disabled="disabled"
    >
      <template #upload-button>
        <a-space>
          <a-button>{{ control === 'image' ? '上传图片' : '上传文件' }}</a-button>
          <a-link v-if="strValue" :href="strValue" target="_blank">已上传</a-link>
        </a-space>
      </template>
    </a-upload>
    <JsonEditor
      v-else-if="control === 'json'"
      :model-value="modelValue"
      :disabled="disabled"
      @update:model-value="emitValue"
    />
    <RichEditor
      v-else-if="control === 'richHtml' || control === 'richMarkdown'"
      :model-value="strValue"
      :disabled="disabled"
      @update:model-value="emitValue"
    />
    <a-input
      v-else-if="control === 'color'"
      type="color"
      :model-value="strValue || '#000000'"
      :disabled="disabled"
      style="width: 64px"
      @update:model-value="emitValue"
    />
    <a-input
      v-else
      :model-value="strValue"
      :disabled="disabled"
      :type="inputType"
      :placeholder="field.description || field.displayName"
      allow-clear
      @update:model-value="emitValue"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { ControlType, FieldMeta } from '@/core/types/field';
import {
  resolveControl,
  resolveNumberPrecision,
  resolveNumberStep,
} from '@/core/utils/fieldControl';
import cubeApi from '@/api';
import LovSelect from './LovSelect.vue';
import JsonEditor from './JsonEditor.vue';
import RichEditor from './RichEditor.vue';

const props = defineProps<{
  field: FieldMeta;
  modelValue?: unknown;
  disabled?: boolean;
  /** 上传所属实体路径 */
  typePath?: string;
  controlOverride?: ControlType;
}>();

const emit = defineEmits<{ 'update:modelValue': [unknown] }>();

const control = computed(
  () => props.controlOverride ?? resolveControl(props.field),
);
const strValue = computed(() =>
  props.modelValue == null ? '' : String(props.modelValue),
);
const numValue = computed(() => {
  if (props.modelValue == null || props.modelValue === '') return undefined;
  return Number(props.modelValue);
});
const displayText = computed(() => strValue.value || '-');
const precision = computed(() => resolveNumberPrecision(props.field));
const step = computed(() => resolveNumberStep(props.field));
const selectValue = computed(() =>
  props.modelValue == null || props.modelValue === '' ? undefined : String(props.modelValue),
);
const inputType = computed(() => {
  switch (control.value) {
    case 'email':
      return 'email';
    case 'tel':
      return 'tel';
    case 'url':
      return 'url';
    default:
      return 'text';
  }
});

function emitValue(v: unknown) {
  emit('update:modelValue', v);
}

/** 下拉值尽量还原数值/布尔，兼容实体字段类型 */
function onSelect(v: unknown) {
  if (v == null || v === '') {
    emitValue(undefined);
    return;
  }
  const s = String(v);
  const tn = props.field.typeName;
  if (tn === 'Boolean') {
    emitValue(s === 'true' || s === '1');
    return;
  }
  if (tn === 'Int32' || tn === 'Int64' || tn === 'Decimal' || tn === 'Double' || tn === 'Single') {
    const n = Number(s);
    emitValue(Number.isNaN(n) ? s : n);
    return;
  }
  emitValue(s);
}

async function onUpload(option: { fileItem: { file?: File }; onSuccess: () => void; onError: () => void }) {
  const file = option.fileItem.file;
  if (!file || !props.typePath) {
    option.onError();
    return;
  }
  try {
    const res = await cubeApi.page.uploadFile(props.typePath, file, { id: 0 });
    const url = (res.data as Record<string, unknown>)?.url
      ?? (res.data as Record<string, unknown>)?.path
      ?? res.data;
    emitValue(url);
    option.onSuccess();
  } catch {
    option.onError();
  }
}
</script>
