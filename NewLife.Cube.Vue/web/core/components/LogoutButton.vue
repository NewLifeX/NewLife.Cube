<script setup lang="ts">
/**
 * LogoutButton - 退出登录按钮组件
 * 使用 ActionButton 基类样式
 */
import { useUserStore } from '@newlifex/cube-vue/core/stores/user';
import ActionButton from './ActionButton.vue';

interface Props {
  /** 是否显示文字标签 */
  showLabel?: boolean;
  /** 按钮文本 */
  label?: string;
  /** 点击回调（可选，传入后优先调用回调，不执行默认退出逻辑） */
  onClick?: () => void;
}

const props = withDefaults(defineProps<Props>(), {
  showLabel: false,
  label: '退出',
});

const userStore = useUserStore();

function handleLogout() {
  if (props.onClick) {
    props.onClick();
  } else {
    userStore.logout();
  }
}
</script>

<template>
  <ActionButton title="退出登录" @click="handleLogout">
    <svg
      width="15"
      height="15"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
      <polyline points="16 17 21 12 16 7" />
      <line x1="21" y1="12" x2="9" y2="12" />
    </svg>
    <span v-if="showLabel" class="logout-label">{{ label }}</span>
  </ActionButton>
</template>

<style lang="scss" scoped>
.logout-label {
  margin-left: 6px;
  font-size: 13px;
  white-space: nowrap;
}
</style>