/**
 * `createCubeEngine` 纯工厂单测（引擎行为的验收矩阵）
 *
 * 按 `docs/architecture/cube-engine.md` §6 的测试矩阵覆盖：首查、分页、关键字、
 * 条件过滤、删除端点、批量删除降级、提交校验与清洗、详情回显、导入 0 行降级、
 * 导出文件名、失败分支、reload 换实体。
 *
 * 依赖全部注入假实现（`vi.fn`），不发真实请求、不依赖配置与后端
 * （规范见 `docs/standards/testing-standard.md`：MSW 禁用，用 `vi.mock` + 依赖注入）。
 *
 * 运行：pnpm test:unit core/engine/__tests__/createCubeEngine.spec.ts
 */
import { describe, expect, it, vi } from 'vitest';
import { createCubeEngine } from '../createCubeEngine';
import type { CubeEngineDeps, CubePageMeta } from '../types';

/** 与后端 EntityController 一致的假响应：列表 `GET {type}`、详情 `GET {type}/Detail` */
const LIST_BODY = { code: 0, data: [{ id: 1, name: 'admin' }], page: { totalCount: 1 } };
const DETAIL_BODY = { code: 0, data: { id: 7, name: '详细名', enable: 'true' } };

const META: CubePageMeta = {
  setting: { enableAdd: true, enableSelect: true },
  list: [
    { name: 'Id', displayName: '编号', typeName: 'Int32', primaryKey: true },
    { name: 'Name', displayName: '名称', typeName: 'String', length: 50 },
  ],
  search: [
    { name: 'Name', displayName: '名称', typeName: 'String', length: 50 },
    { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
  ],
  addForm: [{ name: 'Name', displayName: '名称', typeName: 'String', nullable: false }],
  editForm: [
    { name: 'Name', displayName: '名称', typeName: 'String', nullable: false },
    { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
  ],
  detail: [],
};

interface RecordedCall {
  url: string;
  method?: string;
  params?: Record<string, unknown>;
  data?: unknown;
}

function makeHttp(overrides: { blob?: Blob; detail?: unknown } = {}) {
  const calls: RecordedCall[] = [];
  const respond = (config: RecordedCall): unknown => {
    if (config.method === 'get' && config.url.endsWith('/ExportFile')) {
      return overrides.blob ?? new Blob(['x'], { type: 'text/csv' });
    }
    if (config.method === 'get' && config.url.endsWith('/Detail')) {
      return overrides.detail ?? DETAIL_BODY;
    }
    // 每次返回**新数组**：假响应体若被各用例共享同一引用，
    // 前一个用例写过的行会串进后一个用例（跨用例污染，红得查不到原因）
    if (config.method === 'get') return { ...LIST_BODY, data: [...LIST_BODY.data] };
    return { code: 0, data: null, message: 'ok' };
  };
  const send = vi.fn((config: RecordedCall) => {
    calls.push(config);
    return Promise.resolve(respond(config));
  });
  const withVerb = (method: string) =>
    vi.fn((url: string, arg?: unknown, config?: Record<string, unknown>) => {
      // axios 形态：post(url, data, config) / get & put & delete(url, config)
      const isBodyVerb = method === 'post' || method === 'put';
      const config2 = {
        url,
        method,
        data: isBodyVerb ? arg : undefined,
        ...(isBodyVerb ? config ?? {} : (arg as Record<string, unknown>) ?? {}),
      };
      calls.push(config2);
      return Promise.resolve(respond(config2));
    });

  return Object.assign(send, {
    calls,
    get: withVerb('get'),
    post: withVerb('post'),
    put: withVerb('put'),
    delete: withVerb('delete'),
  });
}

function makeNotify() {
  return { success: vi.fn(), warning: vi.fn(), error: vi.fn(), info: vi.fn() };
}

function makeDeps(overrides: Partial<CubeEngineDeps> & { http?: ReturnType<typeof makeHttp> } = {}) {
  const http = overrides.http ?? makeHttp();
  const getPage = overrides.getPage ?? vi.fn(async () => META);
  const notify = overrides.notify ?? makeNotify();
  // 除以上三项默认值外，其余可选依赖（getDetail / importFile / exportFile /
  // downloadTemplate / deleteSelect）按原样注入，未被覆盖时保持 undefined 走引擎内置实现
  const deps = {
    ...overrides,
    http,
    getPage,
    notify,
  } as CubeEngineDeps;
  return { http, getPage, notify, deps };
}

const ROUTE = '/device/device-profile';
/** routeToApiPrefix 会把路由派生的实体请求补上 /api */
const TYPE = '/api/Device/DeviceProfile';

/** 找出某方法的最后一次调用 */
function lastCall(http: ReturnType<typeof makeHttp>, method: string) {
  return [...http.calls].reverse().find((c) => (c.method ?? 'get') === method);
}

describe('createCubeEngine 元数据驱动初始化', () => {
  it('应该拉 GetPage 后自动首查 当 init 成功时', async () => {
    const { deps, http, getPage } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    expect(ctx.schemaReady.value).toBe(false);

    await ctx.init();

    expect(getPage).toHaveBeenCalledWith(TYPE);
    expect(ctx.schemaReady.value).toBe(true);
    expect(ctx.error.value).toBeNull();
    expect(ctx.list.value).toEqual(LIST_BODY.data);
    expect(ctx.total.value).toBe(1);
    const listCall = lastCall(http, 'get');
    expect(listCall?.url).toBe(TYPE);
    expect(listCall?.params).toMatchObject({ pageIndex: 1, pageSize: 20 });
  });

  it('应该不发起列表请求 当 autoQuery 为 false 时', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE, autoQuery: false }, deps);
    await ctx.init();
    expect(ctx.schemaReady.value).toBe(true);
    expect(http.calls.filter((c) => c.method === 'get')).toHaveLength(0);
  });

  it('应该置错并停在未就绪态 当 GetPage 失败时', async () => {
    const { deps, http } = makeDeps({ getPage: vi.fn(async () => { throw new Error('boom'); }) });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    expect(ctx.error.value).toBe('boom');
    expect(ctx.schemaReady.value).toBe(false);
    expect(http.calls.filter((c) => c.method === 'get')).toHaveLength(0);
  });

  it('应该把搜索模型全部初始化为 undefined 当元数据含布尔字段时', async () => {
    // Boolean 搜索默认 false 会让首查莫名带上「否」条件，页面打开看到的不是全量
    const { deps } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    expect(ctx.searchModel.Name).toBeUndefined();
    expect(ctx.searchModel.Enable).toBeUndefined();
  });

  it('应该过滤主键与 Guid 当生成搜索字段时', async () => {
    const { deps } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    expect(ctx.searchFields.value.map((f) => f.name)).toEqual(['Name', 'Enable']);
    expect(ctx.listFields.value.map((f) => f.name)).toEqual(['Id', 'Name']);
  });

  it('应该不请求 GetPage 当传入前端字段覆盖（配置驱动）时', async () => {
    const { deps, getPage } = makeDeps();
    const ctx = createCubeEngine(
      {
        apiPrefix: TYPE,
        fields: {
          setting: {},
          listFields: [{ name: 'Code', displayName: '编号', typeName: 'String' }],
          addFields: [{ name: 'Code', displayName: '编号', typeName: 'String', nullable: false }],
        },
      },
      deps,
    );
    await ctx.init();
    expect(getPage).not.toHaveBeenCalled();
    expect(ctx.listFields.value.map((f) => f.name)).toEqual(['Code']);
    // 未显式给 searchFields 时由列表字段按口径过滤得到
    expect(ctx.searchFields.value.map((f) => f.name)).toEqual(['Code']);
  });

  it('应该拒绝并给出可读错误 当既没 routePath 也没 apiPrefix 时', async () => {
    const { deps } = makeDeps();
    const ctx = createCubeEngine({}, deps);
    await ctx.init();
    expect(ctx.error.value).toContain('无法定位实体接口');
  });
});

describe('createCubeEngine 分页与查询', () => {
  it('应该用新页码请求 当 changePage 后', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    await ctx.changePage(3);
    const listCall = lastCall(http, 'get');
    expect(listCall?.params).toMatchObject({ pageIndex: 3, pageSize: 20 });
    expect(ctx.page.value).toBe(3);
  });

  it('应该回第 1 页并带上条数 当 changePageSize 后', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    await ctx.changePage(4);
    await ctx.changePageSize(50);
    expect(lastCall(http, 'get')?.params).toMatchObject({ pageIndex: 1, pageSize: 50 });
    expect(ctx.pageSize.value).toBe(50);
  });

  it('应该把关键字作为 q 发送 当 searchByKeyword 时', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.searchByKeyword('  泵  ');
    expect(lastCall(http, 'get')?.params).toMatchObject({ q: '泵', pageIndex: 1 });
  });

  it('应该不带 q 当关闭 keyword 选项时', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE, keyword: false }, deps);
    await ctx.searchByKeyword('泵');
    expect(lastCall(http, 'get')?.params).not.toHaveProperty('q');
  });

  it('应该丢弃空条件并保留 0 当 setFilter 提交筛选时', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    ctx.searchModel.Name = '';
    await ctx.setFilter('Count', 0);
    const params = lastCall(http, 'get')?.params ?? {};
    expect(params).not.toHaveProperty('Name');
    expect(params).toHaveProperty('Count', 0);
  });

  it('应该清空关键字与条件并回首页 当 resetSearch 时', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    ctx.searchModel.Name = 'x';
    await ctx.changePage(5);
    await ctx.resetSearch();
    expect(ctx.keyword.value).toBe('');
    expect(ctx.searchModel.Name).toBeUndefined();
    expect(lastCall(http, 'get')?.params).toMatchObject({ pageIndex: 1 });
  });

  it('应该换实体重新初始化 当 reload 指定新 apiPrefix 时', async () => {
    const { deps, http, getPage } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    await ctx.reload({ apiPrefix: '/api/Molds/Mold' });
    expect(ctx.apiPrefix.value).toBe('/api/Molds/Mold');
    expect(getPage).toHaveBeenLastCalledWith('/api/Molds/Mold');
    expect(lastCall(http, 'get')?.url).toBe('/api/Molds/Mold');
  });
});

describe('createCubeEngine 删除', () => {
  it('应该用路径主键删除单条 当调用 remove 时', async () => {
    // 后端 DELETE /api/[area]/[controller]/{id}（EntityController.Delete）
    const { deps, http, notify } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    const before = http.calls.length;
    const ok = await ctx.remove({ id: 9 });
    expect(ok).toBe(true);
    const del = [...http.calls.slice(before)].reverse().find((c) => c.method === 'delete');
    expect(del?.url).toBe(`${TYPE}/9`);
    expect(del?.params).toBeUndefined();
    expect(notify.success).toHaveBeenCalledWith('删除成功');
  });

  it('应该按元数据主键取值 当主键列名不是 id 时', async () => {
    const { deps, http } = makeDeps({
      getPage: vi.fn(async () => ({
        ...META,
        list: [{ name: 'EquipmentId', displayName: '设备', typeName: 'Int32', primaryKey: true }],
      })),
    });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    await ctx.remove({ EquipmentId: 12 });
    const del = [...http.calls].reverse().find((c) => c.method === 'delete');
    expect(del?.url).toBe(`${TYPE}/12`);
    expect(del?.params).toBeUndefined();
  });

  it('应该拒绝并提示 当行数据没有主键时', async () => {
    const { deps, http, notify } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    const before = http.calls.length;
    expect(await ctx.remove({ name: 'x' })).toBe(false);
    expect(http.calls).toHaveLength(before);
    expect(notify.error).toHaveBeenCalled();
  });

  it('应该走批量删除依赖 当提供 ids 批量端点时', async () => {
    const deleteSelect = vi.fn(async () => undefined);
    const { deps, http } = makeDeps({ deleteSelect });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    ctx.setSelected([{ id: 1 }, { id: 2 }, { name: '无主键' }]);
    expect(await ctx.removeSelected()).toBe(true);
    expect(deleteSelect).toHaveBeenCalledWith(TYPE, [1, 2]);
    expect(http.calls.some((c) => c.method === 'delete')).toBe(false);
    expect(ctx.selected.value).toEqual([]);
  });

  it('应该逐条降级删除 当未提供批量删除依赖时', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    ctx.setSelected([{ id: 1 }, { id: 2 }]);
    expect(await ctx.removeSelected()).toBe(true);
    const dels = http.calls.filter((c) => c.method === 'delete');
    expect(dels).toHaveLength(2);
    expect(dels.map((c) => c.url)).toEqual([`${TYPE}/1`, `${TYPE}/2`]);
  });

  it('应该拒绝空勾选 当未选中任何行时', async () => {
    const { deps, notify } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    expect(await ctx.removeSelected()).toBe(false);
    expect(notify.warning).toHaveBeenCalled();
  });
});

describe('createCubeEngine 表单提交', () => {
  it('应该以 POST 新增并给 Boolean 默认 false 当打开新增并提交时', async () => {
    const { deps, http, notify } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    ctx.openCreate();
    expect(ctx.formMode.value).toBe('add');
    expect(ctx.dialogTitle.value).toBe('新增');
    ctx.formModel.Name = '新记录';
    ctx.formModel.Enable = false;
    expect(await ctx.submitForm()).toBe(true);
    const post = lastCall(http, 'post');
    expect(post?.url).toBe(TYPE);
    expect(post?.data).toMatchObject({ Name: '新记录' });
    expect(notify.success).toHaveBeenCalledWith('新增成功');
    expect(ctx.dialogVisible.value).toBe(false);
  });

  it('应该拦截提交并提示字段名 当必填项留空时', async () => {
    const { deps, http, notify } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    ctx.openCreate();
    expect(await ctx.submitForm()).toBe(false);
    expect(notify.warning).toHaveBeenCalledWith('名称不能为空');
    expect(http.calls.some((c) => c.method === 'post')).toBe(false);
    expect(ctx.dialogVisible.value).toBe(true);
  });

  it('应该取详情回显并把字符串布尔还原 当打开编辑时', async () => {
    const { deps } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    await ctx.openEdit({ id: 7, name: '列表投影值' });
    expect(ctx.formMode.value).toBe('edit');
    // 列表行是显示态投影，编辑初值必须来自详情接口
    expect(ctx.formModel.name).toBe('详细名');
    expect(ctx.formModel.enable).toBe(true);
    expect(ctx.formFields.value.map((f) => f.name)).toEqual(['Name', 'Enable']);
  });

  it('应该走集合路由 PUT 且清洗 PascalCase 重复键 当编辑提交时', async () => {
    const { deps, http, notify } = makeDeps({
      getDetail: async () => ({ id: 5, name: 'a', enable: false, Enable: 1 }),
    });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    await ctx.openEdit({ id: 5 });
    // 表单模型键名跟随详情回包（camelCase），组件层用 resolveKey 写回同一键
    ctx.formModel.name = 'b';
    expect(await ctx.submitForm()).toBe(true);
    const put = lastCall(http, 'put');
    // 后端只有 [HttpPut("/api/[area]/[controller]")]（EntityController.cs:201）
    expect(put?.url).toBe(TYPE);
    expect(put?.data).not.toHaveProperty('Enable');
    expect(put?.data).toMatchObject({ id: 5, name: 'b', enable: false });
    expect(notify.success).toHaveBeenCalledWith('更新成功');
  });

  it('应该只关闭弹窗 当查看模式提交时', async () => {
    const { deps, http } = makeDeps();
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    await ctx.openView({ id: 7 });
    expect(ctx.formMode.value).toBe('view');
    const before = http.calls.length;
    expect(await ctx.submitForm()).toBe(true);
    expect(ctx.dialogVisible.value).toBe(false);
    expect(http.calls).toHaveLength(before);
  });
});

describe('createCubeEngine 导入导出', () => {
  it('应该把 0 行导入降级为警示 当后端文案是「共0行」时', async () => {
    const importFile = vi.fn(async () => ({ code: 0, message: '导入[点检项目.xlsx] 共0行' }));
    const { deps, notify } = makeDeps({ importFile });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    const result = await ctx.importFile(new File(['x'], 'a.xlsx'));
    expect(result?.rows).toBe(0);
    expect(notify.warning).toHaveBeenCalled();
    expect(notify.success).not.toHaveBeenCalled();
  });

  it('应该提示成功并重查列表 当导入有行数时', async () => {
    const { deps, http, notify } = makeDeps({
      importFile: vi.fn(async () => ({ code: 0, message: '导入[a.xlsx] 共3行' })),
    });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    const before = http.calls.length;
    await ctx.importFile(new File(['x'], 'a.xlsx'));
    expect(notify.success).toHaveBeenCalledWith('导入[a.xlsx] 共3行');
    expect(http.calls.length).toBeGreaterThan(before);
  });

  it('应该把 blob 与文件名交给视图层下载 当导出时', async () => {
    const blob = new Blob(['x'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const exportFile = vi.fn(async () => blob);
    const { deps, notify } = makeDeps({ exportFile });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    const name = await ctx.exportFile();
    expect(ctx.error.value).toBeNull();
    expect(exportFile).toHaveBeenCalledWith(TYPE, { format: 'xlsx' });
    // 修正点：xlsx 不能再落成 .bin
    expect(name).toMatch(/^DeviceProfile_\d{14}\.xlsx$/);
    expect(ctx.pendingBlob.value).toEqual({ blob, name });
    expect(notify.success).toHaveBeenCalledWith('导出成功');
  });

  it('应该带上当前关键字与筛选 当导出参数拼装时', async () => {
    const exportFile = vi.fn(async () => new Blob(['x']));
    const { deps } = makeDeps({ exportFile });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    ctx.searchModel.Name = '泵';
    await ctx.searchByKeyword('阀');
    await ctx.exportFile('csv');
    expect(exportFile).toHaveBeenLastCalledWith(TYPE, { format: 'csv', q: '阀', Name: '泵' });
  });

  it('应该提示后端未实现该接口 当下载模板失败时', async () => {
    const { deps, notify } = makeDeps({
      downloadTemplate: vi.fn(async () => { throw new Error('404'); }),
    });
    const ctx = createCubeEngine({ routePath: ROUTE }, deps);
    await ctx.init();
    expect(await ctx.downloadTemplate()).toBeNull();
    expect(notify.error).toHaveBeenCalledWith('下载导入模板失败，请确认后端已实现 DownloadTemplate 接口');
  });
});
