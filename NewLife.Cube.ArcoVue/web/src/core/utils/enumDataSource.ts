import type { FieldOption } from '@/core/types/field';

const NUMERIC_KEY = /^-?\d+$/;

/**
 * 把 GetPage dataSource 转成批量修改/下拉选项。
 * PrepareForApi 对枚举同时写入数字键与枚举名（同 label），只保留数字键，避免提交中文名。
 */
export function dataSourceToOptions(ds: Record<string, string> | undefined | null): FieldOption[] {
  if (!ds) return [];
  const entries = Object.entries(ds);
  if (entries.length === 0) return [];
  const numeric = entries.filter(([k]) => NUMERIC_KEY.test(k));
  if (numeric.length > 0 && numeric.length < entries.length) {
    const seen = new Set<string>();
    return numeric
      .filter(([, label]) => {
        if (seen.has(label)) return false;
        seen.add(label);
        return true;
      })
      .map(([value, label]) => ({ value, label }));
  }
  return entries.map(([value, label]) => ({ value, label }));
}
