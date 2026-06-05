<script setup lang="ts">
/**
 * ActionButton - 导航栏功能按钮基类组件
 *
 * 特性：
 * - 无边框透明背景，悬浮显示背景色
 * - 使用 CSS 变量适配亮/暗模式
 * - 支持自定义图标和点击回调
 */

interface Props {
  /** 按钮尺寸: small(32px) | medium(34px) | large(40px) */
  size?: 'small' | 'medium' | 'large';
  /** 点击回调 */
  onClick?: () => void;
  /** 按钮标题/tooltip */
  title?: string;
  /** 是否禁用 */
  disabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  size: 'medium',
  title: '',
  disabled: false,
});

const emit = defineEmits<{
  click: [];
}>();

function handleClick() {
  if (!props.disabled) {
    if (props.onClick) {
      props.onClick();
    } else {
      emit('click');
    }
  }
}
</script>

<template>
  <button
    class="action-btn"
    :class="[`size-${size}`, { disabled }]"
    :title="title"
    :disabled="disabled"
    @click="handleClick"
  >
    <slot />
  </button>
</template>

<style lang="scss" scoped>
.action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--navbar-text, var(--text-secondary));
  cursor: pointer;
  transition:
    background 0.15s,
    color 0.15s;
  font-family: inherit;
  flex-shrink: 0;
  white-space: nowrap;

  &.size-small {
    min-width: 32px;
    height: 32px;
    padding: 0 9px;

    :deep(svg) {
      width: 14px;
      height: 14px;
    }
  }

  &.size-medium {
    min-width: 34px;
    height: 34px;
    padding: 0 10px;

    :deep(svg) {
      width: 15px;
      height: 15px;
    }
  }

  &.size-large {
    min-width: 40px;
    height: 40px;
    padding: 0 13px;

    :deep(svg) {
      width: 16px;
      height: 16px;
    }
  }

  &:hover:not(.disabled) {
    background: var(--navbar-hover-bg, rgba(0, 0, 0, 0.06));
    color: var(--navbar-text-hover, var(--text-primary));
  }

  &.disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  :deep(svg) {
    flex-shrink: 0;
  }
}
</style>
