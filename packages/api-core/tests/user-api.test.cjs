// 用户中心/安全中心 API 单元测试（node:test + 已构建 dist，零额外依赖）
// 覆盖：profile / updateProfile / changePassword / binds / unbind 的请求路径与方法
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createUserApi } = require('../dist/index.cjs');

/** 构造 mock request 并捕获最后一次调用配置 */
function createMock() {
  const calls = [];
  const request = (config) => {
    calls.push(config);
    return Promise.resolve({ code: 0, data: undefined, message: 'ok' });
  };
  return { api: createUserApi(request), calls };
}

test('profile: GET /Admin/User/Info', () => {
  const { api, calls } = createMock();
  api.profile();
  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, '/Admin/User/Info');
  assert.equal(calls[0].method, 'get');
});

test('updateProfile: POST /Admin/User/Info 透传资料字段', () => {
  const { api, calls } = createMock();
  api.updateProfile({ id: 4, name: 'Stone', displayName: '大石头', sex: 1, updateUser: '大石头' });
  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, '/Admin/User/Info');
  assert.equal(calls[0].method, 'post');
  assert.deepEqual(calls[0].data, { id: 4, name: 'Stone', displayName: '大石头', sex: 1, updateUser: '大石头' });
});

test('changePassword: POST /Admin/User/ChangePassword 携带原/新密码', () => {
  const { api, calls } = createMock();
  api.changePassword({ oldPassword: 'old', newPassword: 'NewPass123!', newPassword2: 'NewPass123!' });
  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, '/Admin/User/ChangePassword');
  assert.equal(calls[0].method, 'post');
  assert.deepEqual(calls[0].data, { oldPassword: 'old', newPassword: 'NewPass123!', newPassword2: 'NewPass123!' });
});

test('binds: GET /Admin/User/Binds', () => {
  const { api, calls } = createMock();
  api.binds();
  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, '/Admin/User/Binds');
  assert.equal(calls[0].method, 'get');
});

test('unbind: GET /Sso/UnBind/{provider}（服务接口不带 /api）', () => {
  const { api, calls } = createMock();
  api.unbind('OpenWeixin');
  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, '/Sso/UnBind/OpenWeixin');
  assert.equal(calls[0].method, 'get');
});
