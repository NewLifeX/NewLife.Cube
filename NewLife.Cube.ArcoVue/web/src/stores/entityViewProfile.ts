import { defineStore } from 'pinia';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import {
  createTableView,
  getActiveView,
  mergeColumns,
  patchActiveChrome,
  patchActiveColumns,
  patchActiveSort,
  rematchStateColumns,
  removeView,
  renameView,
  stateFromWire,
  stateToWirePayload,
  type ColumnPref,
  type EntityViewState,
  type ViewChrome,
  type ViewSort,
} from '@/core/utils/entityViewProfile';

const SAVE_MS = 400;

type CacheEntry = {
  state: EntityViewState;
  metaKeys: string[];
  dirty: boolean;
  timer: ReturnType<typeof setTimeout> | null;
};

export const useEntityViewProfileStore = defineStore('entityViewProfile', {
  state: () => ({
    byType: {} as Record<string, CacheEntry>,
  }),
  getters: {
    getState: (s) => (typePath: string) => s.byType[typePath]?.state ?? null,
    getActive: (s) => (typePath: string) => {
      const st = s.byType[typePath]?.state;
      return st ? getActiveView(st) : null;
    },
  },
  actions: {
    ensureEntry(typePath: string, metaKeys: string[]): CacheEntry {
      if (!this.byType[typePath]) {
        this.byType[typePath] = {
          state: stateFromWire(null, metaKeys),
          metaKeys,
          dirty: false,
          timer: null,
        };
      } else {
        this.byType[typePath].metaKeys = metaKeys;
      }
      return this.byType[typePath];
    },

    async load(typePath: string, metaKeys: string[]) {
      const entry = this.ensureEntry(typePath, metaKeys);
      try {
        const res = await cubeApi.profile.getEntityViewProfile(typePath);
        entry.state = stateFromWire(res.data, metaKeys);
        entry.dirty = false;
      } catch {
        entry.state = stateFromWire(null, metaKeys);
      }
      // 脏数据：views 列为空但元数据已有 → 重合并并写回
      return this.rematch(typePath, metaKeys);
    },

    /** 按最新 GetPage.list 字段重合并列；必要时持久化修复空列 */
    rematch(typePath: string, metaKeys: string[]) {
      const entry = this.ensureEntry(typePath, metaKeys);
      entry.metaKeys = metaKeys;
      const before = JSON.stringify(getActiveView(entry.state).columns);
      entry.state = rematchStateColumns(entry.state, metaKeys);
      // 若活跃视图仍无列且有元数据，强制种子列
      const active = getActiveView(entry.state);
      if (!active.columns.length && metaKeys.length) {
        entry.state = patchActiveColumns(entry.state, mergeColumns(metaKeys, null));
      }
      const after = JSON.stringify(getActiveView(entry.state).columns);
      if (before !== after && getActiveView(entry.state).columns.length) {
        entry.dirty = true;
        this.scheduleSave(typePath, true);
      }
      return entry.state;
    },

    setState(typePath: string, state: EntityViewState, immediate?: boolean) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.state = state;
      entry.dirty = true;
      this.scheduleSave(typePath, immediate);
    },

    updateColumns(typePath: string, columns: ColumnPref[], immediate?: boolean) {
      const entry = this.byType[typePath];
      if (!entry) return;
      this.setState(typePath, patchActiveColumns(entry.state, columns), immediate);
    },

    updateSort(typePath: string, sort: ViewSort | null, immediate?: boolean) {
      const entry = this.byType[typePath];
      if (!entry) return;
      this.setState(typePath, patchActiveSort(entry.state, sort), immediate);
    },

    updateChrome(typePath: string, chrome: ViewChrome, immediate?: boolean) {
      const entry = this.byType[typePath];
      if (!entry) return;
      this.setState(typePath, patchActiveChrome(entry.state, chrome), immediate);
    },

    switchView(typePath: string, viewId: string) {
      const entry = this.byType[typePath];
      if (!entry || !entry.state.views.some((v) => v.id === viewId)) return;
      this.setState(typePath, { ...entry.state, activeViewId: viewId, view: 'table' }, true);
    },

    addView(typePath: string, name: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      try {
        this.setState(typePath, createTableView(entry.state, name, entry.metaKeys), true);
      } catch (e) {
        Message.warning(e instanceof Error ? e.message : '无法创建视图');
      }
    },

    rename(typePath: string, id: string, name: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      try {
        this.setState(typePath, renameView(entry.state, id, name), true);
      } catch (e) {
        Message.warning(e instanceof Error ? e.message : '重命名失败');
      }
    },

    remove(typePath: string, id: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      try {
        this.setState(typePath, removeView(entry.state, id), true);
      } catch (e) {
        Message.warning(e instanceof Error ? e.message : '删除失败');
      }
    },

    async reset(typePath: string, metaKeys: string[]) {
      try {
        await cubeApi.profile.deleteEntityViewProfile(typePath);
      } catch {
        /* ignore */
      }
      const entry = this.ensureEntry(typePath, metaKeys);
      entry.state = stateFromWire(null, metaKeys);
      entry.dirty = false;
    },

    scheduleSave(typePath: string, immediate?: boolean) {
      const entry = this.byType[typePath];
      if (!entry) return;
      if (entry.timer) clearTimeout(entry.timer);
      if (immediate) {
        void this.saveNow(typePath);
        return;
      }
      entry.timer = setTimeout(() => {
        entry.timer = null;
        void this.saveNow(typePath);
      }, SAVE_MS);
    },

    async saveNow(typePath: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      if (entry.timer) {
        clearTimeout(entry.timer);
        entry.timer = null;
      }
      try {
        await cubeApi.profile.putEntityViewProfile(
          stateToWirePayload(typePath, entry.state),
        );
        entry.dirty = false;
      } catch (err) {
        Message.error(formatApiError(err, '保存视图配置失败'));
      }
    },
  },
});
