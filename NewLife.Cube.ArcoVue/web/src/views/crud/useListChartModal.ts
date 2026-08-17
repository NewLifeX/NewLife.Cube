import { computed, nextTick, onBeforeUnmount, ref, watch, markRaw } from 'vue';
import type { ECharts } from 'echarts';
import { ensureEchartsTheme, initEcharts } from '@/core/utils/echartsTheme';
import { useAppStore } from '@/stores/app';

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
  const appStore = useAppStore();
  const visibleProxy = computed({
    get: () => props.visible,
    set: (v: boolean) => emit('update:visible', v),
  });

  const instances = ref<ECharts[]>([]);

  function setRef(el: HTMLElement | null, idx: number) {
    if (!el) return;
    nextTick(async () => {
      if (instances.value[idx]) instances.value[idx].dispose();
      const theme = appStore.loginConfig?.echartsTheme;
      await ensureEchartsTheme(theme);
      const inst = markRaw(initEcharts(el, theme));
      if (props.charts[idx]) inst.setOption(props.charts[idx] as import('echarts').EChartsOption);
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
