// 认证业务逻辑单测（node:test + 已构建 dist，零额外依赖）
// 覆盖登录令牌写入、登出清理、用户资料与权限解析、菜单加载与路径权限解析
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { AuthLogic, Auth } = require('../dist/index.cjs');

/** 构造假的 CubeApi，只实现 AuthLogic 用到的成员 */
function createFakeApi(overrides = {}) {
  const calls = [];
  let token = null;
  const api = {
    client: { defaults: { baseURL: 'http://host/api' } },
    tokenManager: {
      getToken: () => token,
      setToken: (value) => {
        token = value;
      },
      clearToken: () => {
        token = null;
      },
    },
    user: {
      loginWithPassword: async (username, password, options) => {
        calls.push({ api: 'loginWithPassword', username, password, options });
        return { code: 0, data: { accessToken: 'T1', expireIn: 3600 } };
      },
      logout: async () => {
        calls.push({ api: 'logout' });
        return { code: 0 };
      },
      info: async () => {
        calls.push({ api: 'info' });
        return { code: 0, data: { name: 'stone', displayName: '大石头', permission: '1,2,4' } };
      },
    },
    menu: {
      getMenuTree: async () => {
        calls.push({ api: 'getMenuTree' });
        return { code: 0, data: [{ name: '用户', url: '/Admin/User', permissions: { '2': '添加' } }] };
      },
    },
    ...overrides,
  };
  return { api, calls, getToken: () => token };
}

test('login: 成功后写入令牌并透传验证码与记住登录', async () => {
  const { api, calls, getToken } = createFakeApi();
  const logic = new AuthLogic(api, () => {});

  const res = await logic.login('stone', 'Pass123!', 'cid', 'c0de', true);

  assert.equal(res.data.accessToken, 'T1');
  assert.equal(getToken(), 'T1');
  assert.equal(logic.isLoggedIn, true);
  assert.deepEqual(calls[0].options, { captchaId: 'cid', captchaCode: 'c0de', remember: true });
});

test('login: 未返回令牌时不写入令牌', async () => {
  const { api, getToken } = createFakeApi({
    user: { loginWithPassword: async () => ({ code: 1, data: null, message: '密码错误' }) },
  });
  const logic = new AuthLogic(api, () => {});

  const res = await logic.login('stone', 'bad');

  assert.equal(res.code, 1);
  assert.equal(getToken(), null);
  assert.equal(logic.isLoggedIn, false);
});

test('logout: 清理令牌与状态，接口异常也照常清理', async () => {
  const { api, getToken } = createFakeApi({
    user: {
      logout: async () => {
        throw new Error('network');
      },
    },
  });
  const updates = [];
  const logic = new AuthLogic(api, (partial) => updates.push(partial), { userInfo: { name: 'x' } });
  api.tokenManager.setToken('T1');

  await logic.logout();

  assert.equal(getToken(), null);
  assert.deepEqual(logic.getState(), { userInfo: null, permissions: [], menus: [] });
  assert.deepEqual(updates.at(-1), { userInfo: null, permissions: [], menus: [] });
});

test('fetchUserInfo: 解析逗号分隔权限并回写状态', async () => {
  const { api } = createFakeApi();
  const updates = [];
  const logic = new AuthLogic(api, (partial) => updates.push(partial));

  const info = await logic.fetchUserInfo();

  assert.equal(info.name, 'stone');
  assert.deepEqual(logic.getState().permissions, ['1', '2', '4']);
  assert.deepEqual(updates.at(-1).permissions, ['1', '2', '4']);
});

test('fetchUserInfo: 无权限字段时解析为空数组', async () => {
  const { api } = createFakeApi({
    user: { info: async () => ({ code: 0, data: { name: 'stone' } }) },
  });
  const logic = new AuthLogic(api, () => {});

  await logic.fetchUserInfo();

  assert.deepEqual(logic.getState().permissions, []);
});

test('fetchMenus: 加载菜单树，空响应回落空数组', async () => {
  const { api } = createFakeApi();
  const logic = new AuthLogic(api, () => {});

  const menus = await logic.fetchMenus();
  assert.equal(menus.length, 1);
  assert.deepEqual(logic.getMenuPermission('/Admin/User'), { '2': '添加' });
  assert.equal(logic.findMenu('/Admin/User')?.name, '用户');
  assert.equal(Auth.ADD, 2);

  const emptyLogic = new AuthLogic(createFakeApi({ menu: { getMenuTree: async () => ({ code: 0 }) } }).api, () => {});
  assert.deepEqual(await emptyLogic.fetchMenus(), []);
});

test('getMenuPermission: 未命中菜单返回空对象', () => {
  const { api } = createFakeApi();
  const logic = new AuthLogic(api, () => {});

  assert.deepEqual(logic.getMenuPermission('/Admin/Unknown'), {});
});
