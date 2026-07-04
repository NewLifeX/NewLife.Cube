import { defineStore } from 'pinia';
import { type RouteLocationNormalized } from 'vue-router';

/**
 * 标签页数据结构
 */
export interface TabItem {
  /** 唯一标识（由 path + query hash 生成） */
  id: string;
  /** 路由路径（不含 query） */
  path: string;
  /** 完整路径（含 query 和 hash） */
  fullPath: string;
  /** 标签标题 */
  title: string;
  /** 图标 */
  icon?: string;
  /** 路由 query 参数（支持主从过滤等场景） */
  query?: Record<string, unknown>;
  /** 是否可关闭（首页标签不可关闭） */
  closable: boolean;
}

const STORAGE_KEY = 'cube-tabs';

function buildTabId(route: RouteLocationNormalized): string {
  const queryStr = JSON.stringify(route.query || {});
  return route.path + (queryStr !== '{}' ? `?${queryStr}` : '');
}

/**
 * 从 sessionStorage 恢复标签列表
 */
function restoreTabs(): TabItem[] {
  try {
    const stored = sessionStorage.getItem(STORAGE_KEY);
    if (stored) {
      return JSON.parse(stored) as TabItem[];
    }
  } catch {
    // ignore
  }
  return [];
}

/**
 * 保存标签列表到 sessionStorage
 */
function saveTabs(tabs: TabItem[]) {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(tabs));
  } catch {
    // ignore
  }
}

export const useTabsStore = defineStore('tabs', {
  state: () => ({
    tabs: restoreTabs(),
    activeTabId: '' as string,
  }),

  getters: {
    activeTab: (state) => state.tabs.find((t) => t.id === state.activeTabId),
    /** 首页标签（第一个或固定 '/' 的标签） */
    homeTab: (state) => state.tabs.find((t) => t.path === '/' || t.path === '/home'),
    /** 标签数量（用于 badge 显示） */
    tabCount: (state) => state.tabs.length,
  },

  actions: {
    /**
     * 根据路由对象添加标签
     * 如果已存在则激活，不存在则追加
     */
    addTab(route: RouteLocationNormalized) {
      const id = buildTabId(route);
      const existing = this.tabs.find((t) => t.id === id);

      if (existing) {
        // 更新标题（可能因语言切换等原因变化）
        existing.title = (route.meta?.title as string) || route.name?.toString() || route.path;
        existing.fullPath = route.fullPath;
        existing.query = route.query as Record<string, unknown>;
        this.activeTabId = id;
        saveTabs(this.tabs);
        return;
      }

      // 从路由 meta 获取标题和图标
      const title =
        (route.meta?.title as string) || route.name?.toString() || route.path;
      const icon = route.meta?.icon as string | undefined;
      const isHome = route.path === '/' || route.path === '/home';

      const tab: TabItem = {
        id,
        path: route.path,
        fullPath: route.fullPath,
        title,
        icon,
        query: route.query as Record<string, unknown>,
        closable: !isHome,
      };

      this.tabs.push(tab);
      this.activeTabId = id;
      saveTabs(this.tabs);
    },

    /**
     * 关闭指定标签，返回应该激活的标签 id
     */
    closeTab(tabId: string): string | null {
      const idx = this.tabs.findIndex((t) => t.id === tabId);
      if (idx === -1) return null;

      const tab = this.tabs[idx];
      if (!tab.closable) return null;

      this.tabs.splice(idx, 1);

      // 如果关闭的是激活标签，切换到相邻标签
      if (this.activeTabId === tabId) {
        const next = this.tabs[Math.min(idx, this.tabs.length - 1)];
        this.activeTabId = next?.id || '';
        saveTabs(this.tabs);
        return this.activeTabId || null;
      }

      saveTabs(this.tabs);
      return null;
    },

    /**
     * 关闭其他标签，只保留当前和首页
     */
    closeOthers(tabId: string) {
      const home = this.tabs.find((t) => !t.closable);
      this.tabs = this.tabs.filter(
        (t) => !t.closable || t.id === tabId,
      );
      if (!this.tabs.some((t) => t.id === tabId) && tabId) {
        // 被关闭了，激活首页
        this.activeTabId = home?.id || this.tabs[0]?.id || '';
      } else {
        this.activeTabId = tabId;
      }
      saveTabs(this.tabs);
    },

    /**
     * 关闭全部标签（保留首页）
     */
    closeAll() {
      const home = this.tabs.find((t) => !t.closable);
      this.tabs = home ? [home] : [];
      this.activeTabId = home?.id || '';
      saveTabs(this.tabs);
    },

    /**
     * 设置激活标签
     */
    setActive(tabId: string) {
      if (this.tabs.some((t) => t.id === tabId)) {
        this.activeTabId = tabId;
        saveTabs(this.tabs);
      }
    },

    /**
     * 根据 fullPath 查找标签
     */
    findByFullPath(fullPath: string): TabItem | undefined {
      return this.tabs.find((t) => t.fullPath === fullPath);
    },
  },
});
