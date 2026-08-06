/**
 * 筛选构建器草稿逻辑（OSC-0015 纯前端过滤）。
 * FilterBuilderPopover 的 draft ↔ ViewFilter 转换提取为纯函数，便于单测；组件薄壳直接调用。
 */
import type { FieldMeta } from '@/core/types/field';
import {
  normalizeFilter,
  type ViewFilter,
  type ViewFilterCondition,
  type ViewFilterOp,
} from './viewProfile';

/** 数字型 typeName（整数/小数/浮点/双精度等） */
const NUMBER_TYPES: ReadonlySet<string> = new Set([
  'Byte',
  'SByte',
  'Int16',
  'UInt16',
  'Int32',
  'UInt32',
  'Int64',
  'UInt64',
  'Single',
  'Double',
  'Decimal',
]);

/** 日期时间型 typeName */
const DATETIME_TYPES: ReadonlySet<string> = new Set([
  'DateTime',
  'DateTimeOffset',
  'DateOnly',
  'TimeOnly',
]);

/** 人员字段名（创建者/更新者/创建人员/更新人员；含 CreateUser/UpdateUser 等常见命名） */
const PERSON_FIELD_RE =
  /Creator|Updater|CreateUser|UpdateUser|CreateBy|UpdateBy|创建者|更新者|创建人员|更新人员|创建人|更新人/i;

/** 筛选字段类别 */
export type FilterFieldKind = 'enum' | 'string' | 'person' | 'number' | 'datetime';

/** 是否为人员字段（名称含创建/更新人；值控件为用户实体下拉） */
export function isPersonField(field: FieldMeta): boolean {
  return (
    PERSON_FIELD_RE.test(field.name) ||
    PERSON_FIELD_RE.test(field.displayName || '') ||
    PERSON_FIELD_RE.test(field.description || '')
  );
}

/** 解析字段筛选类别：人员 → 枚举/值集 → 数字 → 日期时间 → 字符 */
export function resolveFieldFilterKind(field: FieldMeta): FilterFieldKind {
  if (isPersonField(field)) return 'person';
  const tn = field.typeName;
  if (
    tn === 'Boolean' ||
    tn === 'Enum' ||
    field.lovCode ||
    (field.dataSource && Object.keys(field.dataSource).length > 0)
  ) {
    return 'enum';
  }
  if (NUMBER_TYPES.has(tn)) return 'number';
  if (DATETIME_TYPES.has(tn)) return 'datetime';
  return 'string';
}

/** 各类别可用操作符（OSC-0015） */
export const FILTER_OPS_BY_KIND: Record<FilterFieldKind, readonly ViewFilterOp[]> = {
  // 状态/枚举/值集：等于/不等于/为空/不为空
  enum: ['eq', 'neq', 'isNull', 'notNull'],
  // 字符：等于/不等于/包含/不包含/为空/不为空
  string: ['eq', 'neq', 'contains', 'notContains', 'isNull', 'notNull'],
  // 人员：等于/不等于（用户实体下拉）
  person: ['eq', 'neq'],
  // 数字：等于/不等于/大于/大于或等于/小于/小于或等于/为空/不为空（不含范围）
  number: ['eq', 'neq', 'gt', 'gte', 'lt', 'lte', 'isNull', 'notNull'],
  // 日期/时间/日期时间：等于/晚于/早于/为空/不为空
  datetime: ['eq', 'after', 'before', 'isNull', 'notNull'],
};

/** 操作符中文标签 */
export const FILTER_OP_LABELS: Record<ViewFilterOp, string> = {
  eq: '等于',
  neq: '不等于',
  contains: '包含',
  notContains: '不包含',
  isNull: '为空',
  notNull: '不为空',
  gt: '大于',
  gte: '大于或等于',
  lt: '小于',
  lte: '小于或等于',
  after: '晚于',
  before: '早于',
};

/** 该操作符是否需要值控件（为空/不为空不需要） */
export function opNeedsValue(op: ViewFilterOp): boolean {
  return op !== 'isNull' && op !== 'notNull';
}

/** 条件编辑行：单值条件（纯前端过滤，无 _min/_max 绑定） */
export interface FilterDraftRow {
  cond: ViewFilterCondition;
}

/** 新建空条件行（未选字段，默认等于） */
export function newFilterDraftRow(): FilterDraftRow {
  return { cond: { field: '', op: 'eq', value: undefined } };
}

/** 字段切换：op 重置为该类别默认（eq）并清空值 */
export function resetCondForField(cond: ViewFilterCondition, _kind: FilterFieldKind): void {
  cond.op = 'eq';
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

/** 筛选方案 → 草稿行；非法方案归一为空 */
export function filterToDraftRows(filter: ViewFilter): FilterDraftRow[] {
  const f = normalizeFilter(filter);
  return f.conditions.map((c) => ({ cond: { ...c } }));
}
