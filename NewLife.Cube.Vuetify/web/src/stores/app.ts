import { defineStore } from 'pinia';
import type { LoginConfig } from '@cube/api-core';
import cubeApi from '@/api';

export const useAppStore = defineStore('app', {
  state: () => ({
    collapsed: false,
    darkMode: false,
    loginConfig: null as LoginConfig | null,
  }),
  actions: {
    toggleCollapsed() {
      this.collapsed = !this.collapsed;
    },
    toggleDarkMode() {
      this.darkMode = !this.darkMode;
    },
    async fetchLoginConfig() {
      const res = await cubeApi.user.getLoginConfig();
      if (res.data) this.loginConfig = res.data;
    },
  },
});
