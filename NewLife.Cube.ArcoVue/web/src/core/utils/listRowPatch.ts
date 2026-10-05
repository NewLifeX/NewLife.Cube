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
 * 按主键写回字段，并换新数组引用（行对象复用）。
 * ListTable / 卡片为性能去掉了 records 的 deep watch；树视图展示行可能是 buildTree 克隆，
 * 必须改 tableData 源行并换数组引用，shallow watch 才会 setRecords / 重算树。
 */
export function patchRowInList(
  rows: Record<string, unknown>[],
  pkField: string,
  id: unknown,
  patch: Record<string, unknown>,
): { rows: Record<string, unknown>[]; row: Record<string, unknown> | null } {
  const target = id == null ? '' : String(id);
  let hit: Record<string, unknown> | null = null;
  for (const row of rows) {
    const value = getValueByKey(row, pkField);
    if (value == null || String(value) !== target) continue;
    assignRowFields(row, patch, pkField);
    hit = row;
    break;
  }
  return { rows: hit ? rows.slice() : rows, row: hit };
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
