import { markRaw, nextTick, ref, watch } from 'vue';
import { applyChartData } from '@/core/utils/viewProfile';
import { ensureEchartsTheme, initEcharts } from '@/core/utils/echartsTheme';
import { useAppStore } from '@/stores/app';

/** ChartOptionEditor 组件 props 类型（与 ChartOptionEditor.vue defineProps 泛型逐字一致） */
interface ChartOptionEditorProps {
  visible: boolean;
  /** 当前用户配置的 ECharts option（无则 undefined） */
  chartOption: unknown;
  /** 当前列表行（预览 applyChartData 用） */
  rows: Record<string, unknown>[];
}

/** ChartOptionEditor 组件 emits 类型（与 ChartOptionEditor.vue defineEmits 泛型逐字一致） */
interface ChartOptionEditorEmits {
  'update:visible': [boolean];
  /** 保存（含清除：option=undefined）；父级走 updateInsight */
  save: [option: unknown];
  /** 清除图表配置（chartOption=null，开关可仍为 true） */
  clear: [];
}

type ChartOptionEditorEmit = <K extends keyof ChartOptionEditorEmits>(
  event: K,
  ...args: ChartOptionEditorEmits[K]
) => void;

/** ChartOptionEditor 组件全部业务 TS：JSON 编辑 ECharts option + 列表行预览（OSC-260819e483 P5.3） */
export function useChartOptionEditor(props: ChartOptionEditorProps, emit: ChartOptionEditorEmit) {
  const appStore = useAppStore();
  const text = ref('');
  const error = ref('');
  const previewRef = ref<HTMLElement | null>(null);
  let inst: ReturnType<typeof initEcharts> | null = null;

  function init() {
    text.value =
      props.chartOption && typeof props.chartOption === 'object'
        ? JSON.stringify(props.chartOption, null, 2)
        : '';
    error.value = '';
  }

  watch(
    () => props.visible,
    (v) => {
      if (v) {
        init();
        nextTick(renderPreview);
      } else if (inst) {
        inst.dispose();
        inst = null;
      }
    },
  );

  /** 解析文本为 JSON 对象；空文本→undefined；非法/非对象抛错 */
  function parse(): unknown {
    const t = text.value.trim();
    if (!t) return undefined;
    let o: unknown;
    try {
      o = JSON.parse(t);
    } catch {
      throw new Error('JSON 格式错误');
    }
    if (!o || typeof o !== 'object' || Array.isArray(o)) {
      throw new Error('chartOption 必须是 JSON 对象');
    }
    return o;
  }

  /** 预览：用当前列表行 applyChartData 后 setOption；非法 JSON 时清空图表 */
  async function renderPreview() {
    if (!previewRef.value) return;
    if (inst) inst.dispose();
    const theme = appStore.loginConfig?.echartsTheme;
    await ensureEchartsTheme(theme);
    inst = markRaw(initEcharts(previewRef.value, theme));
    try {
      const o = parse();
      if (o) inst.setOption(applyChartData(o, props.rows));
    } catch {
      /* 非法 JSON：预览留空 */
    }
  }

  function onTextChange() {
    error.value = '';
    nextTick(renderPreview);
  }

  function save() {
    try {
      const o = parse();
      error.value = '';
      emit('save', o);
      emit('update:visible', false);
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'JSON 格式错误';
    }
  }

  function clear() {
    emit('clear');
    emit('update:visible', false);
  }

  return { text, error, previewRef, onTextChange, save, clear };
}
