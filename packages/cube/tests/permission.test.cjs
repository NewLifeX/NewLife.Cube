// 动作权限解析单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { PermissionFlags, parsePermissionString, hasPermissionAction } = require('../dist/index.cjs');

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
