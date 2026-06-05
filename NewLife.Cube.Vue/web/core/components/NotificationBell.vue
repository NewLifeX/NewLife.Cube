<script setup lang="ts">
/**
 * NotificationBell - 通知按钮组件
 * 铃铛图标，点击可展开通知面板
 * 使用 ActionButton 基类样式
 */
import ActionButton from './ActionButton.vue';

interface Props {
  /** 是否显示文字标签 */
  showLabel?: boolean;
  /** 按钮文本 */
  label?: string;
  /** 自定义点击回调，不传则使用默认逻辑 */
  onClick?: () => void;
}

const props = withDefaults(defineProps<Props>(), {
  showLabel: false,
  label: '通知',
});

const emit = defineEmits<{
  click: [];
}>();

function handleClick() {
  if (props.onClick) {
    props.onClick();
  } else {
    emit('click');
  }
}
</script>

<template>
  <ActionButton title="通知" @click="handleClick">
    <svg
      width="15"
      height="15"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
    >
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9" />
      <path d="M13.73 21a2 2 0 01-3.46 0" />
    </svg>
    <span v-if="showLabel" class="bell-label">{{ label }}</span>
  </ActionButton>
</template>

<style lang="scss" scoped>
.bell-label {
  margin-left: 6px;
  font-size: 13px;
  white-space: nowrap;
}
</style>