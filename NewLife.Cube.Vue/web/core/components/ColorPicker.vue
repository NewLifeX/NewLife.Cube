<script setup lang="ts">
/**
 * 颜色选择器（封装 ElColorPicker）
 *
 * v-model 绑定「色值字符串」（resolveControl 映射的 control = 'color'）。
 * 列表色块展示由 ListTableContent 直接渲染，本组件用于表单编辑。
 */
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    modelValue?: string;
    disabled?: boolean;
  }>(),
  { modelValue: '', disabled: false },
);

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

// 当未选择时组件返回 null，统一回写为空串
const current = computed<string>(() => props.modelValue ?? '');

function handleChange(color: string | null) {
  emit('update:modelValue', color ?? '');
}
</script>

<template>
  <div class="color-picker">
    <el-color-picker :model-value="current" :disabled="disabled" @change="handleChange" />
    <span class="color-picker__swatch" :style="{ background: current || 'transparent' }"></span>
    <span class="color-picker__text">{{ current || '—' }}</span>
  </div>
</template>

<style scoped>
.color-picker {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

.color-picker__swatch {
  width: 22px;
  height: 22px;
  border-radius: 4px;
  border: 1px solid var(--el-border-color);
  flex-shrink: 0;
}

.color-picker__text {
  font-family: var(--el-font-family-mono);
  font-size: 13px;
  color: var(--el-text-color-primary);
}
</style>
