import { nextTick } from 'vue';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import {
  buildSortPayload,
  emptyViewFilter,
  serializeNamedView,
  type ColumnPref,
} from '@/core/utils/viewProfile';
import type {
  ViewChrome,
  ViewFilter,
  ViewGroup,
  ViewInsight,
  ViewKind,
  ViewMapping,
  ViewSort,
} from '@/core/utils/viewProfile';
import type { GanttMapping } from '@/core/utils/viewMapping';
import type { ListContext } from './listContext';

interface ListViewsDeps {
  loadData: (skipFetch?: boolean) => Promise<void>;
  applySearchToForm: (params: Record<string, unknown>) => void;
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
    activeSort,
    effectivePageSize,
    effectivePageSizePref,
    pagination,
    selectedKeys,
    fullscreen,
    tableResizeObserver,
    ganttZoomLevel,
    localFilter,
    localGroup,
    activeViewId,
    flags,
    drawerMode,
    tableDataRaw,
    evpStore,
    measureTableHeight,
  } = ctx;
  const { loadData, applySearchToForm } = deps;

  /** 甘特图缩放等级切换（− 缩小 / + 放大，0~4 夹取） */
  function onGanttZoom(delta: number) {
    ganttZoomLevel.value = Math.min(4, Math.max(0, ganttZoomLevel.value + delta));
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
    const oldSort = JSON.stringify(buildSortPayload(activeSort.value));
    const oldPageSize = effectivePageSize.value;
    evpStore.switchView(typePath.value, id);
    syncLocalState();
    selectedKeys.value = [];
    pagination.current = 1;
    // 切换视图后重新计算条件来源并回填表单（OSC-0012）
    searchTouched.value = false;
    applySearchToForm(baseSearch.value);
    // 重绘优化：未显式搜索、且新视图加载量与排序与已加载数据一致时，复用数据避免重复请求后端
    const canReuse =
      !wasTouched &&
      effectivePageSize.value === oldPageSize &&
      JSON.stringify(buildSortPayload(activeSort.value)) === oldSort &&
      tableDataRaw.value.length > 0;
    loadData(canReuse);
  }

  function onCreateView(kind: ViewKind, name: string) {
    // 创建视图时按用户删除权限设置默认「允许删除记录」：有删除权限 → true，无 → false（需求 OSC）
    evpStore.addView(typePath.value, name, kind, {
      allowDelete: flags.value.canDelete,
    });
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

  /** 应用筛选方案：写入 store 持久化（刷新/下次打开保留）；筛选为纯前端过滤，复用已加载数据重过滤 */
  function onFilterApply(filter: ViewFilter) {
    evpStore.updateFilter(typePath.value, filter);
    localFilter.value = filter;
    pagination.current = 1;
    loadData(true);
  }

  /** 保存筛选方案到当前命名视图：写 store 持久化；不立即刷新（下次打开/刷新自动应用） */
  function onFilterSave(filter: ViewFilter) {
    evpStore.updateFilter(typePath.value, filter);
    localFilter.value = filter;
    Message.success('筛选方案已保存到此视图');
  }

  /** 清除筛选方案（工具栏标签）：写入空方案持久化；筛选为纯前端过滤，复用已加载数据重过滤 */
  function onClearFilter() {
    evpStore.updateFilter(typePath.value, emptyViewFilter());
    localFilter.value = emptyViewFilter();
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
    onConfigSort,
    onChromeChange,
    onMappingChange,
    onGanttMappingChange,
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
    onFilterApply,
    onFilterSave,
    onClearFilter,
    onGroupApply,
    onGroupSave,
    onClearGroup,
  };
}

export type ListViews = ReturnType<typeof useListViews>;
