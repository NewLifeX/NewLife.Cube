/**
 * 地图悬停卡片条目构建（OSC-261004d7f4 修复，2026-10-05）。
 *
 * 与卡片视图同源（`features/views/cardHelpers.findBodyField`）：
 * 字段查找大小写不敏感；未命中字段时按列 key 直接容错取值（getValueByKey），
 * 避免视图列 key 与字段名大小写不一致（如 kind vs Kind）时整列渲染为 “-”。
 */
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import { formatFieldValue } from '@/core/utils/fieldFormat';
import { getValueByKey } from '@/core/utils/url';

export interface HoverEntry {
  label: string;
  value: string;
}

/** 大小写不敏感字段查找（对齐卡片视图 findBodyField 语义） */
export function findHoverField(fields: readonly FieldMeta[], key: string): FieldMeta | undefined {
  const k = (key || '').toLowerCase();
  if (!k) return undefined;
  return fields.find((f) => (f.name || '').toLowerCase() === k);
}

/** 单列取值：字段命中走完整格式化；未命中按列 key 容错取值（同卡片视图兜底） */
export function hoverFieldValue(
  row: Record<string, unknown>,
  fields: readonly FieldMeta[],
  key: string,
): string {
  const field = findHoverField(fields, key);
  if (field) return formatFieldValue(field, row);
  const raw = getValueByKey(row, key);
  return raw == null || raw === '' ? '-' : String(raw);
}

/** 构建可见列条目（剔除标题字段，最多 maxRows 行） */
export function buildHoverEntries(
  row: Record<string, unknown>,
  fields: readonly FieldMeta[],
  columns: readonly ColumnPref[],
  titles: Record<string, string> | undefined,
  titleField: string | undefined,
  maxRows = 10,
): HoverEntry[] {
  return columns
    .filter((c) => c.visible && c.key !== titleField)
    .slice(0, maxRows)
    .map((c) => ({
      label: titles?.[c.key] || findHoverField(fields, c.key)?.displayName || c.key,
      value: hoverFieldValue(row, fields, c.key),
    }));
}

/** 标题：优先 titleField（字段命中走格式化），回落主键，再回落「详情」 */
export function resolveHoverTitle(
  row: Record<string, unknown>,
  fields: readonly FieldMeta[],
  titleField: string | undefined,
  rowKey: string,
): string {
  const tf = titleField?.trim();
  if (tf) {
    const field = findHoverField(fields, tf);
    if (field) return formatFieldValue(field, row);
    const v = getValueByKey(row, tf);
    if (v != null && v !== '') return String(v);
  }
  const pk = getValueByKey(row, rowKey);
  return pk == null || pk === '' ? '详情' : String(pk);
}
