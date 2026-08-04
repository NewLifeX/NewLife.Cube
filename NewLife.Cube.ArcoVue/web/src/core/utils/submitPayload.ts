import type { FieldMeta } from '../types/field';
import { serializeSubmitModel } from './fieldControl';

function isEmptyValue(v: unknown): boolean {
  return v === undefined || v === null || v === '';
}

/**
 * 组装新增/编辑提交体：
 * - 多选数组序列化 + 类型归一化（serializeSubmitModel）
 * - 新增时去掉自增主键（避免 Identity 约定冲突）
 * - 空值矩阵（OSC-0008）：String 字段空值提交 ""（对齐 Cube.Vue，避免 DB NOT NULL 报错）；
 *   数值/布尔/日期等空值不提交（避免 JSON 绑到 Int32 失败或覆盖默认值）
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
      // 可空 String 提交空串，避免数据库 NOT NULL 报错；数值/布尔/日期等空值不提交
      if (typeName === 'String') {
        out[key] = '';
      }
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
