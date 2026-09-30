<template>
  <div
    v-if="!columns.length"
    class="view-empty-wrap"
    :style="{ minHeight: (height || 240) + 'px' }"
  >
    <a-empty description="暂无看板数据或未配置分组字段" />
  </div>
  <div v-else class="kanban-board" :class="{ 'kanban-board--compact': compact }" :style="{ minHeight: height + 'px' }">
    <div
      v-for="col in columns"
      :key="col.key"
      class="kanban-col"
      :class="{ 'kanban-col--collapsed': isColumnCollapsed(col.key) }"
    >
      <div
        class="kanban-col-head"
        :class="{ 'kanban-col-head--clickable': !compact }"
        :aria-expanded="isColumnCollapsed(col.key) ? 'false' : 'true'"
        @click="toggleColumn(col.key)"
      >
        <span class="kanban-col-title">{{ col.label }}</span>
        <span class="kanban-col-count">{{ col.rows.length }}</span>
      </div>
      <div
        v-show="!isColumnCollapsed(col.key)"
        class="kanban-col-body"
        :class="{ 'kanban-col-body--over': dropKey === col.key }"
        @scroll="onColScroll(col.key, $event)"
        @dragover="onColDragOver(col.key, $event)"
        @dragleave="onColDragLeave(col.key, $event)"
        @drop="onColDrop(col.key, $event)"
      >
        <div
          v-for="(row, idx) in col.rows.slice(0, colVisible[col.key] ?? INITIAL_VISIBLE)"
          :key="rowKeyOf(row, idx)"
          class="kanban-card-wrap"
          :class="{
            'kanban-card-wrap--draggable': cardDraggable(row),
            'kanban-card-wrap--dragging': isDraggingCard(row),
          }"
          :draggable="cardDraggable(row)"
          @mousedown="onCardPointerDown"
          @dragstart="onCardDragStart(col.key, row, $event)"
          @dragend="onCardDragEnd"
        >
        <RecordCard
          :record="row"
          :title="titleOf(row)"
          :image-url="resolveImageUrl(row, mapping?.imageField)"
          :body-fields="bodyOf(row)"
        :can-view-detail="compact ? false : canViewDetail"
          :enable-table-double-click="compact ? false : enableTableDoubleClick"
          :can-edit="compact ? false : canEdit && !wfRowEditLocked(row)"
          :can-delete="
            compact
              ? false
              : canDelete && !wfRowEditLocked(row) && !isIamRowActionDisabled(typePath, row, 'delete')
          "
          :ops-custom-links="opsCustomLinks"
          :title-format-color="titleFormatColorOf(row)"
          :title-format-bold="titleFormatBoldOf(row)"
          :side-format-color="sideFormatColorOf(row)"
          :collapsible="!compact"
          @detail="$emit('detail', $event)"
          @edit="$emit('edit', $event)"
          @delete="$emit('delete', $event)"
          @ops-link="(link, row) => $emit('opsLink', link, row)"
          @toggle-enable="(row, field) => $emit('toggleEnable', row, field)"
        />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import type { KanbanMapping } from '@/core/utils/viewMapping';
import type { OpsCustomLink } from '@/core/utils/opsAction';
import RecordCard from './RecordCard.vue';
import { useKanbanBoard } from './useKanbanBoard';
import type { ViewFormatRule } from '@/core/utils/viewProfile';
import { isIamRowActionDisabled } from '@/core/utils/iamGuards';
import { wfRowEditLocked } from '@/core/types/workflow';

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    columns: ColumnPref[];
    fields: FieldMeta[];
    mapping?: KanbanMapping | null;
    rowKey: string;
    height?: number;
    canViewDetail: boolean;
    enableTableDoubleClick?: boolean;
    canEdit: boolean;
    canDelete: boolean;
    typePath?: string;
    opsCustomLinks?: OpsCustomLink[];
    formatCell?: (field: FieldMeta, record: Record<string, unknown>) => string;
    formatRules?: ViewFormatRule[];
    compact?: boolean;
    canDragGroup?: boolean;
    groupRequired?: boolean;
  }>(),
  {
    enableTableDoubleClick: true,
    opsCustomLinks: () => [],
    compact: false,
    canDragGroup: false,
    groupRequired: false,
  },
);

const emit = defineEmits<{
  detail: [row: Record<string, unknown>];
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
  toggleEnable: [row: Record<string, unknown>, field: string];
  opsLink: [link: OpsCustomLink, row: Record<string, unknown>];
  move: [payload: { row: Record<string, unknown>; field: string; value: unknown }];
  mappingChange: [mapping: KanbanMapping];
}>();

const {
  columns,
  INITIAL_VISIBLE,
  colVisible,
  onColScroll,
  rowKeyOf,
  titleOf,
  bodyOf,
  titleFormatColorOf,
  titleFormatBoldOf,
  sideFormatColorOf,
  resolveImageUrl,
  isColumnCollapsed,
  toggleColumn,
  dropKey,
  cardDraggable,
  onCardPointerDown,
  onCardDragStart,
  onCardDragEnd,
  isDraggingCard,
  onColDragOver,
  onColDragLeave,
  onColDrop,
} = useKanbanBoard(props, {
  move: (payload) => emit('move', payload),
  mappingChange: (mapping) => emit('mappingChange', mapping),
});
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
.kanban-board--compact .kanban-col {
  flex-basis: 200px;
  min-height: 160px;
}
.kanban-col-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px;
  font-size: var(--cube-font-size-body);
  font-weight: 500;
}
.kanban-col-head--clickable {
  cursor: pointer;
  user-select: none;
}
.kanban-col-head--clickable:hover {
  background: var(--color-fill-2);
  border-radius: 8px 8px 0 0;
}
.kanban-col--collapsed {
  align-self: flex-start;
  min-height: 0;
}
.kanban-col--collapsed .kanban-col-head,
.kanban-col--collapsed .kanban-col-head--clickable:hover {
  border-radius: 8px;
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
.kanban-col-body--over {
  background: var(--color-fill-2);
  box-shadow: inset 0 0 0 1px var(--primary-6);
}
.kanban-card-wrap--draggable {
  cursor: grab;
}
.kanban-card-wrap--dragging {
  opacity: 1;
  position: relative;
  z-index: 2;
}
.kanban-card-wrap--dragging :deep(.record-card) {
  background: var(--color-bg-1);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.18);
}
.kanban-card-wrap--draggable:active {
  cursor: grabbing;
}
</style>
