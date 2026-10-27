// 列表页图表数据接口单测（node:test + 已构建 dist，零额外依赖）
// 覆盖各皮肤共用的 page.getChartData：请求路径与返回值透传
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createPageApi } = require('../dist/index.cjs');

/** 构造 mock request 并捕获最后一次调用配置 */
function createMock(data) {
  const calls = [];
  const request = (config) => {
    calls.push(config);
    return Promise.resolve({ code: 0, data, message: 'ok' });
  };
  return { api: createPageApi(request), calls };
}

test('getChartData: GET {type}/charts', async () => {
  const { api, calls } = createMock([{ series: [{ type: 'bar', data: [1] }] }]);
  const res = await api.getChartData('Admin/User');

  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, 'Admin/User/charts');
  assert.equal(calls[0].method, 'get');
  assert.deepEqual(res.data, [{ series: [{ type: 'bar', data: [1] }] }]);
});

test('getChartData: 后端返回空列表时透传空数组', async () => {
  const { api } = createMock([]);
  const res = await api.getChartData('Admin/Role');

  assert.deepEqual(res.data, []);
});
