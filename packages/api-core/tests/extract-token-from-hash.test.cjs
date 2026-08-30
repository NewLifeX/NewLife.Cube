// SSO 回调 hash token 提取单元测试（node:test + 已构建 dist，零额外依赖）
// 运行：在 packages/api-core 目录 `node --test tests/`（需先 `pnpm build` 生成 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { extractTokenFromHash } = require('../dist/index.cjs');

test('extractTokenFromHash: #token=xxx 提取', () => {
  assert.equal(extractTokenFromHash('#token=abc123'), 'abc123');
});

test('extractTokenFromHash: #/path#token=xxx 兼容路由后 hash', () => {
  assert.equal(extractTokenFromHash('#/home#token=abc123'), 'abc123');
});

test('extractTokenFromHash: JWT 点分格式原样返回', () => {
  const jwt = 'eyJhbGciOiJIUzI1NiJ9.abc.def';
  assert.equal(extractTokenFromHash(`#token=${jwt}`), jwt);
});

test('extractTokenFromHash: 带 URL 编码的值解码', () => {
  assert.equal(extractTokenFromHash('#token=abc%20123'), 'abc 123');
});

test('extractTokenFromHash: 空 hash 返回 null', () => {
  assert.equal(extractTokenFromHash(''), null);
  assert.equal(extractTokenFromHash(undefined), null);
});

test('extractTokenFromHash: 无 token 参数返回 null', () => {
  assert.equal(extractTokenFromHash('#foo=bar'), null);
  assert.equal(extractTokenFromHash('#/home'), null);
});
