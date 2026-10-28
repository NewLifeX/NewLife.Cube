import { defineStore } from 'pinia';
import type { SiteConfig } from '@newlifex/cube';
import cubeApi from '@/api';

export const useAppStore = defineStore('app', {
  state: () => ({
    collapsed: false,
    darkMode: false,
    siteConfig: null as SiteConfig | null,
  }),
  actions: {
    toggleCollapsed() {
      this.collapsed = !this.collapsed;
    },
    toggleDarkMode() {
      this.darkMode = !this.darkMode;
      if (this.darkMode) {
        document.body.setAttribute('arco-theme', 'dark');
      } else {
        document.body.removeAttribute('arco-theme');
      }
    },
    async fetchSiteConfig() {
      const res = await cubeApi.site.getSite();
      if (res.data) this.siteConfig = res.data;
    },
  },
});
