import {
  computed,
  defineAsyncComponent,
  reactive,
  ref,
  type Component,
} from 'vue';
import { useRoute } from 'vue-router';
import { type PageSetting } from '@cube/api-core';
import { EXPORT_FORMATS } from '@cube/page-utils';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useViewProfileStore } from '@/stores/viewProfile';
import { isEmbedMode } from '@/core/utils/embedMode';
import type { FieldMeta } from '@/core/types/field';
import { resolveCrudFlags } from '@/core/utils/permissions';
import { getValueByKey } from '@/core/utils/url';
import { selfOnlyUserAlertText, shouldShowSelfOnlyUserAlert } from '@/core/utils/iamGuards';
import { formatFieldValue } from '@/core/utils/fieldFormat';
import { getSectionLoader } from '@/core/composables/useSections';
import { selectListColumns } from '@/core/utils/listColumns';
import { classifyListLink, partitionListFields } from '@/core/utils/listLinkFields';
import { resolveFieldsForKind } from '@/core/utils/fieldParts';
import {
  defaultBadgeColumnWidth,
  isBadgeField,
  isBooleanToggleField,
  isEnableField,
  resolveCellBadge,
} from '@/core/utils/fieldBadge';
import {
  getActiveView,
  mergeColumns,
  resolveChrome,
  emptyViewFilter,
  type ColumnPref,
  type EntityViewState,
  type FormLayout,
  type ViewFilter,
  type ViewFormatRule,
  type ViewGroup,
  type ViewInsight,
  type ViewKind,
  type ViewSort,
} from '@/core/utils/viewProfile';
import {
  isLargePageViewKind,
  parseViewKind,
  resolveBatchDeleteState,
  resolveBatchEnableState,
  resolveViewPageSize,
  type CalendarMapping,
  type CardMapping,
  type GanttMapping,
  type KanbanMapping,
} from '@/core/utils/viewMapping';
import {
  cleanSearchParams,
  collectSearchKeys,
  parseUrlSearch,
} from '@/core/utils/searchFilters';
import { isPersonField } from '@/core/utils/filterBuilder';
import { detectTreeData } from '@/core/utils/tree';
import { buildTree, canBuildTree } from '@/core/utils/treeBuilder';
import { hexToRgba } from './useListViews';

/**
 * DefaultList 共享状态上下文（OSC-260813c3e9）：全部 ref/reactive/computed/常量只创建一次，
 * 领域 composable 通过 ctx 消费，禁止各自重复创建。
 */
export function createListContext(props: { type: string; authId?: number }) {
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();
  const evpStore = useViewProfileStore();
  const typePath = computed(() => props.type);

  const listFields = ref<FieldMeta[]>([]);
  const searchFields = ref<FieldMeta[]>([]);
  const addFields = ref<FieldMeta[]>([]);
  const editFields = ref<FieldMeta[]>([]);
  const detailFields = ref<FieldMeta[]>([]);
  const pageSetting = ref<PageSetting | null>(null);
  const pkField = ref('id');

  const tableData = ref<Record<string, unknown>[]>([]);
  /** 后端原始数据（未应用视图级前端筛选 viewFilter）；视图切换/筛选变化时复用避免重复请求（重绘优化） */
  const tableDataRaw = ref<Record<string, unknown>[]>([]);
  const loading = ref(false);
  /** Enable 徽标切换请求进行中：防止快速双击并发回跳 */
  const enableBusy = ref(false);
  const selectedKeys = ref<(string | number)[]>([]);
  const statData = ref<Record<string, unknown> | null>(null);
  const labelCache = reactive<Record<string, Record<string, string>>>({});
  /** 地区/级联叶子值 → label 缓存（useListQuery.hydrateAreaLabels 维护，OSC-2608139feb） */
  const areaLabelCache = reactive<Record<string, string>>({});
  const configDrawerVisible = ref(false);
  const viewState = ref<EntityViewState | null>(null);

  const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0,
  });

  const preferredDefaultView = computed<ViewKind>(() =>
    parseViewKind(profileStore.prefs.workspace.defaultView),
  );

  const preferredPageSize = computed(() =>
    Math.max(1, profileStore.prefs.workspace.pageSize || 20),
  );

  /** 页面级 PageSize（typePath 级，OSC-0012）；0 表示未配置，回退旧全局 workspace 种子 */
  const pageProfileSize = computed(() => evpStore.getPageSize(typePath.value));

  const effectivePageSizePref = computed(() => {
    const n = pageProfileSize.value;
    return n > 0 ? n : preferredPageSize.value;
  });

  const searchForm = reactive<Record<string, unknown>>({});
  /** 主时间字段信息（OSC-0016）：GetPage setting 透传 */
  const masterTimeName = ref<string | null>(null);
  const masterTimeDisplayName = ref<string | null>(null);
  /** 关键字 Q 启用（OSC-0016）；缺省 true */
  const enableKey = ref(true);
  /** 当前会话是否已显式执行过搜索/重置；未执行时有效条件取 URL→已保存基准（OSC-0012） */
  const searchTouched = ref(false);
  const route = useRoute();
  const formModel = reactive<Record<string, unknown>>({});
  const drawerVisible = ref(false);
  const drawerMode = ref<'add' | 'edit' | 'detail'>('add');
  /** 当前抽屉记录在可见列表（本页 tableData）中的下标；新建为 -1 */
  const drawerRowIndex = ref(-1);
  const saving = ref(false);
  const chartVisible = ref(false);
  const chartList = ref<unknown[]>([]);
  const tableHeight = 520;
  /** 后端字段级错误（FieldErrors），映射到表单对应字段（OSC-0009） */
  const fieldErrors = ref<{ field: string; message: string }[]>([]);

  const exportFormats = EXPORT_FORMATS;

  const headerSection = computed<Component | null>(() => {
    const loader = getSectionLoader(typePath.value, 'ListPageHeader');
    if (!loader) return null;
    return defineAsyncComponent(loader as () => Promise<{ default: Component }>);
  });

  const flags = computed(() =>
    resolveCrudFlags(userStore.getMenuPermission(typePath.value), pageSetting.value),
  );

  /** 仅管理员角色可使用表单布局（与后端 Roles.Any(e => e.IsSystem) 对齐） */
  const isAdmin = computed(() => userStore.userInfo?.isSystem === true);

  /** 只读实体列表页不展示历史与评论（新建由表单 mode 自行隐藏） */
  const showHistoryTabs = computed(() => pageSetting.value?.isReadOnly !== true);
  /** 魔方设置 EnableTableDoubleClick：默认 true；显式 false 时禁用列表/卡片双击进详情 */
  const enableTableDoubleClick = computed(() => pageSetting.value?.enableTableDoubleClick !== false);


  /** 表单字段分区唯一入口：展示、回填、保存共用同一字段来源（OSC-0009） */
  const fieldParts = computed(() => ({
    list: listFields.value,
    add: addFields.value,
    edit: editFields.value,
    detail: detailFields.value,
    search: searchFields.value,
  }));

  const drawerFields = computed(() =>
    resolveFieldsForKind(drawerMode.value, fieldParts.value),
  );

  /** 当前抽屉模式的受限表单布局（OSC-0013）；无配置返回 null */
  const drawerFormLayout = computed<FormLayout | null>(() =>
    evpStore.getFormModeLayout(typePath.value, drawerMode.value),
  );

  /** 表单布局配置抽屉开关（OSC-0013） */
  const formLayoutDrawerVisible = ref(false);

  const metaKeys = computed(() => selectListColumns(listFields.value).map((f) => f.name));

  /** GetPage 合成 Url/dataAction → 操作列（OSC-2608178bdb） */
  const opsCustomLinks = computed(() => partitionListFields(listFields.value).opsLinks);

  const columnTitles = computed(() => {
    const m: Record<string, string> = {};
    for (const f of listFields.value) m[f.name] = f.displayName || f.name;
    return m;
  });

  /** 洞察统计标签显示名（按 listFields，与 GetList.stat 的 key 对齐，OSC-0012） */
  const statLabels = computed(() => {
    const m: Record<string, string> = {};
    for (const f of listFields.value) m[f.name] = f.displayName || f.name;
    return m;
  });

  /** 展示用列：偏好 ∩ 元数据；偏好空时回落元数据全列 */
  const activeColumns = computed<ColumnPref[]>(() => {
    const keys = metaKeys.value;
    const prefs = viewState.value ? getActiveView(viewState.value).columns : [];
    return mergeColumns(keys, prefs);
  });

  const activeSort = computed<ViewSort | null>(() =>
    viewState.value ? getActiveView(viewState.value).sort || null : null,
  );

  const activeViewKind = computed<ViewKind>(() =>
    viewState.value ? getActiveView(viewState.value).view : 'table',
  );

  /** 甘特图缩放等级（zoomScale.levels 下标 0~4：年/年月/月·日/周·日/日·时），默认月·日 */
  const ganttZoomLevel = ref(2);
  /** 甘特图分页器可选条数（大视图 pageSize 钳制 200~1000，甘特图底部现可翻页） */
  const GANTT_PAGE_SIZE_OPTIONS = [200, 500, 1000];
  /** 甘特图缩放等级名称（与 GanttView.buildZoomLevels 下标一一对应） */
  const ganttZoomLabels = ['年', '年月', '月·日', '周·日', '日·时'];
  /** 当前缩放等级名称（−/+ 按钮中间标签显示） */
  const ganttZoomLabel = computed(
    () => ganttZoomLabels[ganttZoomLevel.value] ?? `${ganttZoomLevel.value}`,
  );

  const activeViewId = computed(() => viewState.value?.activeViewId ?? '');

  /** 合法搜索 key（含范围字段 _min/_max 后缀，OSC-0012） */
  const searchKeys = computed(() => collectSearchKeys(searchFields.value));

  /** URL 搜索参数（当前会话；只影响本页，不自动写回存储） */
  const urlSearch = computed(() =>
    parseUrlSearch(route.query as Record<string, unknown>, searchKeys.value),
  );

  /** 当前命名视图的已保存筛选（经合法 key 集清理） */
  const savedSearch = computed(() => {
    if (!activeViewId.value) return {};
    return cleanSearchParams(
      evpStore.getViewFilters(typePath.value, activeViewId.value) ?? {},
      searchKeys.value,
    );
  });

  /** 未显式搜索时的基准条件：URL → 已保存 → 空 */
  const baseSearch = computed(() => {
    if (Object.keys(urlSearch.value).length) return urlSearch.value;
    if (Object.keys(savedSearch.value).length) return savedSearch.value;
    return {};
  });

  /** 唯一有效搜索条件：会话内未搜索时取基准，搜索/重置后取表单正规化结果（OSC-0012）。
   *  筛选构建器为纯前端过滤，不并入后端请求（OSC-0015） */
  const effectiveSearch = computed(() => {
    if (!searchTouched.value) return baseSearch.value;
    return cleanSearchParams({ ...searchForm }, searchKeys.value);
  });

  /** 当前命名视图的受限洞察配置（OSC-0012） */
  const insight = computed<ViewInsight>(() => {
    if (!viewState.value) return { showStat: false, showChart: false };
    const v = getActiveView(viewState.value);
    return v?.insight ?? { showStat: false, showChart: false };
  });

  const chartData = ref<unknown[]>([]);
  const chartLoading = ref(false);
  const chartError = ref('');
  /** 图表请求序号：切换筛选/视图/刷新时丢弃过期响应 */
  const chartSeq = ref(0);

  const activeMapping = computed(() =>
    viewState.value ? getActiveView(viewState.value).mapping : undefined,
  );

  const activeCardMapping = computed(
    () => (activeMapping.value?.kind === 'card' ? activeMapping.value : null) as CardMapping | null,
  );

  /** 列数/排版变更时强制重挂卡片列表，确保样式立即生效 */
  const cardListKey = computed(() => {
    const m = activeCardMapping.value;
    return `card:${m?.layout ?? 'standard'}:${m?.bodyColumns ?? 2}:${m?.fieldOrientation ?? 'vertical'}`;
  });
  const activeKanbanMapping = computed(
    () =>
      (activeMapping.value?.kind === 'kanban' ? activeMapping.value : null) as KanbanMapping | null,
  );
  const activeCalendarMapping = computed(
    () =>
      (activeMapping.value?.kind === 'calendar'
        ? activeMapping.value
        : null) as CalendarMapping | null,
  );
  const activeGanttMapping = computed(
    () =>
      (activeMapping.value?.kind === 'gantt' ? activeMapping.value : null) as GanttMapping | null,
  );

  const isLargePageView = computed(() => isLargePageViewKind(activeViewKind.value));

  const effectivePageSize = computed(() =>
    resolveViewPageSize(activeViewKind.value, pagination.pageSize, effectivePageSizePref.value),
  );

  const showPagerBar = computed(
    // 甘特图底部显示分页器可翻页（OSC-0019 后续）；看板/日历仍为大视图仅提示
    () => chrome.value.showPager && (!isLargePageView.value || activeViewKind.value === 'gantt'),
  );

  /** 树数据可用：后端已返回 children 树，或扁平行可组装为树 */
  const treeDataDetected = computed(() => {
    const rows = tableData.value;
    return rows.length > 0 && (detectTreeData(rows) || canBuildTree(rows));
  });

  /** 树视图展示数据：后端已返回树或可组装时用树，否则回落原始行；字段元数据仍取自 GetPage */
  const treeRows = computed(() => {
    if (activeViewKind.value !== 'tree') return tableData.value;
    // 后端已返回 children 树，直接使用，避免无谓重建
    if (detectTreeData(tableData.value)) return tableData.value;
    return canBuildTree(tableData.value) ? buildTree(tableData.value) : tableData.value;
  });

  const chrome = computed(() =>
    resolveChrome(viewState.value ? getActiveView(viewState.value) : null),
  );

  /** 批量删除门禁：表格视图 + 删除权限 + 允许删除 + 至少选中一行 */
  const batchDeleteState = computed(() =>
    resolveBatchDeleteState({
      viewKind: activeViewKind.value,
      canDelete: flags.value.canDelete,
      allowDelete: chrome.value.allowDelete,
      selectedCount: selectedKeys.value.length,
    }),
  );

  const hasEnableField = computed(() => listFields.value.some((f) => isEnableField(f)));

  const batchEnableState = computed(() =>
    resolveBatchEnableState({
      viewKind: activeViewKind.value,
      canEdit: flags.value.canEdit,
      enableSelect: pageSetting.value?.enableSelect,
      hasEnableField: hasEnableField.value,
      selectedCount: selectedKeys.value.length,
    }),
  );

  /** 「高级」菜单：导入/导出 | 批量操作 | 自动化流程/表单布局（分享页不展示后两项） */
  const advancedVisible = computed(() => {
    const embed = isEmbedMode();
    return (
      flags.value.canImport ||
      flags.value.canExport ||
      batchDeleteState.value.visible ||
      batchEnableState.value.visible ||
      flags.value.canEdit ||
      (!embed && flags.value.canUpdate) ||
      (!embed && isAdmin.value)
    );
  });

  /** 搜索抽屉开关：默认收起，点击工具栏「搜索」打开 */
  const searchPanelOpen = ref(false);

  /** 筛选/分组/填色弹层互斥 */
  const activePopover = ref<'filter' | 'group' | 'format' | null>(null);
  const filterPopoverVisible = computed({
    get: () => activePopover.value === 'filter',
    set: (v: boolean) => {
      activePopover.value = v ? 'filter' : null;
    },
  });
  const groupPopoverVisible = computed({
    get: () => activePopover.value === 'group',
    set: (v: boolean) => {
      activePopover.value = v ? 'group' : null;
    },
  });
  const formatPopoverVisible = computed({
    get: () => activePopover.value === 'format',
    set: (v: boolean) => {
      activePopover.value = v ? 'format' : null;
    },
  });

  /** 当前命名视图的筛选构建器方案（OSC-0015）：会话内存状态，应用仅改本地、保存才写 store 持久化 */
  const localFilter = ref<ViewFilter>(emptyViewFilter());
  const viewFilter = computed<ViewFilter>(() => localFilter.value);

  /** 当前命名视图的多级分组字段（OSC-0015）：同筛选，应用仅改本地、保存才写 store */
  const localGroup = ref<ViewGroup>([]);
  const viewGroup = computed<ViewGroup>(() => localGroup.value);

  const localFormat = ref<ViewFormatRule[]>([]);
  const viewFormat = computed<ViewFormatRule[]>(() => localFormat.value);
  const formatButtonVisible = computed(() => {
    const k = activeViewKind.value;
    return k === 'table' || k === 'tree' || k === 'card';
  });

  /** 筛选构建器候选字段 = 当前视图可见列 ∪ 人员字段（创建者/更新者等即使列隐藏也可筛选，OSC-0015） */
  const filterFields = computed<FieldMeta[]>(() => {
    const visible = new Set(
      activeColumns.value.filter((c) => c.visible).map((c) => c.key),
    );
    const visibleFields = listFields.value.filter((f) => visible.has(f.name));
    const hiddenPerson = listFields.value.filter(
      (f) => !visible.has(f.name) && isPersonField(f),
    );
    return [...visibleFields, ...hiddenPerson];
  });

  /** 分组展示：仅表格视图且配置了分组字段（树状视图不允许分组，OSC-0015） */
  const isGrouped = computed(
    () => viewGroup.value.length > 0 && activeViewKind.value === 'table',
  );

  /** 表格数据：分组视图时对 treeRows 做多级分组（组头节点 + 数据行） */
  /** 展示行：分组改由 ListTable 内 VTable 原生 groupBy 完成（OSC-0015 重构，参考官方 list-table-group-checkbox），
   *  此处始终传原始行（groupBy 在表格内部按分组字段重组并渲染组标题行） */
  /** 列表/树增量渲染：非分组视图先渲染前 N 条，滚动接近底部（ListTable scrollBottom）再追加；
   *  分组视图需完整数据（VTable 内部 groupBy 分组统计），不做增量 */
  const TABLE_INITIAL_VISIBLE = 100;
  const TABLE_LOAD_STEP = 100;
  const tableVisibleCount = ref(TABLE_INITIAL_VISIBLE);

  const displayRows = computed(() => {
    const rows = treeRows.value;
    if (isGrouped.value) return rows;
    return rows.slice(0, tableVisibleCount.value);
  });

  const listShellStyle = computed(() => {
    const c = chrome.value;
    const style: Record<string, string> = { padding: '0 4px' };
    if (c.widthMode === 'fill') style.width = '100%';
    if (c.heightMode === 'fill') style.minHeight = 'calc(100vh - 180px)';
    return style;
  });

  /** 是否配置了视图自定义背景（作用于搜索+表格整块） */
  const hasChromeBg = computed(() => {
    const c = chrome.value;
    return c.bgPreset === 'custom' && !!c.bgColor;
  });

  /** 背景色：分布 + 搜索 + 表格（含视图 Tab） */
  const listSurfaceStyle = computed(() => {
    const c = chrome.value;
    const style: Record<string, string> = {};
    if (c.bgPreset === 'custom' && c.bgColor && c.bgColor !== 'transparent') {
      style.backgroundColor = c.bgColor.startsWith('#')
        ? hexToRgba(c.bgColor, c.bgOpacity)
        : c.bgColor;
    } else if (c.bgPreset === 'custom' && c.bgColor === 'transparent') {
      style.backgroundColor = 'transparent';
    }
    if (c.bgBlur > 0) style.backdropFilter = `blur(${Math.round(c.bgBlur / 5)}px)`;
    return style;
  });

  /** 预定义查询列表（OSC-0016）：实体级个人配置 */
  const savedQueries = computed(() => evpStore.getQueries(typePath.value).queries);
  /** 当前应用的预定义查询 id（会话内存，刷新后为 null） */
  const appliedQueryId = computed(() => evpStore.getActiveQueryId(typePath.value));
  /** 当前表单参数（cleanSearchParams 后）是否有任一非空键（含 Q/dtStart/dtEnd） */
  const queryHasParams = computed(
    () => Object.keys(cleanSearchParams({ ...searchForm }, searchKeys.value)).length > 0,
  );
  /** 当前表单参数与 activeQuery 是否不一致（OSC-0016：不一致时条目不显示 ✓，应用标记保留） */
  const queryParamsDirty = computed(() => {
    const id = appliedQueryId.value;
    if (!id) return false;
    const q = savedQueries.value.find((x) => x.id === id);
    if (!q) return false;
    const cur = cleanSearchParams({ ...searchForm }, searchKeys.value);
    return JSON.stringify(cur) !== JSON.stringify(q.params);
  });

  /** 列表面板引用：用于测量表格可用高度，保证分页器与外壳底部在首屏可见 */
  const tablePanelRef = ref<HTMLElement | null>(null);
  /** 当前视图是否全屏展示（固定铺满视口，覆盖顶部及左侧导航栏） */
  const fullscreen = ref(false);
  /** 动态测量的表格可用高度（default/fill 模式）；测量前回落固定 tableHeight */
  const measuredTableHeight = ref(tableHeight);

  /**
   * 测量表格可用高度：scroll 可视区底 - 面板顶 - 面板内非表格固定部分（Tab/工具栏/分页器/padding）
   * 预留 16px 底部 gutter（= scroll padding-bottom），使底部间隙与左右上三边一致。
   * 注：design §5.3 将该函数归入 useListViews，但其仅依赖 ctx 状态且被
   * useListQuery.loadData / useDefaultList 生命周期共用，定义于此避免组装期循环依赖。
   */
  function measureTableHeight() {
    if (typeof window === 'undefined') return;
    // fit 模式按内容自适应（分页器随内容在下方），无需测量
    if (chrome.value.heightMode === 'fit') return;
    const panel = tablePanelRef.value;
    if (!panel) return;
    const pr = panel.getBoundingClientRect();
    // 非表格固定部分 = 面板总高 - 当前表格高；表格高度更新后面板高随之变化，该差值保持稳定
    const nonTableH = pr.height - measuredTableHeight.value;
    let avail: number;
    if (fullscreen.value) {
      // 全屏：面板固定铺满视口（顶部贴近视口），可用高度 = 视口高 - 面板顶 - 非表格部分 - gutter
      avail = Math.floor(window.innerHeight - pr.top - nonTableH - 16);
    } else {
      const scroll = document.querySelector<HTMLElement>('.layout-content__scroll');
      if (!scroll) return;
      const sr = scroll.getBoundingClientRect();
      avail = Math.floor(sr.bottom - pr.top - nonTableH - 16);
    }
    const next = Math.max(240, avail);
    // 1px 滚动条/亚像素抖动会改 height → 甘特整表重建闪烁；小于 2px 忽略
    if (Math.abs(measuredTableHeight.value - next) < 2) return;
    measuredTableHeight.value = next;
  }

  const resolvedTableHeight = computed(() => {
    const mode = chrome.value.heightMode;
    if (mode === 'fit') return Math.max(240, 48 + tableData.value.length * 40);
    // default/fill：动态填满可视空间，分页器与外壳底部保持在首屏内
    return measuredTableHeight.value;
  });

  /** scroll 容器尺寸变化（窗口/布局调整/侧栏折叠）时重测的 ResizeObserver（原 script 顶层 let） */
  const tableResizeObserver = ref<ResizeObserver | null>(null);

  /**
   * 单元格渲染（LOV 翻译缓存）：design §5.3 归入 useListViews，但 labelCache 属于本上下文，
   * 且 tableColumns（本文件）与模板 :format-cell 均需使用，定义于此避免循环依赖。
   */
  function renderCell(field: FieldMeta, record: Record<string, unknown>): string {
    return formatFieldValue(field, record, { labelCache, areaLabelCache });
  }

  const tableColumns = computed(() =>
    activeColumns.value.map((pref) => {
      const field = listFields.value.find((f) => f.name === pref.key);
      const badge = !!field && isBadgeField(field);
      const width =
        pref.width ||
        (field && badge ? defaultBadgeColumnWidth(field) : undefined);
      const cellLink =
        field && classifyListLink(field) === 'cell' && field.url?.trim()
          ? { url: field.url.trim(), target: field.target }
          : undefined;
      return {
        pref: width && !pref.width ? { ...pref, width } : pref,
        title: pref.title?.trim() || field?.displayName || pref.key,
        badge,
        // Boolean 字段徽标（Enable 及任意 Boolean 字段）：有 Update 权限时可点击切换状态
        enableToggle: !!field && isBooleanToggleField(field) && flags.value.canEdit,
        badgeOf: field
          ? (row: Record<string, unknown>) => {
              const raw = getValueByKey(row, field.name);
              return resolveCellBadge(field, raw);
            }
          : undefined,
        format: (row: Record<string, unknown>) => {
          if (!field) return String(row[pref.key] ?? '-');
          const text = renderCell(field, row);
          if (cellLink && (!text || text === '-')) {
            return field.displayName || field.name;
          }
          return text;
        },
        cellLink,
      };
    }),
  );

  const drawerCanPrev = computed(
    () => drawerMode.value !== 'add' && drawerRowIndex.value > 0,
  );
  const drawerCanNext = computed(
    () =>
      drawerMode.value !== 'add' &&
      drawerRowIndex.value >= 0 &&
      drawerRowIndex.value < tableData.value.length - 1,
  );

  const showSelfOnlyUserAlert = computed(() =>
    shouldShowSelfOnlyUserAlert({
      typePath: typePath.value,
      isSystemUser: userStore.userInfo?.isSystem,
      total: pagination.total,
      rows: tableData.value,
      currentUserId: userStore.userInfo?.id,
    }),
  );
  const selfOnlyUserAlertMessage = selfOnlyUserAlertText();

  return {
    userStore,
    profileStore,
    evpStore,
    typePath,
    listFields,
    opsCustomLinks,
    searchFields,
    addFields,
    editFields,
    detailFields,
    pageSetting,
    pkField,
    tableData,
    tableDataRaw,
    loading,
    enableBusy,
    selectedKeys,
    statData,
    labelCache,
    areaLabelCache,
    configDrawerVisible,
    viewState,
    pagination,
    preferredDefaultView,
    preferredPageSize,
    pageProfileSize,
    effectivePageSizePref,
    searchForm,
    masterTimeName,
    masterTimeDisplayName,
    enableKey,
    searchTouched,
    route,
    formModel,
    drawerVisible,
    drawerMode,
    drawerRowIndex,
    saving,
    chartVisible,
    chartList,
    tableHeight,
    fieldErrors,
    exportFormats,
    headerSection,
    flags,
    isAdmin,
    showHistoryTabs,
    enableTableDoubleClick,
    fieldParts,
    drawerFields,
    drawerFormLayout,
    formLayoutDrawerVisible,
    metaKeys,
    columnTitles,
    statLabels,
    activeColumns,
    activeSort,
    activeViewKind,
    ganttZoomLevel,
    GANTT_PAGE_SIZE_OPTIONS,
    ganttZoomLabels,
    ganttZoomLabel,
    activeViewId,
    searchKeys,
    urlSearch,
    savedSearch,
    baseSearch,
    effectiveSearch,
    insight,
    chartData,
    chartLoading,
    chartError,
    chartSeq,
    activeMapping,
    activeCardMapping,
    cardListKey,
    activeKanbanMapping,
    activeCalendarMapping,
    activeGanttMapping,
    isLargePageView,
    effectivePageSize,
    showPagerBar,
    treeDataDetected,
    treeRows,
    chrome,
    batchDeleteState,
    batchEnableState,
    advancedVisible,
    searchPanelOpen,
    activePopover,
    filterPopoverVisible,
    groupPopoverVisible,
    formatPopoverVisible,
    localFilter,
    viewFilter,
    localGroup,
    viewGroup,
    localFormat,
    viewFormat,
    formatButtonVisible,
    filterFields,
    isGrouped,
    tableVisibleCount,
    TABLE_INITIAL_VISIBLE,
    TABLE_LOAD_STEP,
    displayRows,
    listShellStyle,
    hasChromeBg,
    listSurfaceStyle,
    savedQueries,
    appliedQueryId,
    queryHasParams,
    queryParamsDirty,
    tablePanelRef,
    fullscreen,
    measuredTableHeight,
    resolvedTableHeight,
    tableColumns,
    measureTableHeight,
    renderCell,
    tableResizeObserver,
    drawerCanPrev,
    drawerCanNext,
    showSelfOnlyUserAlert,
    selfOnlyUserAlertMessage,
  };
}

export type ListContext = ReturnType<typeof createListContext>;
