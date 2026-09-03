<template>
  <WidgetHost ref="hostRef" />
</template>

<script setup lang="ts">
import { ref } from 'vue';
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
}>();

useInsightPanel(props);
const hostRef = ref<{ openAdd?: () => void } | null>(null);
defineExpose({ openAdd: () => hostRef.value?.openAdd?.() });
</script>
