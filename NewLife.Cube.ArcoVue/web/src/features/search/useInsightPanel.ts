import { computed, markRaw, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import * as echarts from 'echarts';
import { resolveStatEntries } from '@/core/utils/searchFilters';
import type { FieldMeta } from '@/core/types/field';
import type { SavedQuery } from '@/core/utils/viewProfile';

/** InsightPanel 组件 props 类型（与 InsightPanel.vue defineProps 泛型逐字一致） */
interface InsightPanelProps {
  /** search 分区字段（用于搜索表单渲染） */
  fields: FieldMeta[];
  /** 搜索表单对象（父组件 reactive，直接读写其属性，保持既有 SearchFieldInput 语义） */
  model: Record<string, unknown>;
  /** 洞察开关：统计标签 */
  showStat: boolean;
  /** 洞察开关：固定图表 */
  showChart: boolean;
  /** GetList 响应 stat（与列表同源） */
  statData: Record<string, unknown> | null;
  /** 统计标签显示名映射（按 listFields 构造；缺省回落字段名） */
  statLabels?: Record<string, string>;
  /** GetChartData 返回的 ECharts option 数组 */
  chartData: unknown[];
  chartLoading: boolean;
  chartError: string;
  /** 主时间字段名（OSC-0016）；无 MasterTime 时不渲染主时间范围 */
  masterTimeName?: string | null;
  /** 主时间字段显示名（OSC-0016） */
  masterTimeDisplayName?: string | null;
  /** 关键字 Q 是否启用（OSC-0016）；false 时不渲染关键字框 */
  enableKey?: boolean;
  /** 预定义查询列表（OSC-0016） */
  queries: SavedQuery[];
  /** 当前应用的预定义查询 id（会话内存） */
  activeQueryId: string | null;
  /** 当前参数与 activeQuery 是否不一致（条目 ✓ 标记控制） */
  paramsDirty: boolean;
  /** 当前参数是否可保存（非空） */
  canSaveQuery: boolean;
}

/** InsightPanel 组件全部业务 TS：主时间/统计/图表/两行布局测量（自 InsightPanel.vue script setup 原样搬移；emits 仅模板 $emit 使用） */
export function useInsightPanel(props: InsightPanelProps) {
  /** 主时间范围值：dtStart/dtEnd 两键映射 [start, end] */
  const masterTimeRange = computed(() => {
    const s = props.model?.dtStart;
    const e = props.model?.dtEnd;
    return s && e ? [String(s), String(e)] : undefined;
  });

  /** 主时间范围变更：写 dtStart/dtEnd，清空时删除两键 */
  function onMasterTimeChange(val: unknown) {
    const arr = Array.isArray(val) ? val : [];
    if (arr.length >= 2) {
      props.model.dtStart = arr[0] ?? '';
      props.model.dtEnd = arr[1] ?? '';
    } else {
      delete props.model.dtStart;
      delete props.model.dtEnd;
    }
  }


  /** 统计标签显示名映射（父组件按 listFields 构造；缺省回落字段名） */
  const statLabels = computed<Record<string, string>>(() => props.statLabels ?? {});

  /** 仅展示 stat 中非 null 的条目；无 stat 时展示「暂无统计」而非编造 0 */
  const statEntries = computed(() => resolveStatEntries(props.statData));

  const chartInstances: echarts.ECharts[] = [];

  function setChartRef(el: HTMLElement | null, idx: number) {
    if (!el) return;
    nextTick(() => {
      if (chartInstances[idx]) chartInstances[idx].dispose();
      const inst = markRaw(echarts.init(el));
      const option = props.chartData[idx];
      if (option && typeof option === 'object') {
        inst.setOption(option as echarts.EChartsOption);
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

  // ---------- 两行布局（面板重构）：第一行 = 前 N 字段 + 主时间/Q（流式）+ 右侧查询按钮，第二行 = 其余字段（默认收起） ----------
  const measureRef = ref<HTMLElement | null>(null);

  /** 面板是否展开（显示第二行；默认收起仅第一行） */
  const expanded = ref(false);
  /** 第一行可容纳的字段数 N（测量得出） */
  const firstRowCount = ref(0);

  /** 第一行字段（前 N 个） */
  const mainFields = computed(() => props.fields.slice(0, firstRowCount.value));
  /** 第二行字段（其余，默认收起） */
  const extraFields = computed(() => props.fields.slice(firstRowCount.value));

  /**
   * 测量第一行可容纳字段数 N：主时间/Q 作为流式条件排在字段之后，测量时为其预留宽度；
   * 左侧字段区（flex:1）可用宽度内按字段宽度累加，放不下则进第二行。查询按钮固定右侧不占字段区。
   */
  function measureMainRow() {
    const m = measureRef.value;
    if (!m) return;
    const clip = m.querySelector('.qip-fields-clip');
    if (!clip) return;
    const fieldItems = Array.from(clip.querySelectorAll<HTMLElement>('.qip-measure-field'));
    // 主时间/Q 为流式条件（排在字段之后），预留其宽度保证与查询按钮同在第一行
    const tailItems = Array.from(clip.querySelectorAll<HTMLElement>('.qip-measure-tail'));
    if (!fieldItems.length) return;
    const gap = 8;
    const avail = clip.clientWidth;
    const tailW = tailItems.reduce((s, el) => s + el.offsetWidth + gap, 0);
    let used = 0;
    let n = 0;
    for (const el of fieldItems) {
      const w = el.offsetWidth + gap;
      if (used + w > avail - tailW) break;
      used += w;
      n++;
    }
    firstRowCount.value = Math.min(n, props.fields.length);
  }

  /** 容器尺寸变化时重测（面板宽度随窗口/侧栏调整） */
  let resizeObserver: ResizeObserver | null = null;

  onMounted(() => {
    nextTick(() => {
      measureMainRow();
      resizeObserver = new ResizeObserver(() => nextTick(measureMainRow));
      if (measureRef.value?.parentElement) resizeObserver.observe(measureRef.value.parentElement);
    });
  });

  watch(
    () => props.fields,
    () => nextTick(measureMainRow),
    { deep: true },
  );

  onBeforeUnmount(() => {
    disposeCharts();
    resizeObserver?.disconnect();
  });

  return {
    mainFields,
    extraFields,
    masterTimeRange,
    onMasterTimeChange,
    statLabels,
    statEntries,
    setChartRef,
    measureRef,
    expanded,
  };
}
