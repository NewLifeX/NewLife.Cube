import type { FieldMeta } from '@/core/types/field';
import { getValueByKey } from '@/core/utils/url';

/** 甘特分组父节点。不是业务记录，id 以 `__group:` 开头 */
export interface GanttGroupParent {
  __ganttGroup: true;
  id: string;
  title: string;
  children: Record<string, unknown>[];
}

/**
 * 按字段把记录包进只读分组父节点（OSC-26092694a1）。
 * 无 field 时原样返回，不合成父节点。空值归入「未分组」。
 */
export function groupGanttRecords(
  records: Record<string, unknown>[],
  field: string | null | undefined,
  fields: FieldMeta[],
): Array<Record<string, unknown> | GanttGroupParent> {
  const name = (field || '').trim();
  if (!name) return records;

  const meta = fields.find((f) => f.name === name);
  const ds = meta?.dataSource;
  const order: string[] = [];
  const buckets = new Map<string, Record<string, unknown>[]>();
  const empty: Record<string, unknown>[] = [];

  for (const row of records) {
    const raw = getValueByKey(row, name);
    if (raw == null || raw === '') {
      empty.push(row);
      continue;
    }
    const key = String(raw);
    if (!buckets.has(key)) {
      buckets.set(key, []);
      order.push(key);
    }
    buckets.get(key)!.push(row);
  }

  const groups: GanttGroupParent[] = order.map((key) => ({
    __ganttGroup: true,
    id: `__group:${key}`,
    title: ds?.[key] || key,
    children: buckets.get(key)!,
  }));
  if (empty.length) {
    groups.push({
      __ganttGroup: true,
      id: '__group:',
      title: '未分组',
      children: empty,
    });
  }
  return groups;
}

export function isGanttGroupParent(row: unknown): row is GanttGroupParent {
  return !!row && typeof row === 'object' && (row as GanttGroupParent).__ganttGroup === true;
}
