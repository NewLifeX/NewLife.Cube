// 页面工具函数单测（node:test + 已构建 dist，零额外依赖）
// 覆盖菜单权限解析、URL 占位符替换、导出 URL 与权限位分解等各皮肤共用逻辑
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  Auth,
  checkAuth,
  findMenu,
  getMenuPermission,
  resolveUrl,
  toCamelCase,
  buildExportUrl,
  decomposePowerOfTwo,
} = require('../dist/index.cjs');

const menus = [
  {
    name: '系统',
    url: null,
    permissions: {},
    children: [
      { name: '用户', url: '/Admin/User', permissions: { '2': '添加', '4': '编辑' } },
      { name: '角色', url: '/Admin/Role', permissions: {} },
    ],
  },
  { name: '首页', url: '/Admin/Index', permissions: { '1': '查看' } },
];

test('findMenu: 命中根节点', () => {
  assert.equal(findMenu(menus, '/Admin/Index')?.name, '首页');
});

test('findMenu: 递归命中子节点', () => {
  assert.equal(findMenu(menus, '/Admin/User')?.name, '用户');
});

test('findMenu: 忽略大小写并按路径后缀匹配', () => {
  assert.equal(findMenu(menus, '/cube/admin/user')?.name, '用户');
  assert.equal(findMenu(menus, '/CUBE/ADMIN/ROLE')?.name, '角色');
});

test('findMenu: 未命中与空菜单返回 undefined', () => {
  assert.equal(findMenu(menus, '/Admin/Unknown'), undefined);
  assert.equal(findMenu([], '/Admin/User'), undefined);
});

test('getMenuPermission: 返回匹配菜单的权限映射，未命中返回空对象', () => {
  assert.deepEqual(getMenuPermission(menus, '/Admin/User'), { '2': '添加', '4': '编辑' });
  assert.deepEqual(getMenuPermission(menus, '/Admin/Role'), {});
  assert.deepEqual(getMenuPermission(menus, '/Admin/Unknown'), {});
});

test('checkAuth: 按权限码判断是否具备权限', () => {
  const perms = getMenuPermission(menus, '/Admin/User');
  assert.equal(checkAuth(perms, Auth.ADD), true);
  assert.equal(checkAuth(perms, Auth.EDIT), true);
  assert.equal(checkAuth(perms, Auth.DELETE), false);
});

test('resolveUrl: 替换行数据占位符，字段名忽略大小写，缺字段留空', () => {
  assert.equal(resolveUrl('User/Detail?id={Id}&name={NAME}', { id: 3, Name: 'Stone' }), 'User/Detail?id=3&name=Stone');
  assert.equal(resolveUrl('User/Detail?id={Id}&miss={None}', { Id: 3 }), 'User/Detail?id=3&miss=');
});

test('resolveUrl: 占位值做 URL 编码', () => {
  assert.equal(resolveUrl('User/Detail?name={Name}', { Name: '大 石' }), `User/Detail?name=${encodeURIComponent('大 石')}`);
});

test('toCamelCase: 下划线与连字符转小驼峰', () => {
  assert.equal(toCamelCase('User_Name'), 'userName');
  assert.equal(toCamelCase('user-name'), 'userName');
  assert.equal(toCamelCase('UserName'), 'userName');
  assert.equal(toCamelCase(''), '');
});

test('buildExportUrl: 拼接导出地址并对格式编码', () => {
  assert.equal(buildExportUrl('/Admin/User', 'Excel'), '/Admin/User/export?format=Excel');
  assert.equal(buildExportUrl('Admin/User', 'Csv', '/api/'), '/api/Admin/User/export?format=Csv');
  assert.equal(buildExportUrl('/Admin/User', '带 空格'), `/Admin/User/export?format=${encodeURIComponent('带 空格')}`);
});

test('decomposePowerOfTwo: 分解权限位', () => {
  assert.deepEqual(decomposePowerOfTwo(23), [1, 2, 4, 16]);
  assert.deepEqual(decomposePowerOfTwo(0), []);
  assert.deepEqual(decomposePowerOfTwo(1), [1]);
});
