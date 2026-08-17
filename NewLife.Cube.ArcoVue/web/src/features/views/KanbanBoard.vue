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
          :ops-custom-links="opsCustomLinks"
          @detail="$emit('detail', $event)"
          @edit="$emit('edit', $event)"
          @delete="$emit('delete', $event)"
          @ops-link="(link, row) => $emit('opsLink', link, row)"
          @toggle-enable="(row, field) => $emit('toggleEnable', row, field)"
        />
      </div>
    </div>
    <a-empty v-if="!columns.length" description="暂无看板数据或未配置分组字段" />
  </div>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import type { KanbanMapping } from '@/core/utils/viewMapping';
import type { OpsCustomLink } from '@/core/utils/opsAction';
import RecordCard from './RecordCard.vue';
import { useKanbanBoard } from './useKanbanBoard';

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    columns: ColumnPref[];
    fields: FieldMeta[];
    mapping?: KanbanMapping | null;
    rowKey: string;
    height?: number;
    canViewDetail: boolean;
    canEdit: boolean;
    canDelete: boolean;
    opsCustomLinks?: OpsCustomLink[];
    formatCell?: (field: FieldMeta, record: Record<string, unknown>) => string;
  }>(),
  {
    opsCustomLinks: () => [],
  },
);

defineEmits<{
  detail: [row: Record<string, unknown>];
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
  toggleEnable: [row: Record<string, unknown>, field: string];
  opsLink: [link: OpsCustomLink, row: Record<string, unknown>];
}>();

const {
  columns,
  INITIAL_VISIBLE,
  colVisible,
  onColScroll,
  rowKeyOf,
  titleOf,
  bodyOf,
  resolveImageUrl,
} = useKanbanBoard(props);
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
