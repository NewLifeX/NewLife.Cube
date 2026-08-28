import { computed, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import type { KanbanMapping } from '@/core/utils/viewMapping';
import { selectListColumns } from '@/core/utils/listColumns';
import type { WidgetQueryResult } from '@cube/api-core';
import type { WidgetCardProps } from './context';
import { fieldLabelOf, findFieldMeta, loadEntityListFields } from './listFieldMeta';

/** 与看板视图 buildCardBodyFields 上限一致 */
export const MINI_KANBAN_DISPLAY_FIELD_MAX = 8;

export function resolveKanbanInteractive(compact: boolean) {
  if (compact) {
    return {
      canEdit: false,
      canDelete: false,
      canViewDetail: false,
      enableTableDoubleClick: false,
    };
  }
  return {
    canEdit: true,
    canDelete: true,
    canViewDetail: true,
    enableTableDoubleClick: true,
  };
}

/** 规范化显示字段列表：去空、去重、排除标题/分组/图片、上限 */
export function normalizeKanbanDisplayFields(
  raw: string[] | undefined,
  exclude: { groupField?: string; titleField?: string; imageField?: string },
  max = MINI_KANBAN_DISPLAY_FIELD_MAX,
): string[] {
  const ban = new Set(
    [exclude.groupField, exclude.titleField, exclude.imageField]
      .map((x) => (x || '').trim())
      .filter(Boolean)
      .map((x) => x.toLowerCase()),
  );
  const out: string[] = [];
  const seen = new Set<string>();
  for (const n of raw ?? []) {
    const name = (n || '').trim();
    if (!name) continue;
    const key = name.toLowerCase();
    if (ban.has(key) || seen.has(key)) continue;
    seen.add(key);
    out.push(name);
    if (out.length >= max) break;
  }
  return out;
}

/**
 * 未配置 mapping.fields 时，回落 List 元数据数据列（对齐看板视图用列表列做正文）。
 */
export function fallbackKanbanDisplayFields(
  metas: FieldMeta[],
  exclude: { groupField?: string; titleField?: string; imageField?: string },
  max = MINI_KANBAN_DISPLAY_FIELD_MAX,
): string[] {
  const names = selectListColumns(metas).map((f) => f.name);
  return normalizeKanbanDisplayFields(names, exclude, max);
}

function readRows(result: unknown): Record<string, unknown>[] {
  if (!result || typeof result !== 'object') return [];
  const r = result as WidgetQueryResult & { Rows?: Record<string, unknown>[] };
  const rows = r.rows ?? r.Rows;
  return Array.isArray(rows) ? rows : [];
}

export function useMiniKanbanWidget(props: WidgetCardProps) {
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
  const excludeKeys = computed(() => ({
    groupField: props.widget.query?.mapping?.groupField,
    titleField: props.widget.query?.mapping?.titleField,
    imageField: props.widget.query?.mapping?.imageField,
  }));

  const displayFields = computed(() => {
    const configured = normalizeKanbanDisplayFields(
      props.widget.query?.mapping?.fields,
      excludeKeys.value,
    );
    if (configured.length) return configured;
    return fallbackKanbanDisplayFields(metaFields.value, excludeKeys.value);
  });

  const mapping = computed<KanbanMapping>(() => ({
    kind: 'kanban',
    groupField: props.widget.query?.mapping?.groupField || '',
    titleField: props.widget.query?.mapping?.titleField || '',
    imageField: props.widget.query?.mapping?.imageField,
  }));

  /** 带 dataSource 的字段元数据；始终用元数据规范名与中文显示名 */
  const fields = computed<FieldMeta[]>(() => {
    const m = mapping.value;
    const names = [
      m.groupField,
      m.titleField,
      m.imageField,
      ...displayFields.value,
    ].filter(Boolean) as string[];
    const uniq = [...new Set(names.map((n) => n.trim()).filter(Boolean))];
    return uniq.map((name) => {
      const hit = findFieldMeta(metaFields.value, name);
      if (hit) return hit;
      return { name, displayName: name, typeName: 'String' };
    });
  });

  /** 仅显示字段进入卡片正文列（标题/分组由 mapping 排除） */
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
  const interactive = computed(() => resolveKanbanInteractive(true));
  return { rows, mapping, fields, columns, interactive, displayFields };
}
