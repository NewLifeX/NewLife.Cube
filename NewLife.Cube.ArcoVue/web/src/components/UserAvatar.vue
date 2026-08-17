<template>
  <a-avatar :size="size" class="user-avatar">
    <img v-if="avatar" :src="avatar" :alt="name || '头像'" />
    <template v-else>{{ initial }}</template>
  </a-avatar>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { avatarInitial } from '@/core/utils/avatar';
import { useAppStore } from '@/stores/app';

/**
 * 用户头像徽章
 *
 * 有头像时展示图片；无头像时回落为用户名缩写（字数取自魔方设置 AvatarChars）。
 */
const props = withDefaults(
  defineProps<{
    /** 用户名 / 显示名 */
    name?: string;
    /** 头像地址；为空时回落为用户名首字符 */
    avatar?: string;
    /** 头像尺寸（px） */
    size?: number;
  }>(),
  { name: '', avatar: '', size: 32 },
);

const appStore = useAppStore();
const initial = computed(() =>
  avatarInitial(props.name, appStore.loginConfig?.avatarChars ?? 1),
);
</script>

<style scoped>
.user-avatar :deep(img) {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
