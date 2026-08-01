<template>
  <div class="kanban-board" :style="{ minHeight: height + 'px' }">
    <div v-for="col in columns" :key="col.key" class="kanban-col">
      <div class="kanban-col-head">
        <span class="kanban-col-title">{{ col.label }}</span>
        <span class="kanban-col-count">{{ col.rows.length }}</span>
      </div>
      <div class="kanban-col-body">
        <RecordCard
          v-for="(row, idx) in col.rows"
          :key="rowKeyOf(row, idx)"
          :record="row"
          :title="titleOf(row)"
          :image-url="resolveImageUrl(row, mapping?.imageField)"
          :body-fields="bodyOf(row)"
          :can-view-detail="canViewDetail"
          :can-edit="canEdit"
          :can-delete="canDelete"
          @detail="$emit('detail', $event)"
          @edit="$emit('edit', $event)"
          @delete="$emit('delete', $event)"
        />
      </div>
    </div>
    <a-empty v-if="!columns.length" description="暂无看板数据或未配置分组字段" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/entityViewProfile';
import { bucketKanban, type KanbanMapping } from '@/core/utils/viewMapping';
import { getValueByKey } from '@/core/utils/url';
import RecordCard from './RecordCard.vue';
import { buildCardBodyFields, cardExcludeKeys, resolveImageUrl } from './cardHelpers';

const props = defineProps<{
  records: Record<string, unknown>[];
  columns: ColumnPref[];
  fields: FieldMeta[];
  mapping?: KanbanMapping | null;
  rowKey: string;
  height?: number;
  canViewDetail: boolean;
  canEdit: boolean;
  canDelete: boolean;
  formatCell?: (field: FieldMeta, record: Record<string, unknown>) => string;
}>();

defineEmits<{
  detail: [row: Record<string, unknown>];
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
}>();

const columns = computed(() => {
  if (!props.mapping?.groupField) return [];
  const field = props.fields.find((f) => f.name === props.mapping!.groupField);
  return bucketKanban(props.records, props.mapping.groupField, field?.dataSource);
});

const exclude = computed(() =>
  props.mapping ? cardExcludeKeys(props.mapping) : [],
);

function rowKeyOf(row: Record<string, unknown>, idx: number) {
  const v = getValueByKey(row, props.rowKey);
  return v == null || v === '' ? idx : String(v);
}

function titleOf(row: Record<string, unknown>) {
  const key = props.mapping?.titleField;
  if (!key) return '-';
  const field = props.fields.find((f) => f.name === key);
  if (field && props.formatCell) return props.formatCell(field, row);
  const raw = getValueByKey(row, key);
  return raw == null || raw === '' ? '-' : String(raw);
}

function bodyOf(row: Record<string, unknown>) {
  return buildCardBodyFields(
    row,
    props.columns,
    props.fields,
    exclude.value,
    props.formatCell,
  );
}
</script>

<style scoped>
.kanban-board {
  display: flex;
  gap: 12px;
  overflow-x: auto;
  padding: 4px 0 12px;
  align-items: stretch;
}
.kanban-col {
  flex: 0 0 280px;
  background: var(--color-fill-1);
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  max-height: inherit;
  min-height: 240px;
}
.kanban-col-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px;
  font-size: 13px;
  font-weight: 500;
}
.kanban-col-count {
  color: var(--color-text-3);
  font-weight: 400;
}
.kanban-col-body {
  padding: 0 10px 10px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  overflow-y: auto;
  flex: 1;
}
</style>
