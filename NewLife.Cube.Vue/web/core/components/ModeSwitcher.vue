<script setup lang="ts">
/**
 * ModeSwitcher - 暗/亮模式切换按钮（太阳/月亮）
 * 使用 ActionButton 基类样式
 */
import { computed } from 'vue';
import { useTheme } from '../composables/useTheme';
import ActionButton from './ActionButton.vue';

interface Props {
  /** 自定义点击回调，不传则使用默认切换逻辑 */
  onClick?: () => void;
}

const props = withDefaults(defineProps<Props>(), {});

const { currentTheme, toggleMode } = useTheme();

const isDark = computed(() => currentTheme.value.mode === 'dark');

const handleClick = () => {
  if (props.onClick) {
    props.onClick();
  } else {
    toggleMode();
  }
};
</script>

<template>
  <ActionButton :title="isDark ? '切换到浅色模式' : '切换到深色模式'" @click="handleClick">
    <!-- 太阳图标（浅色模式） -->
    <svg v-show="!isDark" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <circle cx="12" cy="12" r="5" />
      <line x1="12" y1="1" x2="12" y2="3" />
      <line x1="12" y1="21" x2="12" y2="23" />
      <line x1="4.22" y1="4.22" x2="5.64" y2="5.64" />
      <line x1="18.36" y1="18.36" x2="19.78" y2="19.78" />
      <line x1="1" y1="12" x2="3" y2="12" />
      <line x1="21" y1="12" x2="23" y2="12" />
      <line x1="4.22" y1="19.78" x2="5.64" y2="18.36" />
      <line x1="18.36" y1="5.64" x2="19.78" y2="4.22" />
    </svg>
    <!-- 月亮图标（深色模式） -->
    <svg v-show="isDark" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z" />
    </svg>
  </ActionButton>
</template>