import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import { selectListColumns } from '@/core/utils/listColumns';
import { getValueByKey } from '@/core/utils/url';
import { resolveCellLabel } from '@/core/utils/fieldBadge';
import { buildCardBodyFields, resolveImageUrl } from '@/features/views/cardHelpers';
import type { WidgetQueryResult } from '@cube/api-core';
import type { WidgetCardProps } from './context';
import { normalizeTypePath } from './legacy';
import { fieldLabelOf, findFieldMeta, loadEntityListFields } from './listFieldMeta';
import { normalizeKanbanDisplayFields } from './useMiniKanbanWidget';

const DISPLAY_MAX = 8;
/** 单卡宽度（含间距由样式控制） */
export const DATA_CARD_WIDTH = 200;
export const DATA_CARD_GAP = 10;

function readRows(result: unknown): Record<string, unknown>[] {
  if (!result || typeof result !== 'object') return [];
  const r = result as WidgetQueryResult & { Rows?: Record<string, unknown>[] };
  const rows = r.rows ?? r.Rows;
  return Array.isArray(rows) ? rows : [];
}

export function useDataCardWidget(props: WidgetCardProps) {
  const router = useRouter();
  const metaFields = ref<FieldMeta[]>([]);
  const index = ref(0);

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

  watch(
    () => rows.value.length,
    () => {
      index.value = 0;
    },
  );

  const titleField = computed(
    () => props.widget.query?.mapping?.titleField?.trim() || '',
  );
  const imageField = computed(
    () => props.widget.query?.mapping?.imageField?.trim() || undefined,
  );

  const displayFields = computed(() => {
    const configured = normalizeKanbanDisplayFields(
      props.widget.query?.mapping?.fields,
      { titleField: titleField.value, imageField: imageField.value },
      DISPLAY_MAX,
    );
    if (configured.length) return configured;
    return normalizeKanbanDisplayFields(
      selectListColumns(metaFields.value).map((f) => f.name),
      { titleField: titleField.value, imageField: imageField.value },
      DISPLAY_MAX,
    );
  });

  const fields = computed<FieldMeta[]>(() => {
    const names = [titleField.value, imageField.value, ...displayFields.value].filter(
      Boolean,
    ) as string[];
    const uniq = [...new Set(names)];
    return uniq.map((name) => {
      const hit = findFieldMeta(metaFields.value, name);
      return hit || { name, displayName: name, typeName: 'String' };
    });
  });

  const columns = computed<ColumnPref[]>(() =>
    displayFields.value.map((name) => {
      const hit = findFieldMeta(metaFields.value, name);
      return {
        key: hit?.name || name,
        visible: true,
        title: fieldLabelOf(metaFields.value, name),
      };
    }),
  );

  const exclude = computed(() => {
    const keys = [titleField.value];
    if (imageField.value) keys.push(imageField.value);
    return keys.filter(Boolean);
  });

  function titleOf(row: Record<string, unknown>): string {
    const key = titleField.value;
    if (!key) return '—';
    const field = findFieldMeta(fields.value, key);
    const raw = getValueByKey(row, key);
    if (raw == null || raw === '') return '—';
    if (field) return resolveCellLabel(field, raw) || String(raw);
    return String(raw);
  }

  function bodyOf(row: Record<string, unknown>) {
    return buildCardBodyFields(row, columns.value, fields.value, exclude.value);
  }

  function imageOf(row: Record<string, unknown>) {
    return resolveImageUrl(row, imageField.value);
  }

  function rowKeyOf(row: Record<string, unknown>, idx: number) {
    const v = getValueByKey(row, 'Id') ?? getValueByKey(row, 'id');
    return v == null || v === '' ? idx : String(v);
  }

  const maxIndex = computed(() => Math.max(0, rows.value.length - 1));

  /** 循环：到头回尾、到尾回头（自动轮播与手动按钮一致） */
  function prev() {
    const n = rows.value.length;
    if (n <= 1) return;
    index.value = index.value <= 0 ? n - 1 : index.value - 1;
  }
  function next() {
    const n = rows.value.length;
    if (n <= 1) return;
    index.value = index.value >= n - 1 ? 0 : index.value + 1;
  }

  const trackOffset = computed(
    () => index.value * (DATA_CARD_WIDTH + DATA_CARD_GAP),
  );

  function openList() {
    const tp = normalizeTypePath(props.widget.source?.typePath);
    if (tp) void router.push(`/${tp}`);
  }

  return {
    rows,
    index,
    titleOf,
    bodyOf,
    imageOf,
    rowKeyOf,
    prev,
    next,
    trackOffset,
    openList,
    cardWidth: DATA_CARD_WIDTH,
    cardGap: DATA_CARD_GAP,
    maxIndex,
  };
}
