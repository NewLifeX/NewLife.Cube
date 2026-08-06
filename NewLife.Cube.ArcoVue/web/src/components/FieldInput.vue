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
      :model-value="pickerValue"
      :show-time="dateKind !== 'date'"
      :value-format="pickerFormat"
      style="width: 100%"
      :disabled="disabled"
      @update:model-value="onPickerChange"
    />
    <a-time-picker
      v-else-if="control === 'timePicker'"
      :model-value="pickerValue"
      value-format="HH:mm:ss"
      style="width: 100%"
      :disabled="disabled"
      @update:model-value="onPickerChange"
    />
    <a-select
      v-else-if="control === 'select'"
      :model-value="selectValue"
      :disabled="disabled"
      :placeholder="`请选择${field.displayName || field.name}`"
      allow-clear
      style="width: 100%"
      @update:model-value="onSelect"
    >
      <a-option
        v-for="opt in selectOptions"
        :key="opt.value"
        :value="opt.value"
        :label="opt.label"
      >
        {{ opt.label }}
      </a-option>
    </a-select>
    <CascaderField
      v-else-if="control === 'cascader'"
      :model-value="modelValue as string | number | null"
      :disabled="disabled"
      :placeholder="`请选择${field.displayName || field.name}`"
      @update:model-value="emitValue"
    />
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
import { normalizeDataSource } from '@/core/utils/viewMapping';
import {
  type DateKind,
  fromPickerValue,
  inferDateKind,
  toPickerValue,
} from '@/core/utils/datetime';
import cubeApi from '@/api';
import LovSelect from './LovSelect.vue';
import CascaderField from './CascaderField.vue';
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
/** 枚举/状态字典：按 label 去重并优先数字键（后端 PrepareForApi 同时物化数字键与名称键） */
const dsNorm = computed(() =>
  props.field.dataSource && Object.keys(props.field.dataSource).length
    ? normalizeDataSource(props.field.dataSource)
    : null,
);
const selectOptions = computed(() => dsNorm.value?.options ?? []);
const selectValue = computed(() => {
  if (props.modelValue == null || props.modelValue === '') return undefined;
  const s = String(props.modelValue);
  // 回显兼容：名称键（如「男」）映射回规范数字键（如「1」），避免选中态丢失
  return dsNorm.value?.canonicalByKey.get(s) ?? s;
});

/** 日期种类与 picker 字符串值：壁钟时间，避免时区漂移 */
const dateKind = computed<DateKind>(() =>
  control.value === 'datePicker' || control.value === 'timePicker'
    ? inferDateKind(props.field)
    : 'datetime',
);
const pickerFormat = computed(() =>
  dateKind.value === 'date' ? 'YYYY-MM-DD' : dateKind.value === 'time' ? 'HH:mm:ss' : 'YYYY-MM-DD HH:mm:ss',
);
const pickerValue = computed(() => {
  if (control.value !== 'datePicker' && control.value !== 'timePicker') return undefined;
  if (props.modelValue == null || props.modelValue === '') return undefined;
  return toPickerValue(props.modelValue, dateKind.value);
});

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

/** picker 输出 → naive 本地字符串提交后端 */
function onPickerChange(v: unknown) {
  if (v == null || v === '') {
    emitValue(undefined);
    return;
  }
  emitValue(fromPickerValue(v, dateKind.value));
}

/** 下拉值尽量还原数值/布尔，兼容实体字段类型；Int64 超安全整数保留字符串避免精度丢失（OSC-0009） */
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
  if (tn === 'Int64' || tn === 'UInt64') {
    const n = Number(s);
    emitValue(/^-?\d+$/.test(s.trim()) && Number.isSafeInteger(n) ? n : s);
    return;
  }
  if (tn === 'Int32' || tn === 'Decimal' || tn === 'Double' || tn === 'Single') {
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
