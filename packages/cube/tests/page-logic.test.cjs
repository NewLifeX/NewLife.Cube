// 列表页编排核心单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { PageLogic, registerPagePreset, clearPagePresets } = require('../dist/index.cjs');

const field = (name, extra = {}) => ({ name, displayName: name, typeName: 'String', ...extra });

function createApi(meta, listResponse = { data: [] }) {
  const calls = [];
  const api = {
    calls,
    client: { defaults: { baseURL: 'http://localhost:5000' } },
    page: {
      getPage: async (type) => {
        calls.push({ name: 'getPage', type });
        return { data: meta };
      },
      getList: async (type, params) => {
        calls.push({ name: 'getList', type, params });
        return listResponse;
      },
      add: async (_type, data) => ({ code: 0, data }),
      update: async (_type, data) => ({ code: 0, data }),
      remove: async () => ({ code: 0 }),
      restore: async () => ({ code: 0 }),
      deleteSelect: async (_type, keys) => ({ code: 0, data: keys }),
      deleteAll: async () => ({ code: 0 }),
      getDetail: async (_type, id) => ({ code: 0, data: { id } }),
      importFile: async () => ({ code: 0 }),
      uploadFile: async () => ({ code: 0, data: {} }),
      getChartData: async () => ({ code: 0, data: [] }),
      lookup: async () => ({ code: 0, data: {} }),
    },
  };
  return api;
}

function createLogic(api, options = {}) {
  const updates = [];
  const logic = new PageLogic({ api, update: (partial) => updates.push(partial), ...options });
  return { logic, updates };
}

test('loadFields: 按注册的 preset 合并列表与表单字段', async () => {
  clearPagePresets();
  registerPagePreset('/Admin/User', {
    list: { remove: ['Remark'], patch: { name: { label: '用户名' } }, order: ['Name'], append: [field('DetailLink', { virtual: true })] },
    form: { remove: ['Id'] },
  });

  const api = createApi({
    list: [field('Id', { primaryKey: true }), field('Name'), field('Remark')],
    addForm: [field('Id'), field('Name')],
    editForm: [field('Id'), field('Name')],
    detail: [field('Id'), field('Name')],
  });
  const { logic } = createLogic(api);

  await logic.loadFields('/Admin/User');
  const state = logic.getState();

  assert.deepEqual(state.listFields.map((m) => m.field.name), ['Name', 'Id', 'DetailLink']);
  assert.equal(state.listFields[0].field.header, '用户名');
  assert.deepEqual(state.addFields.map((m) => m.field.name), ['Name']);
  assert.deepEqual(state.editFields.map((m) => m.field.name), ['Name']);
  // 详情视图当前沿用 form 补丁（含 remove），与新增/编辑保持一致
  assert.deepEqual(state.detailFields.map((m) => m.field.name), ['Name']);

  clearPagePresets();
});

test('loadFields: 透传 entity/toolbar/ui 并记录生效 preset', async () => {
  const api = createApi({
    entity: { name: 'Student', displayName: '学生', primaryKey: 'Id' },
    list: [field('Id', { primaryKey: true })],
    toolbar: [{ name: 'Refresh', kind: 'request', action: 'Refresh', include: ['keys'] }],
    ui: { density: 'compact' },
  });
  const preset = { setting: { enableToolbar: true } };
  const { logic } = createLogic(api, { preset });

  await logic.loadFields('/School/Student');
  const state = logic.getState();

  assert.equal(state.entity.primaryKey, 'Id');
  assert.equal(state.toolbar.length, 1);
  assert.equal(state.toolbar[0].action, 'Refresh');
  assert.equal(state.ui.density, 'compact');
  assert.equal(state.preset, preset);
});

test('loadFields: menuId + requiredPermissions + 用户权限串位测试', async () => {
  const api = createApi({
    setting: { menuId: 12, requiredPermissions: { add: 2, update: 4, delete: 8 } },
    list: [field('Name')],
  });
  const { logic } = createLogic(api, { userPermission: '12#2' });

  await logic.loadFields('/Admin/User');
  const state = logic.getState();

  assert.equal(state.canAdd, true);
  assert.equal(state.canEdit, false);
  assert.equal(state.canDelete, false);
  assert.equal(state.canImport, true);
  assert.equal(state.canExport, false);
});

test('loadFields: 系统管理员直通动作权限', async () => {
  const api = createApi({
    setting: { menuId: 12, requiredPermissions: { add: 2, update: 4, delete: 8 } },
    list: [field('Name')],
  });
  const { logic } = createLogic(api, { userPermission: '12#0', isSystemAdmin: true });

  await logic.loadFields('/Admin/User');
  const state = logic.getState();

  assert.equal(state.canAdd, true);
  assert.equal(state.canEdit, true);
  assert.equal(state.canDelete, true);
});

test('loadFields: 只读页面关闭新增/编辑/删除', async () => {
  const api = createApi({ setting: { isReadOnly: true, menuId: 12, requiredPermissions: { add: 2 } }, list: [field('Name')] });
  const { logic } = createLogic(api, { userPermission: '12#2' });

  await logic.loadFields('/Admin/User');
  const state = logic.getState();

  assert.equal(state.canAdd, false);
  assert.equal(state.canEdit, false);
  assert.equal(state.canDelete, false);
});

test('loadFields: meta 未下发 menuId 时回退菜单权限映射', async () => {
  const api = createApi({ list: [field('Name')] });
  const { logic } = createLogic(api, { menuPermissions: { Add: '新增', Detail: '查看' } });

  await logic.loadFields('/Admin/User');
  const state = logic.getState();

  assert.equal(state.canAdd, true);
  assert.equal(state.canEdit, false);
  assert.equal(state.canExport, true);
});

test('loadFields: 无任何权限配置时默认允许', async () => {
  const api = createApi({ list: [field('Name')] });
  const { logic } = createLogic(api, { menuPermissions: {} });

  await logic.loadFields('/Admin/User');
  const state = logic.getState();

  assert.equal(state.canAdd, true);
  assert.equal(state.canEdit, true);
  assert.equal(state.canDelete, true);
});

test('loadFields: 主键优先取 entity.primaryKey，其次列表主键列', async () => {
  const withEntity = createLogic(
    createApi({ entity: { name: 'Student', primaryKey: 'Tid' }, list: [field('Id', { primaryKey: true })] }),
  );
  await withEntity.logic.loadFields('/School/Student');
  assert.equal(withEntity.logic.getState().pkField, 'Tid');

  const withColumn = createLogic(createApi({ list: [field('Tid', { primaryKey: true })] }));
  await withColumn.logic.loadFields('/School/Student');
  assert.equal(withColumn.logic.getState().pkField, 'Tid');

  const empty = createLogic(createApi({ list: [field('Name')] }));
  await empty.logic.loadFields('/School/Student');
  assert.equal(empty.logic.getState().pkField, 'id');
});

test('loadData: 首页与默认页大小不下发冗余参数', async () => {
  const api = createApi({}, { data: [{ Id: 1 }], page: { pageIndex: 1, pageSize: 20, totalCount: 1 }, stat: { Total: 1 } });
  const { logic } = createLogic(api);

  await logic.loadData('/Admin/User', { q: 'abc' });

  const call = api.calls.find((c) => c.name === 'getList');
  assert.deepEqual(call.params, { q: 'abc' });
  assert.equal(logic.getState().tableData.length, 1);
  assert.equal(logic.getState().pagination.totalCount, 1);
  assert.deepEqual(logic.getState().statData, { Total: 1 });
  assert.equal(logic.getState().loading, false);
});

test('loadData: 翻页后下发 pageIndex 与 pageSize', async () => {
  const api = createApi({}, { data: [] });
  const { logic } = createLogic(api);

  logic.setPagination(3, 50);
  await logic.loadData('/Admin/User');

  const call = api.calls.find((c) => c.name === 'getList');
  assert.equal(call.params.pageIndex, 3);
  assert.equal(call.params.pageSize, 50);
});

test('loadData: 请求失败时 loading 仍复位', async () => {
  const api = createApi({});
  api.page.getList = async () => {
    throw new Error('network');
  };
  const { logic } = createLogic(api);

  await assert.rejects(() => logic.loadData('/Admin/User'));
  assert.equal(logic.getState().loading, false);
});

test('add/update_: 提交期间 formLoading 切换', async () => {
  const api = createApi({});
  const { logic, updates } = createLogic(api);

  await logic.add('/Admin/User', { Name: 'a' });
  await logic.update_('/Admin/User', { Id: 1, Name: 'b' });

  const flags = updates.filter((u) => u.formLoading !== undefined).map((u) => u.formLoading);
  assert.deepEqual(flags, [true, false, true, false]);
  assert.equal(logic.getState().formLoading, false);
});

test('getExportUrl: 以 baseURL 拼出导出地址', () => {
  const api = createApi({});
  const { logic } = createLogic(api);

  const url = logic.getExportUrl('/Admin/User', 'xlsx');
  assert.ok(url.includes('/Admin/User'));
  assert.ok(url.toLowerCase().includes('xlsx'));
});
