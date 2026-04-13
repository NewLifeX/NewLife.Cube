import { defineStore } from 'pinia';
import type { SiteInfo } from '@cube/api-core';
import cubeApi from '@/api';

export const useAppStore = defineStore('app', {
  state: () => ({
    collapsed: false,
    darkMode: false,
    siteInfo: null as SiteInfo | null,
  }),
  actions: {
    toggleCollapsed() {
      this.collapsed = !this.collapsed;
    },
    toggleDarkMode() {
      this.darkMode = !this.darkMode;
    },
    async fetchSiteInfo() {
      const res = await cubeApi.config.getSiteInfo();
      if (res.data) this.siteInfo = res.data;
    },
  },
});
