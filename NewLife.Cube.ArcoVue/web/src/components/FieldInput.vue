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
      allow-search
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
    <div v-else-if="control === 'upload' || control === 'image'" class="field-upload">
      <a-input
        :model-value="strValue"
        :disabled="disabled"
        :placeholder="control === 'image' ? '图片路径，如 /Uploads/Cube/xxx.png' : '文件路径'"
        allow-clear
        @update:model-value="emitValue"
      >
        <template #append>
          <a-upload
            :custom-request="onUpload"
            :show-file-list="false"
            :disabled="disabled"
            :accept="control === 'image' ? 'image/*' : undefined"
          >
            <template #upload-button>
              <a-button type="primary" :disabled="disabled">上传</a-button>
            </template>
          </a-upload>
        </template>
      </a-input>
      <div v-if="control === 'image' && strValue" class="field-upload__preview-row">
        <img :src="strValue" alt="" class="field-upload-preview" />
        <a-link :href="strValue" target="_blank">预览</a-link>
      </div>
    </div>
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
    <div v-else-if="control === 'color'" class="field-color-row">
      <button
        type="button"
        class="field-color-swatch"
        :style="{ background: colorValue }"
        :disabled="disabled"
        :title="`选择${field.displayName || field.name}`"
        @click="openColorPicker"
      />
      <input
        ref="colorInputRef"
        type="color"
        :value="colorValue"
        class="field-color-input-hidden"
        :disabled="disabled"
        @input="onColorInput"
      />
      <span class="field-color-text">{{ colorValue }}</span>
    </div>
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
  colorInputRef,
  colorValue,
  openColorPicker,
  onColorInput,
} = useFieldInput(props, emit);
</script>

<style scoped>
.field-upload {
  width: 100%;
}
.field-upload :deep(.arco-upload),
.field-upload :deep(.arco-upload-trigger) {
  display: inline-flex;
  vertical-align: middle;
}
.field-upload :deep(.arco-input-append) {
  padding: 0;
}
.field-upload :deep(.arco-input-append .arco-btn) {
  border-radius: 0;
  height: 100%;
}
.field-upload__preview-row {
  margin-top: 8px;
  display: flex;
  align-items: center;
  gap: 8px;
}
.field-upload-preview {
  max-width: 120px;
  max-height: 48px;
  object-fit: contain;
  border: 1px solid var(--color-border-2);
  border-radius: 4px;
  background: var(--color-fill-1);
}
.field-color-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.field-color-swatch {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid var(--color-border-2);
  padding: 0;
  cursor: pointer;
  flex-shrink: 0;
  background: transparent;
}
.field-color-swatch:hover:not(:disabled) {
  box-shadow: 0 0 0 2px rgb(var(--primary-6));
}
.field-color-swatch:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}
.field-color-input-hidden {
  position: absolute;
  opacity: 0;
  width: 1px;
  height: 1px;
  pointer-events: none;
}
.field-color-text {
  color: var(--color-text-2);
  font-size: 13px;
}
</style>
