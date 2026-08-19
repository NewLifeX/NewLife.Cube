import { computed } from 'vue';

/**
 * 主时间范围双向映射（OSC-0016）：`model.dtStart`/`model.dtEnd` 两键 ↔ 日期范围选择器 [start, end]。
 * 供 InsightPanel（已移除搜索区，仅图表）与 SearchDrawer（搜索抽屉）复用同一实现，避免两份漂移。
 */
export function useMasterTimeRange(model: Record<string, unknown>) {
  /** 主时间范围值：dtStart/dtEnd 两键映射 [start, end] */
  const masterTimeRange = computed(() => {
    const s = model?.dtStart;
    const e = model?.dtEnd;
    return s && e ? [String(s), String(e)] : undefined;
  });

  /** 主时间范围变更：写 dtStart/dtEnd，清空时删除两键 */
  function onMasterTimeChange(val: unknown) {
    const arr = Array.isArray(val) ? val : [];
    if (arr.length >= 2) {
      model.dtStart = arr[0] ?? '';
      model.dtEnd = arr[1] ?? '';
    } else {
      delete model.dtStart;
      delete model.dtEnd;
    }
  }

  return { masterTimeRange, onMasterTimeChange };
}
