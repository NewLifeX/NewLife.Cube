import { defineStore } from 'pinia';
import type { LoginConfig } from '@cube/api-core';
import cubeApi from '@/api';
import { emptyAiRuntimeContext, type AiRuntimeContext } from '@/core/utils/aiChatContext';
import { DEFAULT_AI_CONFIG, parseAiConfig, type AiAssistantConfig } from '@/core/utils/aiConfig';
import { parseInboxUnreadCount } from '@/core/utils/inboxBadge';

/** 应用级状态（登录配置等）。布局/主题请用 userProfileStore。 */
export const useAppStore = defineStore('app', {
  state: () => ({
    loginConfig: null as LoginConfig | null,
    /** GetAiConfig：FAB/面板开关与配色（消息列表不放 store） */
    aiConfig: { ...DEFAULT_AI_CONFIG } as AiAssistantConfig,
    /** 当前页 AI 上下文（列表/对象页登记；消息列表不放 store） */
    aiContext: emptyAiRuntimeContext() as AiRuntimeContext,
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
    async fetchAiConfig() {
      try {
        const res = await cubeApi.config.getAiConfig();
        this.aiConfig = parseAiConfig(res);
      } catch {
        this.aiConfig = { ...DEFAULT_AI_CONFIG };
      }
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
    patchAiContext(partial: Partial<AiRuntimeContext>) {
      this.aiContext = { ...this.aiContext, ...partial };
    },
    clearAiPageContext() {
      this.aiContext = emptyAiRuntimeContext();
    },
    async refreshInboxUnread() {
      try {
        const res = await cubeApi.automation.inboxUnreadCount();
        this.inboxUnreadCount = parseInboxUnreadCount(res.data);
      } catch {
        this.inboxUnreadCount = 0;
      }
    },
  },
});
