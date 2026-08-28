import { computed, reactive, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import type {
  ChartType,
  WidgetInstance,
  WidgetKind,
  WidgetNamedMeta,
  WidgetProvider,
  WidgetSourceItem,
  WidgetWidth,
} from '@cube/api-core';
import cubeApi from '@/api';
import { listWidgets } from './registry';
import { newWidgetId, normalizeSourceRows, normalizeTypePath } from './legacy';
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
    measureFn: 'count' as 'count' | 'sum' | 'avg' | 'min' | 'max',
    measureField: '',
    groupBy: '',
    timeField: '',
    chartType: 'bar' as ChartType,
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
  });

  const showFetchLimit = computed(() => isListFetchKind(draft.kind));
  const limitOptions = DATA_LIST_LIMIT_OPTIONS;
  const fetchAllSelected = computed(() => draft.limit === DATA_LIST_LIMIT_ALL);

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

  const sourceFields = ref<{ name: string; displayName?: string; typeName?: string }[]>([]);

  const numericFields = computed(() =>
    sourceFields.value.filter((f) => /int|long|decimal|double|single|float|byte|short|number/i.test(f.typeName || '')),
  );
  const dateFields = computed(() =>
    sourceFields.value.filter((f) => /date|time/i.test(f.typeName || '')),
  );

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
      if (!isNamed.value) await loadSourceFields(draft.typePath);
      else sourceFields.value = [];
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
        measure: { fn: draft.measureFn, field: draft.measureFn === 'count' ? undefined : draft.measureField },
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
      },
      style: {
        color: draft.color,
        chartType: draft.kind === 'miniChart' ? draft.chartType : undefined,
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
    sourceFields,
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
