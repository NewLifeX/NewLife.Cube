import { FILTER_OP_LABELS } from '@/core/utils/filterBuilder';
import { emptyViewFilter, normalizeFilter, type ViewFilter, type ViewFilterOp } from '@/core/utils/viewProfile';

/** 解析定义 StartFilter / XOR case.filter（JSON 或对象） */
export function parseViewFilterJson(raw: unknown): ViewFilter {
  if (raw == null || raw === '' || raw === '{}') return emptyViewFilter();
  if (typeof raw === 'string') {
    try {
      return normalizeFilter(JSON.parse(raw));
    } catch {
      return emptyViewFilter();
    }
  }
  return normalizeFilter(raw);
}

export function stringifyViewFilter(filter: ViewFilter | null | undefined): string {
  const f = normalizeFilter(filter);
  if (!f.conditions.length) return '{}';
  return JSON.stringify(f);
}

export function viewFilterHasRules(filter: ViewFilter | null | undefined): boolean {
  return (filter?.conditions?.length ?? 0) > 0;
}

/** 人话摘要：「金额 大于 10000」；空则「不限制」 */
export function viewFilterSummary(raw: unknown, fieldLabel?: (name: string) => string): string {
  const f = parseViewFilterJson(raw);
  if (!f.conditions.length) return '不限制';
  const parts = f.conditions.map((c) => {
    const field = fieldLabel?.(c.field) || c.field;
    const op = FILTER_OP_LABELS[c.op as ViewFilterOp] || c.op;
    if (c.op === 'isNull' || c.op === 'notNull') return `${field} ${op}`;
    const val = Array.isArray(c.value) ? c.value.join('/') : String(c.value ?? '');
    return `${field} ${op} ${val}`;
  });
  const join = f.logic === 'any' ? ' 或 ' : ' 且 ';
  return parts.join(join);
}
