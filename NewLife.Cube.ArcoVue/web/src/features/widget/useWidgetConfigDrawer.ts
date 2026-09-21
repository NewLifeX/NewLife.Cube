import { computed, reactive, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import type {
  ChartType,
  MeasureFn,
  WidgetChartOptions,
  WidgetInstance,
  WidgetKind,
  WidgetMeasure,
  WidgetNamedMeta,
  WidgetProvider,
  WidgetSortBy,
  WidgetSortOrder,
  WidgetSourceItem,
  WidgetWidth,
} from '@newlifex/api-core';
import cubeApi from '@/api';
import type { FieldMeta } from '@/core/types/field';
import { emptyViewFilter, normalizeFilter, type ViewFilter } from '@/core/utils/viewProfile';
import { listWidgets } from './registry';
import { newWidgetId, normalizeSourceRows, normalizeTypePath } from './legacy';
import { loadEntityFilterFields } from './listFieldMeta';
import { isNumericTypeName, measureFnOptionsFor, MEASURE_FN_LABELS, resolveChartMeasures } from './chartTemplates';
import {
  DATA_LIST_LIMIT_ALL,
  DATA_LIST_LIMIT_DEFAULT,
  DATA_LIST_LIMIT_OPTIONS,
  formatDataListLimitLabel,
  normalizeDataListLimit,
} from './dataListViewport';
import { resolveWorkbenchIcon, WORKBENCH_NAMED_ICONS } from '@/core/utils/workbench';

function isListFetchKind(kind: WidgetKind): boolean {
  return kind === 'dataList' || kind === 'dataCard' || kind === 'miniKanban';
}

const KIND_META: {
  kind: WidgetKind;
  title: string;
  hint: string;
  defaultW: WidgetWidth;
  icon: string;
}[] = [
  { kind: 'metricCard', title: '指标卡', hint: '计数 / 求和 / 均值', defaultW: 3, icon: 'dashboard' },
  { kind: 'miniChart', title: '迷你图表', hint: '折线 / 柱状 / 条形 / 饼图', defaultW: 6, icon: 'chart-histogram' },
  { kind: 'miniKanban', title: '数据看板', hint: '只读分组卡片', defaultW: 6, icon: 'blackboard' },
  { kind: 'dataList', title: '数据列表', hint: '实体记录表格', defaultW: 6, icon: 'list' },
  { kind: 'dataCard', title: '数据卡片', hint: '单行卡片，左右切换', defaultW: 6, icon: 'application' },
];

export interface WidgetConfigDrawerProps {
  visible: boolean;
  editing: WidgetInstance | null;
  hostTypePath?: string;
  hostFields: { name: string; displayName?: string; typeName?: string }[];
  /** 宿主页 search∪list 字段候选（OSC-260903e2a4：实体页洞察槽注入；工作台无） */
  hostFilterFields?: FieldMeta[];
  /** insight 页面仪表盘仅指标卡 / 迷你图表（禁用看板、列表、卡片） */
  surface?: 'insight' | 'workbench';
}

export interface WidgetConfigDrawerEmits {
  'update:visible': [boolean];
  save: [widget: WidgetInstance];
}

export function useWidgetConfigDrawer(
  props: WidgetConfigDrawerProps,
  emit: (e: 'update:visible' | 'save', ...args: unknown[]) => void,
) {
  const step = ref<'named' | 'kind' | 'source' | 'fields'>('kind');
  const namedList = ref<WidgetNamedMeta[]>([]);
  const wbTab = ref<'named' | 'entity'>('named');
  const sources = ref<WidgetSourceItem[]>([]);
  const isWorkbench = computed(() => props.surface === 'workbench');
  const draft = reactive({
    id: '',
    kind: 'metricCard' as WidgetKind,
    title: '',
    w: 3 as WidgetWidth,
    typePath: '',
    provider: 'entity.aggregate' as WidgetProvider,
    widgetName: '',
    measureFn: 'count' as MeasureFn,
    measureField: '',
    groupBy: '',
    timeField: '',
    chartType: 'bar' as ChartType,
    /** 纵轴模式（OSC-260920）：count=统计记录总数（仅计数）；field=统计字段数值（计数/求和/最大/最小/平均） */
    measureMode: 'count' as 'count' | 'field',
    /** 选择字段行（OSC-260920）：多统计字段；count 模式下空字段=记录总数 Count(*) */
    measures: [] as { fn: MeasureFn; field: string }[],
    /** 排序依据/方向（OSC-260920，仅分组聚合生效） */
    sortBy: 'y' as WidgetSortBy,
    sortOrder: 'desc' as WidgetSortOrder,
    /** 图表显示选项（OSC-260920）：图例/数据标签/坐标轴/网格线 */
    chartOptions: { legend: false, dataLabel: true, axis: true, gridLine: false } as Required<WidgetChartOptions>,
    groupField: '',
    titleField: '',
    imageField: '',
    /** 数据看板 / 列表 / 卡片正文显示字段 */
    displayFields: [] as string[],
    /** 列表类部件一次从后端拉取的条数 */
    limit: DATA_LIST_LIMIT_DEFAULT as number,
    hostField: '',
    sourceField: '',
    color: 'blue',
    badge: '',
    icon: '',
    /** 部件查询条件（OSC-260903e2a4）：存 query.extraFilter；null=无条件 */
    extraFilter: null as ViewFilter | null,
  });

  const showFetchLimit = computed(() => isListFetchKind(draft.kind));
  const limitOptions = DATA_LIST_LIMIT_OPTIONS;
  const fetchAllSelected = computed(() => draft.limit === DATA_LIST_LIMIT_ALL);

  /** 条件编辑器候选：源=宿主时用宿主页候选；跨源/工作台懒加载源实体 search∪list */
  const filterCandidatesLoaded = ref<FieldMeta[]>([]);
  const isSameHost = computed(() => {
    const host = normalizeTypePath(props.hostTypePath);
    const src = normalizeTypePath(draft.typePath);
    return !!host && !!src && host.toLowerCase() === src.toLowerCase();
  });
  const filterCandidates = computed<FieldMeta[]>(() => {
    if (isSameHost.value && (props.hostFilterFields ?? []).length) return props.hostFilterFields!;
    return filterCandidatesLoaded.value;
  });
  /** 宿主字段候选（FilterBuilderPopover hostFields）：工作台无宿主概念 → 空（隐藏宿主引用） */
  const hostEditorFields = computed<FieldMeta[]>(() =>
    props.surface === 'workbench' ? [] : (props.hostFilterFields ?? []),
  );
  /** 条件编辑器 modelValue（无条件下传空方案） */
  const filterModel = computed<ViewFilter>(() =>
    draft.extraFilter && draft.extraFilter.conditions.length
      ? draft.extraFilter
      : emptyViewFilter(),
  );
  const filterCondCount = computed(() => draft.extraFilter?.conditions?.length || 0);
  const filterEditorVisible = ref(false);

  async function loadFilterCandidates() {
    const tp = normalizeTypePath(draft.typePath);
    if (!tp) {
      filterCandidatesLoaded.value = [];
      return;
    }
    // 源=宿主且宿主候选已注入：不重复请求
    if (isSameHost.value && (props.hostFilterFields ?? []).length) return;
    filterCandidatesLoaded.value = await loadEntityFilterFields(tp);
  }

  function onFilterApply(f: ViewFilter) {
    draft.extraFilter = f && f.conditions.length ? f : null;
  }

  function clearWidgetFilter() {
    draft.extraFilter = null;
  }

  const isNamed = computed(
    () => draft.provider === 'named' || !!draft.widgetName,
  );

  const platformKinds = computed(() =>
    KIND_META.filter((k) => {
      if ((props.surface ?? 'insight') === 'insight') {
        if (k.kind === 'miniKanban' || k.kind === 'dataList' || k.kind === 'dataCard') return false;
      }
      return listWidgets().some((d) => d.kind === k.kind) || true;
    }),
  );

  const sourceOptions = computed(() => {
    const host = normalizeTypePath(props.hostTypePath);
    const byKey = new Map<string, WidgetSourceItem>();
    for (const s of sources.value) {
      const tp = normalizeTypePath(s.typePath);
      if (!tp) continue;
      byKey.set(tp.toLowerCase(), { ...s, typePath: tp });
    }
    if (host && !byKey.has(host.toLowerCase())) {
      const short = host.includes('/') ? host.slice(host.lastIndexOf('/') + 1) : host;
      byKey.set(host.toLowerCase(), { typePath: host, displayName: short, name: short });
    }
    const top = host ? byKey.get(host.toLowerCase()) : undefined;
    const rest = [...byKey.values()]
      .filter((s) => normalizeTypePath(s.typePath).toLowerCase() !== host.toLowerCase())
      .sort((a, b) => (a.displayName || a.typePath).localeCompare(b.displayName || b.typePath, 'zh'));
    return top ? [top, ...rest] : rest;
  });

  const isCross = computed(() => {
    const host = normalizeTypePath(props.hostTypePath);
    const src = normalizeTypePath(draft.typePath);
    return !!host && !!src && host.toLowerCase() !== src.toLowerCase();
  });

  const sourceFields = ref<{ name: string; displayName?: string; typeName?: string; primaryKey?: boolean }[]>([]);

  // 数值字段（排除主键/身份列，与后端 ResolveNumeric 一致）：指标卡「数值字段」候选
  const numericFields = computed(() =>
    sourceFields.value.filter((f) => !f.primaryKey && isNumericTypeName(f.typeName)),
  );
  const dateFields = computed(() =>
    sourceFields.value.filter((f) => /date|time/i.test(f.typeName || '')),
  );

  /** 度量行上限（与后端 WidgetQueryService.MaxMeasures 对齐） */
  const MAX_CHART_MEASURES = 5;
  const canAddMeasure = computed(() => draft.measures.length < MAX_CHART_MEASURES);
  /** 图表显示选项：饼图/迷你折线不出坐标轴与网格线 */
  const showAxisOptions = computed(
    () => draft.chartType !== 'pie' && draft.chartType !== 'sparkline',
  );
  /** 排序控件：仅分类轴图表（柱状/条形/饼图）有意义，时间轴恒定按时间正序 */
  const showChartSort = computed(
    () => draft.chartType === 'bar' || draft.chartType === 'hbar' || draft.chartType === 'pie',
  );

  /** 行可用统计方式（OSC-260921）：数值非主键字段 5 种；文本/日期/时间/编号类仅计数 */
  function measureFnOptions(name: string): MeasureFn[] {
    return measureFnOptionsFor(sourceFields.value.find((f) => f.name === name));
  }

  /** 字段是否可用于求和/最大/最小/平均（数值且非主键） */
  function isAggregatableNumeric(name: string) {
    return measureFnOptions(name).includes('sum');
  }

  /** 字段变更后回落非法统计方式（如数值→文本时 求和→计数） */
  function onMeasureFieldChange(index: number) {
    const row = draft.measures[index];
    if (!row) return;
    if (!measureFnOptions(row.field).includes(row.fn)) row.fn = 'count';
  }

  /** 新增一行统计字段（优先未使用的字段；数值字段默认求和，其余默认计数） */
  function addMeasureRow() {
    if (!canAddMeasure.value) return;
    const used = new Set(draft.measures.map((r) => r.field));
    const next = sourceFields.value.find((f) => !used.has(f.name));
    const fn: MeasureFn =
      draft.measureMode === 'count' ? 'count' : next && isAggregatableNumeric(next.name) ? 'sum' : 'count';
    draft.measures.push({ fn, field: next?.name || '' });
  }

  function removeMeasureRow(index: number) {
    draft.measures.splice(index, 1);
  }

  // 切换纵轴模式：计数模式强制计数；字段数值模式下不支持的统计方式回落为计数，避免保存非法组合
  watch(
    () => draft.measureMode,
    (mode) => {
      if (mode === 'count') draft.measures.forEach((r) => (r.fn = 'count'));
      else
        draft.measures.forEach((r) => {
          if (!measureFnOptions(r.field).includes(r.fn)) r.fn = 'count';
        });
    },
  );

  /** 序列显示名：字段显示名（函数） */
  function measureLabel(fn: MeasureFn, field: string) {
    const dn = (sourceFields.value.find((f) => f.name === field)?.displayName || '').trim() || field;
    return `${dn}（${MEASURE_FN_LABELS[fn]}）`;
  }

  /** 保存用度量（OSC-260920）：count 模式统一为计数（空=Count(*)）；field 模式携带序列名 */
  function buildChartMeasures(): WidgetMeasure[] {
    const rows = draft.measures
      .map((r) => ({ fn: r.fn, field: (r.field || '').trim() }))
      .filter((r) => r.field);
    if (draft.measureMode === 'count') {
      if (!rows.length) return [{ fn: 'count' }];
      return rows.map((r) => ({ fn: 'count' as MeasureFn, field: r.field, label: measureLabel('count', r.field) }));
    }
    return rows
      .slice(0, MAX_CHART_MEASURES)
      .map((r) => ({ fn: r.fn, field: r.field, label: measureLabel(r.fn, r.field) }));
  }

  function resetFromEditing() {
    const w = props.editing;
    if (w) step.value = 'fields';
    else if (isWorkbench.value) {
      step.value = 'named';
      wbTab.value = 'named';
    } else step.value = 'kind';
    draft.id = w?.id || newWidgetId();
    draft.kind = (w?.kind as WidgetKind) || 'metricCard';
    draft.title = w?.title || '';
    draft.w = (w?.layout.w as WidgetWidth) || 3;
    draft.provider = (w?.source?.provider as WidgetProvider) || 'entity.aggregate';
    draft.widgetName = w?.source?.widgetName || '';
    draft.typePath = w?.source?.typePath || normalizeTypePath(props.hostTypePath);
    draft.measureFn = w?.query?.measure?.fn || 'count';
    draft.measureField = w?.query?.measure?.field || '';
    draft.groupBy = w?.query?.groupBy || '';
    draft.timeField = w?.query?.timeField || '';
    draft.chartType = w?.style?.chartType || 'bar';
    // 多统计度量（OSC-260920）：measures 优先，legacy measure 迁移；默认 Count(*) 不占字段行
    const ms = resolveChartMeasures(w?.query);
    draft.measures = ms
      .filter((m) => (m.field || m.fn !== 'count') && m.fn !== undefined)
      .map((m) => ({ fn: m.fn, field: (m.field || '').trim() }));
    draft.measureMode = ms.every((m) => m.fn === 'count') ? 'count' : 'field';
    draft.sortBy = (w?.query?.sortBy as WidgetSortBy) || 'y';
    draft.sortOrder = (w?.query?.sortOrder as WidgetSortOrder) || 'desc';
    const co = w?.style?.chartOptions;
    draft.chartOptions = {
      legend: co?.legend === true,
      dataLabel: co?.dataLabel !== false,
      axis: co?.axis !== false,
      gridLine: co?.gridLine === true,
    };
    draft.groupField = w?.query?.mapping?.groupField || '';
    draft.titleField = w?.query?.mapping?.titleField || '';
    draft.imageField = w?.query?.mapping?.imageField || '';
    draft.displayFields = Array.isArray(w?.query?.mapping?.fields)
      ? [...w!.query!.mapping!.fields!]
      : [];
    draft.limit = normalizeDataListLimit(w?.query?.limit);
    draft.hostField = w?.query?.linkFilter?.[0]?.hostField || '';
    draft.sourceField = w?.query?.linkFilter?.[0]?.sourceField || '';
    draft.color = w?.style?.color || 'blue';
    draft.badge = w?.style?.badge || '';
    draft.icon = w?.style?.icon || '';
    const wf = (w?.query?.extraFilter ?? null) as ViewFilter | null | undefined;
    draft.extraFilter =
      wf && Array.isArray(wf.conditions) && wf.conditions.length ? normalizeFilter(wf) : null;
  }

  watch(
    () => props.visible,
    async (v) => {
      if (!v) return;
      resetFromEditing();
      try {
        const res = await cubeApi.widget.sources();
        sources.value = normalizeSourceRows(res);
      } catch {
        sources.value = [];
        Message.error('加载数据源失败');
      }
      if (isWorkbench.value) {
        try {
          const cat = await cubeApi.widget.catalog('workbench');
          const payload = ((cat as { data?: unknown }).data ?? cat) as Record<string, unknown>;
          const named = payload?.named ?? payload?.Named;
          namedList.value = Array.isArray(named)
            ? named.map((n) => {
                const o = n as Record<string, unknown>;
                return {
                  name: String(o.name ?? o.Name ?? ''),
                  title: String(o.title ?? o.Title ?? ''),
                  kind: String(o.kind ?? o.Kind ?? 'metricCard'),
                  cols: Number(o.cols ?? o.Cols ?? 3) || 3,
                  adminOnly: Boolean(o.adminOnly ?? o.AdminOnly),
                  surfaces: String(o.surfaces ?? o.Surfaces ?? ''),
                  color: String(o.color ?? o.Color ?? ''),
                  icon: String(o.icon ?? o.Icon ?? ''),
                };
              })
            : [];
        } catch {
          namedList.value = [];
        }
      }
      if (!isNamed.value) {
        await loadSourceFields(draft.typePath);
        await loadFilterCandidates();
      } else {
        sourceFields.value = [];
        filterCandidatesLoaded.value = [];
      }
    },
    { immediate: true },
  );

  async function loadSourceFields(typePath: string) {
    if (!typePath) {
      sourceFields.value = [];
      return;
    }
    try {
      const res = await cubeApi.automation.meta(typePath);
      sourceFields.value = (res.data ?? []).map((f) => ({
        name: f.name,
        displayName: f.displayName,
        typeName: f.typeName,
        // 主键/身份列后端拒绝 sum/avg/min/max（OSC-260921），保留标志用于统计方式限制
        primaryKey: f.primaryKey,
      }));
    } catch {
      sourceFields.value = props.hostFields;
    }
  }

  function pickKind(kind: WidgetKind) {
    draft.kind = kind;
    const meta = KIND_META.find((k) => k.kind === kind);
    if (meta) draft.w = meta.defaultW;
    if (!draft.title) draft.title = meta?.title || '';
    step.value = 'source';
  }

  function pickNamed(n: WidgetNamedMeta) {
    const w = (n.cols === 2 || n.cols === 3 || n.cols === 4 || n.cols === 6 || n.cols === 8 || n.cols === 12
      ? n.cols
      : 3) as WidgetWidth;
    const h =
      n.kind === 'monitorChart' || n.kind === 'quickLinks'
        ? 3
        : n.kind === 'metricCard'
          ? 1
          : 2;
    const inst: WidgetInstance = {
      id: newWidgetId(),
      kind: n.kind,
      title: (n.title || n.name).slice(0, 40),
      layout: { w, h: h as 1 | 2 | 3 | 4, order: 99 },
      source: { provider: 'named', widgetName: n.name },
      query: {},
      style: {
        icon: n.icon || WORKBENCH_NAMED_ICONS[n.name],
        color: n.color || undefined,
      },
    };
    emit('save', inst);
    emit('update:visible', false);
  }

  function startEntityKinds() {
    step.value = 'kind';
  }

  async function pickSource(typePath: string) {
    draft.typePath = normalizeTypePath(typePath);
    await loadSourceFields(draft.typePath);
    await loadFilterCandidates();
    step.value = 'fields';
  }

  function providerOf(): WidgetProvider {
    if (
      draft.kind === 'miniKanban' ||
      draft.kind === 'dataList' ||
      draft.kind === 'dataCard'
    ) {
      return 'entity.list';
    }
    return 'entity.aggregate';
  }

  function normalizeDisplayFields(): string[] {
    return draft.displayFields
      .map((x) => x.trim())
      .filter(Boolean)
      .filter(
        (x, i, arr) =>
          arr.indexOf(x) === i &&
          x !== draft.groupField &&
          x !== draft.titleField &&
          x !== draft.imageField,
      )
      .slice(0, 8);
  }

  function save() {
    if (draft.kind === 'legacyChart') {
      Message.error('禁止保存旧图表类型');
      return;
    }
    if (props.surface !== 'workbench' && draft.kind === 'miniKanban') {
      Message.error('页面仪表盘不支持数据看板');
      return;
    }
    if (props.surface !== 'workbench' && draft.kind === 'dataList') {
      Message.error('页面仪表盘不支持数据列表');
      return;
    }
    if (props.surface !== 'workbench' && draft.kind === 'dataCard') {
      Message.error('页面仪表盘不支持数据卡片');
      return;
    }

    // 平台 named：只改标题/宽度，不要求数据源
    if (isNamed.value) {
      if (!draft.widgetName) {
        Message.warning('缺少平台部件名称');
        return;
      }
      const namedH =
        draft.kind === 'monitorChart' || draft.kind === 'quickLinks'
          ? (3 as const)
          : draft.kind === 'metricCard'
            ? (1 as const)
            : (2 as const);
      const namedInst: WidgetInstance = {
        id: draft.id || newWidgetId(),
        kind: draft.kind,
        title: (draft.title || '未命名').slice(0, 40),
        layout: {
          w: draft.w,
          h: props.editing?.layout.h ?? namedH,
          order: props.editing?.layout.order ?? 99,
        },
        source: { provider: 'named', widgetName: draft.widgetName },
        query: { ...(props.editing?.query ?? {}) },
        style: {
          ...(props.editing?.style ?? {}),
          icon: draft.icon || props.editing?.style?.icon || WORKBENCH_NAMED_ICONS[draft.widgetName],
          color: draft.color || props.editing?.style?.color,
        },
      };
      emit('save', namedInst);
      emit('update:visible', false);
      return;
    }

    if (!draft.typePath) {
      Message.warning('请选择数据源');
      return;
    }
    if (draft.kind === 'miniChart') {
      if (
        (draft.chartType === 'bar' || draft.chartType === 'hbar' || draft.chartType === 'pie') &&
        !draft.groupBy
      ) {
        Message.warning('柱状/条形/饼图需要分组字段');
        return;
      }
      if ((draft.chartType === 'sparkline' || draft.chartType === 'line') && !draft.timeField) {
        Message.warning('折线需要时间字段');
        return;
      }
      // 纵轴：字段数值模式至少一个字段；非计数方式必须为数值（且非主键）字段（OSC-260920/260921）
      if (draft.measureMode === 'field') {
        const rows = draft.measures.filter((r) => (r.field || '').trim());
        if (!rows.length) {
          Message.warning('请至少选择一个统计字段');
          return;
        }
        const bad = rows.find((r) => r.fn !== 'count' && !isAggregatableNumeric(r.field));
        if (bad) {
          Message.warning(`字段「${bad.field}」不支持该统计方式，请改用计数或更换字段`);
          return;
        }
      }
    }
    if (draft.kind === 'miniKanban' && (!draft.groupField || !draft.titleField)) {
      Message.warning('数据看板需要分组字段和标题字段');
      return;
    }
    if (draft.kind === 'dataCard' && !draft.titleField) {
      Message.warning('数据卡片需要标题字段');
      return;
    }
    const defaultH =
      draft.kind === 'dataList'
        ? (4 as const)
        : draft.kind === 'miniChart' || draft.kind === 'miniKanban' || draft.kind === 'dataCard'
          ? (3 as const)
          : (2 as const);
    const inst: WidgetInstance = {
      id: draft.id || newWidgetId(),
      kind: draft.kind,
      title: (draft.title || '未命名').slice(0, 40),
      layout: {
        w: draft.w,
        h: props.editing?.layout.h ?? defaultH,
        order: props.editing?.layout.order ?? 99,
      },
      source: { provider: providerOf(), typePath: draft.typePath },
      query: {
        // 迷你图表用多度量 measures（OSC-260920）；其余部件保持单值 measure
        measure:
          draft.kind === 'miniChart'
            ? undefined
            : { fn: draft.measureFn, field: draft.measureFn === 'count' ? undefined : draft.measureField },
        measures: draft.kind === 'miniChart' ? buildChartMeasures() : undefined,
        sortBy: draft.kind === 'miniChart' ? draft.sortBy : undefined,
        sortOrder: draft.kind === 'miniChart' ? draft.sortOrder : undefined,
        groupBy: draft.groupBy || undefined,
        timeField: draft.timeField || undefined,
        // 列表/卡片/看板：一次拉取条数；默认 30，上限 300，-1=全部
        limit: isListFetchKind(draft.kind)
          ? normalizeDataListLimit(draft.limit)
          : props.editing?.query?.limit,
        mapping:
          draft.kind === 'miniKanban'
            ? {
                groupField: draft.groupField,
                titleField: draft.titleField,
                imageField: draft.imageField || undefined,
                fields: normalizeDisplayFields(),
              }
            : draft.kind === 'dataList'
              ? { fields: normalizeDisplayFields() }
              : draft.kind === 'dataCard'
                ? {
                    titleField: draft.titleField,
                    imageField: draft.imageField || undefined,
                    fields: normalizeDisplayFields(),
                  }
                : undefined,
        linkFilter:
          isWorkbench.value || !isCross.value || !draft.hostField || !draft.sourceField
            ? undefined
            : [{ hostField: draft.hostField, sourceField: draft.sourceField }],
        // 部件查询条件（OSC-260903e2a4）：合并保留，避免重建 query 丢失既有 extraFilter
        extraFilter:
          draft.extraFilter && draft.extraFilter.conditions.length
            ? draft.extraFilter
            : undefined,
      },
      style: {
        color: draft.color,
        chartType: draft.kind === 'miniChart' ? draft.chartType : undefined,
        chartOptions: draft.kind === 'miniChart' ? { ...draft.chartOptions } : undefined,
        badge:
          draft.kind === 'metricCard' && draft.badge.trim()
            ? draft.badge.trim().slice(0, 12)
            : undefined,
      },
    };
    emit('save', inst);
    emit('update:visible', false);
  }

  function cancel() {
    emit('update:visible', false);
  }

  return {
    step,
    draft,
    platformKinds,
    sourceOptions,
    isCross,
    isWorkbench,
    isNamed,
    namedList,
    wbTab,
    numericFields,
    dateFields,
    MAX_CHART_MEASURES,
    canAddMeasure,
    showAxisOptions,
    showChartSort,
    measureFnOptions,
    onMeasureFieldChange,
    addMeasureRow,
    removeMeasureRow,
    sourceFields,
    filterCandidates,
    hostEditorFields,
    filterModel,
    filterCondCount,
    filterEditorVisible,
    onFilterApply,
    clearWidgetFilter,
    showFetchLimit,
    limitOptions,
    fetchAllSelected,
    formatDataListLimitLabel,
    pickKind,
    pickNamed,
    startEntityKinds,
    pickSource,
    save,
    cancel,
    KIND_META,
    resolveWorkbenchIcon,
  };
}
