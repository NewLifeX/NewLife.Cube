/**
 * 筛选构建器草稿逻辑（OSC-0015）。
 * FilterBuilderPopover 的 draft ↔ ViewFilter 转换提取为纯函数，便于单测；组件薄壳直接调用。
 */
import {
  normalizeFilter,
  type ViewFilter,
  type ViewFilterCondition,
} from './viewProfile';

/** 范围型搜索控件集合（与 searchFilters.collectSearchKeys 一致） */
export const RANGE_CONTROLS: ReadonlySet<string> = new Set([
  'numberRange',
  'dateRange',
  'datetimeRange',
  'timeRange',
]);

/** 搜索控件名是否为范围型（可表达 between） */
export function isRangeControl(control: string): boolean {
  return RANGE_CONTROLS.has(control);
}

/** 条件编辑行：cond=编辑态条件，form=SearchFieldInput 值绑定（含 _min/_max） */
export interface FilterDraftRow {
  cond: ViewFilterCondition;
  form: Record<string, unknown>;
}

/** 新建空条件行（未选字段） */
export function newFilterDraftRow(): FilterDraftRow {
  return { cond: { field: '', op: 'eq', value: undefined }, form: {} };
}

/** 由条件构建 SearchFieldInput 值绑定 form（含 `字段`、`字段_min`、`字段_max`） */
export function buildCondForm(cond: ViewFilterCondition): Record<string, unknown> {
  const f = cond.field;
  const form: Record<string, unknown> = {};
  if (f) {
    form[f] = cond.value;
    form[`${f}_min`] = cond.value;
    form[`${f}_max`] = cond.value2;
  }
  return form;
}

/** 按 form 中 `_min`/`_max` 键同步条件值（范围输入） */
export function applyCondKey(cond: ViewFilterCondition, key: string, value: unknown): void {
  if (key.endsWith('_min')) cond.value = value;
  else if (key.endsWith('_max')) cond.value2 = value;
}

/** 字段切换：非范围字段重置为 eq 并清空值 */
export function resetCondForField(cond: ViewFilterCondition, isRange: boolean): void {
  if (!isRange) cond.op = 'eq';
  cond.value = undefined;
  cond.value2 = undefined;
}

/** 草稿行 → 归一化筛选方案（normalizeFilter 会丢弃空/非法条件） */
export function draftToFilter(logic: 'all' | 'any', rows: FilterDraftRow[]): ViewFilter {
  return normalizeFilter({
    logic,
    conditions: rows.map((r) => ({ ...r.cond })),
  });
}

/** 筛选方案 → 草稿行（含 form 值绑定）；非法方案归一为空 */
export function filterToDraftRows(filter: ViewFilter): FilterDraftRow[] {
  const f = normalizeFilter(filter);
  return f.conditions.map((c) => ({ cond: { ...c }, form: buildCondForm(c) }));
}
