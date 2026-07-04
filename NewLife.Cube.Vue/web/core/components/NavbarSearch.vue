<script setup lang="ts">
/**
 * NavbarSearch - 统一搜索组件
 *
 * 两种模式：
 *   icon — 仅显示搜索图标按钮（ActionButton 基础样式）
 *   box  — 显示带图标的搜索输入框
 *
 * Props:
 *   mode        — 显示模式，'icon' | 'box'，默认 'icon'
 *   placeholder — 输入框占位文字
 */
import ActionButton from './ActionButton.vue';

interface Props {
  /** 显示模式 */
  mode?: 'icon' | 'box';
  /** 输入框占位文字 */
  placeholder?: string;
}

const props = withDefaults(defineProps<Props>(), {
  mode: 'icon',
  placeholder: '搜索...',
});

const emit = defineEmits<{
  search: [value: string];
}>();

function handleSearch(e: Event) {
  emit('search', (e.target as HTMLInputElement).value);
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') {
    emit('search', (e.target as HTMLInputElement).value);
  }
}
</script>

<template>
  <!-- 图标模式：使用 ActionButton 基础样式 -->
  <ActionButton v-if="mode === 'icon'" title="全局搜索">
    <svg
      width="15"
      height="15"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
    >
      <circle cx="11" cy="11" r="8" />
      <path d="M21 21l-4.35-4.35" />
    </svg>
  </ActionButton>

  <!-- 搜索框模式 -->
  <div v-else class="search-box">
    <svg
      class="search-icon"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
    >
      <circle cx="11" cy="11" r="8" />
      <path d="M21 21l-4.35-4.35" />
    </svg>
    <input
      type="text"
      class="search-input"
      :placeholder="placeholder"
      @input="handleSearch"
      @keydown="handleKeydown"
    />
  </div>
</template>

<style lang="scss" scoped>
.search-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--bg-tertiary, rgba(0, 0, 0, 0.04));
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--radius-sm, 8px);
  padding: 7px 12px;
  width: 220px;
  transition: border-color 0.2s;

  &:focus-within {
    border-color: var(--el-color-primary);
  }
}

.search-icon {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.search-input {
  background: none;
  border: none;
  outline: none;
  color: var(--el-text-color-primary);
  font-family: inherit;
  font-size: 13px;
  width: 100%;

  &::placeholder {
    color: var(--el-text-color-secondary);
  }
}
</style>
