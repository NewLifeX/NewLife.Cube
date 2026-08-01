<template>
  <a-textarea
    :model-value="String(modelValue ?? '')"
    :auto-size="{ minRows: 5, maxRows: 16 }"
    :disabled="disabled"
    placeholder="富文本 / Markdown"
    @update:model-value="(v: string) => emit('update:modelValue', v)"
  />
</template>

<script setup lang="ts">
defineProps<{ modelValue?: string; disabled?: boolean }>();
const emit = defineEmits<{ 'update:modelValue': [string] }>();
</script>
