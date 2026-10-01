<template>
  <a-modal v-model:visible="visibleProxy" title="图表" :width="900" :footer="false" unmount-on-close>
    <div v-if="!chartItems.length">
      <a-empty description="暂无图表数据" />
    </div>
    <div
      v-for="(_, idx) in chartItems"
      :key="idx"
      :ref="(el) => setRef(el as HTMLElement, idx)"
      style="width: 100%; height: 360px; margin-bottom: 12px"
    />
  </a-modal>
</template>

<script setup lang="ts">
import { useListChartModal } from './useListChartModal';

const props = defineProps<{
  visible: boolean;
  charts: unknown[];
}>();

const emit = defineEmits<{ 'update:visible': [boolean] }>();

const {
  visibleProxy,
  chartItems,
  setRef,
} = useListChartModal(props, emit);
</script>
