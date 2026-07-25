<script setup lang="ts">
import { ref, watch } from 'vue';
import VueJsonEditor from 'vue-json-editor';

const props = withDefaults(
  defineProps<{
    modelValue?: string;
    disabled?: boolean;
    placeholder?: string;
  }>(),
  { modelValue: '', disabled: false, placeholder: '请输入 Json' },
);

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

function parseValue(str: string): unknown {
  if (!str || !str.trim()) return {};
  try {
    return JSON.parse(str);
  } catch {
    return {};
  }
}

const innerValue = ref<unknown>(parseValue(props.modelValue));
let isInternalChange = false;

watch(
  () => props.modelValue,
  (val) => {
    if (isInternalChange) {
      isInternalChange = false;
      return;
    }
    innerValue.value = parseValue(val);
  },
);

function onInput(val: unknown) {
  isInternalChange = true;
  innerValue.value = val;
  if (val === null || val === undefined) {
    emit('update:modelValue', '');
    return;
  }
  try {
    emit('update:modelValue', JSON.stringify(val, null, 2));
  } catch {
    // ignore
  }
}

function onError(_err: unknown) {
  // JSON syntax error during typing, no action needed
}
</script>

<template>
  <div class="json-editor" :class="{ 'is-disabled': disabled }">
    <VueJsonEditor
      :value="innerValue"
      :mode="'code'"
      :show-btns="false"
      :expanded-on-start="false"
      :lang="'zh'"
      @input="onInput"
      @has-error="onError"
    />
    <p v-if="!modelValue" class="json-editor__placeholder">{{ placeholder }}</p>
  </div>
</template>

<style scoped>
.json-editor {
  width: 100%;
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  background: var(--el-bg-color);
  overflow: hidden;
}

.json-editor.is-disabled {
  pointer-events: none;
  opacity: 0.6;
}

.json-editor__placeholder {
  margin: 0;
  padding: 8px 10px;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}
</style>

<style>
.jsoneditor-vue .jsoneditor-outer {
  min-height: 150px;
}
.jsoneditor-vue div.jsoneditor-tree {
  min-height: 200px;
}
</style>
