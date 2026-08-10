<template>
  <div class="kanban-board" :style="{ minHeight: height + 'px' }">
    <div v-for="col in columns" :key="col.key" class="kanban-col">
      <div class="kanban-col-head">
        <span class="kanban-col-title">{{ col.label }}</span>
        <span class="kanban-col-count">{{ col.rows.length }}</span>
      </div>
      <div class="kanban-col-body" @scroll="onColScroll(col.key, $event)">
        <RecordCard
          v-for="(row, idx) in col.rows.slice(0, colVisible[col.key] ?? INITIAL_VISIBLE)"
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
          @toggle-enable="(row, field) => $emit('toggleEnable', row, field)"
        />
      </div>
    </div>
    <a-empty v-if="!columns.length" description="暂无看板数据或未配置分组字段" />
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
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
  toggleEnable: [row: Record<string, unknown>, field: string];
}>();

const columns = computed(() => {
  if (!props.mapping?.groupField) return [];
  const field = props.fields.find((f) => f.name === props.mapping!.groupField);
  return bucketKanban(props.records, props.mapping.groupField, field?.dataSource);
});

/* ---------------- 滚动懒加载（每列先渲染 100 条，列内滚动到底动态追加） ---------------- */
/** 初始渲染条数与滚动追加步长 */
const INITIAL_VISIBLE = 100;
const LOAD_STEP = 100;
/** 每列已渲染条数（key = 列 key；列头 count 仍显示总数 col.rows.length） */
const colVisible = reactive<Record<string, number>>({});

function initColVisibility(cols: typeof columns.value) {
  for (const k of Object.keys(colVisible)) delete colVisible[k];
  for (const col of cols) colVisible[col.key] = INITIAL_VISIBLE;
}

// 分组变化（数据/分组字段变更）→ 重置各列懒加载计数
watch(
  () => columns.value,
  (cols) => {
    initColVisibility(cols);
  },
  { immediate: true },
);

function onColScroll(key: string, e: Event) {
  const el = e.currentTarget as HTMLElement;
  // 接近列底部（剩余不足 200px）时追加下一批
  if (el.scrollTop + el.clientHeight >= el.scrollHeight - 200) {
    const col = columns.value.find((c) => c.key === key);
    if (!col) return;
    const cur = colVisible[key] ?? INITIAL_VISIBLE;
    if (cur < col.rows.length) colVisible[key] = Math.min(col.rows.length, cur + LOAD_STEP);
  }
}

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
  /* 横向滚动时保留右端空隙，避免贴边 */
  padding-right: 4px;
  max-width: 100%;
  box-sizing: border-box;
  min-width: 0;
}
.kanban-col {
  flex: 0 0 280px;
  background: var(--color-fill-1);
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  max-height: inherit;
  min-height: 240px;
  min-width: 0;
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
