<template>
  <div class="card-list" :style="{ minHeight: height + 'px' }">
    <RecordCard
      v-for="(row, idx) in records"
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
    <a-empty v-if="!records.length" description="暂无数据" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/entityViewProfile';
import type { CardMapping } from '@/core/utils/viewMapping';
import { getValueByKey } from '@/core/utils/url';
import RecordCard from './RecordCard.vue';
import { buildCardBodyFields, cardExcludeKeys, resolveImageUrl } from './cardHelpers';

const props = defineProps<{
  records: Record<string, unknown>[];
  columns: ColumnPref[];
  fields: FieldMeta[];
  mapping?: CardMapping | null;
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
.card-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 12px;
  padding: 4px 0 12px;
  align-content: start;
}
</style>
