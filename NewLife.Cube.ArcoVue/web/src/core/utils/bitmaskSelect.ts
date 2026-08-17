/**
 * Int32 + multipleSelect 位掩码字段（如 CaptchaScene）：表单存 number，多选 UI 用 string[]。
 */

function itemTypeOf(field: { itemType?: string }): string {
  return (field.itemType ?? '').trim().toLowerCase();
}

/** 是否按位掩码多选（Int32 + multipleSelect + 数字键） */
export function isBitmaskMultiSelect(field: {
  name?: string;
  typeName?: string;
  itemType?: string;
  multiple?: boolean;
  dataSource?: Record<string, string>;
}): boolean {
  if ((field.typeName || '').trim() !== 'Int32') return false;
  const multi = !!field.multiple || itemTypeOf(field) === 'multipleselect';
  if (!multi) return false;
  const ds = field.dataSource;
  if (!ds || !Object.keys(ds).length) return false;
  return Object.keys(ds).every((k) => /^-?\d+$/.test(k));
}

/** number → 选中的位键（如 3 → ['1','2']） */
export function bitmaskToKeys(value: unknown, keys: string[]): string[] {
  const n = Number(value);
  if (!Number.isFinite(n) || n === 0) return [];
  return keys.filter((k) => {
    const bit = Number(k);
    return bit !== 0 && (n & bit) === bit;
  });
}

/** 选中键 → OR 合并 number */
export function keysToBitmask(keys: unknown): number {
  if (!Array.isArray(keys)) {
    const n = Number(keys);
    return Number.isFinite(n) ? n : 0;
  }
  let acc = 0;
  for (const k of keys) {
    const bit = Number(k);
    if (Number.isFinite(bit)) acc |= bit;
  }
  return acc;
}
