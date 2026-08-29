<template>
  <div class="shell-brand" :class="{ 'shell-brand--collapsed': collapsed }">
    <img v-if="logoSrc" class="shell-brand__logo" :src="logoSrc" alt="" />
    <span v-if="showName" class="shell-brand__name">{{ nameText }}</span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useAppStore } from '@/stores/app';
import { resolveLoginLogoUrl } from '@/views/login/loginConfig';

const props = withDefaults(
  defineProps<{
    /** 侧栏折叠：有 Logo 时只显示徽标 */
    collapsed?: boolean;
    /** 无 Logo 且折叠时的单字回落 */
    fallbackChar?: string;
  }>(),
  {
    collapsed: false,
    fallbackChar: '魔',
  },
);

const appStore = useAppStore();

const productName = computed(() => appStore.loginConfig?.name || '魔方管理平台');
const logoSrc = computed(() => resolveLoginLogoUrl(appStore.loginConfig));

const showName = computed(() => {
  if (!props.collapsed) return true;
  // 折叠且有 Logo：只显示徽标
  return !logoSrc.value;
});

const nameText = computed(() =>
  props.collapsed && !logoSrc.value ? props.fallbackChar : productName.value,
);
</script>

<style scoped>
.shell-brand {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-width: 0;
  height: 100%;
  color: var(--color-text-1);
  font-weight: 700;
  font-size: 16px;
  line-height: 1.2;
}
.shell-brand--collapsed {
  gap: 0;
}
.shell-brand__logo {
  width: 36px;
  height: 36px;
  object-fit: contain;
  flex-shrink: 0;
  border-radius: 4px;
}
.shell-brand--collapsed .shell-brand__logo {
  width: 28px;
  height: 28px;
}
.shell-brand__name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
