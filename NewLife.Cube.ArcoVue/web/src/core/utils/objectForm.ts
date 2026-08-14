/**
 * 通用对象表单纯函数（OSC-2608139feb）。
 *
 * DefaultObject 的表单组织与保存归一：
 * - groupFieldsByCategory：按后端 DataField.Category 分组，空 Category 归入「基本」，
 *   组顺序与字段原序稳定（不重排）
 * - mergeObjectModel：保存时以表单字段键覆盖原对象，保留未建模的嵌套属性
 */
import type { FieldMeta } from '../types/field';

export interface ObjectFieldGroup {
  category: string;
  fields: FieldMeta[];
}

/** 默认分组名（后端 Category 为空时） */
export const DEFAULT_CATEGORY = '基本';

/**
 * 按 Category 分组字段。空 Category → 默认组；组顺序=首次出现顺序，组内顺序=字段原序。
 * @param fields 字段元数据列表
 */
export function groupFieldsByCategory(fields: FieldMeta[]): ObjectFieldGroup[] {
  const groups: ObjectFieldGroup[] = [];
  const index = new Map<string, number>();
  for (const f of fields) {
    const category = (f.category ?? '').trim() || DEFAULT_CATEGORY;
    let gi = index.get(category);
    if (gi === undefined) {
      gi = groups.length;
      index.set(category, gi);
      groups.push({ category, fields: [] });
    }
    groups[gi].fields.push(f);
  }
  return groups;
}

/**
 * 保存模型合并：以表单字段键覆盖原对象同名键；原对象中未被表单覆盖的键原样保留。
 * @param original 后端返回的原始对象
 * @param form 归一化后的表单值（键=字段名）
 */
export function mergeObjectModel(
  original: Record<string, unknown>,
  form: Record<string, unknown>,
): Record<string, unknown> {
  return { ...original, ...form };
}
