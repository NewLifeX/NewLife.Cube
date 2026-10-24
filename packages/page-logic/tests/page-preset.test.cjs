// 页面呈现配置（F1 preset）单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  applyFieldPreset,
  registerPagePreset,
  getPagePreset,
  clearPagePresets,
} = require('../dist/index.cjs');

const field = (name, extra = {}) => ({ name, displayName: name, typeName: 'String', ...extra });

test('applyFieldPreset: 无配置时原样返回同一数组', () => {
  const fields = [field('Name')];
  assert.equal(applyFieldPreset(fields), fields);
  assert.equal(applyFieldPreset(fields, null), fields);
});

test('applyFieldPreset: 移除、补丁、列序、追加按固定顺序生效', () => {
  const fields = [field('Id', { primaryKey: true }), field('Name'), field('Remark'), field('ClassId')];

  const result = applyFieldPreset(fields, {
    remove: ['remark'],
    patch: { name: { label: '姓名', align: 'center', renderer: 'userLink' }, classid: { hidden: true } },
    order: ['ClassId', 'Name'],
    append: [field('DetailLink', { virtual: true }), field('name')],
  });

  assert.deepEqual(result.map((f) => f.name), ['ClassId', 'Name', 'Id', 'DetailLink']);
  assert.equal(result[1].header, '姓名');
  assert.equal(result[1].textAlign, 'center');
  assert.equal(result[1].view, 'userLink');
  // 补丁 hidden 写回 visible=false；order 中未列出的字段保持原顺序排在后面
  assert.equal(result[0].visible, false);
  // 追加重名字段被跳过（大小写不敏感）
  assert.equal(result[3].name, 'DetailLink');
});

test('applyFieldPreset: 补丁不改原始字段对象', () => {
  const original = field('Name');
  const result = applyFieldPreset([original], { patch: { Name: { label: '姓名' } } });

  assert.equal(original.header, undefined);
  assert.equal(result[0].header, '姓名');
  assert.notEqual(result[0], original);
});

test('applyFieldPreset: hidden=false 打开默认隐藏列', () => {
  const result = applyFieldPreset([field('Remark', { visible: false })], {
    patch: { Remark: { hidden: false } },
  });

  assert.equal(result[0].visible, true);
});

test('applyFieldPreset: 列序中的未知字段被忽略', () => {
  const result = applyFieldPreset([field('Name'), field('Mobile')], { order: ['NotExist', 'Mobile'] });

  assert.deepEqual(result.map((f) => f.name), ['Mobile', 'Name']);
});

test('preset 注册表：路径大小写不敏感、后注册覆盖、清空生效', () => {
  clearPagePresets();
  assert.equal(getPagePreset('/Admin/User'), null);
  assert.equal(getPagePreset(null), null);
  assert.equal(getPagePreset(''), null);

  registerPagePreset('/Admin/User', { list: { order: ['Name'] } });
  registerPagePreset('/admin/user', { list: { order: ['Mobile'] } });

  assert.deepEqual(getPagePreset('/ADMIN/USER').list.order, ['Mobile']);

  clearPagePresets();
  assert.equal(getPagePreset('/Admin/User'), null);
});

test('preset 注册表：空路径不注册', () => {
  clearPagePresets();
  registerPagePreset('', { list: { order: ['Name'] } });

  assert.equal(getPagePreset(''), null);
});
