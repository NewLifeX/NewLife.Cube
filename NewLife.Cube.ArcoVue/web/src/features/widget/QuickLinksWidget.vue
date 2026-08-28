<template>
  <div class="ql-card" :class="{ 'ql-card--host-edit': canEdit }">
    <div class="ql-head">
      <div class="ql-title">{{ widget.title || '快捷入口' }}</div>
      <a-tooltip content="编辑快捷入口">
        <a-button type="text" size="mini" class="ql-edit-btn" @click.stop="openEdit">
          <icon-park type="edit" :size="14" />
        </a-button>
      </a-tooltip>
    </div>
    <a-spin :loading="loading">
      <div v-if="error" class="ql-err">{{ error }}</div>
      <a-empty v-else-if="!links.length" description="暂无入口" />
      <div v-else class="ql-flow">
        <button
          v-for="l in links"
          :key="l.url"
          type="button"
          class="ql-item"
          :title="l.name"
          @click="open(l.url)"
        >
          <icon-park :type="l.icon || 'application'" :size="26" class="ql-ico" />
          <span class="ql-label">{{ l.name }}</span>
        </button>
      </div>
    </a-spin>
    <QuickLinksEditDrawer
      v-model:visible="editVisible"
      :menu-leaves="menuLeaves"
      :pins="pins"
      :saving="saving"
      @save="savePins"
    />
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import QuickLinksEditDrawer from './QuickLinksEditDrawer.vue';
import { useQuickLinksWidget } from './useQuickLinksWidget';

const props = defineProps<WidgetCardProps>();
const {
  links,
  pins,
  menuLeaves,
  editVisible,
  saving,
  open,
  openEdit,
  savePins,
} = useQuickLinksWidget(props);
</script>

<style scoped>
.ql-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 10px 12px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
  min-width: 0;
  min-height: 0;
}
/* 工作台自定义模式时 Host 右上角有操作条，给编辑钮留空 */
.ql-card--host-edit .ql-head {
  padding-right: 52px;
}
.ql-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 8px;
  flex-shrink: 0;
}
.ql-title {
  font-size: var(--font-size-body-3, 14px);
  font-weight: 500;
  color: var(--color-text-2);
}
.ql-edit-btn {
  color: var(--color-text-3) !important;
}
.ql-edit-btn:hover {
  color: rgb(var(--primary-6)) !important;
}
.ql-flow {
  display: flex;
  flex-wrap: wrap;
  align-content: flex-start;
  gap: 8px;
}
/* 正方形：宽度收成与高度一致，图标放大 */
.ql-item {
  box-sizing: border-box;
  width: 64px;
  height: 64px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  padding: 6px 4px;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  background: var(--color-bg-1);
  cursor: pointer;
  color: var(--color-text-1);
  flex-shrink: 0;
}
.ql-item:hover {
  border-color: rgb(var(--primary-6));
  color: rgb(var(--primary-6));
}
.ql-ico {
  flex-shrink: 0;
}
.ql-label {
  font-size: 11px;
  line-height: 1.2;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: center;
}
.ql-err {
  color: rgb(var(--danger-6));
  font-size: 12px;
}
</style>
