import { defineStore } from 'pinia';
import type { RouteLocationNormalizedLoaded } from 'vue-router';

export interface TagViewItem {
  path: string;
  fullPath: string;
  title: string;
  /** keep-alive include 用的组件名（与路由 name 对齐） */
  name: string;
}

function routeCacheName(route: RouteLocationNormalizedLoaded): string {
  const n = route.name;
  if (typeof n === 'string' && n) return n;
  return 'route-' + route.path.replace(/\//g, '-');
}

export const useTagsViewStore = defineStore('tagsView', {
  state: () => ({
    visited: [] as TagViewItem[],
    cached: [] as string[],
  }),
  actions: {
    addView(route: RouteLocationNormalizedLoaded) {
      if (route.meta?.public) return;
      const path = route.path;
      if (!path || path === '/login') return;
      const name = routeCacheName(route);
      const title = (route.meta?.title as string) || name;
      if (!this.visited.some((v) => v.path === path)) {
        this.visited.push({ path, fullPath: route.fullPath, title, name });
      }
      if (name && !this.cached.includes(name)) {
        this.cached.push(name);
      }
    },

    /** 关闭页签并从 keep-alive 缓存剔除 */
    removeView(path: string): TagViewItem | null {
      const idx = this.visited.findIndex((v) => v.path === path);
      if (idx < 0) return null;
      const [removed] = this.visited.splice(idx, 1);
      this.cached = this.cached.filter((n) => n !== removed.name);
      const next = this.visited[idx] || this.visited[idx - 1] || null;
      return next;
    },

    clearAll() {
      this.visited = [];
      this.cached = [];
    },
  },
});
