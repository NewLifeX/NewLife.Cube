import type { ViewFilter, ViewSort } from '@/core/utils/viewProfile';

/**
 * 单字段写回后是否要整表刷新。
 * 字段参与当前排序或筛选时，行序和可见集合会变，必须重新取列表。
 */
export function shouldReloadAfterPatch(
  fieldName: string | null | undefined,
  sorts: ViewSort[] | undefined,
  filter: ViewFilter | undefined,
): boolean {
  const name = (fieldName || '').trim();
  if (!name) return true;
  const same = (other?: string) => !!other && other.toLowerCase() === name.toLowerCase();
  if ((sorts || []).some((s) => same(s.field))) return true;
  if ((filter?.conditions || []).some((c) => same(c.field))) return true;
  return false;
}
