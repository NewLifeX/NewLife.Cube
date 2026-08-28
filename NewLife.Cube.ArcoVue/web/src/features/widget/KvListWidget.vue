<template>
  <div class="widget-card kv-card">
    <div class="widget-card-title">{{ widget.title || '键值' }}</div>
    <a-spin :loading="loading" class="widget-card-body widget-card-spin">
      <div v-if="error" class="kv-err">{{ error }}</div>
      <div v-else class="widget-kv-list">
        <div v-for="it in items" :key="it.label" class="widget-kv-row">
          <span class="widget-kv-label">{{ it.label }}</span>
          <a v-if="it.href" class="widget-kv-value kv-link" @click.prevent="open(it.href)">{{ it.value }}</a>
          <span v-else class="widget-kv-value">{{ it.value || '—' }}</span>
        </div>
        <a-empty v-if="!items.length" description="暂无数据" />
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import { useKvListWidget } from './useKvListWidget';
import './widgetCard.css';

const props = defineProps<WidgetCardProps>();
const { items, open } = useKvListWidget(props);
</script>

<style scoped>
.kv-link {
  color: rgb(var(--primary-6));
  cursor: pointer;
}
.kv-err {
  color: rgb(var(--danger-6));
  font-size: var(--font-size-body-1, 12px);
}
</style>
