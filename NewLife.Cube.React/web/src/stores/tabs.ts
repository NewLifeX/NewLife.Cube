/**
 * 多标签 Store（对齐 Vue 皮肤 stores/tabs.ts）
 */
import { create } from 'zustand';

export interface TabItem {
  /** 路由路径 */
  path: string;
  /** 标题 */
  title: string;
  /** 是否可关闭 */
  closable: boolean;
  /** 是否固定（首页） */
  fixed?: boolean;
}

interface TabsState {
  tabs: TabItem[];
  activePath: string;
  /** 添加标签（同路径去重） */
  addTab: (tab: TabItem) => void;
  /** 移除标签 */
  removeTab: (path: string) => TabItem | undefined;
  /** 设置激活 */
  setActive: (path: string) => void;
  /** 关闭其他 */
  closeOthers: (path: string) => void;
  /** 关闭全部（保留固定标签） */
  closeAll: () => void;
  /** 重置 */
  reset: () => void;
}

export const useTabsStore = create<TabsState>((set, get) => ({
  tabs: [],
  activePath: '',

  addTab: (tab) => {
    const { tabs } = get();
    const idx = tabs.findIndex((t) => t.path === tab.path);
    if (idx >= 0) {
      // 已存在：同路径去重；菜单异步加载后标题变化时更新（如动态页从"默认页面"补全为菜单名）
      const cur = tabs[idx];
      if (tab.title && tab.title !== cur.title) {
        set({
          tabs: tabs.map((t, i) => (i === idx ? { ...t, title: tab.title } : t)),
          activePath: tab.path,
        });
      } else {
        set({ activePath: tab.path });
      }
      return;
    }
    set({ tabs: [...tabs, tab], activePath: tab.path });
  },

  removeTab: (path) => {
    const { tabs, activePath } = get();
    const idx = tabs.findIndex((t) => t.path === path);
    if (idx < 0) return undefined;
    const removed = tabs[idx];
    const next = tabs.filter((t) => t.path !== path);
    let nextActive = activePath;
    if (activePath === path) {
      // 激活相邻标签
      const neighbor = next[idx - 1] ?? next[idx];
      nextActive = neighbor?.path ?? '';
    }
    set({ tabs: next, activePath: nextActive });
    return removed;
  },

  setActive: (path) => set({ activePath: path }),

  closeOthers: (path) => {
    const { tabs } = get();
    set({ tabs: tabs.filter((t) => t.path === path || t.fixed), activePath: path });
  },

  closeAll: () => {
    const { tabs } = get();
    const fixed = tabs.filter((t) => t.fixed);
    set({ tabs: fixed, activePath: fixed[fixed.length - 1]?.path ?? '' });
  },

  reset: () => set({ tabs: [], activePath: '' }),
}));

export default useTabsStore;
