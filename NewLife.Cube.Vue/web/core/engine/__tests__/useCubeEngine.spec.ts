/**
 * `useCubeEngine` / `useCubeContext` 接线层单测
 *
 * 只验证「组装默认依赖 + provide + onMounted 初始化 + 消费端注入」这层接线，
 * 业务行为由 `createCubeEngine.spec.ts` 覆盖。
 * 网络层按规范用 `vi.mock` 模块桩（本项目禁用 MSW，见 `docs/standards/testing-standard.md`）。
 *
 * 运行：pnpm test:unit core/engine/__tests__/useCubeEngine.spec.ts
 */
import { describe, expect, it, vi } from 'vitest';
import { defineComponent, h } from 'vue';
import { mount } from '@vue/test-utils';
import {
  createDefaultCubeEngineDeps,
  tryUseCubeContext,
  useCubeContext,
  useCubeEngine,
} from '../useCubeEngine';
import type { CubeEngineContext } from '../types';

/**
 * 桩与调用记录。
 * 必须放 `vi.hoisted`：`vi.mock` 会被提到所有 import 之前执行，
 * 工厂里引用的外层变量若还是普通 `const` 会命中 TDZ（表现为「Cannot access before initialization」）。
 */
const stubs = vi.hoisted(() => {
  const META = {
    setting: { enableAdd: true },
    list: [{ name: 'Id', displayName: '编号', typeName: 'Int32', primaryKey: true }],
    search: [],
    addForm: [],
    editForm: [],
    detail: [],
  };
  const httpCalls: Array<{ url: string; method?: string }> = [];
  const listResponse = () => ({ code: 0, data: [], page: { totalCount: 0 } });
  const metaResponse = () => ({ code: 0, data: META });
  const fakeHttp = Object.assign(
    vi.fn((config: { url: string; method?: string }) => {
      httpCalls.push(config);
      // /meta 走元数据回包，其余按分页列表回包
      return Promise.resolve(String(config.url).endsWith('/meta') ? metaResponse() : listResponse());
    }),
    {
      get: vi.fn((url: string) => {
        httpCalls.push({ url, method: 'get' });
        return Promise.resolve(listResponse());
      }),
      post: vi.fn(async () => ({ code: 0, data: null })),
      put: vi.fn(async () => ({ code: 0, data: null })),
      delete: vi.fn(async () => ({ code: 0, data: null })),
    },
  );
  return {
    META,
    httpCalls,
    fakeHttp,
    getPage: vi.fn(async () => ({ code: 0, data: META })),
    getDetail: vi.fn(async () => ({ code: 0, data: { id: 1 } })),
    importFile: vi.fn(async () => ({ code: 0, message: '导入[a.xlsx] 共2行' })),
    deleteSelect: vi.fn(async () => ({ code: 0 })),
    elMessage: { success: vi.fn(), warning: vi.fn(), error: vi.fn(), info: vi.fn() },
  };
});

vi.mock('../../utils/request', () => ({
  default: stubs.fakeHttp,
  cubeApi: {
    page: {
      getPage: stubs.getPage,
      getDetail: stubs.getDetail,
      importFile: stubs.importFile,
      deleteSelect: stubs.deleteSelect,
    },
  },
}));

// 引擎只用得到 cubeApi.page.*，桩成与 api-core 一致的 ApiResponse 形态
vi.mock('../../composables/useCubeApi', () => ({
  default: {
    page: {
      getPage: stubs.getPage,
      getDetail: stubs.getDetail,
      importFile: stubs.importFile,
      deleteSelect: stubs.deleteSelect,
    },
  },
}));

vi.mock('element-plus', () => ({ ElMessage: stubs.elMessage }));

describe('useCubeEngine', () => {
  it('应该按路由推导实体并自动首查 当页面根挂载时', async () => {
    stubs.httpCalls.length = 0;
    /** 取出引擎实例，便于断言失败原因而不是只看到「请求没发生」。
     * 用对象持有而不是 `let engine = null`：赋值发生在 setup 回调里，
     * TS 的控制流分析会把 `engine` 收窄成 `never`，断言处直接报类型错误。 */
    const holder: { ctx: CubeEngineContext | null } = { ctx: null };
    const Child = defineComponent({
      setup() {
        const ctx = useCubeContext();
        return () => h('div', String(ctx.list.value.length));
      },
    });
    const Parent = defineComponent({
      setup() {
        holder.ctx = useCubeEngine({ routePath: '/device/device-profile' });
        return () => h(Child);
      },
    });

    const wrapper = mount(Parent);
    const listRequested = () =>
      stubs.httpCalls.some(
        (c) => c.url === '/api/Device/DeviceProfile' && (c.method ?? 'get') === 'get',
      );
    // 首查要穿过多层 await（init → getPage → 归一 → read），必须等真实发生而不是等一拍
    await vi.waitFor(() => expect(listRequested()).toBe(true));

    // routeToApiPrefix 会把路由派生的实体请求补上 /api；元数据请求打的就是 {type}/meta
    expect(stubs.httpCalls.some((c) => c.url === '/api/Device/DeviceProfile/meta')).toBe(true);
    expect(holder.ctx?.error.value).toBeNull();
    expect(listRequested()).toBe(true);
    expect(wrapper.text()).toBe('0');
  });

  it('应该抛出可读错误 当消费端找不到引擎时', () => {
    const Orphan = defineComponent({
      setup() {
        useCubeContext();
        return () => h('div');
      },
    });
    expect(() => mount(Orphan)).toThrow(/CubeEngineKey/);
  });

  it('应该返回 null 当用 tryUseCubeContext 且无引擎时', () => {
    let captured: unknown = 'not-called';
    const Probe = defineComponent({
      setup() {
        captured = tryUseCubeContext();
        return () => h('div');
      },
    });
    mount(Probe);
    expect(captured).toBeNull();
  });

  it('应该保持显式前缀不被路由覆盖 当引擎由 apiPrefix 驱动时', () => {
    const Page = defineComponent({
      setup() {
        const ctx = useCubeEngine({ apiPrefix: '/api/Molds/Mold' });
        return () => h('div', ctx.apiPrefix.value);
      },
    });
    expect(mount(Page).text()).toBe('/api/Molds/Mold');
  });
});

describe('createDefaultCubeEngineDeps', () => {
  it('应该解包 ApiResponse 只交元数据本体 当调用默认 getPage 时', async () => {
    const deps = createDefaultCubeEngineDeps();
    await expect(deps.getPage('/api/Molds/Mold')).resolves.toEqual(stubs.META);
  });

  it('应该返回 null 当控制器没有 /meta（接口型控制器）', async () => {
    const deps = createDefaultCubeEngineDeps();
    // 404 时空响应体：不能抛，交给引擎标记 metaMissing 渲染说明页
    stubs.fakeHttp.mockResolvedValueOnce('' as never);
    await expect(deps.getPage('/api/VTest1/Mfa')).resolves.toBeNull();
  });

  it('应该把导入响应原样交回由引擎解析 当调用默认 importFile 时', async () => {
    const deps = createDefaultCubeEngineDeps();
    await expect(
      deps.importFile?.('/api/Molds/Mold', new File(['x'], 'a.xlsx')),
    ).resolves.toEqual({ code: 0, message: '导入[a.xlsx] 共2行' });
  });

  it('应该把轻提示接到 ElMessage 当引擎发出通知时', () => {
    const deps = createDefaultCubeEngineDeps();
    deps.notify?.success?.('删除成功');
    deps.notify?.warning?.('注意');
    deps.notify?.error?.('失败');
    expect(stubs.elMessage.success).toHaveBeenCalledWith('删除成功');
    expect(stubs.elMessage.warning).toHaveBeenCalledWith('注意');
    expect(stubs.elMessage.error).toHaveBeenCalledWith('失败');
  });
});
