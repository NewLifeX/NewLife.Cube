import { defineStore } from 'pinia';
import type { LoginConfig } from '@newlifex/api-core';
import cubeApi from '@/api';
import { emptyAiRuntimeContext, type AiRuntimeContext } from '@/core/utils/aiChatContext';
import { DEFAULT_AI_CONFIG, parseAiConfig, type AiAssistantConfig } from '@/core/utils/aiConfig';
import { DEFAULT_MAP_CONFIG, parseMapConfig, type MapSetting } from '@/core/utils/mapConfig';
import { parseInboxUnreadCount } from '@/core/utils/inboxBadge';

/** 应用级状态（登录配置等）。布局/主题请用 userProfileStore。 */
export const useAppStore = defineStore('app', {
  state: () => ({
    loginConfig: null as LoginConfig | null,
    loginConfigLoaded: false,
    aiConfigLoaded: false,
    mapConfigLoaded: false,
    _loginConfigRequest: null as Promise<void> | null,
    _aiConfigRequest: null as Promise<void> | null,
    _mapConfigRequest: null as Promise<void> | null,
    _inboxRequest: null as Promise<void> | null,
    _workflowRequest: null as Promise<void> | null,
    /** 会话失效（登出/注销）世代：挡住迟到写回 */
    _sessionGeneration: 0,
    /** 各稳定资源 force 世代：作废本资源 in-flight，不误伤其它资源 */
    _loginConfigGen: 0,
    _aiConfigGen: 0,
    _mapConfigGen: 0,
    /** GetAiConfig：FAB/面板开关与配色（消息列表不放 store） */
    aiConfig: { ...DEFAULT_AI_CONFIG } as AiAssistantConfig,
    /** GetMapConfig：地图散点视图服务商/密钥（系统级单服务商；provider=null 未配置） */
    mapConfig: { ...DEFAULT_MAP_CONFIG } as MapSetting,
    /** 当前页 AI 上下文（列表/对象页登记；消息列表不放 store） */
    aiContext: emptyAiRuntimeContext() as AiRuntimeContext,
    /** 外观设置抽屉（不用路由页签） */
    appearanceDrawerVisible: false,
    /** 站内通知抽屉 */
    inboxDrawerVisible: false,
    /** 站内信未读数 */
    inboxUnreadCount: 0,
    /** OA 审批：Meta.enabled（顶栏「审批」槽可见性） */
    workflowEnabled: false,
    /** OA 审批待办角标（Meta.todoCount） */
    workflowTodoCount: 0,
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
    async fetchLoginConfig(force = false) {
      if (!force && this.loginConfigLoaded) return;
      if (!force && this._loginConfigRequest) return this._loginConfigRequest;
      // force：作废本资源 in-flight（不 bump 会话世代，避免误伤角标等其它请求）
      if (force) {
        this._loginConfigGen++;
        this._loginConfigRequest = null;
        this.loginConfigLoaded = false;
      }
      const sessionGen = this._sessionGeneration;
      const resourceGen = this._loginConfigGen;
      const request = cubeApi.user
        .getLoginConfig()
        .then((res) => {
          if (sessionGen !== this._sessionGeneration || resourceGen !== this._loginConfigGen) return;
          if (res.data) this.loginConfig = res.data;
          this.loginConfigLoaded = true;
        })
        .finally(() => {
          if (this._loginConfigRequest === request) this._loginConfigRequest = null;
        });
      this._loginConfigRequest = request;
      return request;
    },
    async fetchAiConfig(force = false) {
      if (!force && this.aiConfigLoaded) return;
      if (!force && this._aiConfigRequest) return this._aiConfigRequest;
      if (force) {
        this._aiConfigGen++;
        this._aiConfigRequest = null;
        this.aiConfigLoaded = false;
      }
      const sessionGen = this._sessionGeneration;
      const resourceGen = this._aiConfigGen;
      const request = cubeApi.config
        .getAiConfig()
        .then((res) => {
          if (sessionGen !== this._sessionGeneration || resourceGen !== this._aiConfigGen) return;
          this.aiConfig = parseAiConfig(res);
          this.aiConfigLoaded = true;
        })
        .catch(() => {
          if (sessionGen !== this._sessionGeneration || resourceGen !== this._aiConfigGen) return;
          this.aiConfig = { ...DEFAULT_AI_CONFIG };
        })
        .finally(() => {
          if (this._aiConfigRequest === request) this._aiConfigRequest = null;
        });
      this._aiConfigRequest = request;
      return request;
    },
    async fetchMapConfig(force = false) {
      if (!force && this.mapConfigLoaded) return;
      if (!force && this._mapConfigRequest) return this._mapConfigRequest;
      if (force) {
        this._mapConfigGen++;
        this._mapConfigRequest = null;
        this.mapConfigLoaded = false;
      }
      const sessionGen = this._sessionGeneration;
      const resourceGen = this._mapConfigGen;
      const request = cubeApi.config
        .getMapConfig()
        .then((res) => {
          if (sessionGen !== this._sessionGeneration || resourceGen !== this._mapConfigGen) return;
          this.mapConfig = parseMapConfig(res);
          this.mapConfigLoaded = true;
        })
        .catch(() => {
          if (sessionGen !== this._sessionGeneration || resourceGen !== this._mapConfigGen) return;
          this.mapConfig = { ...DEFAULT_MAP_CONFIG };
        })
        .finally(() => {
          if (this._mapConfigRequest === request) this._mapConfigRequest = null;
        });
      this._mapConfigRequest = request;
      return request;
    },
    clearSessionMetadata() {
      this._sessionGeneration++;
      this._loginConfigGen++;
      this._aiConfigGen++;
      this._mapConfigGen++;
      this.loginConfig = null;
      this.loginConfigLoaded = false;
      this.aiConfig = { ...DEFAULT_AI_CONFIG };
      this.aiConfigLoaded = false;
      this.mapConfig = { ...DEFAULT_MAP_CONFIG };
      this.mapConfigLoaded = false;
      this._loginConfigRequest = null;
      this._aiConfigRequest = null;
      this._mapConfigRequest = null;
      this._inboxRequest = null;
      this._workflowRequest = null;
      this.inboxUnreadCount = 0;
      this.workflowEnabled = false;
      this.workflowTodoCount = 0;
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
      if (this._inboxRequest) return this._inboxRequest;
      const generation = this._sessionGeneration;
      const request = cubeApi.automation
        .inboxUnreadCount()
        .then((res) => {
          if (generation !== this._sessionGeneration) return;
          this.inboxUnreadCount = parseInboxUnreadCount(res.data);
        })
        .catch(() => {
          if (generation !== this._sessionGeneration) return;
          this.inboxUnreadCount = 0;
        })
        .finally(() => {
          if (this._inboxRequest === request) this._inboxRequest = null;
        });
      this._inboxRequest = request;
      return request;
    },
    /** 刷新 OA 审批 Meta（enabled + todoCount），供顶栏待办槽（IA §1） */
    async refreshWorkflowMeta() {
      if (this._workflowRequest) return this._workflowRequest;
      const generation = this._sessionGeneration;
      const request = cubeApi.workflow
        .meta()
        .then((res) => {
          if (generation !== this._sessionGeneration) return;
          const data = res.data as { enabled?: boolean; todoCount?: number } | undefined;
          this.workflowEnabled = data?.enabled === true;
          const n = Number(data?.todoCount);
          this.workflowTodoCount = Number.isFinite(n) && n > 0 ? Math.floor(n) : 0;
        })
        .catch(() => {
          if (generation !== this._sessionGeneration) return;
          this.workflowEnabled = false;
          this.workflowTodoCount = 0;
        })
        .finally(() => {
          if (this._workflowRequest === request) this._workflowRequest = null;
        });
      this._workflowRequest = request;
      return request;
    },
  },
});
