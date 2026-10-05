import { computed, reactive, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import {
  applyFrozenLeftTo,
  applyFrozenRightTo,
  arrangeFrozenColumns,
  DEFAULT_CHROME,
  normalizeInsight,
  type ColumnPref,
  type HeightMode,
  type ViewChrome,
  type ViewInsight,
  type ViewKind,
  type ViewMapping,
  type ViewSort,
  type WidthMode,
} from '@/core/utils/viewProfile';
import type { FieldMeta } from '@/core/types/field';
import {
  MAP_COORD_SYSTEMS,
  MAP_MAX_POINTS,
  MAP_MAX_RULES,
  VIEW_KIND_LABEL,
  buildMapCategoryRules,
  colorFieldCandidates,
  dateFieldCandidates,
  groupFieldCandidates,
  imageFieldCandidates,
  mapCategoryCandidates,
  normalizeCardBodyColumns,
  normalizeMapping,
  titleFieldCandidates,
  type CardBodyColumns,
  type CardFieldOrientation,
  type CardLayout,
  type DataSourceOption,
  type MapCategoryRule,
} from '@/core/utils/viewMapping';
import type { CoordMode, CoordOrder, CoordSystem } from '@/core/utils/mapCoord';
import { MAP_MARKER_ICONS } from '@/core/utils/iconRegistry';
import { useUserProfileStore } from '@/stores/userProfile';
import { PRESET_THEME_COLORS, type PresetThemeColor } from '@/core/utils/presetColors';

type Swatch = { key: string; label: string; color?: string; none?: boolean };

const recommendedColors: Swatch[] = [
  // 第 1 行
  { key: 'blue', label: '蓝', color: '#3370FF' },
  { key: 'purple', label: '紫', color: '#7B67EE' },
  { key: 'teal', label: '青', color: '#14C0FF' },
  { key: 'cyan', label: '靛', color: '#00D6B9' },
  { key: 'sky', label: '天蓝', color: '#4DC3FF' },
  { key: 'yellow', label: '黄', color: '#FFC60A' },
  { key: 'orange', label: '橙', color: '#FF8800' },
  { key: 'red', label: '红', color: '#F54A45' },
  { key: 'magenta', label: '品红', color: '#F01D94' },
  // 第 2 行（明亮饱满）
  { key: 'vivid-blue', label: '亮蓝', color: '#2B6CFF' },
  { key: 'vivid-indigo', label: '靛蓝', color: '#5B5CFF' },
  { key: 'vivid-violet', label: '亮紫', color: '#A855F7' },
  { key: 'vivid-fuchsia', label: '玫红', color: '#E11D8F' },
  { key: 'vivid-rose', label: '玫紫', color: '#FF2D55' },
  { key: 'vivid-coral', label: '珊瑚', color: '#FF5A36' },
  { key: 'vivid-amber', label: '琥珀', color: '#FFB020' },
  { key: 'vivid-lime', label: '青绿', color: '#32D74B' },
  { key: 'vivid-emerald', label: '翠绿', color: '#00C2A8' },
  // 第 3 行（明亮饱满）
  { key: 'neon-cyan', label: '霓虹青', color: '#00E5FF' },
  { key: 'neon-azure', label: '宝蓝', color: '#1E90FF' },
  { key: 'neon-purple', label: '电紫', color: '#8B5CFF' },
  { key: 'neon-pink', label: '亮粉', color: '#FF4D9E' },
  { key: 'neon-orange', label: '亮橙', color: '#FF6B00' },
  { key: 'neon-gold', label: '金黄', color: '#FFD60A' },
  { key: 'neon-chartreuse', label: '草绿', color: '#A8E10C' },
  { key: 'neon-mint', label: '薄荷绿', color: '#00E39A' },
  { key: 'neon-turquoise', label: '松石', color: '#00CFC8' },
  // 无 / 白
  { key: 'none', label: '无', none: true },
  { key: 'white', label: '白', color: '#FFFFFF' },
];

/** 分类样式推荐色板（选择分类字段自动生成时循环取色，用基础调色板前 9 色） */
const mapCategoryPalette: string[] = recommendedColors
  .filter((s) => !!s.color)
  .slice(0, 9)
  .map((s) => s.color!);

type PanelKey = 'bg' | null;

/**
 * ViewConfigDrawer 组件 props 类型（与 ViewConfigDrawer.vue defineProps 泛型逐字一致；
 * viewKind/fields 取 withDefaults 默认值解析后的必填类型）
 */
interface ViewConfigDrawerProps {
  visible: boolean;
  typePath: string;
  viewName: string;
  columns: ColumnPref[];
  titles: Record<string, string>;
  sort: ViewSort | null;
  chrome?: ViewChrome | null;
  viewKind: ViewKind;
  fields: FieldMeta[];
  mapping?: ViewMapping | null;
  insight?: ViewInsight | null;
  /** 当前列表行（图表配置预览用，OSC-260819e483 P5） */
  chartRows?: Record<string, unknown>[];
  /** 分类字段值获取（拉数据抽样提取去重值；无 dataSource 字段用，OSC-261004d7f4 增强） */
  loadCategoryValues?: (field: string) => Promise<string[]>;
}

/** ViewConfigDrawer 组件 emits 类型（与 ViewConfigDrawer.vue defineEmits 泛型逐字一致） */
interface ViewConfigDrawerEmits {
  'update:visible': [boolean];
  'update:columns': [cols: ColumnPref[]];
  'update:sort': [sort: ViewSort | null];
  'update:chrome': [chrome: ViewChrome];
  'update:name': [name: string];
  'update:mapping': [mapping: ViewMapping | undefined];
  openDashboard: [];
}

type ViewConfigDrawerEmit = <K extends keyof ViewConfigDrawerEmits>(event: K, ...args: ViewConfigDrawerEmits[K]) => void;

/** ViewConfigDrawer 组件全部业务 TS：状态、字段配置、自定义 chrome/映射/洞察（自 ViewConfigDrawer.vue script setup 原样搬移） */
export function useViewConfigDrawer(props: ViewConfigDrawerProps, emit: ViewConfigDrawerEmit) {
  const activeTab = ref('basic');
  const openPanel = ref<PanelKey>(null);
  const topBarOpen = ref(true);
  const listAreaOpen = ref(true);
  const localColumns = ref<ColumnPref[]>([]);
  const localName = ref('');
  const localSort = ref<ViewSort | null>(null);
  const chrome = reactive<Required<ViewChrome>>({ ...DEFAULT_CHROME });
  const localInsight = ref<ViewInsight>({ showStat: false, showChart: false });
  const localMapping = reactive({
    titleField: '',
    imageField: undefined as string | undefined,
    groupField: '',
    startField: '',
    endField: undefined as string | undefined,
    colorField: undefined as string | undefined,
    // 甘特（OSC-0019）：计划/实际/固定颜色
    plannedStartField: '',
    plannedEndField: '',
    actualStartField: undefined as string | undefined,
    actualEndField: undefined as string | undefined,
    barColor: undefined as string | undefined,
    layout: 'standard' as CardLayout,
    bodyColumns: 2 as CardBodyColumns,
    fieldOrientation: 'vertical' as CardFieldOrientation,
    // 地图（OSC-261004d7f4）
    mapCoordMode: 'latlng' as CoordMode,
    mapLngField: '',
    mapLatField: '',
    mapCoordField: '',
    mapCoordOrder: 'lnglat' as CoordOrder,
    mapCoordSystem: 'gcj02' as CoordSystem,
    mapCluster: true,
    mapMaxPoints: MAP_MAX_POINTS,
    mapZoom: 5,
    mapCenter: '',
    mapCategoryField: '',
    mapCategoryRules: [] as MapCategoryRule[],
    mapDefaultIcon: 'local',
    mapDefaultColor: '#165DFF',
  });

  /** 当前主题主色（外观设置自定义主色 hex；缺省极客蓝） */
  const profileStore = useUserProfileStore();
  function currentPrimaryColor(): string {
    return profileStore.prefs.theme.primaryColor || '#165DFF';
  }
  /** 任务条颜色缺省显示值：当前主题主色；选回该值视为恢复主题色（OSC-0019） */
  const barColorShown = ref(currentPrimaryColor());
  /** 自定义任务条颜色取色器（隐藏原生 color input，与外观设置「自定义主色」一致） */
  const barColorInputRef = ref<HTMLElement | null>(null);

  const cardLayouts: { value: CardLayout; label: string }[] = [
    { value: 'standard', label: '标准' },
    { value: 'large', label: '偏大' },
    { value: 'row', label: '整行' },
  ];
  const cardBodyColumnOptions: { value: CardBodyColumns; label: string }[] = [
    { value: 1, label: '一列' },
    { value: 2, label: '两列' },
    { value: 3, label: '三列' },
  ];
  let dragFrom = -1;

  const drawerTitle = computed(() => `${VIEW_KIND_LABEL[props.viewKind]}视图`);
  const listAreaLabel = computed(() => {
    switch (props.viewKind) {
      case 'table':
        return '表格区';
      case 'tree':
        return '树图区';
      case 'card':
        return '卡片区';
      case 'kanban':
        return '看板区';
      case 'calendar':
        return '日历区';
      case 'gantt':
        return '甘特图区';
      case 'map':
        return '地图区';
      default:
        return '视图区';
    }
  });
  const titleCandidates = computed(() => titleFieldCandidates(props.fields));
  const imageCandidates = computed(() => imageFieldCandidates(props.fields));
  const groupCandidates = computed(() => groupFieldCandidates(props.fields));
  const dateCandidates = computed(() => dateFieldCandidates(props.fields));
  const colorCandidates = computed(() => colorFieldCandidates(props.fields));

  // ---- 地图（OSC-261004d7f4）----
  const mapCoordSystemOptions = MAP_COORD_SYSTEMS;
  const mapMarkerIcons = MAP_MARKER_ICONS;
  const mapMaxPointsLimit = MAP_MAX_POINTS;
  const mapMaxRules = MAP_MAX_RULES;
  /** 经度/纬度候选：数值字段 */
  const mapNumericCandidates = computed(() =>
    props.fields.filter(
      (f) =>
        !f.primaryKey &&
        ['Byte', 'Int16', 'Int32', 'Int64', 'Single', 'Double', 'Decimal'].includes(f.typeName || ''),
    ),
  );
  /** 合并坐标候选：非主键字段（坐标通常存 String） */
  const mapMergedCandidates = computed(() => props.fields.filter((f) => !f.primaryKey));
  /** 分类字段候选（状态/枚举/值集/类型名启发；含地区实体 Kind=省/市/区县） */
  const mapCategoryOptions = computed(() => mapCategoryCandidates([...props.fields]));
  /** 分类字段值候选（所选字段的 dataSource；无则自由输入） */
  const mapCategoryValueOptions = computed<DataSourceOption[]>(() => {
    const f = props.fields.find((x) => x.name === localMapping.mapCategoryField);
    if (!f?.dataSource) return [];
    return Object.entries(f.dataSource).map(([value, label]) => ({ value, label: String(label) }));
  });

  function fieldLabel(f: FieldMeta): string {
    return (f.displayName?.trim() || f.name).toString();
  }

  const visibleCount = computed(() => localColumns.value.filter((c) => c.visible).length);

  function isColorValue(v: string | null | undefined): boolean {
    return !!v && v !== 'default' && v !== 'transparent' && /^#([0-9a-fA-F]{6})$/.test(v);
  }

  function contrastText(hex: string): string {
    const h = hex.replace('#', '');
    if (h.length !== 6) return '#1f2329';
    const r = parseInt(h.slice(0, 2), 16);
    const g = parseInt(h.slice(2, 4), 16);
    const b = parseInt(h.slice(4, 6), 16);
    const yiq = (r * 299 + g * 587 + b * 114) / 1000;
    return yiq >= 160 ? '#1f2329' : '#ffffff';
  }

  function chipStyle(color: string | null | undefined): Record<string, string> {
    if (!isColorValue(color)) {
      return {
        background:
          'linear-gradient(to top right, transparent calc(50% - 1px), #f54a45 calc(50% - 1px), #f54a45 calc(50% + 1px), transparent calc(50% + 1px)), #fff',
      };
    }
    return { background: color! };
  }

  const bgFilled = computed(
    () => chrome.bgPreset === 'custom' && isColorValue(chrome.bgColor),
  );

  const bgTriggerLabel = computed(() =>
    chrome.bgPreset === 'default' || !chrome.bgColor
      ? '默认'
      : chrome.bgColor === 'transparent'
        ? '无'
        : chrome.bgColor.toUpperCase(),
  );

  const bgTriggerStyle = computed(() => {
    if (!bgFilled.value) return {};
    return {
      backgroundColor: chrome.bgColor,
      color: contrastText(chrome.bgColor),
      borderColor: 'transparent',
    };
  });

  function togglePanel(key: Exclude<PanelKey, null>) {
    openPanel.value = openPanel.value === key ? null : key;
  }

  function syncMappingFromProps() {
    localMapping.titleField = '';
    localMapping.imageField = undefined;
    localMapping.groupField = '';
    localMapping.startField = '';
    localMapping.endField = undefined;
    localMapping.colorField = undefined;
    localMapping.plannedStartField = '';
    localMapping.plannedEndField = '';
    localMapping.actualStartField = undefined;
    localMapping.actualEndField = undefined;
    localMapping.barColor = undefined;
    barColorShown.value = currentPrimaryColor();
    localMapping.layout = 'standard';
    localMapping.bodyColumns = 2;
    localMapping.fieldOrientation = 'vertical';
    localMapping.mapCoordMode = 'latlng';
    localMapping.mapLngField = '';
    localMapping.mapLatField = '';
    localMapping.mapCoordField = '';
    localMapping.mapCoordOrder = 'lnglat';
    localMapping.mapCoordSystem = 'gcj02';
    localMapping.mapCluster = true;
    localMapping.mapMaxPoints = MAP_MAX_POINTS;
    localMapping.mapZoom = 5;
    localMapping.mapCenter = '';
    localMapping.mapCategoryField = '';
    localMapping.mapCategoryRules = [];
    localMapping.mapDefaultIcon = 'local';
    localMapping.mapDefaultColor = '#165DFF';

    const kind = props.viewKind;
    if (kind === 'table' || kind === 'tree') return;

    const m = normalizeMapping(kind, props.mapping, props.fields);
    if (!m) return;

    if (m.kind === 'card') {
      localMapping.titleField = m.titleField;
      localMapping.imageField = m.imageField;
      localMapping.layout = m.layout;
      localMapping.bodyColumns = m.bodyColumns;
      localMapping.fieldOrientation = m.fieldOrientation;
    } else if (m.kind === 'kanban') {
      localMapping.groupField = m.groupField;
      localMapping.titleField = m.titleField;
      localMapping.imageField = m.imageField;
    } else if (m.kind === 'calendar') {
      localMapping.startField = m.startField;
      localMapping.endField = m.endField;
      localMapping.titleField = m.titleField;
      localMapping.colorField = m.colorField;
    } else if (m.kind === 'gantt') {
      localMapping.titleField = m.titleField;
      localMapping.plannedStartField = m.plannedStartField;
      localMapping.plannedEndField = m.plannedEndField;
      localMapping.actualStartField = m.actualStartField;
      localMapping.actualEndField = m.actualEndField;
      localMapping.barColor = m.barColor;
      localMapping.groupField = m.groupField || '';
      barColorShown.value = m.barColor ?? currentPrimaryColor();
    } else if (m.kind === 'map') {
      localMapping.titleField = m.titleField || '';
      localMapping.mapCoordMode = m.coordMode;
      localMapping.mapLngField = m.lngField || '';
      localMapping.mapLatField = m.latField || '';
      localMapping.mapCoordField = m.coordField || '';
      localMapping.mapCoordOrder = m.coordOrder || 'lnglat';
      localMapping.mapCoordSystem = m.coordSystem || 'gcj02';
      localMapping.mapCluster = m.cluster !== false;
      localMapping.mapMaxPoints = m.maxPoints ?? MAP_MAX_POINTS;
      localMapping.mapZoom = m.zoom ?? 5;
      localMapping.mapCenter = m.DefaultCenter || '';
      localMapping.mapCategoryField = m.categoryField || '';
      localMapping.mapCategoryRules = (m.categoryRules || []).map((r) => ({ ...r }));
      localMapping.mapDefaultIcon = m.DefaultIcon || 'local';
      localMapping.mapDefaultColor = m.defaultColor || '#165DFF';
    }
  }

  function syncFromProps() {
    localColumns.value = (props.columns || []).map((c) => ({ ...c }));
    localName.value = props.viewName || '';
    localSort.value = props.sort ? { ...props.sort } : null;
    Object.assign(chrome, DEFAULT_CHROME, props.chrome || {});
    localInsight.value = normalizeInsight(props.insight);
    syncMappingFromProps();
  }

  watch(
    () => props.visible,
    (vis) => {
      if (vis) {
        activeTab.value = 'basic';
        openPanel.value = null;
        syncFromProps();
      }
    },
  );

  watch(
    () =>
      [
        props.columns,
        props.viewName,
        props.sort,
        props.chrome,
        props.mapping,
        props.viewKind,
        props.fields,
        props.insight,
      ] as const,
    () => {
      if (props.visible) syncFromProps();
    },
    { deep: true },
  );

  function commitColumns() {
    arrangeFrozenColumns(localColumns.value);
    emit(
      'update:columns',
      localColumns.value.map((c) => ({ ...c })),
    );
  }

  function displayTitle(col: ColumnPref): string {
    return (col.title?.trim() || props.titles[col.key] || col.key).toString();
  }

  function onTitleEdit(col: ColumnPref, raw: string) {
    const meta = props.titles[col.key] || col.key;
    const next = (raw ?? '').trim();
    if (!next || next === meta) {
      delete col.title;
    } else {
      col.title = next;
    }
    commitColumns();
  }

  function toggleVisible(col: ColumnPref) {
    col.visible = !col.visible;
    commitColumns();
  }

  /** 左冻结：钉住该列并归到最左；再点取消 */
  function toggleFreeze(col: ColumnPref) {
    applyFrozenLeftTo(localColumns.value, col.key);
    commitColumns();
  }

  /** 右冻结：钉住该列并归到最右；再点取消 */
  function toggleFreezeRight(col: ColumnPref) {
    applyFrozenRightTo(localColumns.value, col.key);
    commitColumns();
  }


  function onDragStart(idx: number, e: DragEvent) {
    dragFrom = idx;
    e.dataTransfer?.setData('text/plain', String(idx));
  }

  function onDrop(toIdx: number) {
    if (dragFrom < 0 || dragFrom === toIdx) return;
    const arr = localColumns.value.slice();
    const [item] = arr.splice(dragFrom, 1);
    arr.splice(toIdx, 0, item);
    localColumns.value = arr;
    dragFrom = -1;
    commitColumns();
  }

  function onSortField(
    v: string | number | boolean | Record<string, unknown> | (string | number | boolean | Record<string, unknown>)[],
  ) {
    const field = v == null || v === '' ? '' : String(v);
    if (!field) {
      localSort.value = null;
      emit('update:sort', null);
      return;
    }
    localSort.value = { field, desc: !!localSort.value?.desc };
    emit('update:sort', { ...localSort.value });
  }

  function onSortDir(v: string | number | boolean) {
    if (!localSort.value?.field) return;
    localSort.value = { field: localSort.value.field, desc: String(v) === 'desc' };
    emit('update:sort', { ...localSort.value });
  }

  function emitChrome() {
    emit('update:chrome', { ...chrome });
  }

  /** 洞察开关已迁至页面仪表盘，ViewConfigDrawer 不再写入 insight */
  function emitInsight() {
    /* no-op OSC-2608280e9e */
  }

  // ---- 图表配置（OSC-260819e483 P5）：ViewConfigDrawer 内「配置图表」与 InsightPanel 同一套 chartOption ----
  const chartConfigVisible = ref(false);

  function openChartConfig() {
    chartConfigVisible.value = true;
  }

  function onChartConfigSave(option: unknown) {
    localInsight.value = { ...localInsight.value, chartOption: option };
    emitInsight();
  }

  function onChartConfigClear() {
    localInsight.value = { ...localInsight.value, chartOption: undefined };
    emitInsight();
  }

  function emitMapping() {
    const kind = props.viewKind;
    if (kind === 'table' || kind === 'tree') {
      emit('update:mapping', undefined);
      return;
    }
    if (kind === 'card') {
      if (!localMapping.titleField) return;
      const layout = localMapping.layout;
      const bodyColumns = normalizeCardBodyColumns(localMapping.bodyColumns, layout);
      localMapping.bodyColumns = bodyColumns;
      emit('update:mapping', {
        kind: 'card',
        titleField: localMapping.titleField,
        imageField: localMapping.imageField || undefined,
        layout,
        bodyColumns,
        fieldOrientation: localMapping.fieldOrientation === 'horizontal' ? 'horizontal' : 'vertical',
      });
      return;
    }
    if (kind === 'kanban') {
      if (!localMapping.groupField || !localMapping.titleField) return;
      const prev = props.mapping?.kind === 'kanban' ? props.mapping : undefined;
      const sameGroup = prev?.groupField === localMapping.groupField;
      emit('update:mapping', {
        kind: 'kanban',
        groupField: localMapping.groupField,
        titleField: localMapping.titleField,
        imageField: localMapping.imageField || undefined,
        collapsedColumns: sameGroup ? prev?.collapsedColumns : undefined,
      });
      return;
    }
    if (kind === 'calendar') {
      if (!localMapping.startField || !localMapping.titleField) return;
      emit('update:mapping', {
        kind: 'calendar',
        startField: localMapping.startField,
        endField: localMapping.endField || undefined,
        titleField: localMapping.titleField,
        colorField: localMapping.colorField || undefined,
      });
      return;
    }
    if (kind === 'gantt') {
      if (!localMapping.plannedStartField || !localMapping.plannedEndField || !localMapping.titleField)
        return;
      // 实际字段成对生效：仅配一个视为未配置实际（OSC-0019）
      const actualStart = localMapping.actualStartField || undefined;
      const actualEnd = localMapping.actualEndField || undefined;
      const hasActual = !!(actualStart && actualEnd);
      const prev = props.mapping?.kind === 'gantt' ? props.mapping : undefined;
      emit('update:mapping', {
        kind: 'gantt',
        titleField: localMapping.titleField,
        plannedStartField: localMapping.plannedStartField,
        plannedEndField: localMapping.plannedEndField,
        actualStartField: hasActual ? actualStart : undefined,
        actualEndField: hasActual ? actualEnd : undefined,
        barColor: localMapping.barColor || undefined,
        tableWidth: prev?.tableWidth,
        groupField: localMapping.groupField || '',
      });
      return;
    }
    if (kind === 'map') {
      // 坐标必填：按模式校验（缺失时不提交，保持上次有效配置）
      if (localMapping.mapCoordMode === 'latlng') {
        if (!localMapping.mapLngField || !localMapping.mapLatField) return;
      } else if (!localMapping.mapCoordField) {
        return;
      }
      const prev = props.mapping?.kind === 'map' ? props.mapping : undefined;
      const next: Record<string, unknown> = {
        kind: 'map',
        coordMode: localMapping.mapCoordMode,
        lngField: localMapping.mapLngField || undefined,
        latField: localMapping.mapLatField || undefined,
        coordField: localMapping.mapCoordField || undefined,
        coordOrder: localMapping.mapCoordOrder,
        coordSystem: localMapping.mapCoordSystem,
        titleField: localMapping.titleField || '',
        categoryField: localMapping.mapCategoryField || undefined,
        categoryRules: localMapping.mapCategoryRules.length
          ? localMapping.mapCategoryRules.map((r) => ({ ...r }))
          : undefined,
        DefaultIcon: localMapping.mapDefaultIcon || undefined,
        defaultColor: localMapping.mapDefaultColor || undefined,
        cluster: localMapping.mapCluster,
        maxPoints: localMapping.mapMaxPoints,
        // 默认视野不再提供 UI（旧配置值保留不回写；定位改用「地图中心（名称）」）
        DefaultLocation: prev?.DefaultLocation,
        DefaultCenter: localMapping.mapCenter.trim() || undefined,
        zoom: localMapping.mapZoom,
      };
      const normalized = normalizeMapping('map', next, props.fields);
      if (normalized) emit('update:mapping', normalized);
    }
  }

  function setMapCoordMode(mode: CoordMode) {
    localMapping.mapCoordMode = mode;
    emitMapping();
  }

  /** 分类字段变更（OSC-261004d7f4）：清空后自动列出该字段分类值并预填推荐图标/颜色 */
  let categorySeq = 0;
  async function onMapCategoryChange() {
    const fieldName = localMapping.mapCategoryField;
    localMapping.mapCategoryRules = [];
    if (!fieldName) {
      emitMapping();
      return;
    }
    const seq = ++categorySeq;
    const field = props.fields.find((x) => x.name === fieldName);
    let values: string[];
    if (field?.dataSource) {
      values = Object.keys(field.dataSource);
    } else if (props.loadCategoryValues) {
      values = await props.loadCategoryValues(fieldName);
    } else {
      values = [];
    }
    if (seq !== categorySeq) return; // 已切换到其他字段，本次结果作废
    if (!values.length) {
      emitMapping();
      Message.info('未获取到该字段的分类值，可点「添加分类样式」手动配置');
      return;
    }
    localMapping.mapCategoryRules = buildMapCategoryRules(values, MAP_MARKER_ICONS, mapCategoryPalette);
    if (values.length > MAP_MAX_RULES) Message.warning(`分类值较多，仅展示前 ${MAP_MAX_RULES} 项`);
    emitMapping();
  }

  function addMapRule() {
    if (localMapping.mapCategoryRules.length >= MAP_MAX_RULES) return;
    localMapping.mapCategoryRules = [
      ...localMapping.mapCategoryRules,
      { value: '', icon: localMapping.mapDefaultIcon, color: localMapping.mapDefaultColor },
    ];
  }

  function removeMapRule(idx: number) {
    localMapping.mapCategoryRules = localMapping.mapCategoryRules.filter((_, i) => i !== idx);
    emitMapping();
  }

  function updateMapRule(idx: number, patch: Partial<MapCategoryRule>) {
    localMapping.mapCategoryRules = localMapping.mapCategoryRules.map((r, i) =>
      i === idx ? { ...r, ...patch } : r,
    );
    emitMapping();
  }

  function onMapRuleColorInput(idx: number, value: string) {
    updateMapRule(idx, { color: value });
  }

  function setMapDefaultIcon(icon: string) {
    localMapping.mapDefaultIcon = icon;
    emitMapping();
  }

  function onMapDefaultColorInput(value: string) {
    localMapping.mapDefaultColor = value;
    emitMapping();
  }

  /** 恢复地图默认样式：清空规则 + 默认图标/颜色回落 */
  function resetMapStyle() {
    localMapping.mapCategoryRules = [];
    localMapping.mapDefaultIcon = 'local';
    localMapping.mapDefaultColor = '#165DFF';
    emitMapping();
  }

  /** 当前任务条颜色是否落在预置色板（自定义色块选中态） */
  function isBarPresetActive(): boolean {
    const v = barColorShown.value.toLowerCase();
    return PRESET_THEME_COLORS.some((c) => c.color.toLowerCase() === v);
  }

  /** 点击预置色 → 写入任务条颜色并提交 */
  function pickBarPresetColor(c: PresetThemeColor) {
    barColorShown.value = c.color;
    onBarColorChange();
  }

  /** 打开自定义取色器（隐藏原生 color input，与外观设置一致） */
  function openBarColorPicker() {
    barColorInputRef.value?.click();
  }

  /** 自定义取色器输入 → 写入任务条颜色并提交 */
  function onBarColorInput(e: Event) {
    barColorShown.value = (e.target as HTMLInputElement).value;
    onBarColorChange();
  }

  /** 任务条颜色变化：选回当前主题主色视为未配置（恢复主题色） */
  function onBarColorChange() {
    localMapping.barColor =
      barColorShown.value.toLowerCase() === currentPrimaryColor().toLowerCase()
        ? undefined
        : barColorShown.value;
    emitMapping();
  }

  /** 恢复任务条颜色为当前主题主色（barColor = undefined） */
  function clearBarColor() {
    barColorShown.value = currentPrimaryColor();
    localMapping.barColor = undefined;
    emitMapping();
  }

  function onCardLayoutChange() {
    localMapping.bodyColumns = normalizeCardBodyColumns(localMapping.bodyColumns, localMapping.layout);
    emitMapping();
  }

  function setBodyColumns(cols: CardBodyColumns) {
    if (cols === 3 && localMapping.layout !== 'row') return;
    localMapping.bodyColumns = cols;
    emitMapping();
  }

  function setFieldOrientation(orient: CardFieldOrientation) {
    localMapping.fieldOrientation = orient;
    emitMapping();
  }

  function emitName() {
    const name = localName.value.trim();
    if (name && name !== props.viewName) emit('update:name', name);
  }

  function restoreBgDefault() {
    chrome.bgPreset = 'default';
    chrome.bgColor = DEFAULT_CHROME.bgColor;
    chrome.bgOpacity = DEFAULT_CHROME.bgOpacity;
    chrome.bgBlur = DEFAULT_CHROME.bgBlur;
    emitChrome();
  }

  function isBgSwatchSelected(c: Swatch) {
    if (c.none) return chrome.bgPreset === 'custom' && chrome.bgColor === 'transparent';
    return chrome.bgPreset === 'custom' && chrome.bgColor.toUpperCase() === (c.color || '').toUpperCase();
  }

  function pickBgSwatch(c: Swatch) {
    chrome.bgPreset = 'custom';
    chrome.bgColor = c.none ? 'transparent' : c.color || '#FFFFFF';
    emitChrome();
  }

  function onBgColorPick() {
    chrome.bgPreset = 'custom';
    emitChrome();
  }

  function setWidth(mode: WidthMode) {
    chrome.widthMode = mode;
    emitChrome();
  }

  /** 标准/偏大禁用 3 列；整行三列都可用 */
  function bodyColumnDisabled(n: CardBodyColumns): boolean {
    return n === 3 && localMapping.layout !== 'row';
  }

  function setHeight(mode: HeightMode) {
    chrome.heightMode = mode;
    emitChrome();
  }

  return {
    activeTab,
    openPanel,
    topBarOpen,
    listAreaOpen,
    localColumns,
    localName,
    localSort,
    chrome,
    localInsight,
    localMapping,
    barColorShown,
    barColorInputRef,
    cardLayouts,
    cardBodyColumnOptions,
    bodyColumnDisabled,
    recommendedColors,
    drawerTitle,
    listAreaLabel,
    titleCandidates,
    imageCandidates,
    groupCandidates,
    dateCandidates,
    colorCandidates,
    fieldLabel,
    visibleCount,
    chipStyle,
    bgFilled,
    bgTriggerLabel,
    bgTriggerStyle,
    togglePanel,
    displayTitle,
    onTitleEdit,
    toggleVisible,
    toggleFreeze,
    toggleFreezeRight,
    onDragStart,
    onDrop,
    onSortField,
    onSortDir,
    emitChrome,
    emitInsight,
    emitMapping,
    isBarPresetActive,
    pickBarPresetColor,
    openBarColorPicker,
    onBarColorInput,
    clearBarColor,
    onCardLayoutChange,
    setBodyColumns,
    setFieldOrientation,
    emitName,
    restoreBgDefault,
    isBgSwatchSelected,
    pickBgSwatch,
    onBgColorPick,
    setWidth,
    setHeight,
    chartConfigVisible,
    openChartConfig,
    onChartConfigSave,
    onChartConfigClear,
    mapCoordSystemOptions,
    mapMarkerIcons,
    mapMaxPointsLimit,
    mapMaxRules,
    mapNumericCandidates,
    mapMergedCandidates,
    mapCategoryOptions,
    mapCategoryValueOptions,
    setMapCoordMode,
    onMapCategoryChange,
    addMapRule,
    removeMapRule,
    updateMapRule,
    onMapRuleColorInput,
    setMapDefaultIcon,
    onMapDefaultColorInput,
    resetMapStyle,
  };
}
