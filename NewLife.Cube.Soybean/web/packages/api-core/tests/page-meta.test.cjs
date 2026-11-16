// meta 展开与缓存单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createPageApi, clearPageMetaCache, expandPageMeta } = require('../dist/index.cjs');

const field = (name, extra = {}) => ({ name, displayName: name, typeName: 'String', ...extra });

test('expandPageMeta: 各视图直接展开字段数组', () => {
  const meta = expandPageMeta({
    entity: { name: 'Student', displayName: '学生', primaryKey: 'Id' },
    setting: { enableAdd: true },
    list: [field('Id', { primaryKey: true }), field('Name'), field('Remark', { visible: false })],
    form: [field('Name', { required: true })],
    search: [{ name: 'q', kind: 'keyword', displayName: '关键字', typeName: 'String' }, field('Name')],
    toolbar: [{ name: 'Refresh', kind: 'request', action: 'Refresh', include: ['keys'] }],
    ui: { density: 'compact' },
  });

  assert.equal(meta.entity.primaryKey, 'Id');
  assert.deepEqual(meta.list.map((f) => f.name), ['Id', 'Name', 'Remark']);
  assert.equal(meta.list[2].visible, false);
  assert.equal(meta.editForm[0].required, true);
  assert.deepEqual(meta.addForm.map((f) => f.name), ['Name']);
  assert.deepEqual(meta.detail.map((f) => f.name), ['Name']);
  assert.equal(meta.search[0].kind, 'keyword');
  assert.equal(meta.toolbar[0].action, 'Refresh');
  assert.equal(meta.ui.density, 'compact');
});

test('expandPageMeta: add/detail 省略时回退 form', () => {
  const meta = expandPageMeta({ form: [field('Name')] });

  assert.deepEqual(meta.addForm.map((f) => f.name), ['Name']);
  assert.deepEqual(meta.detail.map((f) => f.name), ['Name']);
});

test('expandPageMeta: 旧服务端数组响应原样返回', () => {
  const legacy = { setting: {}, list: [field('Name')], editForm: [field('Name')] };
  assert.equal(expandPageMeta(legacy), legacy);
});

test('getPage: scope 隔离缓存，refresh 强制请求', async () => {
  clearPageMetaCache();

  let calls = 0;
  let scope = 'u1';
  const request = async () => {
    calls++;
    return { code: 0, data: { form: { fields: [field('Name')] } } };
  };
  const api = createPageApi(request, undefined, () => scope);

  await api.getPage('/Admin/Log');
  await api.getPage('/Admin/Log');
  assert.equal(calls, 1, '同一 scope 命中缓存');

  scope = 'u2';
  await api.getPage('/Admin/Log');
  assert.equal(calls, 2, '换 scope 应重新请求');

  await api.getPage('/Admin/Log', { refresh: true });
  assert.equal(calls, 3, 'refresh 应强制请求');
});
