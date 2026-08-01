import type { FieldMeta } from '../types/field';
import { serializeSubmitModel } from './fieldControl';

const NUMERIC_TYPES = new Set([
  'Int32',
  'Int64',
  'Decimal',
  'Double',
  'Single',
  'Byte',
  'Int16',
  'UInt32',
  'UInt64',
]);

function isEmptyValue(v: unknown): boolean {
  return v === undefined || v === null || v === '';
}

/**
 * 组装新增/编辑提交体：
 * - 多选数组序列化
 * - 新增时去掉自增主键（避免 Identity 约定冲突）
 * - 去掉空字符串；数值字段空值不提交（避免 JSON 绑到 Int32 失败）
 */
export function prepareSubmitPayload(
  model: Record<string, unknown>,
  fields: FieldMeta[],
  options: { mode: 'add' | 'edit'; pkField: string },
): Record<string, unknown> {
  const serialized = serializeSubmitModel({ ...model }, fields);
  const fieldMap = new Map(fields.map((f) => [f.name, f]));
  const out: Record<string, unknown> = {};

  for (const [key, value] of Object.entries(serialized)) {
    if (options.mode === 'add' && key.toLowerCase() === options.pkField.toLowerCase()) {
      continue;
    }

    const meta = fieldMap.get(key);
    const typeName = meta?.typeName ?? '';

    if (isEmptyValue(value)) {
      // 空字符串不要发给数值列；可选字符串省略由后端保持默认
      if (NUMERIC_TYPES.has(typeName)) continue;
      if (value === '') continue;
      continue;
    }

    out[key] = value;
  }

  return out;
}

/** 与后端 ValidateEntityFields 一致：非空列（非主键）视为必填 */
export function isFieldRequired(field: FieldMeta): boolean {
  return !field.primaryKey && field.nullable === false;
}
