// 请求客户端行为单元测试（node:test + 已构建 dist，零额外依赖）
// 运行：在 packages/api-core 目录 `node --test tests/`（需先 `pnpm build` 生成 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const core = require('../dist/index.cjs');

// 内存 Token 存储，避免依赖浏览器 document
function memToken(initial) {
  let t = initial;
  return {
    getToken: () => t,
    setToken: (v) => { t = v; },
    clearToken: () => { t = undefined; },
    _get: () => t,
  };
}

// axios 自定义 adapter 工厂：根据 handler 返回 {data,status,headers}；
// status>=400 时按 axios 行为 reject 一个带 response 的错误对象（复刻 validateStatus）。
function makeAdapter(handler) {
  return (config) => {
    const r = handler(config) || {};
    const { status = 200, data, headers = {} } = r;
    if (status >= 400) {
      const err = new Error(`Request failed with status code ${status}`);
      err.isAxiosError = true;
      err.config = config;
      err.response = { status, data, headers, config };
      err.code = status === 401 ? 'ERR_BAD_REQUEST' : 'ERR_BAD_RESPONSE';
      return Promise.reject(err);
    }
    return Promise.resolve({ data, status, statusText: 'OK', headers, config, request: {} });
  };
}

function makeClient(opts = {}) {
  const storage = opts.__storage || memToken('tok');
  const tokenManager = new core.TokenManager(storage);
  const client = core.createApiClient({ tokenManager, ...opts });
  return { client, storage };
}

// 捕获最终请求 config 的 adapter
function capturingAdapter(initialDataResolver) {
  const captured = { config: null };
  const handler = (config) => {
    captured.config = config;
    return initialDataResolver ? initialDataResolver(config) : { data: { code: 0, data: 'ok' } };
  };
  return { adapter: makeAdapter(handler), captured };
}

test('URL 解析：实体补 /api', async () => {
  const { adapter, captured } = capturingAdapter();
  const { client } = makeClient({ baseURL: 'http://host:5000' });
  client.defaults.adapter = adapter;
  await client.get('/Admin/Lov');
  assert.equal(captured.config.url, 'http://host:5000/api/Admin/Lov');
});

test('URL 解析：服务接口不带 /api', async () => {
  const { adapter, captured } = capturingAdapter();
  const { client } = makeClient({ baseURL: 'http://host:5000' });
  client.defaults.adapter = adapter;
  await client.get('/Auth/Login');
  assert.equal(captured.config.url, 'http://host:5000/Auth/Login');
});

test('token 注入 + tokenHeaderPrefix', async () => {
  const { adapter, captured } = capturingAdapter();
  const { client } = makeClient({ baseURL: 'http://host:5000', tokenHeaderPrefix: 'bearer ' });
  client.defaults.adapter = adapter;
  await client.get('/Admin/Lov');
  assert.equal(captured.config.headers.Authorization, 'bearer tok');
});

test('additionalRequestHeaders 合并', async () => {
  const { adapter, captured } = capturingAdapter();
  const { client } = makeClient({ baseURL: 'http://host:5000', additionalRequestHeaders: { 'X-App': 'cube' } });
  client.defaults.adapter = adapter;
  await client.get('/Admin/Lov');
  assert.equal(captured.config.headers['X-App'], 'cube');
});

test('additionalRequestHeaders 支持函数式', async () => {
  const { adapter, captured } = capturingAdapter();
  const { client } = makeClient({ baseURL: 'http://host:5000', additionalRequestHeaders: () => ({ 'X-Tenant': 't1' }) });
  client.defaults.adapter = adapter;
  await client.get('/Admin/Lov');
  assert.equal(captured.config.headers['X-Tenant'], 't1');
});

test('withCredentials 透传', async () => {
  const { adapter, captured } = capturingAdapter();
  const { client } = makeClient({ baseURL: 'http://host:5000', withCredentials: true });
  client.defaults.adapter = adapter;
  await client.get('/Admin/Lov');
  assert.equal(captured.config.withCredentials, true);
});

test('unwrapResponse=true 返回 ApiResponse（response.data）', async () => {
  const { adapter } = capturingAdapter();
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true });
  client.defaults.adapter = adapter;
  const r = await client.get('/Admin/Lov');
  assert.deepEqual(r, { code: 0, data: 'ok' });
});

test('unwrapResponse=false 返回完整 AxiosResponse', async () => {
  const { adapter } = capturingAdapter();
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: false });
  client.defaults.adapter = adapter;
  const r = await client.get('/Admin/Lov');
  assert.equal(r.status, 200);
  assert.deepEqual(r.data, { code: 0, data: 'ok' });
});

test('content-type 二进制透传（octet-stream）', async () => {
  const buf = Buffer.from([1, 2, 3, 4]);
  const { adapter } = capturingAdapter(() => ({ data: buf, headers: { 'content-type': 'application/octet-stream' } }));
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true });
  client.defaults.adapter = adapter;
  const r = await client.get('/x');
  assert.ok(Buffer.isBuffer(r));
});

test('content-type 透传（arraybuffer）', async () => {
  const ab = new ArrayBuffer(8);
  const { adapter } = capturingAdapter(() => ({ data: ab, headers: { 'content-type': 'arraybuffer' } }));
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true });
  client.defaults.adapter = adapter;
  const r = await client.get('/x');
  assert.ok(r instanceof ArrayBuffer);
});

test('204 返回 undefined（风险2：createRequest 不崩溃）', async () => {
  const { adapter } = capturingAdapter(() => ({ status: 204, data: '' }));
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true });
  client.defaults.adapter = adapter;
  const r = await client.get('/x');
  assert.equal(r, undefined);
});

test('createCubeApi 链路 204 经 createRequest 不崩溃（风险2 回归）', async () => {
  const { adapter } = capturingAdapter(() => ({ status: 204, data: '' }));
  const api = core.createCubeApi({ baseURL: 'http://host:5000', tokenStorage: memToken('tok') });
  // entityClient 经 createRequest，204 原返回 undefined，createRequest 需 res?.data 不会崩
  api.client.defaults.adapter = adapter;
  const r = await api.client.request({ url: '/Admin/User', method: 'get' });
  assert.equal(r, undefined);
});

test('业务错误 code≠0：onBusinessError 触发 + reject ApiError + onResponseError 不触发（防重复弹窗）', async () => {
  const { adapter } = capturingAdapter(() => ({ data: { code: 500, message: 'boom' } }));
  let biz = null, respErr = null;
  const { client } = makeClient({
    baseURL: 'http://host:5000', unwrapResponse: true,
    onBusinessError: (c, m) => { biz = { c, m }; },
    onResponseError: () => { respErr = true; },
  });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/x'), (e) => e.name === 'ApiError' && e.code === 500);
  assert.ok(biz && biz.c === 500 && biz.m === 'boom');
  assert.equal(respErr, null, '业务错误不应触发 onResponseError（避免重复弹窗）');
});

test('风险1：业务错误（非 401）触发 onResponseHook', async () => {
  const { adapter } = capturingAdapter(() => ({ data: { code: 500, message: 'boom' } }));
  let hooked = false;
  const { client } = makeClient({
    baseURL: 'http://host:5000', unwrapResponse: true,
    onResponseHook: () => { hooked = true; },
  });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/x'));
  assert.equal(hooked, true, '业务错误（非401）应触发 onResponseHook');
});

test('成功路径触发 onResponseHook', async () => {
  const { adapter } = capturingAdapter();
  let hooked = false;
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true, onResponseHook: () => { hooked = true; } });
  client.defaults.adapter = adapter;
  await client.get('/Admin/Lov');
  assert.equal(hooked, true);
});

test('HTTP 500：onResponseError 触发且 message/description 正确', async () => {
  const { adapter } = capturingAdapter(() => ({ status: 500, data: { description: 'd', detailsMessage: 'dt', code: 600, message: 'm600' } }));
  let info = null;
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true, onResponseError: (i) => { info = i; } });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/x'));
  assert.ok(info, '应触发 onResponseError');
  assert.equal(info.isNetwork, false);
  assert.equal(info.message, 'm600');
  assert.ok(info.description.includes('d') && info.description.includes('dt'));
});

test('网络错误：onResponseError isNetwork=true', async () => {
  const adapter = () => {
    const err = new Error('Network Error');
    err.isAxiosError = true;
    err.code = 'ERR_NETWORK';
    err.config = {};
    err.response = undefined;
    return Promise.reject(err);
  };
  let info = null;
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true, onResponseError: (i) => { info = i; } });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/x'));
  assert.ok(info && info.isNetwork === true);
});

test('401 业务码：onUnauthorized 触发且 token 清除', async () => {
  const { adapter } = capturingAdapter(() => ({ data: { code: 401, message: 'no' } }));
  let url = null;
  const storage = memToken('tok');
  const { client } = makeClient({ __storage: storage, baseURL: 'http://host:5000', unwrapResponse: true, onUnauthorized: (u) => { url = u; } });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/Admin/User/Info'));
  assert.equal(url, 'http://host:5000/api/Admin/User/Info');
  assert.equal(storage._get(), undefined, 'token 应被清除');
});

test('401 HTTP：onUnauthorized 触发且 token 清除', async () => {
  const { adapter } = capturingAdapter(() => ({ status: 401, data: { message: 'no' } }));
  let url = null;
  const storage = memToken('tok');
  const { client } = makeClient({ __storage: storage, baseURL: 'http://host:5000', unwrapResponse: true, onUnauthorized: (u) => { url = u; } });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/Admin/User/Info'));
  assert.equal(url, 'http://host:5000/api/Admin/User/Info');
  assert.equal(storage._get(), undefined, 'token 应被清除');
});

test('403 HTTP：清除 token 不触发 onUnauthorized（仅提示）', async () => {
  const { adapter } = capturingAdapter(() => ({ status: 403, data: { message: 'forbidden' } }));
  let unauthorized = false;
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true, onUnauthorized: () => { unauthorized = true; } });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/x'));
  assert.equal(unauthorized, false);
});

test('createCubeApi：entity 与 service 地址派生正确', async () => {
  const { adapter, captured } = capturingAdapter();
  const api = core.createCubeApi({ baseURL: 'http://host:5000/api', tokenStorage: memToken('tok') });
  api.client.defaults.adapter = adapter;
  // 实体接口走 entityClient（baseURL 含 /api）
  await api.client.request({ url: '/Admin/Lov', method: 'get' });
  assert.equal(captured.config.url, 'http://host:5000/api/Admin/Lov');
});

test('service 客户端（getServiceBaseUrl 派生）地址解析：服务不带 /api', async () => {
  const { adapter, captured } = capturingAdapter();
  // 等价于 createCubeApi 内部 serviceClient：baseURL 由 getServiceBaseUrl 派生
  const { client } = makeClient({ baseURL: core.getServiceBaseUrl('http://host:5000/api') });
  client.defaults.adapter = adapter;
  await client.get('/Auth/Login');
  assert.equal(captured.config.url, 'http://host:5000/Auth/Login');
});

test('createCubeApi：page.getExportUrl 返回 host/api/...ExportFile?format=', () => {
  const api = core.createCubeApi({ baseURL: 'http://host:5000/api', tokenStorage: memToken('tok') });
  const url = api.page.getExportUrl('Admin/User', 'xlsx');
  assert.equal(url, 'http://host:5000/api/Admin/User/ExportFile?format=xlsx');
});

// ── 反向断言：收敛审查盲区 ──────────────────────────────────────────

test('反向断言：401 业务码不触发 onResponseHook（仅触发 onUnauthorized）', async () => {
  const { adapter } = capturingAdapter(() => ({ data: { code: 401, message: 'no' } }));
  let hooked = false, unauthorized = false;
  const { client } = makeClient({
    baseURL: 'http://host:5000', unwrapResponse: true,
    onResponseHook: () => { hooked = true; },
    onUnauthorized: () => { unauthorized = true; },
  });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/Admin/User/Info'));
  assert.equal(hooked, false, '401 业务码不应触发 onResponseHook');
  assert.equal(unauthorized, true);
});

test('反向断言：HTTP 500 错误路径触发 onResponseHook', async () => {
  const { adapter } = capturingAdapter(() => ({ status: 500, data: { message: 'x' } }));
  let hooked = false;
  const { client } = makeClient({
    baseURL: 'http://host:5000', unwrapResponse: true,
    onResponseHook: () => { hooked = true; },
  });
  client.defaults.adapter = adapter;
  await assert.rejects(() => client.get('/x'));
  assert.equal(hooked, true, 'HTTP 错误路径应触发 onResponseHook');
});

test('登录结果字段归一化：大写 Token/RefreshToken/ExpireIn → 小驼峰', async () => {
  const { adapter } = capturingAdapter(() => ({
    data: { code: 0, data: { Token: 't', RefreshToken: 'r', ExpireIn: 3600 } },
  }));
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true });
  client.defaults.adapter = adapter;
  const res = await client.post('/Auth/Login');
  assert.equal(res.data.accessToken, 't');
  assert.equal(res.data.refreshToken, 'r');
  assert.equal(res.data.expireIn, 3600);
});

test('登录结果字段归一化：snake_case access_token/refresh_token/expire_in → 小驼峰（真实后端 Demo 返回）', async () => {
  const { adapter } = capturingAdapter(() => ({
    data: { code: 0, data: { access_token: 'at-xyz', refresh_token: 'rt-xyz', expire_in: 7200 } },
  }));
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true });
  client.defaults.adapter = adapter;
  const res = await client.post('/Auth/Login');
  assert.equal(res.data.accessToken, 'at-xyz', 'snake_case access_token 必须归一为 accessToken');
  assert.equal(res.data.refreshToken, 'rt-xyz');
  assert.equal(res.data.expireIn, 7200);
});

test('登录结果字段归一化：snake_case 缺失 access_token 时回退空串，不误取 undefined', async () => {
  const { adapter } = capturingAdapter(() => ({
    data: { code: 0, data: { code: 0, message: 'ok' } }, // 无令牌字段（异常登录响应）
  }));
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true });
  client.defaults.adapter = adapter;
  const res = await client.post('/Auth/Login');
  assert.equal(res.data.accessToken, '', '无令牌时应回退空串而非 undefined');
});

test('content-type text 分支：非字符串 data 经 JSON.stringify 透传', async () => {
  const payload = { a: 1, b: 'x' };
  const { adapter } = capturingAdapter(() => ({ data: payload, headers: { 'content-type': 'text' } }));
  const { client } = makeClient({ baseURL: 'http://host:5000', unwrapResponse: true });
  client.defaults.adapter = adapter;
  const res = await client.get('/x');
  assert.equal(typeof res, 'string');
  assert.deepEqual(JSON.parse(res), payload);
});

// ── 批量删除端点与序列化（回归：id[0]= 序列化 vs 后端单 id 校验 400 报错）──
// 注意：axios 1.16 中 params 序列化发生在标准 adapter 内部，自定义 adapter 收到的是原始 config.params，
// 故此处断言 URL 端点 + params 结构，并用 qs 直接验证最终序列化格式（与 client.ts 的 paramsSerializer 一致）。

const qs = require('qs');

test('page.deleteSelect 指向 DeleteSelect 端点且传数组参数', async () => {
  const { adapter, captured } = capturingAdapter();
  const api = core.createCubeApi({ baseURL: 'http://host:5000/api', tokenStorage: memToken('tok') });
  api.client.defaults.adapter = adapter;
  await api.page.deleteSelect('/Admin/User', [125, 124, 123]);
  assert.equal(captured.config.url, 'http://host:5000/api/Admin/User/DeleteSelect', '批量删除应走 DeleteSelect 端点');
  assert.equal(captured.config.method, 'delete');
  assert.deepEqual(captured.config.params, { id: [125, 124, 123] });
  // 与 client.ts paramsSerializer（qs allowDots）一致：数组序列化为索引形式，后端 String[] 可绑定
  const q = qs.stringify(captured.config.params, { allowDots: true });
  assert.equal(q, 'id%5B0%5D=125&id%5B1%5D=124&id%5B2%5D=123');
});

test('page.deleteSelect compatCommaJoin 传逗号分隔 id=1,2', async () => {
  const { adapter, captured } = capturingAdapter();
  const api = core.createCubeApi({ baseURL: 'http://host:5000/api', tokenStorage: memToken('tok') });
  api.client.defaults.adapter = adapter;
  await api.page.deleteSelect('/Admin/User', [125, 124], { compatCommaJoin: true });
  assert.equal(captured.config.url, 'http://host:5000/api/Admin/User/DeleteSelect', '批量删除应走 DeleteSelect 端点');
  assert.deepEqual(captured.config.params, { id: '125,124' }, 'compatCommaJoin 应传逗号分隔字符串');
});

test('page.deleteAll 指向 DeleteAll 端点且透传搜索条件', async () => {
  const { adapter, captured } = capturingAdapter();
  const api = core.createCubeApi({ baseURL: 'http://host:5000/api', tokenStorage: memToken('tok') });
  api.client.defaults.adapter = adapter;
  await api.page.deleteAll('/Admin/User', { name: 'admin' });
  assert.equal(captured.config.url, 'http://host:5000/api/Admin/User/DeleteAll', '按条件删除应走 DeleteAll 端点');
  assert.equal(captured.config.method, 'delete');
  assert.deepEqual(captured.config.params, { name: 'admin' });
});
