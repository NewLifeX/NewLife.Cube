import { defineStore } from 'pinia';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { useUserStore } from './user';
import type { FieldMeta } from '@/core/types/field';
import {
  clearFormModeLayout,
  clearSavedViewFilters,
  createNamedView,
  createTableView,
  duplicateView,
  emptyFormJson,
  emptySavedFilters,
  emptySavedQueries,
  emptyViewFilter,
  generateQueryId,
  getActiveView,
  hasFiltersDomain,
  hasViewsDomain,
  getFormModeLayout,
  mergeColumns,
  parseFormJson,
  parseQueriesWire,
  parseSavedFilters,
  patchActiveChrome,
  patchActiveColumns,
  patchActiveFilter,
  patchActiveGroup,
  patchActiveInsight,
  patchActiveMapping,
  patchActiveSort,
  rematchStateColumns,
  rematchStateMappings,
  removeView,
  renameView,
  restoreNamedView,
  serializeFormJson,
  serializeNamedView,
  serializeQueriesWire,
  serializeSavedFilters,
  setFormModeLayout,
  setSavedViewFilters,
  stateFromWire,
  stateToWirePayload,
  type ColumnPref,
  type EntityViewState,
  type FilterDomainSource,
  type FormJsonWire,
  type FormLayout,
  type FormMode,
  type SavedFiltersWire,
  type SavedQueriesWire,
  type ViewChrome,
  type ViewDomainSource,
  type ViewFilter,
  type ViewGroup,
  type ViewInsight,
  type ViewKind,
  type ViewMapping,
  type ViewSeedOptions,
  type ViewSort,
} from '@/core/utils/viewProfile';
import { canCreateViewKind, normalizePageSize } from '@/core/utils/viewMapping';

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
  /** 已保存筛选（FiltersJson，OSC-0012）：key 为 NamedView.id */
  filters: SavedFiltersWire;
  committedFilters: SavedFiltersWire;
  /** 页面级 PageSize（typePath 级，OSC-0012）：0 表示未配置 */
  pageSize: number;
  committedPageSize: number;
  /** 表单布局（FormJson，OSC-0013）：add/edit/detail 三模式独立 */
  formJson: FormJsonWire;
  committedFormJson: FormJsonWire;
  /** 视图域来源（OSC-0014）：personal / template / system */
  viewsSource: ViewDomainSource;
  /** 筛选域来源（OSC-0014）：personal / template / system */
  filtersSource: FilterDomainSource;
  /** 视图域本会话是否修改（待保存 / 待 materialize 个人副本） */
  viewsDirty: boolean;
  /** 筛选域本会话是否修改 */
  filtersDirty: boolean;
  /** 预定义查询（QueriesJson，OSC-0016）：实体级个人配置，不走模板域 */
  queries: SavedQueriesWire;
  committedQueries: SavedQueriesWire;
  /** 当前应用的预定义查询 id（会话内存，不持久化；刷新后为 null） */
  activeQueryId: string | null;
  /** 个人视图域原始 ViewsJson；null=无个人域（回落模板/系统） */
  personalViewsJson: string | null;
  /** 全局模板视图域原始 ViewsJson；null=无模板 */
  templateViewsJson: string | null;
  /** 全局模板筛选域线缆；null=无模板 */
  templateFilters: SavedFiltersWire | null;
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
    /** 视图域来源（OSC-0014） */
    getViewsSource: (s) => (typePath: string) => s.byType[typePath]?.viewsSource ?? 'system',
    /** 筛选域来源（OSC-0014） */
    getFiltersSource: (s) => (typePath: string) => s.byType[typePath]?.filtersSource ?? 'system',
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
          filters: emptySavedFilters(),
          committedFilters: emptySavedFilters(),
          queries: emptySavedQueries(),
          committedQueries: emptySavedQueries(),
          activeQueryId: null,
          pageSize: 0,
          committedPageSize: 0,
          formJson: emptyFormJson(),
          committedFormJson: emptyFormJson(),
          viewsSource: 'system',
          filtersSource: 'system',
          viewsDirty: false,
          filtersDirty: false,
          personalViewsJson: null,
          templateViewsJson: null,
          templateFilters: null,
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
      // 并行拉个人配置 + 全局模板（视图/筛选域，OSC-0014）；模板 403/失败视为无模板
      const [personalRes, templateRes] = await Promise.allSettled([
        cubeApi.profile.getViewProfile(typePath),
        cubeApi.profile.getViewProfileTemplate(typePath),
      ]);
      const personal = personalRes.status === 'fulfilled' ? personalRes.value?.data : null;
      const template = templateRes.status === 'fulfilled' ? templateRes.value?.data : null;

      const personalViewsJson = personal?.viewsJson ?? null;
      const templateViewsJson = template?.viewsJson ?? null;
      const personalFiltersJson = personal?.filtersJson ?? null;
      const templateFiltersJson = template?.filtersJson ?? null;

      // 视图域：个人 present > 模板 present > 系统默认
      const hasPersonalViews = hasViewsDomain(personalViewsJson);
      const hasTemplateViews = hasViewsDomain(templateViewsJson);
      entry.viewsSource = hasPersonalViews
        ? 'personal'
        : hasTemplateViews
          ? 'template'
          : 'system';
      const stateModel =
        hasPersonalViews
          ? personal
          : hasTemplateViews
            ? { typePath, viewsJson: templateViewsJson, activeViewId: template?.activeViewId }
            : null;
      entry.state = stateFromWire(stateModel, metaKeys, opts);
      entry.personalViewsJson = hasPersonalViews ? personalViewsJson : null;
      entry.templateViewsJson = hasTemplateViews ? templateViewsJson : null;

      // 筛选域：个人 present > 模板 present > 空
      const hasPersonalFilters = hasFiltersDomain(personalFiltersJson);
      const hasTemplateFilters = hasFiltersDomain(templateFiltersJson);
      entry.filtersSource = hasPersonalFilters
        ? 'personal'
        : hasTemplateFilters
          ? 'template'
          : 'system';
      entry.filters = hasPersonalFilters
        ? parseSavedFilters(personalFiltersJson)
        : hasTemplateFilters
          ? parseSavedFilters(templateFiltersJson)
          : emptySavedFilters();
      entry.templateFilters = hasTemplateFilters ? parseSavedFilters(templateFiltersJson) : null;

      entry.pageSize = normalizePageSize(personal?.pageSize);
      entry.formJson = parseFormJson(personal?.formJson);
      // 预定义查询为实体级个人配置（OSC-0016）：仅个人域，不走模板回退；activeQueryId 会话态不持久化
      entry.queries = parseQueriesWire(personal?.queriesJson ?? null, entry.fields);
      entry.activeQueryId = null;
      entry.viewsDirty = false;
      entry.filtersDirty = false;
      entry.dirty = false;

      if (entry.fields.length) {
        entry.state = rematchStateMappings(entry.state, entry.fields);
      }
      entry.committedState = cloneState(entry.state);
      entry.committedFilters = entry.filters;
      entry.committedQueries = entry.queries;
      entry.committedPageSize = entry.pageSize;
      entry.committedFormJson = entry.formJson;
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
      // 视图域有修改：非 personal 来源将在保存时 materialize 个人副本（OSC-0014）
      entry.viewsDirty = true;
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

    /** 更新当前命名视图的受限洞察配置（OSC-0012：统计/图表双开关） */
    updateInsight(typePath: string, insight: ViewInsight, immediate?: boolean) {
      const entry = this.byType[typePath];
      if (!entry) return;
      this.setState(typePath, patchActiveInsight(entry.state, insight), immediate);
    },

    /** 更新当前命名视图的筛选构建器方案（OSC-0015）；空方案等价清除 */
    updateFilter(typePath: string, filter: ViewFilter, immediate = true) {
      const entry = this.byType[typePath];
      if (!entry) return;
      this.setState(typePath, patchActiveFilter(entry.state, filter), immediate);
    },

    /** 读取当前命名视图的筛选构建器方案（OSC-0015） */
    getFilter(typePath: string): ViewFilter {
      const entry = this.byType[typePath];
      if (!entry) return emptyViewFilter();
      const v = getActiveView(entry.state);
      return v?.filter ?? emptyViewFilter();
    },

    /** 更新当前命名视图的多级分组字段（OSC-0015）；空数组等价清除 */
    updateGroup(typePath: string, group: ViewGroup, immediate = true) {
      const entry = this.byType[typePath];
      if (!entry) return;
      this.setState(typePath, patchActiveGroup(entry.state, group), immediate);
    },

    /** 读取当前命名视图的多级分组字段（OSC-0015） */
    getGroup(typePath: string): ViewGroup {
      const entry = this.byType[typePath];
      if (!entry) return [];
      const v = getActiveView(entry.state);
      return v?.group ?? [];
    },

    /** 读取当前 typePath 的已保存筛选线缆（FiltersJson，OSC-0012） */
    getSavedFilters(typePath: string): SavedFiltersWire {
      return this.byType[typePath]?.filters ?? emptySavedFilters();
    },

    /** 读取指定命名视图的已保存筛选；无则 undefined */
    getViewFilters(
      typePath: string,
      viewId: string,
    ): Record<string, unknown> | undefined {
      const entry = this.byType[typePath];
      if (!entry) return undefined;
      return entry.filters.views[viewId];
    },

    /** 以完整筛选对象保存到当前命名视图（仅显式保存触发），只影响该视图 */
    saveViewFilters(
      typePath: string,
      viewId: string,
      filters: Record<string, unknown>,
      immediate = true,
    ) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.filters = setSavedViewFilters(entry.filters, viewId, filters);
      entry.dirty = true;
      // 筛选域有修改：非 personal 来源将在保存时 materialize 个人副本（OSC-0014）
      entry.filtersDirty = true;
      this.scheduleSave(typePath, immediate);
    },

    /** 清除当前命名视图的已保存筛选（删除该 key），只影响该视图 */
    clearViewFilters(typePath: string, viewId: string, immediate = true) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.filters = clearSavedViewFilters(entry.filters, viewId);
      entry.dirty = true;
      entry.filtersDirty = true;
      this.scheduleSave(typePath, immediate);
    },

    /** 读取当前 typePath 的页面级 PageSize；0 表示未配置（OSC-0012） */
    getPageSize(typePath: string): number {
      return this.byType[typePath]?.pageSize ?? 0;
    },

    /** 保存当前 typePath 的页面级 PageSize（仅接受 PAGE_SIZE_OPTIONS 合法值；非法值归一为 0） */
    setPageSize(typePath: string, size: number, immediate = true) {
      const entry = this.byType[typePath];
      if (!entry) return;
      const next = normalizePageSize(size);
      if (entry.pageSize === next) return;
      entry.pageSize = next;
      entry.dirty = true;
      this.scheduleSave(typePath, immediate);
    },

    /** 读取当前 typePath 的 FormJson 线缆（OSC-0013） */
    getFormJson(typePath: string): FormJsonWire {
      return this.byType[typePath]?.formJson ?? emptyFormJson();
    },

    /** 读取指定模式的表单布局；无则 null */
    getFormModeLayout(typePath: string, mode: FormMode): FormLayout | null {
      const entry = this.byType[typePath];
      if (!entry) return null;
      return getFormModeLayout(entry.formJson, mode);
    },

    /** 以完整布局替换当前模式（只影响该模式，保留另两模式） */
    updateFormLayout(
      typePath: string,
      mode: FormMode,
      layout: FormLayout,
      immediate = true,
    ) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.formJson = setFormModeLayout(entry.formJson, mode, layout);
      entry.dirty = true;
      this.scheduleSave(typePath, immediate);
    },

    /** 恢复当前模式默认布局（删除该模式 key），不影响视图/筛选/PageSize 域 */
    resetFormLayout(typePath: string, mode: FormMode, immediate = true) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.formJson = clearFormModeLayout(entry.formJson, mode);
      entry.dirty = true;
      this.scheduleSave(typePath, immediate);
    },

    /** 整体替换当前 typePath 的 FormJson 线缆（三模式一次性手动提交，OSC-0013）；仅显式保存触发 */
    setFormJson(typePath: string, wire: FormJsonWire, immediate = true) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.formJson = { ...wire };
      entry.dirty = true;
      this.scheduleSave(typePath, immediate);
    },

    /** 读取当前 typePath 的预定义查询列表（QueriesJson，OSC-0016） */
    getQueries(typePath: string): SavedQueriesWire {
      return this.byType[typePath]?.queries ?? emptySavedQueries();
    },

    /** 读取当前应用的预定义查询 id（会话内存） */
    getActiveQueryId(typePath: string): string | null {
      return this.byType[typePath]?.activeQueryId ?? null;
    },

    /** 将当前查询保存为预定义查询：新增条目、activeQueryId 指向新条目并立即保存 */
    saveQueryAs(
      typePath: string,
      name: string,
      params: Record<string, unknown>,
    ): string | null {
      const entry = this.byType[typePath];
      if (!entry) return null;
      const trimmed = name.trim();
      if (!trimmed) return null;
      const id = generateQueryId();
      entry.queries = {
        version: 1,
        queries: [
          ...entry.queries.queries,
          { id, name: trimmed.slice(0, 50), params: { ...params } },
        ],
      };
      entry.activeQueryId = id;
      entry.dirty = true;
      this.scheduleSave(typePath, true);
      return id;
    },

    /** 重命名当前应用的预定义查询（仅作用于 activeQueryId 对应条目） */
    renameQuery(typePath: string, id: string, name: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      const trimmed = name.trim();
      if (!trimmed) return;
      entry.queries = {
        ...entry.queries,
        queries: entry.queries.queries.map((q) =>
          q.id === id ? { ...q, name: trimmed.slice(0, 50) } : q,
        ),
      };
      entry.dirty = true;
      this.scheduleSave(typePath, true);
    },

    /** 删除指定预定义查询；若为当前应用条目则清除应用标记（不清空表单参数） */
    deleteQuery(typePath: string, id: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.queries = {
        ...entry.queries,
        queries: entry.queries.queries.filter((q) => q.id !== id),
      };
      if (entry.activeQueryId === id) entry.activeQueryId = null;
      entry.dirty = true;
      this.scheduleSave(typePath, true);
    },

    /** 应用预定义查询：设置 activeQueryId 并返回该条目 params（供 DefaultList 整体回填后执行） */
    applyQuery(typePath: string, id: string): Record<string, unknown> | null {
      const entry = this.byType[typePath];
      if (!entry) return null;
      const q = entry.queries.queries.find((x) => x.id === id);
      if (!q) return null;
      entry.activeQueryId = id;
      return { ...q.params };
    },

    /** 清除当前应用标记（会话内存，不持久化） */
    clearActiveQuery(typePath: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      entry.activeQueryId = null;
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

    addView(
      typePath: string,
      name: string,
      kind: ViewKind = 'table',
      chromeOverride?: Partial<ViewChrome>,
    ) {
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
          createNamedView(
            entry.state,
            name,
            kind,
            entry.metaKeys,
            entry.fields,
            chromeOverride,
          ),
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

    /** 恢复视图：把指定视图重置为创建时的默认状态（保留 id/名称） */
    restoreView(typePath: string, id: string) {
      const entry = this.byType[typePath];
      if (!entry) return;
      try {
        this.setState(
          typePath,
          restoreNamedView(entry.state, id, entry.metaKeys, entry.fields),
          true,
        );
      } catch (e) {
        Message.warning(e instanceof Error ? e.message : '恢复视图失败');
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
      // 用户级配置（列/视图/筛选/PageSize）已删除；表单布局为系统全局配置（管理员定义），
      // 重新加载以恢复全局布局，而非清空为默认（OSC-0013 全局表单布局）
      await this.load(typePath, metaKeys, undefined, opts);
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
      const rollbackFilters = entry.committedFilters;
      const rollbackQueries = entry.committedQueries;
      const rollbackPageSize = entry.committedPageSize;
      const rollbackFormJson = entry.committedFormJson;
      const rollbackViewsSource = entry.viewsSource;
      const rollbackFiltersSource = entry.filtersSource;
      const payload = stateToWirePayload(typePath, entry.state);
      // 视图域（OSC-0014）：个人域直接覆盖；非 personal 且本会话修改过视图才 materialize
      // 个人副本；否则不携带视图域，避免误创建个人副本
      const carryViews = entry.viewsSource === 'personal' || entry.viewsDirty;
      if (!carryViews) {
        delete payload.viewsJson;
        delete payload.view;
        delete payload.activeViewId;
        delete payload.columnsJson;
      }
      // 筛选域（OSC-0014）：个人域直接覆盖；非 personal 且修改过筛选才 materialize
      const carryFilters = entry.filtersSource === 'personal' || entry.filtersDirty;
      if (carryFilters) {
        payload.filtersJson = serializeSavedFilters(entry.filters);
      }
      payload.pageSize = entry.pageSize || 0;
      // 预定义查询为实体级个人配置，始终随保存提交（OSC-0016）
      payload.queriesJson = serializeQueriesWire(entry.queries);
      // 表单布局为系统全局唯一配置（管理员定义，作用于所有用户）：
      // 仅管理员保存时提交；非管理员不发送，避免把全局布局写回或触发后端 403
      const userStore = useUserStore();
      if (userStore.userInfo?.isSystem === true) {
        payload.formJson = serializeFormJson(entry.formJson);
      }
      try {
        await cubeApi.profile.putViewProfile(payload);
        entry.dirty = false;
        entry.committedState = cloneState(entry.state);
        entry.committedFilters = entry.filters;
        entry.committedQueries = entry.queries;
        entry.committedPageSize = entry.pageSize;
        entry.committedFormJson = entry.formJson;
        // materialize 成功后提升为个人域（OSC-0014）
        if (carryViews) {
          entry.viewsSource = 'personal';
          entry.personalViewsJson = entry.state.views.length
            ? JSON.stringify(entry.state.views.map(serializeNamedView))
            : null;
        }
        if (carryFilters) entry.filtersSource = 'personal';
        entry.viewsDirty = false;
        entry.filtersDirty = false;
      } catch (err) {
        entry.state = rollback;
        entry.filters = rollbackFilters;
        entry.queries = rollbackQueries;
        entry.pageSize = rollbackPageSize;
        entry.formJson = rollbackFormJson;
        entry.viewsSource = rollbackViewsSource;
        entry.filtersSource = rollbackFiltersSource;
        entry.dirty = false;
        Message.error(formatApiError(err, '保存视图配置失败，已恢复上次配置'));
      }
    },

    /** 恢复视图域：删除个人视图域副本，回落模板或系统默认（OSC-0014） */
    async restoreViewDomain(typePath: string) {
      const entry = this.byType[typePath];
      if (!entry || entry.viewsSource !== 'personal') return;
      try {
        await cubeApi.profile.putViewProfile({ typePath, viewsJson: '' });
      } catch (err) {
        Message.error(formatApiError(err, '恢复视图域失败'));
        return;
      }
      entry.personalViewsJson = null;
      entry.viewsSource = entry.templateViewsJson ? 'template' : 'system';
      entry.state = entry.templateViewsJson
        ? stateFromWire(
            { typePath, viewsJson: entry.templateViewsJson },
            entry.metaKeys,
          )
        : stateFromWire(null, entry.metaKeys);
      if (entry.fields.length) entry.state = rematchStateMappings(entry.state, entry.fields);
      entry.committedState = cloneState(entry.state);
      entry.viewsDirty = false;
    },

    /** 恢复筛选域：删除个人筛选副本，回落模板或空（OSC-0014） */
    async restoreFilterDomain(typePath: string) {
      const entry = this.byType[typePath];
      if (!entry || entry.filtersSource !== 'personal') return;
      try {
        await cubeApi.profile.putViewProfile({ typePath, filtersJson: '' });
      } catch (err) {
        Message.error(formatApiError(err, '恢复筛选域失败'));
        return;
      }
      entry.filters = entry.templateFilters ?? emptySavedFilters();
      entry.filtersSource = entry.templateFilters ? 'template' : 'system';
      entry.committedFilters = entry.filters;
      entry.filtersDirty = false;
    },
  },
});
