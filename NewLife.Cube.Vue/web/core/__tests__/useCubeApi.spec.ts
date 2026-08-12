/**
 * useCubeApi.ts 单元测试
 *
 * 验证：
 *   1. createCubeApi 接线（baseURL=host 去尾斜杠、tokenStorage=localStorage）；
 *   2. usePageApi 生成的 CRUD 方法正确透传「/area/controller」类型前缀；
 *   3. getAction 经裸 axios 返回后取 .data（与 request.ts 的 unwrap 语义区分）；
 *   4. onFieldError 聚合字段错误并经 ElMessage.error 展示；
 *   5. onUnauthorized 在非登录页回退到根路径。
 *
 * 底层 HTTP 行为已由 @cube/api-core 覆盖，此处 mock createCubeApi 捕获实例与回调后直接驱动。
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
  return {
    captured: null as any,
    page,
    client,
    elMessageError: vi.fn(),
    config: { request: { baseUrl: 'http://localhost:5000/' } },
  };
});

vi.mock('@cube/api-core', () => ({
  createCubeApi: (options: Record<string, unknown>) => {
    h.captured = options;
    return { page: h.page, client: h.client };
  },
}));

vi.mock('../configure', () => ({ getConfig: () => h.config }));
vi.mock('element-plus', () => ({ ElMessage: { error: h.elMessageError } }));

describe('useCubeApi — 全局 API 实例与 usePageApi', () => {
  let mod: any;

  beforeEach(async () => {
    vi.clearAllMocks();
    vi.resetModules();
    mod = await import('../composables/useCubeApi');
  });

  it('createCubeApi 以 baseURL=host(去尾斜杠) 创建，tokenStorage=localStorage', () => {
    expect(h.captured?.baseURL).toBe('http://localhost:5000'); // 尾斜杠被去掉
    expect(h.captured?.tokenStorage).toBe('localStorage');
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

  it('getAction 返回 ApiResponse.data（裸 axios 需 .then(res=>res.data)）', async () => {
    const payload = { code: 0, data: 'PAYLOAD', message: '' };
    h.client.request.mockResolvedValue({ data: payload });
    const api = mod.usePageApi('ProcessCard', 'ProcessCard');
    const r = await api.getAction('DoSomething');
    expect(h.client.request).toHaveBeenCalledWith({
      url: '/ProcessCard/ProcessCard/DoSomething',
      method: 'get',
    });
    expect(r).toEqual(payload); // 已取到 .data，证明 unwrap 语义绑定在 .then 上
  });

  it('onFieldError 拼接 message 并经 ElMessage.error 展示', () => {
    h.captured?.onFieldError([
      { field: 'code', message: '编码不可空' },
      { field: 'name', message: '名称不可空' },
    ]);
    expect(h.elMessageError).toHaveBeenCalledWith('编码不可空；名称不可空');
  });

  it('onUnauthorized 在非登录页时回退到根路径', () => {
    // jsdom 不支持通过 href 赋值导航，用可控的 location 桩捕获赋值并派生 pathname
    const fakeLocation = {
      _href: 'http://localhost/dashboard',
      pathname: '/dashboard',
      get href() {
        return this._href;
      },
      set href(value: string) {
        this._href = value;
        this.pathname = new URL(value, 'http://localhost').pathname;
      },
    };
    const original = window.location;
    Object.defineProperty(window, 'location', { configurable: true, value: fakeLocation });
    try {
      h.captured?.onUnauthorized();
      expect(fakeLocation.pathname).toBe('/');
    } finally {
      Object.defineProperty(window, 'location', { configurable: true, value: original });
    }
  });

  it('onUnauthorized 已在登录页时不回退（避免循环跳转）', () => {
    const fakeLocation = {
      _href: 'http://localhost/login',
      pathname: '/login',
      get href() {
        return this._href;
      },
      set href(_value: string) {
        throw new Error('不应发生导航');
      },
    };
    const original = window.location;
    Object.defineProperty(window, 'location', { configurable: true, value: fakeLocation });
    try {
      expect(() => h.captured?.onUnauthorized()).not.toThrow();
      expect(fakeLocation.pathname).toBe('/login'); // 未改动
    } finally {
      Object.defineProperty(window, 'location', { configurable: true, value: original });
    }
  });
});
