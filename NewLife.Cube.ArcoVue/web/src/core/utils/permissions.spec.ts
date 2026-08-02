import { describe, expect, it } from 'vitest';
import { Auth, resolveCrudFlags } from './permissions';

describe('resolveCrudFlags', () => {
  it('allows all when perms empty', () => {
    const f = resolveCrudFlags({}, null);
    expect(f.canAdd && f.canEdit && f.canDelete).toBe(true);
  });

  it('respects numeric Auth keys', () => {
    const f = resolveCrudFlags({ [String(Auth.ADD)]: '新增' }, null);
    expect(f.canAdd).toBe(true);
    expect(f.canEdit).toBe(false);
    expect(f.canDelete).toBe(false);
  });

  it('maps export/import to backend Detail/Insert when 16/32 absent', () => {
    const f = resolveCrudFlags(
      {
        [String(Auth.VIEW)]: '查看',
        [String(Auth.ADD)]: '新增',
        [String(Auth.EDIT)]: '编辑',
      },
      null,
    );
    expect(f.canExport).toBe(true);
    expect(f.canImport).toBe(true);
    expect(f.canDelete).toBe(false);
  });

  it('honors pageSetting isReadOnly / enableAdd', () => {
    const perms = {
      [String(Auth.ADD)]: '新增',
      [String(Auth.EDIT)]: '编辑',
      [String(Auth.DELETE)]: '删除',
    };
    expect(resolveCrudFlags(perms, { isReadOnly: true }).canAdd).toBe(false);
    expect(resolveCrudFlags(perms, { isReadOnly: true }).canImport).toBe(false);
    expect(resolveCrudFlags(perms, { enableAdd: false }).canAdd).toBe(false);
    expect(resolveCrudFlags(perms, { enableAdd: true }).canAdd).toBe(true);
  });
});
