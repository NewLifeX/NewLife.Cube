import { describe, expect, it } from 'vitest';
import {
  buildPermForest,
  checkedKeysFromRole,
  isMenuPermissionField,
  isRolePermissionField,
  loadAllMenusForPermTree,
  normalizeIamTypePath,
  parseMenuPermissionCatalog,
  parseRolePermission,
  roleMapFromCheckedKeys,
  serializeRolePermission,
  togglePermKey,
  leafTitleColumnEm,
  collectPermKeysUnderNode,
  nodePermCheckState,
  toggleNodePerms,
  findPermNode,
} from './rolePermission';

describe('normalizeIamTypePath', () => {
  it('strips slashes and lowercases', () => {
    expect(normalizeIamTypePath('/Admin/Role')).toBe('admin/role');
    expect(normalizeIamTypePath('Admin\\Role')).toBe('admin/role');
  });
});

describe('isRolePermissionField', () => {
  it('matches Admin/Role Permission', () => {
    expect(isRolePermissionField('/Admin/Role', { name: 'Permission' })).toBe(true);
    expect(isRolePermissionField('Admin/Role', { name: 'remark' })).toBe(false);
    expect(isMenuPermissionField('/Admin/Menu', { name: 'permission' })).toBe(true);
  });
});

describe('parseMenuPermissionCatalog', () => {
  it('defaults to four flags when empty', () => {
    expect(parseMenuPermissionCatalog('')).toEqual([
      { flag: 1, name: '查看' },
      { flag: 2, name: '添加' },
      { flag: 4, name: '修改' },
      { flag: 8, name: '删除' },
    ]);
  });

  it('keeps custom flags such as 16', () => {
    const c = parseMenuPermissionCatalog('1#查看,16#导出');
    expect(c).toEqual([
      { flag: 1, name: '查看' },
      { flag: 16, name: '导出' },
    ]);
  });

  it('later duplicate flag wins', () => {
    expect(parseMenuPermissionCatalog('1#看,1#查看')).toEqual([{ flag: 1, name: '查看' }]);
  });
});

describe('parseRolePermission / serializeRolePermission', () => {
  it('sorts by menuId and drops invalid segments', () => {
    const map = parseRolePermission('15#7,12#x,12#3');
    expect(map.get(15)).toBe(7);
    expect(map.get(12)).toBe(3);
    expect(serializeRolePermission(map)).toBe('12#3,15#7');
  });

  it('empty map serializes to empty string', () => {
    expect(serializeRolePermission(new Map())).toBe('');
  });

  it('treats XCode All (-1) as all catalog flags, like CubeNC role.Has', () => {
    const forest = buildPermForest([{ id: 12, parentId: 0, name: 'A', permission: '' }]);
    const map = parseRolePermission('12#-1');
    expect(map.get(12)).toBe(0xffffffff);
    const keys = checkedKeysFromRole(map, forest);
    expect(keys.sort()).toEqual(['p:12:1', 'p:12:2', 'p:12:4', 'p:12:8']);
    expect(serializeRolePermission(map)).toBe('12#-1');
  });

  it('reads Permissions dictionary from Detail JSON', () => {
    const map = parseRolePermission({ permissions: { '12': 7, '15': 15 } });
    expect(map.get(12)).toBe(7);
    expect(map.get(15)).toBe(15);
  });
});

describe('buildPermForest', () => {
  it('attaches inline perms only on menus without children', () => {
    const forest = buildPermForest([
      { id: 1, parentId: 0, displayName: '系统', permission: '' },
      { id: 2, parentId: 1, displayName: '角色', permission: '1#查看,16#导出' },
    ]);
    expect(forest).toHaveLength(1);
    const sys = forest[0];
    expect(sys.perms).toBeUndefined();
    expect(sys.children?.[0].title).toBe('角色');
    expect(sys.children?.[0].children).toBeUndefined();
    expect(sys.children?.[0].perms?.map((l) => l.title)).toEqual(['查看', '导出']);
    expect(sys.children?.[0].perms?.map((l) => l.key)).toEqual(['p:2:1', 'p:2:16']);
  });
});

describe('checked keys round-trip', () => {
  it('restores the same role string', () => {
    const forest = buildPermForest([
      { id: 12, parentId: 0, name: 'A', permission: '' },
      { id: 15, parentId: 0, name: 'B', permission: '' },
    ]);
    const map = parseRolePermission('15#15,12#7');
    const keys = checkedKeysFromRole(map, forest);
    expect(keys).toContain('p:12:1');
    expect(keys).toContain('p:12:2');
    expect(keys).toContain('p:12:4');
    expect(keys).not.toContain('p:12:8');
    expect(serializeRolePermission(roleMapFromCheckedKeys(keys, forest))).toBe('12#7,15#15');
  });
});

describe('leafTitleColumnEm', () => {
  it('uses the longest leaf menu title', () => {
    const forest = buildPermForest([
      { id: 1, parentId: 0, displayName: '教务系统', permission: '' },
      { id: 2, parentId: 1, displayName: 'Abc', permission: '' },
      { id: 3, parentId: 1, displayName: '班级', permission: '' },
    ]);
    expect(leafTitleColumnEm(forest)).toBe(2);
  });

  it('prefers a longer CJK leaf over a short ASCII leaf', () => {
    const forest = buildPermForest([
      { id: 1, parentId: 0, displayName: '系统', permission: '' },
      { id: 2, parentId: 1, displayName: 'Abc', permission: '' },
      { id: 3, parentId: 1, displayName: '权限管理', permission: '' },
    ]);
    expect(leafTitleColumnEm(forest)).toBe(4);
  });
});

describe('togglePermKey', () => {
  it('adds and removes a flag key', () => {
    expect(togglePermKey(['p:12:1'], 'p:12:2', true).sort()).toEqual(['p:12:1', 'p:12:2']);
    expect(togglePermKey(['p:12:1', 'p:12:2'], 'p:12:2', false)).toEqual(['p:12:1']);
  });
});

describe('loadAllMenusForPermTree', () => {
  it('maps 403 to 无权文案', async () => {
    const r = await loadAllMenusForPermTree(async () => {
      throw { code: 403, message: 'forbidden' };
    });
    expect(r.menus).toEqual([]);
    expect(r.error).toBe('无权加载菜单目录');
  });

  it('maps HTTP 403 status to 无权文案', async () => {
    const r = await loadAllMenusForPermTree(async () => {
      throw { response: { status: 403 } };
    });
    expect(r.error).toBe('无权加载菜单目录');
  });
});

describe('node perm bulk check', () => {
  const forest = () =>
    buildPermForest([
      { id: 1, parentId: 0, displayName: '教务系统', permission: '' },
      { id: 2, parentId: 1, displayName: 'Abc', permission: '' },
      { id: 3, parentId: 1, displayName: '升级', permission: '' },
    ]);

  it('collects descendant perm keys for a group and a leaf menu', () => {
    const f = forest();
    const group = f[0];
    const abc = group.children![0];
    expect(collectPermKeysUnderNode(abc).sort()).toEqual(['p:2:1', 'p:2:2', 'p:2:4', 'p:2:8']);
    expect(collectPermKeysUnderNode(group)).toHaveLength(8);
    expect(findPermNode(f, { key: 'm:1' })?.title).toBe('教务系统');
  });

  it('toggles a group all-on / all-off and reports some', () => {
    const f = forest();
    const group = f[0];
    const all = toggleNodePerms([], group, true);
    expect(nodePermCheckState(all, group)).toBe('all');
    const abc = group.children![0];
    expect(nodePermCheckState(all, abc)).toBe('all');
    const half = toggleNodePerms(all, abc, false);
    expect(nodePermCheckState(half, group)).toBe('some');
    expect(nodePermCheckState(half, abc)).toBe('none');
    expect(toggleNodePerms(half, group, false)).toEqual([]);
  });
});
