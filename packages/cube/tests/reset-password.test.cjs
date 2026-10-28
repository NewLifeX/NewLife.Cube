// 重置密码（忘记密码）单元测试（node:test + 已构建 dist，零额外依赖）
// 覆盖：Challenge 公钥加密新密码/确认密码、challengeId 透传、公钥缺失与加密失败时降级明文
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createUserApi } = require('../dist/index.cjs');

/** 构造 mock：/Auth/Challenge 返回给定公钥，其余请求返回成功，捕获全部请求配置 */
function createMock({ publicKey, challengeId } = {}) {
  const calls = [];
  const request = (cfg) => {
    calls.push(cfg);
    const isChallenge = cfg.url === '/Auth/Challenge';
    return Promise.resolve({
      code: 0,
      message: 'ok',
      data: isChallenge ? { challengeId, publicKey } : true,
    });
  };
  return { api: createUserApi(request), calls };
}

/** 取重置请求（calls 里最后一条非 Challenge 请求） */
function resetCall(calls) {
  return calls.filter((c) => c.url !== '/Auth/Challenge').pop();
}

test('resetPassword: 无公钥 → 明文提交，不携带 challengeId', async () => {
  const { api, calls } = createMock({ publicKey: '', challengeId: 'ch-empty' });
  const res = await api.resetPassword({
    username: 'dev@example.com',
    code: '123456',
    newPassword: 'NewPass123!',
    confirmPassword: 'NewPass123!',
  });

  assert.equal(calls[0].url, '/Auth/Challenge');
  const call = resetCall(calls);
  assert.equal(call.url, '/Auth/ResetPassword');
  assert.equal(call.method, 'post');
  assert.equal(call.data.newPassword, 'NewPass123!');
  assert.equal(call.data.challengeId, undefined);
  assert.equal(res.data, true);
});

test('resetPassword: 公钥无效致加密失败 → 降级明文且不带 challengeId', async () => {
  const { api, calls } = createMock({ publicKey: 'not-a-valid-pem', challengeId: 'ch-bad' });
  await api.resetPassword({
    username: '13800000000',
    code: '000000',
    newPassword: 'NewPass123!',
    confirmPassword: 'NewPass123!',
  });

  const call = resetCall(calls);
  assert.equal(call.data.newPassword, 'NewPass123!');
  assert.equal(call.data.confirmPassword, 'NewPass123!');
  assert.equal(call.data.challengeId, undefined);
});

test('resetPassword: 有效公钥 → 新密码与确认密码都加密并携带 challengeId', async () => {
  const { publicKey } = await import('node:crypto').then((crypto) => {
    const { publicKey } = crypto.generateKeyPairSync('rsa', {
      modulusLength: 2048,
      publicKeyEncoding: { type: 'spki', format: 'pem' },
      privateKeyEncoding: { type: 'pkcs8', format: 'pem' },
    });
    return { publicKey };
  });

  const { api, calls } = createMock({ publicKey, challengeId: 'ch-ok' });
  await api.resetPassword({
    username: 'dev@example.com',
    code: '123456',
    newPassword: 'NewPass123!',
    confirmPassword: 'NewPass123!',
  });

  const call = resetCall(calls);
  assert.equal(call.data.challengeId, 'ch-ok');
  // 密文为 Base64，且与明文不同
  assert.notEqual(call.data.newPassword, 'NewPass123!');
  assert.notEqual(call.data.confirmPassword, 'NewPass123!');
  assert.match(call.data.newPassword, /^[A-Za-z0-9+/]+=*$/);
  assert.equal(Buffer.from(call.data.newPassword, 'base64').length, 256);
  // 其余字段原样透传
  assert.equal(call.data.username, 'dev@example.com');
  assert.equal(call.data.code, '123456');
});
