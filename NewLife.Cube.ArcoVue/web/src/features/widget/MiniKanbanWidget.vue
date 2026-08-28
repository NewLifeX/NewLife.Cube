<template>
  <div class="mini-kanban">
    <div class="mini-kanban-head">
      <span class="mini-kanban-title">{{ widget.title || '未命名' }}</span>
      <a-tag v-if="unlinked" size="small">未联动</a-tag>
    </div>
    <a-spin :loading="loading">
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
import { useMiniKanbanWidget } from './useMiniKanbanWidget';

const props = defineProps<WidgetCardProps>();
const { rows, mapping, fields, columns, interactive } = useMiniKanbanWidget(props);
</script>

<style scoped>
.mini-kanban {
  flex: 1;
  padding: var(--cube-space-md, 16px);
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  min-width: 0;
  overflow: hidden;
}
.mini-kanban-head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}
.mini-kanban-title {
  flex: 1;
  font-size: 13px;
  color: var(--color-text-2);
}
.mini-kanban-err {
  color: rgb(var(--danger-6));
  font-size: 12px;
}
</style>
