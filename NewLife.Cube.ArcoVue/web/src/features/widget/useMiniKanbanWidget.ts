import { computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import type { KanbanMapping } from '@/core/utils/viewMapping';
import type { WidgetQueryResult } from '@cube/api-core';
import type { WidgetCardProps } from './context';

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

export function useMiniKanbanWidget(props: WidgetCardProps) {
  const rows = computed(
    () => (props.result as WidgetQueryResult | undefined)?.rows ?? [],
  );
  const mapping = computed<KanbanMapping>(() => ({
    kind: 'kanban',
    groupField: props.widget.query?.mapping?.groupField || '',
    titleField: props.widget.query?.mapping?.titleField || '',
    imageField: props.widget.query?.mapping?.imageField,
  }));
  const fields = computed<FieldMeta[]>(() => {
    const m = mapping.value;
    const names = [m.groupField, m.titleField, m.imageField].filter(Boolean) as string[];
    return names.map((name) => ({ name, typeName: 'String' }));
  });
  const columns = computed<ColumnPref[]>(() =>
    fields.value.map((f) => ({ key: f.name, visible: true })),
  );
  const interactive = computed(() => resolveKanbanInteractive(true));
  return { rows, mapping, fields, columns, interactive };
}
