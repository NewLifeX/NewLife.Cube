import { defineStore } from 'pinia';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import type { FieldMeta } from '@/core/types/field';
import {
  createNamedView,
  createTableView,
  duplicateView,
  getActiveView,
  mergeColumns,
  patchActiveChrome,
  patchActiveColumns,
  patchActiveMapping,
  patchActiveSort,
  rematchStateColumns,
  rematchStateMappings,
  removeView,
  renameView,
  stateFromWire,
  stateToWirePayload,
  type ColumnPref,
  type EntityViewState,
  type ViewChrome,
  type ViewKind,
  type ViewMapping,
  type ViewSeedOptions,
  type ViewSort,
} from '@/core/utils/viewProfile';
import { canCreateViewKind } from '@/core/utils/viewMapping';

const SAVE_MS = 400;

function cloneState(state: EntityViewState): EntityViewState {
  return JSON.parse(JSON.stringify(state)) as EntityViewState;
}

type CacheEntry = {
  state: EntityViewState;
  committedState: EntityViewState;
  metaKeys: string[];
  fields: FieldMeta[];
  dirty: boolean;
  timer: ReturnType<typeof setTimeout> | null;
};

export const useViewProfileStore = defineStore('viewProfile', {
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
    ensureEntry(typePath: string, metaKeys: string[], opts?: ViewSeedOptions): CacheEntry {
      if (!this.byType[typePath]) {
        const state = stateFromWire(null, metaKeys, opts);
        this.byType[typePath] = {
          state,
          committedState: cloneState(state),
          metaKeys,
          fields: [],
          dirty: false,
          timer: null,
        };
      } else {
        this.byType[typePath].metaKeys = metaKeys;
      }
      return this.byType[typePath];
    },

    setFields(typePath: string, fields: FieldMeta[]) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.fields = fields;
      entry.state = rematchStateMappings(entry.state, fields);
      if (!entry.dirty) entry.committedState = cloneState(entry.state);
    },

    async load(typePath: string, metaKeys: string[], fields?: FieldMeta[], opts?: ViewSeedOptions) {
      const entry = this.ensureEntry(typePath, metaKeys, opts);
      if (fields) entry.fields = fields;
      try {
        const res = await cubeApi.profile.getViewProfile(typePath);
        entry.state = stateFromWire(res.data, metaKeys, opts);
        entry.dirty = false;
      } catch {
        entry.state = stateFromWire(null, metaKeys, opts);
      }
      if (entry.fields.length) {
        entry.state = rematchStateMappings(entry.state, entry.fields);
      }
      entry.committedState = cloneState(entry.state);
      return this.rematch(typePath, metaKeys);
    },

    /** 按最新 GetPage.list 字段重合并列；必要时持久化修复空列 */
    rematch(typePath: string, metaKeys: string[]) {
      const entry = this.ensureEntry(typePath, metaKeys);
      entry.metaKeys = metaKeys;
      const before = JSON.stringify(getActiveView(entry.state).columns);
      entry.state = rematchStateColumns(entry.state, metaKeys);
      if (entry.fields.length) {
        entry.state = rematchStateMappings(entry.state, entry.fields);
      }
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

    updateMapping(typePath: string, mapping: ViewMapping | undefined, immediate?: boolean) {
      const entry = this.byType[typePath];
      if (!entry) return;
      const active = getActiveView(entry.state);
      const normalized =
        entry.fields.length && mapping
          ? rematchStateMappings(
              {
                ...entry.state,
                views: entry.state.views.map((v) =>
                  v.id === active.id ? { ...v, mapping } : v,
                ),
              },
              entry.fields,
            )
          : patchActiveMapping(entry.state, mapping);
      this.setState(typePath, normalized, immediate);
    },

    switchView(typePath: string, viewId: string) {
      const entry = this.byType[typePath];
      if (!entry || !entry.state.views.some((v) => v.id === viewId)) return;
      const active = entry.state.views.find((v) => v.id === viewId)!;
      this.setState(
        typePath,
        { ...entry.state, activeViewId: viewId, view: active.view },
        true,
      );
    },

    addView(typePath: string, name: string, kind: ViewKind = 'table') {
      const entry = this.byType[typePath];
      if (!entry) return;
      const gate = canCreateViewKind(kind, entry.fields, typePath);
      if (!gate.ok) {
        Message.warning(gate.reason || '无法创建该视图类型');
        return;
      }
      try {
        this.setState(
          typePath,
          createNamedView(entry.state, name, kind, entry.metaKeys, entry.fields),
          true,
        );
      } catch (e) {
        Message.warning(e instanceof Error ? e.message : '无法创建视图');
      }
    },

    /** @deprecated 用 addView(type, name, 'table') */
    addTableView(typePath: string, name: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      try {
        this.setState(typePath, createTableView(entry.state, name, entry.metaKeys), true);
      } catch (e) {
        Message.warning(e instanceof Error ? e.message : '无法创建视图');
      }
    },

    duplicate(typePath: string, id: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      try {
        this.setState(typePath, duplicateView(entry.state, id), true);
      } catch (e) {
        Message.warning(e instanceof Error ? e.message : '复制失败');
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

    async reset(typePath: string, metaKeys: string[], opts?: ViewSeedOptions) {
      try {
        await cubeApi.profile.deleteViewProfile(typePath);
      } catch {
        /* ignore */
      }
      const entry = this.ensureEntry(typePath, metaKeys, opts);
      entry.state = stateFromWire(null, metaKeys, opts);
      if (entry.fields.length) {
        entry.state = rematchStateMappings(entry.state, entry.fields);
      }
      entry.committedState = cloneState(entry.state);
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
      const rollback = cloneState(entry.committedState);
      try {
        await cubeApi.profile.putViewProfile(
          stateToWirePayload(typePath, entry.state),
        );
        entry.dirty = false;
        entry.committedState = cloneState(entry.state);
      } catch (err) {
        entry.state = rollback;
        entry.dirty = false;
        Message.error(formatApiError(err, '保存视图配置失败，已恢复上次配置'));
      }
    },
  },
});
