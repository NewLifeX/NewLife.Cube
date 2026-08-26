import { getValueByKey } from './url';
import { normalizeIamTypePath } from './rolePermission';

function isSystemFlag(record: Record<string, unknown> | null | undefined): boolean {
  if (!record) return false;
  const v = getValueByKey(record, 'isSystem') ?? getValueByKey(record, 'IsSystem');
  return v === true || v === 1 || v === '1' || v === 'true';
}

/** 系统角色行不提供删除 */
export function isIamRowActionDisabled(
  typePath: unknown,
  record: Record<string, unknown> | null | undefined,
  action: string,
): boolean {
  if (action !== 'delete') return false;
  if (normalizeIamTypePath(typePath) !== 'admin/role') return false;
  return isSystemFlag(record ?? undefined);
}

/** 编辑系统角色时锁定名称 */
export function isSystemRoleNameLocked(
  typePath: unknown,
  model: Record<string, unknown> | null | undefined,
  fieldName: unknown,
): boolean {
  if (normalizeIamTypePath(typePath) !== 'admin/role') return false;
  if (String(fieldName ?? '').toLowerCase() !== 'name') return false;
  return isSystemFlag(model ?? undefined);
}

/** 编辑已有系统角色时禁止关掉 isSystem */
export function isSystemRoleFlagLocked(
  typePath: unknown,
  model: Record<string, unknown> | null | undefined,
  fieldName: unknown,
  mode: string | undefined,
): boolean {
  if (mode !== 'edit') return false;
  if (normalizeIamTypePath(typePath) !== 'admin/role') return false;
  if (String(fieldName ?? '').toLowerCase() !== 'issystem') return false;
  return isSystemFlag(model ?? undefined);
}

/** 批量删除选中行含系统角色时整批拒绝 */
export function isIamBatchDeleteBlocked(
  typePath: unknown,
  selectedRows: Record<string, unknown>[],
): boolean {
  return selectedRows.some((row) => isIamRowActionDisabled(typePath, row, 'delete'));
}

const SELF_ONLY_ALERT =
  '当前数据权限仅允许查看自己的账号。管理全部用户需要系统角色或调整数据权限。';

export function selfOnlyUserAlertText(): string {
  return SELF_ONLY_ALERT;
}

/** 非系统用户在用户列表仅看见自己时展示说明 */
export function shouldShowSelfOnlyUserAlert(input: {
  typePath: unknown;
  isSystemUser: boolean | undefined;
  total: number;
  rows: Record<string, unknown>[];
  currentUserId: unknown;
}): boolean {
  if (normalizeIamTypePath(input.typePath) !== 'admin/user') return false;
  if (input.isSystemUser === true) return false;
  const n = input.rows.length;
  if (n !== 1) return false;
  const total = Number(input.total);
  const onlyOne = total === 1 || !Number.isFinite(total) || total === 0;
  if (!onlyOne) return false;
  const row = input.rows[0];
  const rowId = getValueByKey(row, 'id') ?? getValueByKey(row, 'ID');
  if (rowId == null || rowId === '' || input.currentUserId == null || input.currentUserId === '') {
    return false;
  }
  return String(rowId) === String(input.currentUserId);
}
