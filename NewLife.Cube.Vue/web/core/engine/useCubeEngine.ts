/**
 * `useCubeEngine` —— 引擎的薄 composable 接线层（生命周期 + provide/inject）
 *
 * 职责边界（见 `docs/architecture/cube-engine.md` §4.3）：
 *   - 纯工厂 `createCubeEngine(deps)` 只有逻辑，不碰生命周期；
 *   - 本文件负责「组装默认 deps → 创建上下文 → `provide` → `onMounted(init)` → 路由变化重载」；
 *   - 子组件 / 插槽用 `useCubeContext()` 消费，不重复创建引擎。
 *
 * 命名约定：页面根用 `useCubeEngine()`（创建 + 提供），消费端用 `useCubeContext()`（注入）。
 */
import { inject, onMounted, provide, watch, type InjectionKey } from 'vue';
import { useRoute } from 'vue-router';
import type { RouteLocationNormalizedLoaded } from 'vue-router';
import { ElMessage } from 'element-plus';
import { createCubeEngine } from './createCubeEngine';
import type {
  CubeEngineContext,
  CubeEngineDeps,
  CubeEngineOptions,
  CubeHttpLike,
  CubePageMeta,
} from './types';

/** 引擎上下文的注入键 */
export const CubeEngineKey: InjectionKey<CubeEngineContext> = Symbol('CubeEngine');

/**
 * 真实请求层惰性加载。
 *
 * `core/utils/request` 在模块作用域就 `getConfig()` 取配置并 `createCubeApi`，
 * 一旦被静态 import，就会把 axios / cookie / i18n 整条链拖进：
 *   - 纯逻辑单测（本不该关心网络）；
 *   - CT gallery（它把 `@newlifex/cube-vue/core/utils/request` 别名成桩，但**相对路径 import 不吃别名**，
 *     真实文件仍会被加载，配置虚拟模块在 gallery 里是空壳 → 直接抛错）；
 *   - 只想注入自管 `deps` 的业务页（不该为它买单）。
 * 所以这里只在真正要发请求时才 `import()`，同一次加载内复用同一份模块实例。
 */
let requestLayer: Promise<{ request: CubeHttpLike; cubeApi: CubeApiSurface }> | null = null;
function loadRequestLayer() {
  if (!requestLayer) {
    requestLayer = Promise.all([
      import('../utils/request'),
      import('../composables/useCubeApi'),
    ]).then(([req, api]) => ({
      request: req.default as unknown as CubeHttpLike,
      cubeApi: api.default as unknown as CubeApiSurface,
    }));
  }
  return requestLayer;
}

/** `cubeApi.page` 的最小面（够引擎用即可，不锁死 api-core 的具体类型） */
interface CubeApiSurface {
  page: {
    getPage: (type: string) => Promise<{ data?: unknown }>;
    getDetail: (type: string, id: number | string) => Promise<{ data?: unknown }>;
    importFile: (type: string, file: File) => Promise<unknown>;
    deleteSelect: (type: string, keys: (number | string)[]) => Promise<unknown>;
  };
}

/**
 * 构造默认依赖：元数据 / 详情 / 批量删除 / 导入走 `cubeApi.page`（`@newlifex/api-core` 统一实现），
 * 文件流与列表读写走 `request`。
 *
 * 两处解包口径（`docs/standards/api-contract.md`）：
 *   - `cubeApi.page.*` 返回 `ApiResponse`，元数据/详情要取 `.data`；
 *   - `request` 已做薄解包，blob 场景返回值本身就是文件流。
 */
export function createDefaultCubeEngineDeps(): CubeEngineDeps {
  const http: CubeHttpLike = Object.assign(
    async (config: Parameters<CubeHttpLike>[0]) => {
      const { request } = await loadRequestLayer();
      return request(config);
    },
    {
      get: async (url: string, config?: Record<string, unknown>) => {
        const { request } = await loadRequestLayer();
        return request.get(url, config);
      },
      post: async (url: string, data?: unknown, config?: Record<string, unknown>) => {
        const { request } = await loadRequestLayer();
        return request.post(url, data, config);
      },
      put: async (url: string, data?: unknown, config?: Record<string, unknown>) => {
        const { request } = await loadRequestLayer();
        return request.put(url, data, config);
      },
      delete: async (url: string, config?: Record<string, unknown>) => {
        const { request } = await loadRequestLayer();
        return request.delete(url, config);
      },
    },
  );

  return {
    http,
    getPage: async (type: string) => {
      const { cubeApi } = await loadRequestLayer();
      const res = await cubeApi.page.getPage(type);
      return (res?.data ?? null) as CubePageMeta | null;
    },
    getDetail: async (type: string, id: number | string) => {
      const { cubeApi } = await loadRequestLayer();
      const res = await cubeApi.page.getDetail(type, id);
      return (res?.data ?? {}) as Record<string, unknown>;
    },
    exportFile: async (type: string, params: Record<string, unknown>) => {
      const { request } = await loadRequestLayer();
      return (await request.get(`${type}/export`, {
        params,
        responseType: 'blob',
      })) as unknown as Blob;
    },
    downloadTemplate: async (type: string) => {
      const { request } = await loadRequestLayer();
      return (await request.get(`${type}/DownloadTemplate`, {
        responseType: 'blob',
      })) as unknown as Blob;
    },
    importFile: async (type: string, file: File) => {
      const { cubeApi } = await loadRequestLayer();
      return (await cubeApi.page.importFile(type, file)) as unknown;
    },
    deleteSelect: async (type, keys) => {
      const { cubeApi } = await loadRequestLayer();
      await cubeApi.page.deleteSelect(type, keys);
    },
    notify: {
      success: (message: string) => ElMessage.success(message),
      warning: (message: string) => ElMessage.warning(message),
      error: (message: string) => ElMessage.error(message),
      info: (message: string) => ElMessage.info(message),
    },
  };
}

/** 安全取当前路由：脱离 router 上下文（组件单测、独立挂载）时返回 null 而不是抛错 */
function safeRoute(): RouteLocationNormalizedLoaded | null {
  try {
    return useRoute();
  } catch {
    return null;
  }
}

/**
 * 创建并提供页面引擎（页面根调用）。
 *
 * @param options 引擎入参；`routePath` / `apiPrefix` 都缺省时按当前路由推导
 */
export function useCubeEngine(options: CubeEngineOptions = {}): CubeEngineContext {
  const route = safeRoute();
  const resolved: CubeEngineOptions = {
    ...options,
    routePath: options.routePath ?? (options.apiPrefix ? undefined : route?.path ?? ''),
  };

  const ctx = createCubeEngine(resolved, createDefaultCubeEngineDeps());

  provide(CubeEngineKey, ctx);
  onMounted(() => ctx.init());

  // 同组件实例复用（菜单切到另一实体 / 详情返回换主键）时重初始化。
  // 显式给了 apiPrefix 或前端字段覆盖的引擎不跟随路由，否则会把调用方的配置冲掉。
  if (route && !resolved.apiPrefix && !resolved.fields) {
    watch(
      () => route.path,
      (path) => {
        if (path) void ctx.reload({ routePath: path });
      },
    );
  }

  return ctx;
}

/** 消费引擎上下文（子组件 / 插槽内调用） */
export function useCubeContext(): CubeEngineContext {
  const ctx = inject(CubeEngineKey, null);
  if (!ctx) {
    throw new Error('useCubeContext 必须在 CubeTable / useCubeEngine 之下使用（未找到 CubeEngineKey）');
  }
  return ctx;
}

/** 可选消费：找不到引擎时返回 null（用于「有引擎就增强、没有就退化」的组件） */
export function tryUseCubeContext(): CubeEngineContext | null {
  return inject(CubeEngineKey, null);
}
