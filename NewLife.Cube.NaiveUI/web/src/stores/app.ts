import { defineStore } from 'pinia';

export const useAppStore = defineStore('app', {
  state: () => ({
    /** 暗黑模式 */
    darkMode: false,
    /** 侧边栏折叠 */
    collapsed: false,
    /** 站点名称 */
    siteName: '魔方管理平台',
    /** Logo URL */
    logo: '',
  }),
  actions: {
    toggleDark() {
      this.darkMode = !this.darkMode;
    },
    toggleCollapse() {
      this.collapsed = !this.collapsed;
    },
    setSiteInfo(info: { displayName?: string; logo?: string }) {
      if (info.displayName) this.siteName = info.displayName;
      if (info.logo) this.logo = info.logo;
    },
  },
});
