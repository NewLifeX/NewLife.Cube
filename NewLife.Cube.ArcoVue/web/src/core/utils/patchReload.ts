import type { ViewFilter, ViewSort } from '@/core/utils/viewProfile';

/** 看板 / 日历 / 甘特映射里，改了就会改变行所在位置的字段 */
export interface ReloadWatchSource {
  groupField?: string;
  startField?: string;
  plannedStartField?: string;
  plannedEndField?: string;
  actualStartField?: string;
  actualEndField?: string;
}

function sameName(a: string, b?: string): boolean {
  return !!b && b.trim().toLowerCase() === a.toLowerCase();
}

function pick(value?: string): string[] {
  const name = (value || '').trim();
  return name ? [name] : [];
}

/**
 * 当前视图里，写回后必须整表刷新的字段。
 * 表格和卡片没有这类字段；树视图由调用方一律刷新，不走这里。
 */
export function reloadWatchFields(
  kind: string,
  mapping: ReloadWatchSource | null | undefined,
): string[] {
  const source = mapping || {};
  if (kind === 'kanban') return pick(source.groupField);
  if (kind === 'calendar') return pick(source.startField);
  if (kind === 'gantt') {
    return [
      ...pick(source.plannedStartField),
      ...pick(source.plannedEndField),
      ...pick(source.actualStartField),
      ...pick(source.actualEndField),
      ...pick(source.groupField),
    ];
  }
  return [];
}

/**
 * 一次写回后是否要整表刷新。
 * 没有字段名时保守刷新。任一字段命中排序、筛选或视图观察字段时刷新。
 */
export function shouldReloadAfterWrite(
  fieldNames: Array<string | null | undefined> | undefined,
  sorts: ViewSort[] | undefined,
  filter: ViewFilter | undefined,
  watchFields: Array<string | null | undefined> | undefined,
): boolean {
  const names = (fieldNames || []).map((name) => (name || '').trim()).filter(Boolean);
  if (!names.length) return true;
  return names.some(
    (name) =>
      (sorts || []).some((sort) => sameName(name, sort.field)) ||
      (filter?.conditions || []).some((condition) => sameName(name, condition.field)) ||
      (watchFields || []).some((field) => sameName(name, field || undefined)),
  );
}

/**
 * 单字段写回后是否要整表刷新。
 * 字段参与当前排序或筛选时，行序和可见集合会变，必须重新取列表。
 */
export function shouldReloadAfterPatch(
  fieldName: string | null | undefined,
  sorts: ViewSort[] | undefined,
  filter: ViewFilter | undefined,
): boolean {
  return shouldReloadAfterWrite([fieldName], sorts, filter, []);
}
