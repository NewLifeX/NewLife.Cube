/**
 * 表单必填校验
 *
 * 各皮肤的新增/编辑表单都按字段元数据里的 `required` 拦截空值。校验口径统一收敛到本模块，
 * 避免各皮肤各写一套（Vue 皮肤已有等价实现，SPA 皮肤直接复用本函数）。
 */

import type { DataField } from '../api';
import type { FieldMapping } from '../field';

/** 校验错误：字段名与提示文案 */
export interface FieldValidationError {
  /** 字段名（提交时的键） */
  name: string;
  /** 提示文案 */
  message: string;
}

/** 判断字段值是否为空（空串、仅空白、空数组、null/undefined 都算空） */
export function isBlankValue(value: unknown): boolean {
  if (value === null || value === undefined) return true;
  if (typeof value === 'string') return value.trim() === '';
  if (Array.isArray(value)) return value.length === 0;

  return false;
}

/**
 * 校验字段列表中的必填项
 *
 * @param fields 表单字段（字段映射或字段元数据，按界面顺序）
 * @param values 当前表单值
 * @param keyOf 字段名到表单值键的映射。多数皮肤直接同名；个别皮肤把字段名转小驼峰后入表单
 * @returns 错误列表；全部通过时返回空数组
 */
export function validateRequiredFields(
  fields: Array<FieldMapping | DataField>,
  values: Record<string, unknown>,
  keyOf?: (name: string) => string,
): FieldValidationError[] {
  const errors: FieldValidationError[] = [];
  for (const mapping of fields) {
    // 兼容两种入参：字段映射（含 widget）与裸字段元数据
    const field = (mapping as FieldMapping).field ?? (mapping as DataField);
    if (!field?.required) continue;
    const key = keyOf ? keyOf(field.name) : field.name;
    if (!isBlankValue(values?.[key])) continue;
    errors.push({ name: key, message: `${field.displayName ?? field.name} 不能为空` });
  }

  return errors;
}
