import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { LoginConfig } from '@cube/api-core';

export const useAppStore = defineStore('app', () => {
  const collapsed = ref(false);
  const darkMode = ref(false);
  const loginConfig = ref<LoginConfig | null>(null);

  const siteTitle = computed(() => loginConfig.value?.name || '魔方管理平台');

  function toggleDark() {
    darkMode.value = !darkMode.value;
    document.documentElement.setAttribute('theme-mode', darkMode.value ? 'dark' : '');
  }

  return { collapsed, darkMode, loginConfig, siteTitle, toggleDark };
});
