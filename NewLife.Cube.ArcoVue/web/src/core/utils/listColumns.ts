import type { FieldMeta } from '../types/field';
import { isDataListField } from './listLinkFields';

/**
 * 敏感列不进可见列（OSC-2608273d95）。
 * 后端 GetPage 下发 sensitive=true 表示当前用户无敏感字段查看权限；
 * 藏列只是展示策略，授权与脱敏以服务端为准。
 */
export function rejectSensitiveColumns(fields: FieldMeta[]): FieldMeta[] {
  return fields.filter((f) => f.sensitive !== true);
}

/**
 * GetPage.list 中的字段即为列表列。
 * 不可根据 DataField.visible 过滤：后端 Visible 默认 false，Fill 不会置 true。
 * OSC-2608178bdb：合成 Url/dataAction 操作链接不进数据列。
 * OSC-2608273d95：敏感列不进可见列。
 */
export function selectListColumns(fields: FieldMeta[]): FieldMeta[] {
  return rejectSensitiveColumns(fields).filter((f) => !!f.name && isDataListField(f));
}
