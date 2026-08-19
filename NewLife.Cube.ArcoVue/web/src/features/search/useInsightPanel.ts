import { computed, markRaw, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import { resolveStatEntries } from '@/core/utils/searchFilters';
import { ensureEchartsTheme, initEcharts } from '@/core/utils/echartsTheme';
import { useAppStore } from '@/stores/app';

/** InsightPanel 组件 props 类型（与 InsightPanel.vue defineProps 泛型逐字一致） */
interface InsightPanelProps {
  /** 洞察开关：统计标签 */
  showStat: boolean;
  /** 洞察开关：固定图表 */
  showChart: boolean;
  /** GetList 响应 stat（与列表同源） */
  statData: Record<string, unknown> | null;
  /** 统计标签显示名映射（按 listFields 构造；缺省回落字段名） */
  statLabels?: Record<string, string>;
  /** 图表数据：开发者 GetChartData 非空数组，或用户 chartOption applyChartData 后的单元素数组（OSC-260819e483 P5） */
  chartData: unknown[];
  chartLoading: boolean;
  chartError: string;
  /** 用户配置的 ECharts option（OSC-260819e483 P5）；无则 undefined */
  chartOption?: unknown;
  /** 当前列表行（图表配置预览用） */
  chartRows?: Record<string, unknown>[];
}

/** InsightPanel 组件 emits 类型（与 InsightPanel.vue defineEmits 泛型逐字一致） */
interface InsightPanelEmits {
  /** 图表配置保存/清除（OSC-260819e483 P5）：父级走 updateInsight */
  chartOptionChange: [option: unknown];
}

type InsightPanelEmit = <K extends keyof InsightPanelEmits>(
  event: K,
  ...args: InsightPanelEmits[K]
) => void;

/**
 * InsightPanel 组件全部业务 TS：统计标签 + 一张固定图表（OSC-0012 / OSC-260819e483 P5）。
 * 仅作简易看板/图表展示区，不含搜索表单——查询统一走工具栏「搜索」抽屉 SearchDrawer（规范见 web/README.md）。
 */
export function useInsightPanel(props: InsightPanelProps, emit: InsightPanelEmit) {
  const appStore = useAppStore();

  /** 统计标签显示名映射（父组件按 listFields 构造；缺省回落字段名） */
  const statLabels = computed<Record<string, string>>(() => props.statLabels ?? {});

  /** 仅展示 stat 中非 null 的条目；无 stat 时展示「暂无统计」而非编造 0 */
  const statEntries = computed(() => resolveStatEntries(props.statData));

  const chartInstances: ReturnType<typeof initEcharts>[] = [];

  function setChartRef(el: HTMLElement | null, idx: number) {
    if (!el) return;
    nextTick(async () => {
      if (chartInstances[idx]) chartInstances[idx].dispose();
      const theme = appStore.loginConfig?.echartsTheme;
      await ensureEchartsTheme(theme);
      const inst = markRaw(initEcharts(el, theme));
      const option = props.chartData[idx];
      if (option && typeof option === 'object') {
        inst.setOption(option as import('echarts').EChartsOption);
      }
      chartInstances[idx] = inst;
    });
  }

  function disposeCharts() {
    for (const i of chartInstances) i?.dispose();
    chartInstances.length = 0;
  }

  watch(
    () => props.chartData,
    () => {
      nextTick(() => {
        disposeCharts();
        // 重绘由 v-for ref 回调触发
      });
    },
    { deep: true },
  );

  onBeforeUnmount(() => {
    disposeCharts();
  });

  // ---------- 图表配置（OSC-260819e483 P5）：空态/有图时打开 JSON 编辑器，保存/清除走 chartOptionChange ----------
  const chartConfigVisible = ref(false);

  /** 打开图表配置弹窗 */
  function openChartConfig() {
    chartConfigVisible.value = true;
  }

  /** 保存（含清除：option=undefined）；父级走 updateInsight 并刷新图表 */
  function onChartConfigSave(option: unknown) {
    emit('chartOptionChange', option);
  }

  /** 清除：chartOption=null，开关可仍为 true（回到空态 + 配置入口） */
  function onChartConfigClear() {
    emit('chartOptionChange', undefined);
  }

  return {
    statLabels,
    statEntries,
    setChartRef,
    chartConfigVisible,
    openChartConfig,
    onChartConfigSave,
    onChartConfigClear,
  };
}
