import { computed, nextTick, onBeforeUnmount, ref, watch, markRaw } from 'vue';
import * as echarts from 'echarts';

/** ListChartModal 组件 props 类型（与 ListChartModal.vue defineProps 泛型逐字一致） */
interface ListChartModalProps {
  visible: boolean;
  charts: unknown[];
}

/** ListChartModal 组件 emits 类型（与 ListChartModal.vue defineEmits 泛型逐字一致） */
interface ListChartModalEmits {
  'update:visible': [boolean];
}

type ListChartModalEmit = <K extends keyof ListChartModalEmits>(event: K, ...args: ListChartModalEmits[K]) => void;

/** ListChartModal 组件全部业务 TS：ECharts 实例挂载与销毁（自 ListChartModal.vue script setup 原样搬移） */
export function useListChartModal(props: ListChartModalProps, emit: ListChartModalEmit) {
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

  return {
    visibleProxy,
    setRef,
  };
}
