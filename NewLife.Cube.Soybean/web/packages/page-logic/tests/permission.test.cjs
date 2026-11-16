// 动作权限解析单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  PermissionFlags,
  parsePermissionString,
  hasPermissionAction,
  flattenRolePermissionTree,
  collectRolePermissionColumns,
  getPermissionCheckState,
  isMenuFlagGranted,
  toggleMenuPermissionFlag,
  setMenuPermission,
  serializePermissionString,
} = require('../dist/index.cjs');

test('parsePermissionString: 解析 menuId#flag 列表', () => {
  const map = parsePermissionString('1#15,2#4, 12#2');

  assert.equal(map.get(1), 15);
  assert.equal(map.get(2), 4);
  assert.equal(map.get(12), 2);
  assert.equal(map.size, 3);
});

test('parsePermissionString: 空值与非数字项被跳过', () => {
  assert.equal(parsePermissionString().size, 0);
  assert.equal(parsePermissionString(null).size, 0);
  assert.equal(parsePermissionString('').size, 0);

  const map = parsePermissionString('bad#8,3#x,,4#8');
  assert.equal(map.has(3), false);
  assert.equal(map.get(4), 8);
});

test('parsePermissionString: 同一菜单重复出现时后者覆盖', () => {
  const map = parsePermissionString('2#1,2#8');

  assert.equal(map.get(2), 8);
});

test('hasPermissionAction: 缺少 menuId 或 flag 时无法判定', () => {
  const map = parsePermissionString('12#6');

  assert.equal(hasPermissionAction(map, undefined, 2), null);
  assert.equal(hasPermissionAction(map, 12, undefined), null);
});

test('hasPermissionAction: 权限串中没有该菜单视为无权限', () => {
  const map = parsePermissionString('12#6');

  assert.equal(hasPermissionAction(map, 99, PermissionFlags.Insert), false);
});

test('hasPermissionAction: 位包含判定', () => {
  const map = parsePermissionString('12#6');

  assert.equal(hasPermissionAction(map, 12, PermissionFlags.Insert), true);
  assert.equal(hasPermissionAction(map, 12, PermissionFlags.Update), true);
  // 6 = 2 | 4，不含删除位 8
  assert.equal(hasPermissionAction(map, 12, PermissionFlags.Delete), false);
  assert.equal(hasPermissionAction(map, 12, PermissionFlags.Insert | PermissionFlags.Delete), false);
});

test('PermissionFlags: 位值与 XCode 权限枚举对齐', () => {
  assert.equal(PermissionFlags.Detail, 1);
  assert.equal(PermissionFlags.Insert, 2);
  assert.equal(PermissionFlags.Update, 4);
  assert.equal(PermissionFlags.Delete, 8);
});

const flags = [
  { value: 1, name: '查看' },
  { value: 2, name: '添加' },
  { value: 4, name: '修改' },
  { value: 8, name: '删除' },
];

test('flattenRolePermissionTree: 深度优先展开并带层级', () => {
  const tree = [
    {
      id: 4,
      name: 'Admin',
      displayName: '系统管理',
      flags: [],
      children: [
        { id: 7, name: 'Core', displayName: '基本设置', flags: [flags[0], flags[2]] },
        { id: 6, name: 'AccessRule', flags: flags },
      ],
    },
    { id: 8, name: 'Cube' },
  ];

  const rows = flattenRolePermissionTree(tree);
  assert.deepEqual(
    rows.map((row) => [row.id, row.depth]),
    [
      [4, 0],
      [7, 1],
      [6, 1],
      [8, 0],
    ],
  );
  // 缺省显示名回退菜单名，缺省 flags 视为空
  assert.equal(rows[2].displayName, 'AccessRule');
  assert.deepEqual(rows[3].flags, []);
  assert.deepEqual(flattenRolePermissionTree(undefined), []);
});

test('collectRolePermissionColumns: 位值升序去重', () => {
  const rows = flattenRolePermissionTree([
    { id: 1, flags: [flags[3], flags[0]] },
    { id: 2, flags: [flags[1], { value: 16, name: '释放内存' }] },
    { id: 3, flags: [] },
  ]);

  assert.deepEqual(
    collectRolePermissionColumns(rows).map((item) => item.value),
    [1, 2, 8, 16],
  );
  assert.deepEqual(collectRolePermissionColumns([]), []);
});

test('getPermissionCheckState/isMenuFlagGranted: -1 视为全部', () => {
  assert.equal(getPermissionCheckState(-1, flags), 'all');
  assert.equal(getPermissionCheckState(4294967295, flags), 'all');
  assert.equal(getPermissionCheckState(0, flags), 'none');
  assert.equal(getPermissionCheckState(3, flags), 'some');
  assert.equal(getPermissionCheckState(15, flags), 'all');
  assert.equal(getPermissionCheckState(-1, []), 'none');

  assert.equal(isMenuFlagGranted(-1, 8), true);
  assert.equal(isMenuFlagGranted(3, 2), true);
  assert.equal(isMenuFlagGranted(3, 4), false);
});

test('toggleMenuPermissionFlag: 勾选/取消单个位，归零时移除菜单', () => {
  let map = parsePermissionString('');

  map = toggleMenuPermissionFlag(map, 12, flags, 1, true);
  map = toggleMenuPermissionFlag(map, 12, flags, 4, true);
  assert.equal(map.get(12), 5);

  map = toggleMenuPermissionFlag(map, 12, flags, 1, false);
  assert.equal(map.get(12), 4);

  // “全部”首次切换时展开为具体位
  const all = new Map([[13, -1]]);
  assert.equal(toggleMenuPermissionFlag(all, 13, flags, 8, false).get(13), 7);
  assert.equal(all.get(13), -1, '不修改入参');

  map = toggleMenuPermissionFlag(map, 12, flags, 4, false);
  assert.equal(map.has(12), false);
});

test('setMenuPermission: 整行授权授予全部可授权位，取消移除', () => {
  const granted = setMenuPermission(new Map(), 5, flags, true);
  assert.equal(granted.get(5), 15);

  assert.equal(setMenuPermission(granted, 5, flags, false).has(5), false);
  assert.equal(setMenuPermission(new Map(), 6, [], true).has(6), false);
});

test('serializePermissionString: 按菜单编号升序，跳过空项', () => {
  const map = new Map([
    [12, 3],
    [2, 0],
    [5, -1],
    [1, 8],
  ]);

  assert.equal(serializePermissionString(map), '1#8,5#-1,12#3');
  assert.equal(serializePermissionString(new Map()), '');
  assert.equal(serializePermissionString(null), '');

  // 与解析互逆
  assert.equal(serializePermissionString(parsePermissionString('4#1,7#15')), '4#1,7#15');
});
