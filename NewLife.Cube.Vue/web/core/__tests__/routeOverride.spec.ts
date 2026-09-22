import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { createRouter, createMemoryHistory, type Router } from 'vue-router';
import {
  addExternalRoute,
  addFrameworkRoute,
  applyExternalRoutes,
  bindRoutePriority,
  declareExternalRoutes,
  getRoutePrioritySnapshot,
  isDeclaredExternal,
  normalizeRoutePath,
  resetRoutePriority,
} from '@newlifex/cube-vue/core/router/routeOverride';
import type { ConfigRoute } from '@newlifex/cube-vue/core/typings';

/** 占位组件：单测只验证路由匹配结果，不渲染组件 */
const Stub = { render: () => null };

/** 框架静态路由（复刻 core/routes/index.ts 的关键几条） */
const FRAMEWORK_ROUTES: ConfigRoute[] = [
  { path: '/', name: 'home', component: Stub },
  { path: '/login', name: 'login', component: Stub },
  { path: '/:pathMatch(.*)*', name: 'DefaultEntity', component: Stub },
];

/**
 * 创建只含框架静态路由的 router
 * 复刻 core/router/index.ts 的注册方式：createRouter 空表 + 逐条走 addFrameworkRoute
 */
function createFrameworkRouter(frameworkRoutes: ConfigRoute[] = FRAMEWORK_ROUTES): Router {
  resetRoutePriority();
  const router = createRouter({ history: createMemoryHistory(), routes: [] });
  bindRoutePriority(router);
  frameworkRoutes.forEach((route) => addFrameworkRoute(route, router));
  return router;
}

describe('normalizeRoutePath（路径归一化）', () => {
  it('动态段统一为占位符，便于判定同形态', () => {
    expect(normalizeRoutePath('/Nodes/Node/:id')).toBe('/Nodes/Node/:__param__');
    expect(normalizeRoutePath('/Nodes/Node/:name')).toBe('/Nodes/Node/:__param__');
    expect(normalizeRoutePath('/Nodes/Node/:id(\\d+)')).toBe('/Nodes/Node/:__param__');
  });

  it('catch-all 归一化为单占位符', () => {
    expect(normalizeRoutePath('/:pathMatch(.*)*')).toBe('/:__param__');
  });

  it('去尾部斜杠，根路径保持不变', () => {
    expect(normalizeRoutePath('/Trace/Detail/')).toBe('/Trace/Detail');
    expect(normalizeRoutePath('/')).toBe('/');
  });

  it('大小写敏感：不把 /Trace 与 /trace 视为同一路由', () => {
    expect(normalizeRoutePath('/Trace')).not.toBe(normalizeRoutePath('/trace'));
  });
});

describe('外部路由优先（框架侧让位）', () => {
  let warnSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.spyOn(console, 'info').mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    resetRoutePriority();
  });

  it('外部先声明时，框架同形态静态路由让位（不注册）', () => {
    resetRoutePriority();
    const router = createRouter({ history: createMemoryHistory(), routes: [] });
    bindRoutePriority(router);

    // 外部声明在前（例如业务入口先 import 了外部路由模块）
    declareExternalRoutes([{ path: '/', name: 'HomeRedirect', redirect: '/stardust/overview' }]);
    expect(isDeclaredExternal({ path: '/', name: 'home' })).toBe(true);

    // 框架随后注册同形态静态路由 → 让位
    const unregister = addFrameworkRoute({ path: '/', name: 'home', component: Stub }, router);
    expect(unregister).toBeNull();
    expect(router.hasRoute('home')).toBe(false);

    // 外部路由落盘后根路径命中外部声明
    applyExternalRoutes(undefined, router);
    expect(String(router.resolve('/').name)).toBe('HomeRedirect');
    expect(warnSpy.mock.calls.some((args) => String(args[0]).includes('让位于外部声明'))).toBe(true);
  });

  it('框架先注册、外部后接管：清掉框架记录并接管同形态路径', () => {
    const router = createFrameworkRouter();
    // 接管前：根路径命中框架的 home
    expect(String(router.resolve('/').name)).toBe('home');

    applyExternalRoutes(
      [
        { path: '/', name: 'HomeRedirect', redirect: '/stardust/overview' },
        { path: '/login', name: 'BizLogin', component: Stub },
      ],
      router,
    );

    expect(String(router.resolve('/').name)).toBe('HomeRedirect');
    expect(router.hasRoute('home')).toBe(false);
    expect(String(router.resolve('/login').name)).toBe('BizLogin');
    // catch-all 未被声明 → 仍由框架兜底
    expect(String(router.resolve('/anything-else').name)).toBe('DefaultEntity');
  });

  it('同形态但参数名不同也能接管（/:id 与 /:name 视为同一形态）', () => {
    const router = createFrameworkRouter([
      { path: '/Nodes/Node/:id', name: 'NodeDetail', component: Stub },
    ]);
    applyExternalRoutes([{ path: '/Nodes/Node/:name', name: 'MyNodeDetail', component: Stub }], router);

    expect(String(router.resolve('/Nodes/Node/5').name)).toBe('MyNodeDetail');
    expect(router.hasRoute('NodeDetail')).toBe(false);
  });

  it('无名框架路由同样能被接管（走 addRoute 返回的退订函数）', () => {
    const router = createFrameworkRouter([{ path: '/hidden/:id', component: Stub }]);
    expect(router.resolve('/hidden/1').name).toBeUndefined();

    applyExternalRoutes([{ path: '/hidden/:id', name: 'MyHidden', component: Stub }], router);
    expect(String(router.resolve('/hidden/1').name)).toBe('MyHidden');
  });

  it('同名不同路径也能接管（按 name 清理）', () => {
    const router = createFrameworkRouter([{ path: '/a', name: 'dup', component: Stub }]);
    applyExternalRoutes([{ path: '/b', name: 'dup', component: Stub }], router);

    expect(String(router.resolve('/b').name)).toBe('dup');
    expect(router.hasRoute('dup')).toBe(true);
    expect(router.getRoutes().some((r) => r.path === '/a')).toBe(false);
  });

  it('未被外部声明的框架路由不受影响', () => {
    const router = createFrameworkRouter([
      { path: '/login', name: 'login', component: Stub },
      { path: '/Nodes/Node', name: 'menu-NodesNode', component: Stub },
    ]);
    applyExternalRoutes([{ path: '/', name: 'HomeRedirect', redirect: '/stardust/overview' }], router);

    expect(String(router.resolve('/login').name)).toBe('login');
    expect(String(router.resolve('/Nodes/Node').name)).toBe('menu-NodesNode');
  });

  it('applyExternalRoutes 幂等：重复应用不产生重复路由', () => {
    const router = createFrameworkRouter();
    const declarations: ConfigRoute[] = [
      { path: '/', name: 'HomeRedirect', redirect: '/stardust/overview' },
      { path: '/Trace/Graph', name: 'TraceGraph', component: Stub },
    ];

    applyExternalRoutes(declarations, router);
    const countAfterFirst = router.getRoutes().length;
    applyExternalRoutes(declarations, router);
    expect(router.getRoutes().length).toBe(countAfterFirst);
    expect(String(router.resolve('/Trace/Graph').name)).toBe('TraceGraph');
  });

  it('addExternalRoute 可运行时追加，并立即触发规则', () => {
    const router = createFrameworkRouter();
    addExternalRoute({ path: '/', name: 'LateOverride', redirect: '/late' }, router);
    expect(String(router.resolve('/').name)).toBe('LateOverride');
    expect(router.hasRoute('home')).toBe(false);
  });

  it('撤销外部路由时同步撤销声明，框架侧可重新注册（避免框架永久让位）', () => {
    const router = createFrameworkRouter();
    const unregister = addExternalRoute(
      { path: '/', name: 'HomeRedirect', redirect: '/stardust/overview' },
      router,
    );
    expect(isDeclaredExternal({ path: '/', name: 'home' })).toBe(true);

    unregister?.();

    // 路由与声明都撤销
    expect(router.hasRoute('HomeRedirect')).toBe(false);
    expect(isDeclaredExternal({ path: '/', name: 'home' })).toBe(false);
    // 框架侧重新注册不再被拦截
    const reRegister = addFrameworkRoute({ path: '/', name: 'home', component: Stub }, router);
    expect(reRegister).not.toBeNull();
    expect(String(router.resolve('/').name)).toBe('home');
  });

  it('撤销框架路由时记录表同步清理（快照不残留失效记录）', () => {
    resetRoutePriority();
    const router = createRouter({ history: createMemoryHistory(), routes: [] });
    bindRoutePriority(router);
    const unregister = addFrameworkRoute({ path: '/temp', name: 'Temp', component: Stub }, router);

    unregister?.();

    expect(router.hasRoute('Temp')).toBe(false);
    const snapshot = getRoutePrioritySnapshot();
    expect(snapshot.records.some((r) => r.name === 'Temp')).toBe(false);
  });
});

describe('覆盖守门（只告警，不拦截）', () => {
  beforeEach(() => {
    vi.spyOn(console, 'info').mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    resetRoutePriority();
  });

  it('覆盖安全关键路径（/login）时告警，但仍允许覆盖', () => {
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const router = createFrameworkRouter();
    applyExternalRoutes([{ path: '/login', name: 'BizLogin', component: Stub }], router);

    expect(String(router.resolve('/login').name)).toBe('BizLogin');
    expect(warnSpy.mock.calls.some((args) => String(args[0]).includes('安全关键路径'))).toBe(true);
  });

  it('外部路由缺字符串 name 时告警', () => {
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const router = createFrameworkRouter();
    applyExternalRoutes([{ path: '/no-name', component: Stub }], router);

    expect(warnSpy.mock.calls.some((args) => String(args[0]).includes('未设置字符串 name'))).toBe(true);
  });

  it('外部路由既无 component 也无 redirect 时告警（命中会白屏）', () => {
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const router = createFrameworkRouter();
    // 刻意构造非法路由（无 component / redirect）以驱动守门告警，故此处用断言收窄类型
    applyExternalRoutes([{ path: '/blank', name: 'Blank' } as ConfigRoute], router);

    expect(warnSpy.mock.calls.some((args) => String(args[0]).includes('命中后会白屏'))).toBe(true);
  });
});
