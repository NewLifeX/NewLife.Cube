<template>
  <div class="widget-card ib-card" @click="openInbox">
    <div class="ib-head">
      <span class="widget-card-title ib-title">{{ widget.title || '站内信' }}</span>
      <a-badge :count="unread" />
    </div>
    <a-spin :loading="loading" class="widget-card-body widget-card-spin">
      <div v-if="error" class="ib-err">{{ error }}</div>
      <a-empty v-else-if="!items.length" description="暂无消息" />
      <div v-else class="widget-kv-list">
        <div v-for="it in items" :key="String(it.id)" class="widget-kv-row">
          <span class="widget-kv-value ib-row-title">{{ it.title || '（无标题）' }}</span>
          <span class="ib-row-time">{{ it.createTime }}</span>
        </div>
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import { useInboxWidget } from './useInboxWidget';
import './widgetCard.css';

const props = defineProps<WidgetCardProps>();
const { unread, items, openInbox } = useInboxWidget(props);
</script>

<style scoped>
.ib-card {
  cursor: pointer;
}
.ib-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  flex-shrink: 0;
  margin-bottom: 8px;
}
.ib-title {
  margin-bottom: 0;
  flex: 1;
}
.ib-row-title {
  text-align: left;
  color: var(--color-text-1);
}
.ib-row-time {
  color: var(--color-text-3);
  flex-shrink: 0;
  font-size: var(--font-size-body-1, 12px);
}
.ib-err {
  color: rgb(var(--danger-6));
  font-size: var(--font-size-body-1, 12px);
}
</style>
