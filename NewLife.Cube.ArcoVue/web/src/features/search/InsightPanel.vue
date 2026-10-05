<template>
  <div class="insight-panel" :class="{ 'insight-panel--vacant': !hasVisibleContent }">
    <a-alert
      v-if="developerChartError && !hasDeveloperWidgets"
      type="warning"
      show-icon
      class="insight-dev-alert"
    >
      {{ developerChartError }}
    </a-alert>
    <WidgetHost ref="hostRef" />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import WidgetHost from '@/features/widget/WidgetHost.vue';
import type { FieldMeta } from '@/core/types/field';
import type { ViewFilter } from '@/core/utils/viewProfile';
import { useInsightPanel } from './useInsightPanel';

defineOptions({ name: 'InsightPanel' });

const props = defineProps<{
  typePath: string;
  showStat: boolean;
  showChart: boolean;
  statData: Record<string, unknown> | null;
  chartData: unknown[];
  chartLoading: boolean;
  chartError: string;
  chartOption?: unknown;
  hostFilter: ViewFilter | null;
  listFields?: { name: string; displayName?: string; typeName?: string }[];
  /** 宿主页 search∪list 字段候选（OSC-260903e2a4） */
  filterFields?: FieldMeta[];
  /** Admin/UserStat 等：经 WidgetHost 以 legacyChart 部件样式展示 */
  developerCharts?: unknown[];
  developerChartError?: string;
}>();

const { developerChartError, dashboard, hasVisibleContent } = useInsightPanel(props);

const hasDeveloperWidgets = computed(() =>
  (dashboard.value.widgets ?? []).some((w) => String(w.id).startsWith('dev-chart-')),
);

const hostRef = ref<{ openAdd?: () => void } | null>(null);
defineExpose({ openAdd: () => hostRef.value?.openAdd?.() });
</script>

<style scoped>
.insight-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
/* 未配置仪表盘：不占位，避免 list-surface gap 在多维视图顶部留空（含 padding） */
.insight-panel--vacant {
  display: none;
  margin: 0;
  padding: 0;
  gap: 0;
}
.insight-dev-alert {
  margin: 0;
}
</style>
