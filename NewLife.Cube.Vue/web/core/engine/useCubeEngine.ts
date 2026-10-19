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
import request from '../utils/request';
import cubeApi from '../composables/useCubeApi';
import { createCubeEngine } from './createCubeEngine';
import type { CubeEngineContext, CubeEngineDeps, CubeEngineOptions, CubePageMeta } from './types';

/** 引擎上下文的注入键 */
export const CubeEngineKey: InjectionKey<CubeEngineContext> = Symbol('CubeEngine');

/**
 * 构造默认依赖：元数据 / 详情 / LOV 走 `cubeApi.page`（`@newlifex/api-core` 统一实现），
 * 文件流走 `request`（其返回值已是业务体 `ApiResponse`，blob 场景即响应体本身）。
 *
 * 注意两处解包口径（`docs/standards/api-contract.md`）：
 *   - `cubeApi.page.*` 返回 `ApiResponse`，元数据要取 `.data`；
 *   - `request` 已做薄解包，blob 直接就是返回值，不需要再 `.data`。
 */
export function createDefaultCubeEngineDeps(): CubeEngineDeps {
  return {
    http: request as unknown as CubeEngineDeps['http'],
    getPage: async (type: string) => {
      const res = await cubeApi.page.getPage(type);
      return (res?.data ?? null) as CubePageMeta | null;
    },
    getDetail: async (type: string, id: number | string) => {
      const res = await cubeApi.page.getDetail(type, id);
      return (res?.data ?? {}) as Record<string, unknown>;
    },
    exportFile: async (type: string, params: Record<string, unknown>) => {
      const blob = await request.get(`${type}/ExportFile`, {
        params,
        responseType: 'blob',
      });
      return blob as unknown as Blob;
    },
    downloadTemplate: async (type: string) => {
      const blob = await request.get(`${type}/DownloadTemplate`, { responseType: 'blob' });
      return blob as unknown as Blob;
    },
    importFile: async (type: string, file: File) =>
      (await cubeApi.page.importFile(type, file)) as unknown,
    deleteSelect: async (type, keys, options) => {
      await cubeApi.page.deleteSelect(type, keys, { compatCommaJoin: options?.commaJoin });
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
