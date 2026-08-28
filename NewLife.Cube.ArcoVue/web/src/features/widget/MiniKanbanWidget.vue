<template>
  <div class="mini-kanban">
    <div class="mini-kanban-head">
      <span class="mini-kanban-title">{{ widget.title || '数据看板' }}</span>
      <WidgetLinkBadge :widget="widget" />
    </div>
    <a-spin :loading="!!loading" class="mini-kanban-spin">
      <div v-if="error" class="mini-kanban-err">{{ error }}</div>
      <KanbanBoard
        v-else
        compact
        :records="rows"
        :columns="columns"
        :fields="fields"
        :mapping="mapping"
        row-key="Id"
        :height="220"
        :can-view-detail="interactive.canViewDetail"
        :enable-table-double-click="interactive.enableTableDoubleClick"
        :can-edit="interactive.canEdit"
        :can-delete="interactive.canDelete"
      />
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import KanbanBoard from '@/features/views/KanbanBoard.vue';
import type { WidgetCardProps } from './context';
import WidgetLinkBadge from './WidgetLinkBadge.vue';
import { useMiniKanbanWidget } from './useMiniKanbanWidget';

const props = defineProps<WidgetCardProps>();
const { rows, mapping, fields, columns, interactive } = useMiniKanbanWidget(props);
</script>

<style scoped>
.mini-kanban {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
  height: 100%;
  padding: 10px 12px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  overflow: hidden;
  box-sizing: border-box;
}
.mini-kanban-head {
  display: flex;
  align-items: center;
  gap: 6px;
  height: 22px;
  margin-bottom: 8px;
  padding-right: 52px;
  flex-shrink: 0;
  box-sizing: border-box;
}
.mini-kanban-title {
  flex: 1;
  min-width: 0;
  font-size: var(--font-size-body-3, 14px);
  font-weight: 500;
  line-height: 22px;
  color: var(--color-text-2);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.mini-kanban-spin {
  flex: 1;
  min-height: 0;
  overflow: hidden;
}
.mini-kanban-err {
  color: rgb(var(--danger-6));
  font-size: 12px;
}
</style>
