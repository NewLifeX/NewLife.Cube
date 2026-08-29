import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import { isBooleanToggleField, resolveCellBadge, resolveCellLabel, type CellBadge } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';
import type { CardMapping, KanbanMapping } from '@/core/utils/viewMapping';

export type CardBodyField = {
  key: string;
  label: string;
  value: string;
  /** 长文本/多行文本独占整行 */
  fullRow: boolean;
  /** 状态/枚举/值集徽标（卡片/看板也显示徽章）；null 表示纯文本 */
  badge?: CellBadge | null;
  /** 启用/Enable 徽标：可点击切换启用/禁用 */
  enableToggle?: boolean;
};

/** 多行/富文本控件判定集；命中即整行，与文本长度无关 */
const FULL_ROW_ITEM_TYPES = new Set(['textarea', 'multiline', 'richtext', 'html']);

/** 备注/说明/评论等语义字段名或显示名，无视列数一律整行 */
const FULL_ROW_NAME_RE =
  /^(remark|remarks|comment|comments|description|desc|note|notes|memo)$|备注|说明|评论|描述|附注/i;

/** 长字段判定：语义长文本字段、多行/富文本控件，或格式化值不少于 33 个 Unicode 码位 */
export function isCardBodyFieldFullRow(
  field: FieldMeta | undefined,
  value: string,
): boolean {
  const itemType = (field?.itemType || '').toLowerCase();
  if (FULL_ROW_ITEM_TYPES.has(itemType)) return true;
  const name = field?.name || '';
  const label = field?.displayName || '';
  if (FULL_ROW_NAME_RE.test(name) || FULL_ROW_NAME_RE.test(label)) return true;
  return Array.from(value).length >= 33;
}

function findBodyField(fields: FieldMeta[], key: string): FieldMeta | undefined {
  const k = (key || '').toLowerCase();
  if (!k) return undefined;
  return fields.find((f) => (f.name || '').toLowerCase() === k);
}

export function buildCardBodyFields(
  record: Record<string, unknown>,
  columns: ColumnPref[],
  fields: FieldMeta[],
  exclude: string[],
  format?: (field: FieldMeta, record: Record<string, unknown>) => string,
): CardBodyField[] {
  const skip = new Set(
    exclude.filter(Boolean).map((x) => String(x).toLowerCase()),
  );
  const out: CardBodyField[] = [];
  for (const col of columns) {
    if (!col.visible || skip.has((col.key || '').toLowerCase())) continue;
    const field = findBodyField(fields, col.key);
    const label =
      col.title?.trim() || field?.displayName?.trim() || field?.name || col.key;
    let value = '-';
    let badge: CellBadge | null = null;
    if (field && format) value = format(field, record);
    else if (field) {
      const raw = getValueByKey(record, field.name);
      value = raw == null || raw === '' ? '-' : resolveCellLabel(field, raw);
    } else {
      const raw = getValueByKey(record, col.key);
      value = raw == null || raw === '' ? '-' : String(raw);
    }
    // 状态/枚举/值集字段在卡片/看板也渲染为徽标（与列表一致）
    if (field) {
      const raw = getValueByKey(record, field.name);
      if (raw != null && raw !== '') badge = resolveCellBadge(field, raw);
    }
    out.push({
      key: col.key,
      label,
      value,
      fullRow: isCardBodyFieldFullRow(field, value),
      badge,
      // Boolean 字段徽标（Enable 及任意 Boolean 字段）：有 Update 权限时可点击切换状态
      enableToggle: !!field && isBooleanToggleField(field),
    });
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
