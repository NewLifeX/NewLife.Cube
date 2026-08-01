<template>
  <a-textarea
    :model-value="text"
    :auto-size="{ minRows: 4, maxRows: 12 }"
    :disabled="disabled"
    placeholder="JSON"
    @update:model-value="onInput"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  modelValue?: unknown;
  disabled?: boolean;
}>();

const emit = defineEmits<{ 'update:modelValue': [unknown] }>();

const text = computed(() => {
  const v = props.modelValue;
  if (v == null) return '';
  if (typeof v === 'string') return v;
  try {
    return JSON.stringify(v, null, 2);
  } catch {
    return String(v);
  }
});

function onInput(s: string) {
  try {
    emit('update:modelValue', JSON.parse(s));
  } catch {
    emit('update:modelValue', s);
  }
}
</script>
