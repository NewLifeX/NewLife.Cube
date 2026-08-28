import { computed, reactive, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import type { ChartType, WidgetInstance, WidgetKind, WidgetProvider, WidgetSourceItem } from '@cube/api-core';
import cubeApi from '@/api';
import { listWidgets } from './registry';
import { newWidgetId, normalizeSourceRows, normalizeTypePath } from './legacy';

const KIND_META: { kind: WidgetKind; title: string; hint: string; defaultW: 3 | 4 | 6 | 12 }[] = [
  { kind: 'metricCard', title: '指标卡', hint: '计数 / 求和 / 均值', defaultW: 3 },
  { kind: 'miniChart', title: '迷你图表', hint: '折线 / 柱状 / 条形 / 饼图', defaultW: 6 },
  { kind: 'miniKanban', title: '迷你看板', hint: '只读分组卡片', defaultW: 12 },
];

export interface WidgetConfigDrawerProps {
  visible: boolean;
  editing: WidgetInstance | null;
  hostTypePath?: string;
  hostFields: { name: string; displayName?: string; typeName?: string }[];
  /** insight 页面仪表盘禁用迷你看板 */
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
  const step = ref<'kind' | 'source' | 'fields'>('kind');
  const sources = ref<WidgetSourceItem[]>([]);
  const draft = reactive({
    id: '',
    kind: 'metricCard' as WidgetKind,
    title: '',
    w: 3 as 3 | 4 | 6 | 12,
    typePath: '',
    measureFn: 'count' as 'count' | 'sum' | 'avg' | 'min' | 'max',
    measureField: '',
    groupBy: '',
    timeField: '',
    chartType: 'bar' as ChartType,
    groupField: '',
    titleField: '',
    imageField: '',
    hostField: '',
    sourceField: '',
    color: 'blue',
    badge: '',
  });

  const platformKinds = computed(() =>
    KIND_META.filter((k) => {
      if ((props.surface ?? 'insight') === 'insight' && k.kind === 'miniKanban') return false;
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
    step.value = w ? 'fields' : 'kind';
    draft.id = w?.id || newWidgetId();
    draft.kind = (w?.kind as WidgetKind) || 'metricCard';
    draft.title = w?.title || '';
    draft.w = (w?.layout.w as 3 | 4 | 6 | 12) || 3;
    draft.typePath = w?.source?.typePath || normalizeTypePath(props.hostTypePath);
    draft.measureFn = w?.query?.measure?.fn || 'count';
    draft.measureField = w?.query?.measure?.field || '';
    draft.groupBy = w?.query?.groupBy || '';
    draft.timeField = w?.query?.timeField || '';
    draft.chartType = w?.style?.chartType || 'bar';
    draft.groupField = w?.query?.mapping?.groupField || '';
    draft.titleField = w?.query?.mapping?.titleField || '';
    draft.imageField = w?.query?.mapping?.imageField || '';
    draft.hostField = w?.query?.linkFilter?.[0]?.hostField || '';
    draft.sourceField = w?.query?.linkFilter?.[0]?.sourceField || '';
    draft.color = w?.style?.color || 'blue';
    draft.badge = w?.style?.badge || '';
  }

  watch(
    () => props.visible,
    async (v) => {
      if (!v) return;
      resetFromEditing();
      try {
        const res = await cubeApi.widget.sources();
        // createRequest 已解包为 ApiResponse；亦兼容误解包成数组的情况
        sources.value = normalizeSourceRows(res);
      } catch {
        sources.value = [];
        Message.error('加载数据源失败');
      }
      await loadSourceFields(draft.typePath);
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

  async function pickSource(typePath: string) {
    draft.typePath = normalizeTypePath(typePath);
    await loadSourceFields(draft.typePath);
    step.value = 'fields';
  }

  function providerOf(): WidgetProvider {
    if (draft.kind === 'miniKanban') return 'entity.list';
    return 'entity.aggregate';
  }

  function save() {
    if (draft.kind === 'legacyChart') {
      Message.error('禁止保存旧图表类型');
      return;
    }
    if (props.surface !== 'workbench' && draft.kind === 'miniKanban') {
      Message.error('页面仪表盘不支持迷你看板');
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
      Message.warning('迷你看板需要分组字段和标题字段');
      return;
    }
    const defaultH =
      draft.kind === 'miniChart' || draft.kind === 'miniKanban' ? (3 as const) : (2 as const);
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
        mapping:
          draft.kind === 'miniKanban'
            ? { groupField: draft.groupField, titleField: draft.titleField, imageField: draft.imageField || undefined }
            : undefined,
        linkFilter:
          isCross.value && draft.hostField && draft.sourceField
            ? [{ hostField: draft.hostField, sourceField: draft.sourceField }]
            : undefined,
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
    numericFields,
    dateFields,
    sourceFields,
    pickKind,
    pickSource,
    save,
    cancel,
    KIND_META,
  };
}
