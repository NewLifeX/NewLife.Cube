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

// ── loginWithPassword（密码登录通用逻辑：Challenge 加密 / 降级明文 / 验证码与记住透传）──

/** 构造 mock：/Auth/Challenge 返回给定公钥，/Auth/Login 返回给定 token，捕获全部请求 */
function createLoginMock({ publicKey, challengeId, token = 'tk' }) {
  const calls = [];
  const request = (cfg) => {
    calls.push(cfg);
    const isChallenge = cfg.url === '/Auth/Challenge';
    return Promise.resolve({
      code: 0,
      message: 'ok',
      data: isChallenge ? { challengeId, publicKey } : { accessToken: token },
    });
  };
  return { api: createUserApi(request), calls };
}

test('loginWithPassword: 公钥为空 → 明文登录，不携带 challengeId', async () => {
  const { api, calls } = createLoginMock({ publicKey: '', challengeId: 'ch-empty' });
  const res = await api.loginWithPassword('admin', 'Admin123!');
  assert.equal(calls.length, 2);
  assert.equal(calls[0].url, '/Auth/Challenge');
  assert.equal(calls[1].url, '/Auth/Login');
  assert.equal(calls[1].data.password, 'Admin123!'); // 明文
  assert.equal(calls[1].data.challengeId, undefined); // 公钥为空不进加密分支
  assert.equal(res.data.accessToken, 'tk');
});

test('loginWithPassword: 公钥无效致加密失败 → 降级明文', async () => {
  const { api, calls } = createLoginMock({ publicKey: 'not-a-valid-pem', challengeId: 'ch-bad' });
  await api.loginWithPassword('admin', 'pwd');
  assert.equal(calls[1].data.password, 'pwd'); // 加密失败兜底明文
  assert.equal(calls[1].data.challengeId, undefined);
});

test('loginWithPassword: 验证码与记住登录态透传（captchaId/captchaCode/remember）', async () => {
  const { api, calls } = createLoginMock({ publicKey: '', challengeId: undefined });
  await api.loginWithPassword('admin', 'pwd', { captchaId: 'c1', captchaCode: '1234', remember: true });
  assert.equal(calls[1].data.captchaId, 'c1');
  assert.equal(calls[1].data.captchaCode, '1234');
  assert.equal(calls[1].data.remember, true);
});
