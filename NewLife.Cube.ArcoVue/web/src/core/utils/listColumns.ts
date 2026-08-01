import type { FieldMeta } from '../types/field';

/**
 * GetPage.list 中的字段即为列表列。
 * 不可根据 DataField.visible 过滤：后端 Visible 默认 false，Fill 不会置 true。
 */
export function selectListColumns(fields: FieldMeta[]): FieldMeta[] {
  return fields.filter((f) => !!f.name);
}
