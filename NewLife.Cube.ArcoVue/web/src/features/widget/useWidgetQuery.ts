import { computed, reactive, watch, type Ref } from 'vue';
import { ApiError, type WidgetInstance, type WidgetQueryBody, type WidgetQueryResult } from '@cube/api-core';
import cubeApi from '@/api';
import { getWidget } from './registry';
import { isUnlinkedWidget, normalizeTypePath } from './legacy';
import { readChartItems } from './useMiniChartWidget';
import type { ViewFilter } from '@/core/utils/viewProfile';

/** 归一化 Query 结果，兼容 PascalCase；误传整包 ApiResponse 时再解一层 */
export function normalizeQueryResult(data: unknown): WidgetQueryResult | null {
  if (data == null) return null;
  if (typeof data !== 'object') return { value: data };
  const r = data as Record<string, unknown>;
  if (
    'code' in r &&
    r.data != null &&
    typeof r.data === 'object' &&
    !Array.isArray(r.data)
  ) {
    const inner = r.data as Record<string, unknown>;
    if (
      'items' in inner ||
      'Items' in inner ||
      'value' in inner ||
      'Value' in inner ||
      'rows' in inner ||
      'Rows' in inner
    ) {
      return normalizeQueryResult(inner);
    }
  }
  const rows = r.rows ?? r.Rows;
  return {
    value: r.value ?? r.Value,
    items: readChartItems(r),
    rows: Array.isArray(rows) ? (rows as Record<string, unknown>[]) : undefined,
    hostFilterApplied: Boolean(r.hostFilterApplied ?? r.HostFilterApplied),
  };
}

export interface WidgetQueryState {
  loading: boolean;
  error: string;
  locked: boolean;
  result: WidgetQueryResult | null;
  unlinked: boolean;
}

function emptyState(unlinked: boolean): WidgetQueryState {
  return { loading: false, error: '', locked: false, result: null, unlinked };
}

function hostValuesFromFilter(
  filter: ViewFilter | null | undefined,
  widget: WidgetInstance,
): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  const links = widget.query?.linkFilter ?? [];
  const conds = filter?.conditions ?? [];
  for (const link of links) {
    const hit = conds.find((c) => c.field === link.hostField && (c.op === 'eq' || !c.op));
    if (hit && hit.value !== undefined) out[link.hostField] = hit.value;
  }
  return out;
}

export function buildQueryBody(
  widget: WidgetInstance,
  hostTypePath: string | undefined,
  hostFilter: ViewFilter | null,
): WidgetQueryBody | null {
  const typePath = normalizeTypePath(widget.source?.typePath);
  if (!typePath) return null;
  const kind = widget.kind;
  const mode: 'aggregate' | 'list' = kind === 'miniKanban' ? 'list' : 'aggregate';
  const q = widget.query ?? {};
  return {
    mode,
    typePath,
    measure: q.measure,
    groupBy: q.groupBy,
    timeField: q.timeField,
    buckets: q.buckets,
    limit: q.limit,
    extraFilter: q.extraFilter,
    hostTypePath: hostTypePath ? normalizeTypePath(hostTypePath) : undefined,
    hostFilter: hostFilter ?? undefined,
    linkFilter: q.linkFilter,
    hostValues: hostValuesFromFilter(hostFilter, widget),
  };
}

export function shouldQueryWidget(widget: WidgetInstance): boolean {
  if (widget.kind === 'legacyChart') return false;
  const def = getWidget(widget.kind);
  if (!def) return false;
  // named → /Cube/Widget/Data；entity.* → Query
  if (widget.source?.provider === 'named') return !!widget.source.widgetName;
  return true;
}

/** 请求指纹：避免 deep watch 在 surface 重写/空筛选对象换引用时重复打 Query */
function queryFingerprint(
  widgets: WidgetInstance[],
  hostTypePath: string | undefined,
  hostFilter: ViewFilter | null,
): string {
  return JSON.stringify({
    host: normalizeTypePath(hostTypePath),
    filter: hostFilter ?? null,
    widgets: widgets.map((w) => ({
      id: w.id,
      kind: w.kind,
      source: w.source,
      query: w.query,
      style: w.style,
    })),
  });
}

export function useWidgetQuery(
  widgets: Ref<WidgetInstance[]>,
  hostTypePath: Ref<string | undefined>,
  hostFilter: Ref<ViewFilter | null>,
) {
  const states = reactive<Record<string, WidgetQueryState>>({});
  let lastFingerprint = '';
  let refreshSeq = 0;

  async function refresh() {
    const fp = queryFingerprint(widgets.value, hostTypePath.value, hostFilter.value);
    if (fp === lastFingerprint) return;
    lastFingerprint = fp;
    const seq = ++refreshSeq;
    const list = widgets.value;
    const ids = new Set(list.map((w) => w.id));
    for (const id of Object.keys(states)) {
      if (!ids.has(id)) delete states[id];
    }
    await Promise.all(
      list.map(async (w) => {
        const unlinked = isUnlinkedWidget(w, hostTypePath.value);
        if (!shouldQueryWidget(w)) {
          if (seq === refreshSeq) states[w.id] = emptyState(unlinked);
          return;
        }
        if (w.source?.provider === 'named' && w.source.widgetName) {
          if (seq === refreshSeq) states[w.id] = { ...emptyState(unlinked), loading: true };
          try {
            const res = await cubeApi.widget.data(w.source.widgetName, {
              hostTypePath: hostTypePath.value,
            });
            if (seq !== refreshSeq) return;
            states[w.id] = {
              loading: false,
              error: '',
              locked: false,
              result: normalizeQueryResult(res.data) ?? { value: res.data },
              unlinked,
            };
          } catch (err) {
            if (seq !== refreshSeq) return;
            const code = err instanceof ApiError ? err.code : 0;
            states[w.id] = {
              loading: false,
              error: err instanceof Error ? err.message : '加载失败',
              locked: code === 403,
              result: null,
              unlinked,
            };
          }
          return;
        }
        const body = buildQueryBody(w, hostTypePath.value, hostFilter.value);
        if (!body) {
          if (seq === refreshSeq) states[w.id] = emptyState(unlinked);
          return;
        }
        if (seq === refreshSeq) states[w.id] = { ...emptyState(unlinked), loading: true };
        try {
          const res = await cubeApi.widget.query(body);
          if (seq !== refreshSeq) return;
          const result = normalizeQueryResult(res.data);
          states[w.id] = {
            loading: false,
            error: '',
            locked: false,
            result,
            unlinked:
              unlinked ||
              (result?.hostFilterApplied === false && isUnlinkedWidget(w, hostTypePath.value)),
          };
        } catch (err) {
          if (seq !== refreshSeq) return;
          const code = err instanceof ApiError ? err.code : 0;
          states[w.id] = {
            loading: false,
            error: err instanceof Error ? err.message : '加载失败',
            locked: code === 403,
            result: null,
            unlinked,
          };
        }
      }),
    );
  }

  watch(
    [widgets, hostTypePath, hostFilter],
    () => {
      void refresh();
    },
    { deep: true, immediate: true },
  );

  const byId = computed(() => states);
  return { states: byId, refresh };
}
