// 地址解析单元测试（node:test + 已构建 dist，零额外依赖）
// 运行：在 packages/api-core 目录 `node --test tests/`（需先 `pnpm build` 生成 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { resolveRequestUrl, getServiceBaseUrl, isServiceApiPath } = require('../dist/index.cjs');

test('resolveRequestUrl: base 带 /api，实体保留 /api', () => {
  assert.equal(resolveRequestUrl('http://host:5000/api', '/Admin/Lov'), 'http://host:5000/api/Admin/Lov');
});

test('resolveRequestUrl: base 带 /api，服务接口去掉 /api', () => {
  assert.equal(resolveRequestUrl('http://host:5000/api', '/Auth/Login'), 'http://host:5000/Auth/Login');
});

test('resolveRequestUrl: base 带 /api，url 自带 /api 去重（避免 //api/api）', () => {
  assert.equal(resolveRequestUrl('http://host:5000/api', '/api/Admin/Lov'), 'http://host:5000/api/Admin/Lov');
});

test('resolveRequestUrl: base 不带 /api，实体缺 /api 则补', () => {
  assert.equal(resolveRequestUrl('http://host:5000', '/Admin/Lov'), 'http://host:5000/api/Admin/Lov');
});

test('resolveRequestUrl: base 不带 /api，url 已带 /api 不重复补', () => {
  assert.equal(resolveRequestUrl('http://host:5000', '/api/Admin/Lov'), 'http://host:5000/api/Admin/Lov');
});

test('resolveRequestUrl: base 不带 /api，服务接口不补', () => {
  assert.equal(resolveRequestUrl('http://host:5000', '/Auth/Login'), 'http://host:5000/Auth/Login');
});

test('resolveRequestUrl: 空 base（同源），实体补 /api', () => {
  assert.equal(resolveRequestUrl('', '/Admin/Lov'), '/api/Admin/Lov');
});

test('resolveRequestUrl: 空 base（同源），服务接口不补', () => {
  assert.equal(resolveRequestUrl('', '/Auth/Login'), '/Auth/Login');
});

test('resolveRequestUrl: /Cube 服务动作（MenuTree）去 /api', () => {
  assert.equal(resolveRequestUrl('http://host:5000/api', '/Cube/MenuTree'), 'http://host:5000/Cube/MenuTree');
});

test('resolveRequestUrl: /Cube 实体（App）保留 /api', () => {
  assert.equal(resolveRequestUrl('http://host:5000/api', '/Cube/App'), 'http://host:5000/api/Cube/App');
});

test('resolveRequestUrl: /Cube 自动化服务去 /api', () => {
  assert.equal(resolveRequestUrl('http://host:5000/api', '/Cube/Automation'), 'http://host:5000/Cube/Automation');
  assert.equal(resolveRequestUrl('/api', '/Cube/Automation/Run'), '/Cube/Automation/Run');
  assert.equal(resolveRequestUrl('http://host:5000/api', '/Cube/Widget/Query'), 'http://host:5000/Cube/Widget/Query');
  assert.equal(resolveRequestUrl('/api', '/Cube/Widget/Sources'), '/Cube/Widget/Sources');
});

test('resolveRequestUrl: 绝对地址（含协议）原样返回', () => {
  assert.equal(resolveRequestUrl('http://host:5000/api', 'https://other.com/x'), 'https://other.com/x');
});

test('resolveRequestUrl: 绝对地址（// 协议相对）原样返回', () => {
  assert.equal(resolveRequestUrl('http://host:5000/api', '//cdn.com/x'), '//cdn.com/x');
});

test('resolveRequestUrl: base 尾部多余斜杠被清理', () => {
  assert.equal(resolveRequestUrl('http://host:5000///', '/Admin/Lov'), 'http://host:5000/api/Admin/Lov');
});

test('getServiceBaseUrl: 跨域带 /api → 去掉 /api', () => {
  assert.equal(getServiceBaseUrl('http://host:5000/api'), 'http://host:5000');
});

test('getServiceBaseUrl: 同域 /api → 空', () => {
  assert.equal(getServiceBaseUrl('/api'), '');
});

test('getServiceBaseUrl: 空 → 空', () => {
  assert.equal(getServiceBaseUrl(''), '');
});

test('isServiceApiPath: 服务前缀识别', () => {
  assert.equal(isServiceApiPath('/Auth/Login'), true);
  assert.equal(isServiceApiPath('/Sso/Login'), true);
  assert.equal(isServiceApiPath('/Mfa/Verify'), true);
  assert.equal(isServiceApiPath('/OAuth/xxx'), true);
});

test('isServiceApiPath: 实体路径返回 false', () => {
  assert.equal(isServiceApiPath('/Admin/User'), false);
  assert.equal(isServiceApiPath('/Cube/App'), false);
});

test('isServiceApiPath: /Cube 按动作区分', () => {
  assert.equal(isServiceApiPath('/Cube/MenuTree'), true);
  assert.equal(isServiceApiPath('/Cube/Setting'), true);
  assert.equal(isServiceApiPath('/Cube/Automation'), true);
  assert.equal(isServiceApiPath('/Cube/Automation/Run'), true);
  assert.equal(isServiceApiPath('/Cube/Widget'), true);
  assert.equal(isServiceApiPath('/Cube/Widget/Query'), true);
  assert.equal(isServiceApiPath('/Cube/App'), false);
});

test('isServiceApiPath: 绝对地址返回 false', () => {
  assert.equal(isServiceApiPath('https://x.com/Auth/Login'), false);
});
