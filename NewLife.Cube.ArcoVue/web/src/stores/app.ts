import { defineStore } from 'pinia';
import type { LoginConfig } from '@cube/api-core';
import cubeApi from '@/api';

/** 应用级状态（登录配置等）。布局/主题请用 userProfileStore。 */
export const useAppStore = defineStore('app', {
  state: () => ({
    loginConfig: null as LoginConfig | null,
    /** 外观设置抽屉（不用路由页签） */
    appearanceDrawerVisible: false,
    /** 站内通知抽屉 */
    inboxDrawerVisible: false,
    /** 站内信未读数 */
    inboxUnreadCount: 0,
    /** 浏览器标题页名前段覆盖（配置中心左侧选中项等） */
    shellPageTitle: null as string | null,
    /** 浏览器标题「显示名称」覆盖（系统设置表单 DisplayName 实时值） */
    shellSysTitle: null as string | null,
  }),
  actions: {
    setDocumentTitleParts(page: string | null, sys?: string | null) {
      this.shellPageTitle = page;
      if (sys !== undefined) this.shellSysTitle = sys;
    },
    clearDocumentTitleParts() {
      this.shellPageTitle = null;
      this.shellSysTitle = null;
    },
    async fetchLoginConfig() {
      const res = await cubeApi.user.getLoginConfig();
      if (res.data) this.loginConfig = res.data;
    },
    openAppearanceDrawer() {
      this.appearanceDrawerVisible = true;
    },
    closeAppearanceDrawer() {
      this.appearanceDrawerVisible = false;
    },
    openInboxDrawer() {
      this.inboxDrawerVisible = true;
    },
    closeInboxDrawer() {
      this.inboxDrawerVisible = false;
    },
    async refreshInboxUnread() {
      try {
        const res = await cubeApi.automation.inboxUnreadCount();
        this.inboxUnreadCount = Number(res.data?.count ?? 0);
      } catch {
        this.inboxUnreadCount = 0;
      }
    },
  },
});
