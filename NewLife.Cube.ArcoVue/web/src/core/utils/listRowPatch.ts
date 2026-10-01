import { getValueByKey, setValueByKey, toPascalAndCamel } from './url';

function isPkKey(key: string, pkField: string): boolean {
  const pk = pkField.trim().toLowerCase();
  if (!pk) return false;
  if (key.trim().toLowerCase() === pk) return true;
  return toPascalAndCamel(key).trim().toLowerCase() === pk;
}

/**
 * 把保存结果写回已有行。主键不覆盖。键名按已有键、大小写翻转、原键的顺序落点。
 */
export function assignRowFields(
  row: Record<string, unknown>,
  patch: Record<string, unknown>,
  pkField: string,
): void {
  for (const [key, value] of Object.entries(patch)) {
    if (isPkKey(key, pkField)) continue;
    setValueByKey(row, key, value);
  }
}

/**
 * 按主键从当前页去掉一行。找不到时原样返回。
 */
export function removeListRow(
  rows: Record<string, unknown>[],
  pkField: string,
  id: unknown,
): { rows: Record<string, unknown>[]; removed: boolean } {
  const target = id == null ? '' : String(id);
  let removed = false;
  const next = rows.filter((row) => {
    const value = getValueByKey(row, pkField);
    if (value != null && String(value) === target) {
      removed = true;
      return false;
    }
    return true;
  });
  return { rows: removed ? next : rows, removed };
}

/**
 * 单行删除后的跟进。本页还有行，或已经在第一页，就只改本地；否则回到上一页再取数。
 */
export function deleteFollowUp(pageIndex: number, remaining: number): 'local' | 'prevPage' {
  if (remaining > 0 || pageIndex <= 0) return 'local';
  return 'prevPage';
}
