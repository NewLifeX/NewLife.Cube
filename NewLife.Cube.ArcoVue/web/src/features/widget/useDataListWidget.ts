import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import type { FieldMeta } from '@/core/types/field';
import { selectListColumns } from '@/core/utils/listColumns';
import { resolveCellLabel } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';
import type { WidgetQueryResult } from '@cube/api-core';
import type { WidgetCardProps } from './context';
import { rotateDataListWindow, visibleDataListRows } from './dataListViewport';
import { normalizeTypePath } from './legacy';
import { fieldLabelOf, findFieldMeta, loadEntityListFields } from './listFieldMeta';
import { useAutoStep } from './useAutoStep';
import { normalizeKanbanDisplayFields } from './useMiniKanbanWidget';

const DISPLAY_MAX = 8;

function readRows(result: unknown): Record<string, unknown>[] {
  if (!result || typeof result !== 'object') return [];
  const r = result as WidgetQueryResult & { Rows?: Record<string, unknown>[] };
  const rows = r.rows ?? r.Rows;
  return Array.isArray(rows) ? rows : [];
}

export function useDataListWidget(props: WidgetCardProps) {
  const router = useRouter();
  const metaFields = ref<FieldMeta[]>([]);

  watch(
    () => props.widget.source?.typePath,
    (tp) => {
      void loadEntityListFields(tp).then((list) => {
        metaFields.value = list;
      });
    },
    { immediate: true },
  );

  const rows = computed(() => readRows(props.result));
  const visibleRows = computed(() => visibleDataListRows(props.widget.layout?.h));
  const shouldAutoScroll = computed(() => rows.value.length > visibleRows.value);
  const windowOffset = ref(0);
  const displayRows = computed(() =>
    rotateDataListWindow(rows.value, windowOffset.value, visibleRows.value),
  );

  function scrollStep() {
    if (!shouldAutoScroll.value) return;
    const n = rows.value.length;
    if (n <= 0) return;
    windowOffset.value = (windowOffset.value + 1) % n;
  }

  useAutoStep(shouldAutoScroll, scrollStep);

  watch(
    () => [rows.value.length, visibleRows.value] as const,
    () => {
      windowOffset.value = 0;
    },
  );

  const columnKeys = computed(() => {
    const configured = normalizeKanbanDisplayFields(
      props.widget.query?.mapping?.fields,
      {},
      DISPLAY_MAX,
    );
    if (configured.length) return configured;
    const fromMeta = selectListColumns(metaFields.value)
      .map((f) => f.name)
      .slice(0, DISPLAY_MAX);
    if (fromMeta.length) return fromMeta;
    // 元数据未到时，用首行键兜底，避免有数据却无列导致白板
    const first = rows.value[0];
    if (!first) return [];
    return Object.keys(first)
      .filter((k) => !/^_/i.test(k))
      .slice(0, DISPLAY_MAX);
  });

  const columns = computed(() =>
    columnKeys.value.map((name) => {
      const hit = findFieldMeta(metaFields.value, name);
      return {
        key: hit?.name || name,
        title: fieldLabelOf(metaFields.value, name),
        field: hit,
      };
    }),
  );

  function cellText(row: Record<string, unknown>, key: string, field?: FieldMeta): string {
    const raw = getValueByKey(row, key);
    if (raw == null || raw === '') return '—';
    if (field) return resolveCellLabel(field, raw) || String(raw);
    return String(raw);
  }

  function openList() {
    const tp = normalizeTypePath(props.widget.source?.typePath);
    if (tp) void router.push(`/${tp}`);
  }

  return { rows, displayRows, columns, cellText, openList };
}
