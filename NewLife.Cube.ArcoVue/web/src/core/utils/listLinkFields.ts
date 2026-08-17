import type { FieldMeta } from '../types/field';
import type { OpsCustomLink } from './opsAction';

/** GetPage.list Url 字段分流种类（OSC-2608178bdb 方案 E） */
export type ListLinkKind = 'none' | 'cell' | 'opsNav' | 'opsAction';

/** 操作列自定义链接直出上限（其余进「更多」） */
export const OPS_LINK_INLINE_MAX = 2;

/** 按 Url / dataAction / hasTypeName 分流 */
export function classifyListLink(field: FieldMeta): ListLinkKind {
  const url = field.url?.trim();
  if (!url) return 'none';
  if (field.dataAction?.trim()) return 'opsAction';
  if (field.hasTypeName === true) return 'cell';
  return 'opsNav';
}

/** 是否应出现在数据列（合成 ops 链接列剔除） */
export function isDataListField(field: FieldMeta): boolean {
  const kind = classifyListLink(field);
  return kind === 'none' || kind === 'cell';
}

/** FieldMeta → 操作列自定义链接 */
export function toOpsCustomLink(field: FieldMeta): OpsCustomLink | null {
  const kind = classifyListLink(field);
  if (kind !== 'opsNav' && kind !== 'opsAction') return null;
  const url = field.url?.trim();
  if (!url) return null;
  return {
    name: field.name,
    label: field.displayName || field.name,
    url,
    target: field.target,
    dataAction: field.dataAction?.trim() || undefined,
  };
}

/** 一次拆分：数据列字段 + 操作列链接 */
export function partitionListFields(fields: FieldMeta[]): {
  dataFields: FieldMeta[];
  opsLinks: OpsCustomLink[];
} {
  const dataFields: FieldMeta[] = [];
  const opsLinks: OpsCustomLink[] = [];
  for (const f of fields) {
    if (!f.name) continue;
    const link = toOpsCustomLink(f);
    if (link) {
      opsLinks.push(link);
      continue;
    }
    dataFields.push(f);
  }
  return { dataFields, opsLinks };
}
