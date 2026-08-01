<script setup lang="ts">
/**
 * UserAvatar - 用户头像和名称组件
 * 显示用户头像（首字母）和名称
 */
import { computed } from 'vue';
import { useUserStore } from '@newlifex/cube-vue/core/stores/user';

interface Props {
  /** 是否显示用户名 */
  showName?: boolean;
  /** 头像大小: small(28px) | medium(32px) | large(40px) */
  size?: 'small' | 'medium' | 'large';
  /** 主题变体: default | green (for TopMenu) */
  variant?: 'default' | 'green';
  /** 点击回调（可选） */
  onClick?: () => void;
}

const props = withDefaults(defineProps<Props>(), {
  showName: true,
  size: 'medium',
  variant: 'default',
});

const userStore = useUserStore();

const currentUser = computed(() => userStore.userInfo);
const userInitial = computed(() =>
  (currentUser.value?.displayName || currentUser.value?.name || 'U').charAt(0).toUpperCase()
);
const userName = computed(() => currentUser.value?.displayName || currentUser.value?.name || '');

const sizeClass = computed(() => `size-${props.size}`);
const variantClass = computed(() => `variant-${props.variant}`);

function handleClick() {
  if (props.onClick) {
    props.onClick();
  }
}
</script>

<template>
  <div class="user-avatar-wrapper" :class="variantClass" @click="handleClick">
    <div class="user-avatar" :class="sizeClass">
      {{ userInitial }}
    </div>
    <span v-if="showName" class="user-name">{{ userName }}</span>
  </div>
</template>

<style lang="scss" scoped>
.user-avatar-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;

  // Default variant (blue theme)
  &.variant-default .user-avatar {
    border-radius: 10px;
    background: linear-gradient(
      135deg,
      var(--el-color-primary-light-9) 0%,
      var(--el-color-primary-light-7) 100%
    );
    color: var(--el-color-primary);
  }

  // Green variant (for TopMenu)
  &.variant-green .user-avatar {
    border-radius: 50%;
    background: linear-gradient(
      135deg,
      var(--el-color-primary) 0%,
      var(--el-color-primary-light-5) 100%
    );
    color: var(--el-color-white);
  }
}

.user-avatar {
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  &.size-small {
    width: 28px;
    height: 28px;
    font-size: 11px;
  }

  &.size-medium {
    width: 32px;
    height: 32px;
    font-size: 13px;
  }

  &.size-large {
    width: 40px;
    height: 40px;
    font-size: 15px;
  }
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--el-text-color-regular);
  white-space: nowrap;
}

.variant-green .user-name {
  color: var(--el-text-color-primary);
}
</style>