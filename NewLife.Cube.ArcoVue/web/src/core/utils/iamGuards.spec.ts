import { describe, expect, it } from 'vitest';
import {
  isIamBatchDeleteBlocked,
  isIamRowActionDisabled,
  isSystemRoleFlagLocked,
  isSystemRoleNameLocked,
  selfOnlyUserAlertText,
  shouldShowSelfOnlyUserAlert,
} from './iamGuards';

describe('isIamRowActionDisabled', () => {
  it('hides delete on system roles', () => {
    expect(isIamRowActionDisabled('/Admin/Role', { isSystem: true }, 'delete')).toBe(true);
    expect(isIamRowActionDisabled('Admin/Role', { IsSystem: true }, 'delete')).toBe(true);
  });

  it('does not intercept other entities or missing isSystem', () => {
    expect(isIamRowActionDisabled('/Admin/Role', { isSystem: false }, 'delete')).toBe(false);
    expect(isIamRowActionDisabled('/Admin/Role', {}, 'delete')).toBe(false);
    expect(isIamRowActionDisabled('/Admin/User', { isSystem: true }, 'delete')).toBe(false);
    expect(isIamRowActionDisabled('/Admin/Role', { isSystem: true }, 'edit')).toBe(false);
  });
});

describe('isSystemRoleNameLocked', () => {
  it('locks name on system role', () => {
    expect(isSystemRoleNameLocked('/Admin/Role', { isSystem: true }, 'Name')).toBe(true);
    expect(isSystemRoleNameLocked('/Admin/Role', { isSystem: true }, 'remark')).toBe(false);
  });
});

describe('isSystemRoleFlagLocked', () => {
  it('locks isSystem only when editing', () => {
    expect(isSystemRoleFlagLocked('/Admin/Role', { isSystem: true }, 'isSystem', 'edit')).toBe(true);
    expect(isSystemRoleFlagLocked('/Admin/Role', { isSystem: true }, 'isSystem', 'add')).toBe(false);
  });
});

describe('isIamBatchDeleteBlocked', () => {
  it('rejects the whole batch when any selected role is system', () => {
    expect(
      isIamBatchDeleteBlocked('/Admin/Role', [
        { isSystem: false },
        { IsSystem: true },
      ]),
    ).toBe(true);
    expect(isIamBatchDeleteBlocked('/Admin/Role', [{ isSystem: false }])).toBe(false);
    expect(isIamBatchDeleteBlocked('/Admin/User', [{ isSystem: true }])).toBe(false);
  });
});

describe('shouldShowSelfOnlyUserAlert', () => {
  const text =
    '当前数据权限仅允许查看自己的账号。管理全部用户需要系统角色或调整数据权限。';

  it('shows for non-system user seeing only self', () => {
    expect(selfOnlyUserAlertText()).toBe(text);
    expect(
      shouldShowSelfOnlyUserAlert({
        typePath: 'Admin/User',
        isSystemUser: false,
        total: 1,
        rows: [{ id: 8 }],
        currentUserId: 8,
      }),
    ).toBe(true);
  });

  it('hides for system users or extra rows', () => {
    expect(
      shouldShowSelfOnlyUserAlert({
        typePath: 'Admin/User',
        isSystemUser: true,
        total: 1,
        rows: [{ id: 8 }],
        currentUserId: 8,
      }),
    ).toBe(false);
    expect(
      shouldShowSelfOnlyUserAlert({
        typePath: 'Admin/User',
        isSystemUser: false,
        total: 2,
        rows: [{ id: 8 }],
        currentUserId: 8,
      }),
    ).toBe(false);
  });
});
