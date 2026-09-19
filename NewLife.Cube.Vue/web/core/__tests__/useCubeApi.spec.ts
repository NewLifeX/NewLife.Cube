/**
 * useCubeApi.ts 单元测试
 *
 * useCubeApi 不再自建 createCubeApi，而是复用 request.ts 的统一实例，组装为
 * { client, tokenManager, user, menu, page, config }。此文件验证：
 *   1. usePageApi 生成的 CRUD 方法正确透传「/area/controller」类型前缀；
 *   2. getAction 经裸 client.request 返回后取 .data（统一解包语义）；
 *   3. cubeApi 默认导出组装了 client / page 等能力。
 *
 * createCubeApi 的初始化接线（unwrap、onFieldError/onBusinessError/onUnauthorized 等）
 * 已合并至 request.ts，其行为由 request.spec.ts 覆盖，此处 mock '../utils/request' 提供实例。
 *
 * 运行：pnpm test:unit core/__tests__/useCubeApi.spec.ts
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';

const h = vi.hoisted(() => {
  const page = {
    getPage: vi.fn(),
    getFields: vi.fn(),
    getList: vi.fn(),
    getDetail: vi.fn(),
    add: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
    deleteSelect: vi.fn(),
    uploadFile: vi.fn(),
    importFile: vi.fn(),
    getExportUrl: vi.fn(),
    lookup: vi.fn(),
    getChartData: vi.fn(),
  };
  const client = { request: vi.fn() };
  return { page, client };
});

vi.mock('../utils/request', () => ({
  client: h.client,
  request: h.client,
  cubeAxios: h.client,
  user: {},
  menu: {},
  page: h.page,
  config: {},
  tokenManager: {},
}));

describe('useCubeApi — 全局 API 实例与 usePageApi', () => {
  let mod: any;

  beforeEach(async () => {
    vi.clearAllMocks();
    vi.resetModules();
    mod = await import('../composables/useCubeApi');
  });

  it('默认导出 cubeApi 组装了 client / tokenManager / user / menu / page / config', () => {
    expect(mod.default.client).toBe(h.client);
    expect(mod.default.page).toBe(h.page);
    expect(mod.default.tokenManager).toBeDefined();
    expect(mod.default.user).toBeDefined();
    expect(mod.default.menu).toBeDefined();
    expect(mod.default.config).toBeDefined();
  });

  it('usePageApi 路径前缀为 /area/controller', () => {
    const api = mod.usePageApi('ProcessCard', 'ProcessCard');
    api.getList({ pageIndex: 0, pageSize: 20 });
    expect(h.page.getList).toHaveBeenCalledWith('/ProcessCard/ProcessCard', { pageIndex: 0, pageSize: 20 });
  });

  it('getDetail 透传 type 与 id', () => {
    const api = mod.usePageApi('School', 'Student');
    api.getDetail(7);
    expect(h.page.getDetail).toHaveBeenCalledWith('/School/Student', 7);
  });

  it('add / update / remove / lookup 透传 type', () => {
    const api = mod.usePageApi('School', 'Student');
    api.add({ name: 'a' });
    api.update({ id: 1, name: 'b' });
    api.remove(3);
    api.lookup('Some.Enum');
    expect(h.page.add).toHaveBeenCalledWith('/School/Student', { name: 'a' });
    expect(h.page.update).toHaveBeenCalledWith('/School/Student', { id: 1, name: 'b' });
    expect(h.page.remove).toHaveBeenCalledWith('/School/Student', 3);
    expect(h.page.lookup).toHaveBeenCalledWith('Some.Enum');
  });

  it('getAction 经裸 client.request 返回后取 .data（统一解包语义）', async () => {
    const payload = { code: 0, data: 'PAYLOAD', message: '' };
    h.client.request.mockResolvedValue({ data: payload });
    const api = mod.usePageApi('ProcessCard', 'ProcessCard');
    const r = await api.getAction('DoSomething');
    expect(h.client.request).toHaveBeenCalledWith({
      url: '/ProcessCard/ProcessCard/DoSomething',
      method: 'get',
    });
    expect(r).toEqual(payload); // 已取到 .data
  });
});