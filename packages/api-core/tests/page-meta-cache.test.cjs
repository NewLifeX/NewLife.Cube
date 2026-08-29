// GetPage 内存缓存单元测试（node:test + 已构建 dist，零额外依赖）
// 运行：在 packages/api-core 目录 `node --test tests/`（需先 `pnpm build` 生成 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createPageApi, clearPageMetaCache } = require('../dist/index.cjs');

test('getPage: 同一 type 二次调用命中缓存，request 只发一次', async () => {
  clearPageMetaCache();

  let calls = 0;
  const request = async () => {
    calls++;
    return { code: 0, data: { setting: {}, list: [], search: [], addForm: [], editForm: [], detail: [] } };
  };
  const api = createPageApi(request);

  const r1 = await api.getPage('/Admin/Log');
  const r2 = await api.getPage('/Admin/Log');
  assert.equal(calls, 1, '命中缓存后不应再次请求');
  assert.equal(r1.data.list.length, 0);
  assert.deepEqual(r2, r1, '缓存响应应与首次一致');
});

test('getPage: 不同 type 分别缓存', async () => {
  clearPageMetaCache();

  let calls = 0;
  const request = async () => {
    calls++;
    return { code: 0, data: { setting: {}, list: [], search: [], addForm: [], editForm: [], detail: [] } };
  };
  const api = createPageApi(request);

  await api.getPage('/Admin/Log');
  await api.getPage('/Admin/User');
  assert.equal(calls, 2, '不同 type 各请求一次');
});

test('getPage: clearPageMetaCache 后重新请求', async () => {
  clearPageMetaCache();

  let calls = 0;
  const request = async () => {
    calls++;
    return { code: 0, data: { setting: {}, list: [], search: [], addForm: [], editForm: [], detail: [] } };
  };
  const api = createPageApi(request);

  await api.getPage('/Admin/Log');
  clearPageMetaCache();
  await api.getPage('/Admin/Log');
  assert.equal(calls, 2, '清空缓存后应重新请求');
});

test('getPage: 非实体页（data 为字符串）不缓存', async () => {
  clearPageMetaCache();

  let calls = 0;
  const request = async () => {
    calls++;
    return { code: 0, data: '<html>SPA fallback</html>' };
  };
  const api = createPageApi(request);

  await api.getPage('/Admin/Cube');
  await api.getPage('/Admin/Cube');
  assert.equal(calls, 2, 'HTML 字符串响应不应被缓存');
});
