<template>
  <div class="widget-card ll-card">
    <div class="widget-card-title">{{ widget.title || '登录与在线' }}</div>
    <a-spin :loading="loading" class="widget-card-body widget-card-spin">
      <div v-if="error" class="ll-err">{{ error }}</div>
      <div v-else class="ll-cols">
        <div class="ll-col">
          <div class="ll-sub">最近登录</div>
          <div v-for="(row, i) in logins" :key="i" class="widget-kv-row">
            <span class="widget-kv-value ll-name">{{ row.userName }}</span>
            <span class="ll-muted">{{ row.action }} · {{ row.createIP }}</span>
          </div>
          <a-empty v-if="!logins.length" description="暂无登录" />
        </div>
        <div class="ll-col">
          <div class="ll-sub">当前在线</div>
          <div v-for="(row, i) in onlines" :key="i" class="widget-kv-row">
            <span class="widget-kv-value ll-name">{{ row.name }}</span>
            <span class="ll-muted">{{ row.oAuthProvider || '本地' }}</span>
          </div>
          <a-empty v-if="!onlines.length" description="暂无在线" />
        </div>
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import type { WidgetCardProps } from './context';
import { useLoginLogWidget } from './useLoginLogWidget';
import './widgetCard.css';

const props = defineProps<WidgetCardProps>();
const { logins, onlines } = useLoginLogWidget(props);
</script>

<style scoped>
.ll-cols {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  width: 100%;
}
.ll-col {
  min-width: 0;
  width: 100%;
}
.ll-sub {
  font-size: var(--font-size-body-1, 12px);
  font-weight: 500;
  margin-bottom: 4px;
  color: var(--color-text-2);
}
.ll-name {
  text-align: left;
  font-size: var(--font-size-body-1, 12px);
}
.ll-muted {
  color: var(--color-text-3);
  flex-shrink: 0;
  font-size: var(--font-size-body-1, 12px);
}
.ll-err {
  color: rgb(var(--danger-6));
  font-size: var(--font-size-body-1, 12px);
}
</style>
