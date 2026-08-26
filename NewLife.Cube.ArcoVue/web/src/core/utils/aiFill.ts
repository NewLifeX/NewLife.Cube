import type { FieldMeta } from '@/core/types/field';
import { setValueByKey } from '@/core/utils/url';

/**
 * 将 fill_form values 合并进当前 add/edit 可写字段（大小写不敏感）。
 * 跳过主键与只读；返回已写入的显示名（用于成功提示）。
 */
export function mergeFillFormValues(
  model: Record<string, unknown>,
  values: Record<string, unknown> | null | undefined,
  fields: FieldMeta[],
): string[] {
  const filled: string[] = [];
  if (!values || typeof values !== 'object' || Array.isArray(values)) return filled;
  const byLower = new Map(fields.map((f) => [f.name.toLowerCase(), f]));
  for (const [key, val] of Object.entries(values)) {
    const field = byLower.get(key.toLowerCase());
    if (!field) continue;
    if (field.primaryKey || field.readOnly) continue;
    setValueByKey(model, field.name, val);
    filled.push(field.displayName || field.name);
  }
  return filled;
}

/** 解析 SSE fill_form 工具完成 payload；失败返回 null */
export function parseFillFormValue(raw: unknown): Record<string, unknown> | null {
  if (typeof raw !== 'string' || !raw.trim()) return null;
  try {
    const data = JSON.parse(raw) as { kind?: string; values?: unknown };
    if (data?.kind !== 'fill_form' || !data.values || typeof data.values !== 'object' || Array.isArray(data.values)) {
      return null;
    }
    return data.values as Record<string, unknown>;
  } catch {
    return null;
  }
}
