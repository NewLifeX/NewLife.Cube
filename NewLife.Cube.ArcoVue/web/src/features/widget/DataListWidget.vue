<template>
  <div class="dl-card" :class="{ 'dl-card--editing': !!canEdit }">
    <div class="dl-head">
      <span class="dl-title">{{ widget.title || '数据列表' }}</span>
      <WidgetLinkBadge :widget="widget" />
      <a-tooltip content="更多">
        <a-button type="text" size="mini" class="dl-more" @click="openList">
          <icon-park type="share" :size="14" />
        </a-button>
      </a-tooltip>
    </div>
    <a-spin :loading="!!loading" class="dl-spin">
      <div v-if="error" class="dl-err">{{ error }}</div>
      <a-empty v-else-if="!rows.length" description="暂无数据" />
      <div v-else class="dl-body">
        <a-table
          class="dl-table"
          size="mini"
          :data="displayRows"
          :pagination="false"
          :bordered="false"
          :stripe="true"
          :scroll="tableScroll"
          :scrollbar="true"
          row-key="Id"
          :columns="arcoColumns"
        />
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import { computed, h } from 'vue';
import { BADGE_BORDER_RADIUS, resolveCellBadge } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';
import type { WidgetCardProps } from './context';
import WidgetLinkBadge from './WidgetLinkBadge.vue';
import { useDataListWidget } from './useDataListWidget';

const props = defineProps<WidgetCardProps>();
const { rows, displayRows, columns, cellText, openList } = useDataListWidget(props);

const tableScroll = computed(() => ({
  x: Math.max(480, columns.value.length * 112),
}));

const arcoColumns = computed(() =>
  columns.value.map((col) => ({
    title: col.title,
    dataIndex: col.key,
    key: col.key,
    width: 112,
    ellipsis: true,
    tooltip: true,
    render: ({ record }: { record: Record<string, unknown> }) => {
      const field = col.field;
      const raw = getValueByKey(record, col.key);
      if (field && raw != null && raw !== '') {
        const badge = resolveCellBadge(field, raw);
        if (badge) {
          return h(
            'span',
            {
              class: 'dl-badge',
              style: {
                display: 'inline-block',
                padding: '0 4px',
                lineHeight: '16px',
                fontSize: '11px',
                borderRadius: `${BADGE_BORDER_RADIUS}px`,
                background: badge.buttonColor,
                color: badge.textColor,
                border: `1px solid ${badge.buttonBorderColor}`,
                maxWidth: '100%',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                verticalAlign: 'middle',
              },
            },
            badge.label,
          );
        }
      }
      return h('span', cellText(record, col.key, field));
    },
  })),
);
</script>

<style scoped>
.dl-card {
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
.dl-head {
  display: flex;
  align-items: center;
  gap: 6px;
  height: 22px;
  margin-bottom: 8px;
  padding-right: 0;
  flex-shrink: 0;
  box-sizing: border-box;
}
.dl-card--editing .dl-head {
  padding-right: 52px;
}
.dl-title {
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
.dl-more {
  margin-left: auto;
  flex-shrink: 0;
  width: 22px !important;
  height: 22px !important;
  padding: 0 !important;
  color: var(--color-text-3) !important;
}
.dl-more:hover {
  color: rgb(var(--primary-6)) !important;
}
.dl-spin {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.dl-spin :deep(.arco-spin) {
  display: flex;
  flex: 1;
  min-height: 0;
  flex-direction: column;
}
.dl-spin :deep(.arco-spin-children) {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.dl-body {
  flex: 0 0 auto;
  min-height: 0;
  overflow: hidden;
}
.dl-table {
  width: 100%;
}
.dl-table :deep(.arco-table-th),
.dl-table :deep(.arco-table-td) {
  height: 22px;
  font-size: 12px;
  line-height: 18px;
}
.dl-table :deep(.arco-table-cell) {
  padding: 1px 8px;
}
.dl-table :deep(.arco-table-th .arco-table-cell) {
  padding: 2px 8px;
  font-weight: 500;
}
.dl-table :deep(.arco-scrollbar-container) {
  overflow-x: auto !important;
}
.dl-table :deep(.arco-table-stripe .arco-table-tr:nth-child(even) .arco-table-td) {
  background-color: var(--color-fill-2);
}
.dl-table :deep(.arco-table-hover .arco-table-tr:hover .arco-table-td) {
  background-color: var(--color-fill-3);
}
.dl-err {
  color: rgb(var(--danger-6));
  font-size: var(--font-size-body-1, 12px);
}
</style>
