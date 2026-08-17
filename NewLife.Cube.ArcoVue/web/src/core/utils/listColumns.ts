import type { FieldMeta } from '../types/field';
import { isDataListField } from './listLinkFields';

/**
 * GetPage.list 中的字段即为列表列。
 * 不可根据 DataField.visible 过滤：后端 Visible 默认 false，Fill 不会置 true。
 * OSC-2608178bdb：合成 Url/dataAction 操作链接不进数据列。
 */
export function selectListColumns(fields: FieldMeta[]): FieldMeta[] {
  return fields.filter((f) => !!f.name && isDataListField(f));
}
