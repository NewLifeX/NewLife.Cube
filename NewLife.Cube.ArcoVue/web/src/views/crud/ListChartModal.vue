<template>
  <a-modal v-model:visible="visibleProxy" title="图表" :width="900" :footer="false" unmount-on-close>
    <div v-if="!charts.length">
      <a-empty description="暂无图表数据" />
    </div>
    <div
      v-for="(_, idx) in charts"
      :key="idx"
      :ref="(el) => setRef(el as HTMLElement, idx)"
      style="width: 100%; height: 360px; margin-bottom: 12px"
    />
  </a-modal>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch, markRaw } from 'vue';
import * as echarts from 'echarts';

const props = defineProps<{
  visible: boolean;
  charts: unknown[];
}>();

const emit = defineEmits<{ 'update:visible': [boolean] }>();

const visibleProxy = computed({
  get: () => props.visible,
  set: (v: boolean) => emit('update:visible', v),
});

const instances = ref<echarts.ECharts[]>([]);

function setRef(el: HTMLElement | null, idx: number) {
  if (!el) return;
  nextTick(() => {
    if (instances.value[idx]) instances.value[idx].dispose();
    const inst = markRaw(echarts.init(el));
    if (props.charts[idx]) inst.setOption(props.charts[idx] as echarts.EChartsOption);
    instances.value[idx] = inst;
  });
}

watch(
  () => props.visible,
  (v) => {
    if (!v) {
      for (const i of instances.value) i?.dispose();
      instances.value = [];
    }
  },
);

onBeforeUnmount(() => {
  for (const i of instances.value) i?.dispose();
});
</script>
