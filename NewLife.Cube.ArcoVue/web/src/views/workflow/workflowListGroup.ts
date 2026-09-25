/**
 * 审批列表分组：与实体列表同一套多级分组和时间分桶。
 * 分组作用在当前页已加载的数据上。
 */
import type { FieldMeta } from '@/core/types/field';
import { groupRows, isGroupHeaderRow } from '@/core/utils/viewMapping';
import { stripTimeBucketLabel, TIME_BUCKET_LABELS, timeBucketOf, UNKNOWN_TIME_LABEL } from '@/core/utils/timeBucket';

export interface WorkflowGroupRow {
  __group?: boolean;
  __groupLabel?: string;
  id?: string | number;
  [key: string]: unknown;
}

function bucketDataSource(): Record<string, string> {
  const ds: Record<string, string> = {};
  TIME_BUCKET_LABELS.forEach((label, i) => {
    ds[`${i}·${label}`] = label;
  });
  ds[`7·${UNKNOWN_TIME_LABEL}`] = UNKNOWN_TIME_LABEL;
  return ds;
}

function flatten(
  nodes: Record<string, unknown>[],
  fields: FieldMeta[],
): WorkflowGroupRow[] {
  const out: WorkflowGroupRow[] = [];
  for (const node of nodes) {
    if (!isGroupHeaderRow(node)) {
      out.push(node as WorkflowGroupRow);
      continue;
    }
    const fieldName = String(node.groupField ?? '');
    const original = fieldName.startsWith('__tb_') ? fieldName.slice(5) : fieldName;
    const fm = fields.find((f) => f.name === original);
    const isTime = fm?.typeName === 'DateTime';
    const valueLabel = isTime
      ? stripTimeBucketLabel(node.value) || '未明确时间'
      : String(node.label || '未分组');
    const text = isTime ? `${fm?.displayName || '时间'} 在 ${valueLabel}` : valueLabel;
    out.push({
      __group: true,
      id: `g:${String(node.path ?? text)}`,
      __groupLabel: `📁 ${text} (${node.count ?? 0})`,
    });
    const children = node.children;
    if (Array.isArray(children)) out.push(...flatten(children as Record<string, unknown>[], fields));
  }
  return out;
}

/** 按所选字段分组当前页；空分组返回原行 */
export function groupWorkflowRows(
  rows: Record<string, unknown>[],
  group: string[],
  fields: FieldMeta[],
): WorkflowGroupRow[] {
  if (!group.length || !rows.length) return rows as WorkflowGroupRow[];
  const timeNames = new Set(fields.filter((f) => f.typeName === 'DateTime').map((f) => f.name));
  const prepared = rows.map((row) => {
    const copy: Record<string, unknown> = { ...row };
    for (const name of group) {
      if (timeNames.has(name)) copy[`__tb_${name}`] = timeBucketOf(copy[name]);
    }
    return copy;
  });
  const names = group.map((name) => (timeNames.has(name) ? `__tb_${name}` : name));
  const meta = fields.map((f) =>
    timeNames.has(f.name) ? { ...f, name: `__tb_${f.name}`, dataSource: bucketDataSource() } : f,
  );
  return flatten(groupRows(prepared, names, meta) as Record<string, unknown>[], fields);
}
