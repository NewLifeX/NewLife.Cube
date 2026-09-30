import { nextTick } from 'vue';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import {
  buildSortsPayload,
  emptyViewFilter,
  serializeNamedView,
  type ColumnPref,
} from '@/core/utils/viewProfile';
import type {
  ViewChrome,
  ViewFilter,
  ViewFormatRule,
  ViewGroup,
  ViewInsight,
  ViewKind,
  ViewMapping,
  ViewSort,
} from '@/core/utils/viewProfile';
import type { GanttMapping, KanbanMapping } from '@/core/utils/viewMapping';
import { shiftCalendarCursor } from '@/features/views/useCalendarMonth';
import { readQueryEmbed } from '@/core/utils/embedMode';
import type { ListContext } from './listContext';

interface ListViewsDeps {
  loadData: (skipFetch?: boolean) => Promise<void>;
  applySearchToForm: (params: Record<string, unknown>) => void;
  /** 重新应用指定的预定义查询方案（映射 useListQuery.handleApplyQuery），保持实体级预定义方案随视图切换不丢 */
  applySavedQuery: (id: string) => void;
  /** 恢复未命名当前查询（Q/自定义条件未保存为方案）到表单与 viewFilter；无则返回 false（切换视图保持该查询） */
  applyLastQuery: () => boolean;
}

/** 16 进制色转 rgba（设计器透明度百分比 → 0~1） */
export function hexToRgba(hex: string, opacityPct: number): string {
  const h = hex.replace('#', '');
  if (h.length !== 6) return hex;
  const r = parseInt(h.slice(0, 2), 16);
  const g = parseInt(h.slice(2, 4), 16);
  const b = parseInt(h.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${Math.max(0, Math.min(1, opacityPct / 100))})`;
}

/**
 * DefaultList 视图领域（OSC-260813c3e9）：命名视图 / 筛选分组 / 列排序 chrome mapping /
 * 全屏测高 / 甘特缩放。测高 measureTableHeight 与渲染 renderCell 因依赖 ctx 状态且被多域共用，
 * 定义于 listContext.ts（见该文件注释）。
 */
export function useListViews(ctx: ListContext, deps: ListViewsDeps) {
  const {
    typePath,
    listFields,
    viewState,
    preferredDefaultView,
    metaKeys,
    searchTouched,
    baseSearch,
    activeSorts,
    effectivePageSize,
    effectivePageSizePref,
    pagination,
    selectedKeys,
    fullscreen,
    tableResizeObserver,
    ganttZoomLevel,
    calendarCursor,
    calendarMode,
    activeViewKind,
    localFilter,
    localGroup,
    localFormat,
    activeViewId,
    drawerMode,
    tableDataRaw,
    searchForm,
    evpStore,
    measureTableHeight,
  } = ctx;
  const { loadData, applySearchToForm } = deps;

  /** 甘特图缩放等级切换（− 缩小 / + 放大，0~4 夹取） */
  function onGanttZoom(delta: number) {
    ganttZoomLevel.value = Math.min(4, Math.max(0, ganttZoomLevel.value + delta));
  }

  /** 日历导航：按当前模式位移（日 ±1 天 / 周 ±7 天 / 月 ±1 月） */
  function onCalendarShift(delta: number) {
    calendarCursor.value = shiftCalendarCursor(calendarCursor.value, delta, calendarMode.value);
    if (activeViewKind.value === 'calendar') void loadData();
  }

  /** 日历导航：回到今天（日/周/月均定位到今天） */
  function onCalendarToday() {
    calendarCursor.value = new Date();
    if (activeViewKind.value === 'calendar') void loadData();
  }

  /** 日历模式切换（工具栏分段：日 / 周 / 月） */
  function onCalendarModeChange(value: string | number | boolean) {
    if (value !== 'day' && value !== 'week' && value !== 'month') return;
    calendarMode.value = value;
    if (activeViewKind.value === 'calendar') void loadData();
  }

  /** 分组值显示标签：按分组字段 dataSource 枚举翻译（OSC-0015）；无映射回落显示原值 */
  function groupLabelOf(field: string, value: unknown): string | undefined {
    const fm = listFields.value.find((f) => f.name === field);
    if (fm?.dataSource && value != null) return fm.dataSource[String(value)];
    return undefined;
  }

  /** 切换全屏/默认展示；布局变化后延迟多次重测表格高度，确保面板尺寸稳定后再填满 */
  function onToggleFullscreen() {
    fullscreen.value = !fullscreen.value;
    nextTick(measureTableHeight);
    window.setTimeout(measureTableHeight, 200);
    window.setTimeout(measureTableHeight, 600);
  }

  /** Esc 退出全屏 */
  function onKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape' && fullscreen.value) onToggleFullscreen();
  }

  /** scroll 容器尺寸变化（窗口/布局调整/侧栏折叠）时重测 */
  function observeTableHeight() {
    tableResizeObserver.value?.disconnect();
    tableResizeObserver.value = null;
    const scroll = document.querySelector<HTMLElement>('.layout-content__scroll');
    if (!scroll || typeof ResizeObserver === 'undefined') return;
    tableResizeObserver.value = new ResizeObserver(() => nextTick(measureTableHeight));
    tableResizeObserver.value.observe(scroll);
  }

  async function loadProfile() {
    viewState.value = await evpStore.load(
      typePath.value,
      metaKeys.value,
      listFields.value,
      { defaultView: preferredDefaultView.value },
    );
    evpStore.setFields(typePath.value, listFields.value);
    // 再 rematch 一次，确保与最新 listFields 对齐
    if (metaKeys.value.length) {
      viewState.value = evpStore.rematch(typePath.value, metaKeys.value);
    }
    // 分享链接 ?viewId= 打开指定命名视图
    const { viewId } = readQueryEmbed();
    if (viewId && viewState.value?.views?.some((v) => v.id === viewId)) {
      if (viewState.value.activeViewId !== viewId) {
        evpStore.switchView(typePath.value, viewId);
        syncLocalState();
      }
    }
  }

  function applyWorkspacePrefs() {
    // 页面级 PageSize 优先（OSC-0012），未配置才回落旧全局 workspace 种子
    const size = effectivePageSizePref.value;
    if (pagination.pageSize !== size) pagination.pageSize = size;
  }

  function syncLocalState() {
    viewState.value = evpStore.getState(typePath.value);
    // 会话内筛选/分组与 store（含已保存配置）对齐（OSC-0015）
    localFilter.value = evpStore.getFilter(typePath.value);
    localGroup.value = evpStore.getGroup(typePath.value);
    localFormat.value = evpStore.getFormat(typePath.value);
  }

  function onColumnsChange(cols: ColumnPref[]) {
    // 列宽/顺序写入当前命名视图（ViewsJson + columnsJson）
    evpStore.updateColumns(typePath.value, cols);
    syncLocalState();
  }

  function onSortChange(payload: { field: string; desc: boolean } | null) {
    evpStore.updateSort(typePath.value, payload, true);
    syncLocalState();
    pagination.current = 1;
    loadData();
  }

  function onSortsApply(sorts: ViewSort[]) {
    evpStore.updateSorts(typePath.value, sorts, true);
    syncLocalState();
    pagination.current = 1;
    loadData();
  }

  /** 点击排序徽标：清空多列排序并重新查询（与分组/填色徽标清除一致） */
  function onClearSort() {
    evpStore.updateSorts(typePath.value, [], true);
    syncLocalState();
    ctx.activePopover.value = null;
    pagination.current = 1;
    loadData();
    Message.success('已清除排序');
  }

  function onConfigSort(sort: ViewSort | null) {
    onSortChange(sort);
  }

  function onChromeChange(next: ViewChrome) {
    evpStore.updateChrome(typePath.value, next);
    syncLocalState();
  }

  function onMappingChange(mapping: ViewMapping | undefined) {
    evpStore.updateMapping(typePath.value, mapping);
    syncLocalState();
  }

  /** 甘特拖拽表格宽度上报：tableWidth 随 mapping 持久化到 ViewsJson（OSC-0019） */
  function onGanttMappingChange(mapping: GanttMapping) {
    evpStore.updateMapping(typePath.value, mapping);
    syncLocalState();
  }

  /** 看板列折叠写入当前命名视图的 mapping（ViewsJson） */
  function onKanbanMappingChange(mapping: KanbanMapping) {
    evpStore.updateMapping(typePath.value, mapping, true);
    syncLocalState();
  }

  function onInsightChange(insight: ViewInsight) {
    evpStore.updateInsight(typePath.value, insight);
    syncLocalState();
  }

  /** Category 折叠切换：更新当前模式布局的 collapsedCategories（OSC-0013） */
  function onToggleCollapse(category: string) {
    const mode = drawerMode.value;
    const cur = evpStore.getFormModeLayout(typePath.value, mode) ?? {
      order: [],
      hidden: [],
      collapsedCategories: [],
    };
    const set = new Set(cur.collapsedCategories);
    if (set.has(category)) set.delete(category);
    else set.add(category);
    evpStore.updateFormLayout(
      typePath.value,
      mode,
      { ...cur, collapsedCategories: [...set] },
      false,
    );
  }

  function onConfigRename(name: string) {
    if (!viewState.value) return;
    onRenameView(viewState.value.activeViewId, name);
  }

  function onSwitchView(id: string) {
    const wasTouched = searchTouched.value;
    const oldSort = JSON.stringify(buildSortsPayload(activeSorts.value));
    const oldPageSize = effectivePageSize.value;
    evpStore.switchView(typePath.value, id);
    syncLocalState();
    selectedKeys.value = [];
    pagination.current = 1;
    // 实体级预定义查询方案随实体保持（OSC-260830a1b2）：切换视图后仍应用当前勾选的预定义方案（回填 Q+filter），
    // 避免切到其它视图时方案被丢弃
    const activeId = evpStore.getActiveQueryId(typePath.value);
    if (activeId) {
      deps.applySavedQuery(activeId);
      return;
    }
    // 未勾选预定义方案：恢复未命名当前查询（Q/自定义条件未保存为方案），切换视图保持该查询，不被基准条件覆盖
    if (deps.applyLastQuery()) {
      loadData();
      return;
    }
    // 切换视图后重新计算条件来源并回填表单（OSC-0012）
    searchTouched.value = false;
    applySearchToForm(baseSearch.value);
    // 重绘优化：未显式搜索、且新视图加载量与排序与已加载数据一致时，复用数据避免重复请求后端
    const canReuse =
      !wasTouched &&
      effectivePageSize.value === oldPageSize &&
      JSON.stringify(buildSortsPayload(activeSorts.value)) === oldSort &&
      tableDataRaw.value.length > 0;
    loadData(canReuse);
  }

  function onCreateView(kind: ViewKind, name: string) {
    evpStore.addView(typePath.value, name, kind);
    syncLocalState();
    selectedKeys.value = [];
    pagination.current = 1;
    loadData();
  }

  function onRenameView(id: string, name: string) {
    evpStore.rename(typePath.value, id, name);
    syncLocalState();
  }

  function onRemoveView(id: string) {
    evpStore.remove(typePath.value, id);
    syncLocalState();
    selectedKeys.value = [];
    loadData();
  }

  function onDuplicateView(id: string) {
    evpStore.duplicate(typePath.value, id);
    syncLocalState();
    selectedKeys.value = [];
    loadData();
  }

  function onResetViews() {
    // 「恢复默认」= 当前视图恢复到创建时的默认状态（保留视图本身，仅重置配置；不删除用户自定义视图）
    if (!activeViewId.value) return;
    evpStore.restoreView(typePath.value, activeViewId.value);
    syncLocalState();
    selectedKeys.value = [];
    pagination.current = 1;
    // 恢复配置不改变数据源，复用已加载数据（避免重复请求后端）
    loadData(true);
  }

  /** 系统管理员：将当前视图方案发布为全局模板（该实体默认视图；回落用户可见） */
  async function onSaveAsDefault() {
    const st = evpStore.getState(typePath.value);
    if (!st?.views?.length) {
      Message.warning('当前无视图可保存');
      return;
    }
    if (!window.confirm('将当前视图方案保存为该实体默认视图？未个性化用户将默认看到此方案。')) return;
    try {
      await cubeApi.profile.putViewProfileTemplate({
        typePath: typePath.value,
        viewsJson: JSON.stringify(st.views.map(serializeNamedView)),
      });
      Message.success('已保存为默认视图');
      // 重新加载刷新模板来源域（管理员个人视图域不受影响）
      const entry = evpStore.byType[typePath.value];
      if (entry) await evpStore.load(typePath.value, entry.metaKeys);
    } catch (err) {
      Message.error(formatApiError(err, '保存默认视图失败'));
    }
  }

  /** 筛选弹层可见性（互斥：打开筛选关闭分组） */
  function onFilterPopoverVisible(v: boolean) {
    ctx.activePopover.value = v ? 'filter' : null;
  }

  /** 分组弹层可见性（互斥：打开分组关闭筛选） */
  function onGroupPopoverVisible(v: boolean) {
    ctx.activePopover.value = v ? 'group' : null;
  }

  function onSortPopoverVisible(v: boolean) {
    ctx.activePopover.value = v ? 'sort' : null;
  }

  function onFormatPopoverVisible(v: boolean) {
    ctx.activePopover.value = v ? 'format' : null;
  }

  function onFormatChange(format: ViewFormatRule[]) {
    evpStore.updateFormat(typePath.value, format);
    localFormat.value = format;
  }

  function onClearFormat() {
    evpStore.updateFormat(typePath.value, []);
    localFormat.value = [];
    Message.success('已清除填色');
  }

  /** 应用筛选方案：写入 store 持久化（刷新/下次打开保留）；筛选为纯前端过滤，复用已加载数据重过滤 */
  /** 应用筛选构建器条件（OSC-0015）：仅对当前查询生效，持久化到 sessionStorage（未命名查询），
   *  不写入服务端视图 filter——否则未保存为方案的查询条件会残留在服务端，
   *  违背「关闭该多维视图时不持久化非命名查询条件」（OSC-260830a1b2）。 */
  function onFilterApply(filter: ViewFilter) {
    localFilter.value = filter;
    evpStore.persistLastQuery(typePath.value, { q: String(searchForm.Q ?? ''), filter });
    pagination.current = 1;
    loadData(true);
  }

  /** 保存筛选方案到当前命名视图：写 store 持久化；不立即刷新（下次打开/刷新自动应用） */
  function onFilterSave(filter: ViewFilter) {
    evpStore.updateFilter(typePath.value, filter);
    localFilter.value = filter;
    evpStore.persistLastQuery(typePath.value, { q: String(searchForm.Q ?? ''), filter });
    Message.success('筛选方案已保存到此视图');
  }

  /** 清除筛选方案（工具栏标签）：写入空方案持久化；筛选为纯前端过滤，复用已加载数据重过滤 */
  function onClearFilter() {
    evpStore.updateFilter(typePath.value, emptyViewFilter());
    localFilter.value = emptyViewFilter();
    evpStore.persistLastQuery(typePath.value, { q: String(searchForm.Q ?? ''), filter: emptyViewFilter() });
    pagination.current = 1;
    loadData(true);
    Message.success('已清除筛选');
  }

  /** 应用分组方案：写入 store 持久化（刷新保留）并本地重分组 */
  function onGroupApply(group: ViewGroup) {
    evpStore.updateGroup(typePath.value, group);
    localGroup.value = group;
  }

  /** 保存分组方案到当前命名视图：写 store 持久化 */
  function onGroupSave(group: ViewGroup) {
    evpStore.updateGroup(typePath.value, group);
    localGroup.value = group;
    Message.success('分组方案已保存到此视图');
  }

  /** 清除分组方案：写入空方案持久化 */
  function onClearGroup() {
    evpStore.updateGroup(typePath.value, []);
    localGroup.value = [];
    Message.success('已清除分组');
  }

  return {
    onGanttZoom,
    onCalendarShift,
    onCalendarToday,
    onCalendarModeChange,
    groupLabelOf,
    hexToRgba,
    onToggleFullscreen,
    onKeydown,
    observeTableHeight,
    loadProfile,
    applyWorkspacePrefs,
    syncLocalState,
    onColumnsChange,
    onSortChange,
    onSortsApply,
    onClearSort,
    onSortPopoverVisible,
    onConfigSort,
    onChromeChange,
    onMappingChange,
    onGanttMappingChange,
    onKanbanMappingChange,
    onInsightChange,
    onToggleCollapse,
    onConfigRename,
    onSwitchView,
    onCreateView,
    onRenameView,
    onRemoveView,
    onDuplicateView,
    onResetViews,
    onSaveAsDefault,
    onFilterPopoverVisible,
    onGroupPopoverVisible,
    onFormatPopoverVisible,
    onFilterApply,
    onFilterSave,
    onClearFilter,
    onGroupApply,
    onGroupSave,
    onClearGroup,
    onFormatChange,
    onClearFormat,
  };
}

export type ListViews = ReturnType<typeof useListViews>;
