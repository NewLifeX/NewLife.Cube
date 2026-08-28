import type { FieldMeta } from '@/core/types/field';

/** 字段名归一：去空白、小写 */
function fieldNameOf(field: FieldMeta): string {
  return String(field.name ?? '').trim().toLowerCase();
}

/**
 * 详情抽屉审计字段去重：优先显示名称快照（创建者/更新者），
 * 当同批字段中已有名称列时隐藏对应 ID 列（创建用户/更新用户）。
 * GetPage 元数据不变；仅前端详情展示用。
 */
export function filterDetailAuditFields(fields: FieldMeta[]): FieldMeta[] {
  if (!fields?.length) return fields ?? [];

  const names = new Set(fields.map(fieldNameOf));
  const hasCreateName = names.has('createuser') || names.has('createusername');
  const hasUpdateName = names.has('updateuser') || names.has('updateusername');

  return fields.filter((field) => {
    const n = fieldNameOf(field);
    if (hasCreateName && n === 'createuserid') return false;
    if (hasUpdateName && n === 'updateuserid') return false;
    return true;
  });
}
