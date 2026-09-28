/** 服务端排序列（与视图 sorts 一致，最多 3 列） */
export interface ServerSort {
  field: string;
  desc: boolean;
}

/** VTable sortState 单项。order 用 asc/desc，不触发内部排序 */
export interface VTableSortItem {
  field: string;
  order: 'asc' | 'desc';
}

/**
 * 把视图排序写成 VTable 表头状态。
 * 空为 null；有列时始终返回数组，便于 multipleSort 下每一列都画出对应图标。
 */
export function toVTableSortState(
  sorts: ServerSort[] | ServerSort | null | undefined,
): VTableSortItem[] | null {
  const list = (Array.isArray(sorts) ? sorts : sorts?.field ? [sorts] : [])
    .filter((s) => !!s?.field)
    .slice(0, 3)
    .map((s) => ({ field: s.field, order: s.desc ? ('desc' as const) : ('asc' as const) }));
  return list.length ? list : null;
}

/**
 * 写入 options.sortState / setRecords 的值。空为 []，不能是 null。
 * multipleSort 下 setRecords 会把 null 包成 [null]，setSortState 读 item.field 抛错，
 * 表格构造中断，列表停在 loading。
 */
export function toVTableSortOption(
  sorts: ServerSort[] | ServerSort | null | undefined,
): VTableSortItem[] {
  return toVTableSortState(sorts) ?? [];
}

/** 排序签名。监听用字符串，避免每次新建数组都触发 setRecords。 */
export function vtableSortKey(sorts: ServerSort[] | ServerSort | null | undefined): string {
  return toVTableSortOption(sorts)
    .map((s) => `${s.field}:${s.order}`)
    .join(',');
}
