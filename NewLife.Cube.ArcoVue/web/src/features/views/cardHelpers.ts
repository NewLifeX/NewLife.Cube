import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/entityViewProfile';
import { resolveCellLabel } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';
import type { CardMapping, KanbanMapping } from '@/core/utils/viewMapping';

export function buildCardBodyFields(
  record: Record<string, unknown>,
  columns: ColumnPref[],
  fields: FieldMeta[],
  exclude: string[],
  format?: (field: FieldMeta, record: Record<string, unknown>) => string,
): { key: string; label: string; value: string }[] {
  const skip = new Set(exclude.filter(Boolean));
  const out: { key: string; label: string; value: string }[] = [];
  for (const col of columns) {
    if (!col.visible || skip.has(col.key)) continue;
    const field = fields.find((f) => f.name === col.key);
    const label = col.title?.trim() || field?.displayName || col.key;
    let value = '-';
    if (field && format) value = format(field, record);
    else if (field) {
      const raw = getValueByKey(record, field.name);
      value = raw == null || raw === '' ? '-' : resolveCellLabel(field, raw);
    } else {
      const raw = record[col.key];
      value = raw == null || raw === '' ? '-' : String(raw);
    }
    out.push({ key: col.key, label, value });
    if (out.length >= 8) break;
  }
  return out;
}

export function resolveImageUrl(record: Record<string, unknown>, imageField?: string): string {
  if (!imageField) return '';
  const raw = getValueByKey(record, imageField) ?? record[imageField];
  if (raw == null || raw === '') return '';
  if (typeof raw === 'string') return raw;
  if (typeof raw === 'object' && raw && 'url' in (raw as object)) {
    return String((raw as { url?: unknown }).url || '');
  }
  return String(raw);
}

export function cardExcludeKeys(mapping: CardMapping | KanbanMapping): string[] {
  const keys = [mapping.titleField];
  if (mapping.imageField) keys.push(mapping.imageField);
  if (mapping.kind === 'kanban') keys.push(mapping.groupField);
  return keys;
}
