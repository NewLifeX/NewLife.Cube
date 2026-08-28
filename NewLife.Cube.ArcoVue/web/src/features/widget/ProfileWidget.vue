<template>
  <div class="widget-card pf-card">
    <div class="widget-card-title">{{ widget.title || '个人信息' }}</div>
    <a-spin :loading="loading" class="widget-card-body widget-card-spin">
      <div v-if="error" class="pf-err">{{ error }}</div>
      <div v-else class="widget-kv-list">
        <div v-for="row in rows" :key="row.label" class="widget-kv-row">
          <span class="widget-kv-label">{{ row.label }}</span>
          <span class="widget-kv-value">{{ row.value || '—' }}</span>
        </div>
        <a-empty v-if="!rows.length" description="暂无数据" />
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import { useProfileWidget } from './useProfileWidget';
import './widgetCard.css';

const props = defineProps<WidgetCardProps>();
const { rows } = useProfileWidget(props);
</script>

<style scoped>
.pf-err {
  color: rgb(var(--danger-6));
  font-size: var(--font-size-body-1, 12px);
}
</style>
