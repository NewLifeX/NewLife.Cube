import type { FieldMeta } from '@/core/types/field';
import type { ViewKind } from '@/core/utils/viewMapping';
import {
  generateFormatId,
  type FormatApply,
  type ViewFilterOp,
  type ViewFormatRule,
} from '@/core/utils/viewProfile';
import { opNeedsValue } from '@/core/utils/filterBuilder';
import { matchesViewFilter } from '@/core/utils/searchFilters';

export const ROW_SIDE_WIDTH_PX = 3;

export const FORMAT_APPLY_LABELS: Record<FormatApply, string> = {
  cell: '单元格',
  side: '行侧边',
  row: '整行',
  column: '整列',
};

/** 色板列数（3 行 × 10 列，贴近飞书填色） */
export const FORMAT_PRESET_COLS = 10;

/**
 * 预置色：浅 / 中 / 饱和 三行。
 * 饱和行含常见红、橙、黄、绿、青、蓝、靛、紫、粉、灰。
 */
export const FORMAT_PRESET_COLORS = [
  '#E8F7FF',
  '#E8FFFB',
  '#E8FFEA',
  '#F3FFD6',
  '#FFF7E8',
  '#FFE8CC',
  '#FFECE8',
  '#FFE8F1',
  '#F5E8FF',
  '#F2F3F5',
  '#B5E4FF',
  '#9AEFE0',
  '#B3F0A0',
  '#E0F07A',
  '#FFE08A',
  '#FFC07A',
  '#FF9E8A',
  '#FF8AB8',
  '#C89AFF',
  '#C9CDD4',
  '#F53F3F',
  '#FF7D00',
  '#F7BA1E',
  '#00B42A',
  '#14C9C9',
  '#165DFF',
  '#3491FA',
  '#722ED1',
  '#F5319D',
  '#4E5969',
] as const;

export const DEFAULT_FORMAT_COLOR = '#FFF7E8';

export interface CellFormatStyle {
  color?: string;
  bold?: boolean;
}

export function formatApplyOptions(viewKind: ViewKind): FormatApply[] {
  if (viewKind === 'card') return ['side', 'row'];
  return ['cell', 'side', 'row', 'column'];
}

/** 整列按字段铺满，不设条件；其余范围需要字段+操作符+值 */
export function formatRuleNeedsCondition(apply: FormatApply): boolean {
  return apply !== 'column';
}

export function newFormatRule(opts: {
  apply: FormatApply;
  field: string;
  op?: ViewFilterOp;
  color?: string;
  bold?: boolean;
}): ViewFormatRule {
  const rule: ViewFormatRule = {
    id: generateFormatId(),
    apply: opts.apply,
    color: opts.color || DEFAULT_FORMAT_COLOR,
    field: opts.field,
    op: opts.op || 'eq',
  };
  if (opts.bold) rule.bold = true;
  return rule;
}

/**
 * 弹层打开：无规则时用第一字段种一条；已有规则若缺字段则补第一字段。
 * 无需变更时返回 null。
 */
export function seedFormatRulesOnOpen(
  rules: ViewFormatRule[],
  opts: { firstField: string; apply: FormatApply },
): ViewFormatRule[] | null {
  const firstField = opts.firstField || '';
  if (!rules.length) {
    if (!firstField) return null;
    return [newFormatRule({ apply: opts.apply, field: firstField })];
  }
  if (!firstField) return null;
  let changed = false;
  const next = rules.map((r) => {
    if (r.field) return r;
    changed = true;
    return { ...r, field: firstField };
  });
  return changed ? next : null;
}

export function moveFormatRule(rules: ViewFormatRule[], from: number, to: number): ViewFormatRule[] {
  if (from === to) return [...rules];
  if (from < 0 || to < 0 || from >= rules.length || to >= rules.length) return [...rules];
  const next = [...rules];
  const [item] = next.splice(from, 1);
  next.splice(to, 0, item);
  return next;
}

export function ruleMatchesRow(
  row: Record<string, unknown>,
  rule: ViewFormatRule,
  fields: FieldMeta[],
): boolean {
  if (!rule.field) return false;
  if (opNeedsValue(rule.op) && (rule.value === undefined || rule.value === '')) return false;
  return matchesViewFilter(
    row,
    {
      logic: 'all',
      conditions: [{ field: rule.field, op: rule.op, value: rule.value }],
    },
    fields,
  );
}

function fieldEq(a: string, b: string): boolean {
  return a.toLowerCase() === b.toLowerCase();
}

function styleOf(rule: ViewFormatRule): CellFormatStyle {
  return { color: rule.color, bold: !!rule.bold };
}

/** 背景通道：跳过 side；column 无条件铺该字段列；cell 仅条件命中行的该列；row 命中行全部数据列 */
export function resolveCellFormat(
  row: Record<string, unknown>,
  columnField: string,
  rules: ViewFormatRule[],
  fields: FieldMeta[],
): CellFormatStyle | undefined {
  for (const rule of rules) {
    if (rule.apply === 'side') continue;
    if (rule.apply === 'column') {
      if (rule.field && fieldEq(columnField, rule.field)) return styleOf(rule);
      continue;
    }
    if (!ruleMatchesRow(row, rule, fields)) continue;
    if (rule.apply === 'row') return styleOf(rule);
    if (rule.apply === 'cell' && fieldEq(columnField, rule.field)) return styleOf(rule);
  }
  return undefined;
}

export function resolveCellFormatColor(
  row: Record<string, unknown>,
  columnField: string,
  rules: ViewFormatRule[],
  fields: FieldMeta[],
): string | undefined {
  return resolveCellFormat(row, columnField, rules, fields)?.color;
}

/** 整行填色：仅 apply=row；供勾选列 / 操作列等 chrome 列与数据列铺同一底 */
export function resolveRowFormat(
  row: Record<string, unknown>,
  rules: ViewFormatRule[],
  fields: FieldMeta[],
): CellFormatStyle | undefined {
  for (const rule of rules) {
    if (rule.apply !== 'row') continue;
    if (ruleMatchesRow(row, rule, fields)) return styleOf(rule);
  }
  return undefined;
}

/** 侧边通道：仅 apply=side */
export function resolveRowSideColor(
  row: Record<string, unknown>,
  rules: ViewFormatRule[],
  fields: FieldMeta[],
): string | undefined {
  for (const rule of rules) {
    if (rule.apply !== 'side') continue;
    if (ruleMatchesRow(row, rule, fields)) return rule.color;
  }
  return undefined;
}

/** 卡片标题行：仅 apply=row */
export function resolveCardTitleFormat(
  row: Record<string, unknown>,
  rules: ViewFormatRule[],
  fields: FieldMeta[],
): CellFormatStyle | undefined {
  for (const rule of rules) {
    if (rule.apply !== 'row') continue;
    if (ruleMatchesRow(row, rule, fields)) return styleOf(rule);
  }
  return undefined;
}

export function resolveCardTitleFormatColor(
  row: Record<string, unknown>,
  rules: ViewFormatRule[],
  fields: FieldMeta[],
): string | undefined {
  return resolveCardTitleFormat(row, rules, fields)?.color;
}
