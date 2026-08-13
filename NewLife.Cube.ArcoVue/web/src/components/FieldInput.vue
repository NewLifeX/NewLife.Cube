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
import type { ControlType, FieldMeta } from '@/core/types/field';
import LovSelect from './LovSelect.vue';
import CascaderField from './CascaderField.vue';
import JsonEditor from './JsonEditor.vue';
import RichEditor from './RichEditor.vue';
import { useFieldInput } from './useFieldInput';

const props = defineProps<{
  field: FieldMeta;
  modelValue?: unknown;
  disabled?: boolean;
  /** 上传所属实体路径 */
  typePath?: string;
  controlOverride?: ControlType;
}>();

const emit = defineEmits<{ 'update:modelValue': [unknown] }>();

const {
  control,
  displayText,
  strValue,
  emitValue,
  numValue,
  precision,
  step,
  pickerValue,
  dateKind,
  pickerFormat,
  onPickerChange,
  selectValue,
  selectOptions,
  onSelect,
  onUpload,
  inputType,
} = useFieldInput(props, emit);
</script>
