/**
 * 路由优先级中心：外部（业务应用）路由优先于框架路由
 *
 * ## 为什么需要它
 * vue-router 4 的匹配逻辑是「matchers 数组中首个命中者胜出」（`matchers.find(m => m.re.test(path))`），
 * 而同形态（同 path 结构）的路由按注册先后插入，**先注册者排在前面**。框架静态路由在
 * `core/router/index.ts` 的 `createRouter` 阶段就已注册，业务应用的代码无论如何都晚于它，
 * 因此业务侧裸调 `router.addRoute` 无法覆盖框架路由 —— 实测 `resolve('/')` 会命中框架的 `home`，
 * 业务声明的首页重定向成为死路由。
 *
 * ## 语义（外部路由拥有最终裁决权）
 * - 外部路由登记进「声明表」，按 name 与归一化 path 双索引；
 * - 框架侧任何来源（静态表 / 微应用 / 后端菜单）注册前都先查表，命中则跳过并告警 —— 覆盖「框架晚于外部注册」；
 * - 外部注册时强制移除框架已登记的同 name / 同形态 path 项后再注册 —— 覆盖「框架早于外部注册」。
 *
 * 两侧配合后，外部声明的优先级**不依赖注册时序**，任何来源、任何路径都可被业务接管。
 */

import type { Router, RouteRecordRaw } from 'vue-router';
import type { ConfigRoute } from '../typings';

/** 路由来源标识 */
type RouteSource = 'framework' | 'external';

/** 已登记的路由记录（用于按 name / 归一化 path 做冲突清理） */
interface RegisteredRecord {
  /** 来源：框架侧 or 外部侧 */
  source: RouteSource;
  /** 归一化 path（动态段占位符化，用于同形态比较） */
  key: string;
  /** 路由 name（无名路由无法用 router.removeRoute 撤销，只能用退订函数） */
  name?: string;
  /** router.addRoute 返回的退订函数 */
  unregister?: () => void;
}

/** 外部路由声明表：归一化 path → 路由 */
const externalByPath = new Map<string, ConfigRoute>();

/** 外部路由声明表：name → 路由 */
const externalByName = new Map<string, ConfigRoute>();

/** 全部已登记记录（框架 + 外部），用于冲突清理与调试快照 */
let records: RegisteredRecord[] = [];

/** 当前绑定的 router 实例 */
let currentRouter: Router | null = null;

/**
 * 安全关键路径：被外部覆盖时给出告警（**只告警，不拦截**）
 *
 * 这三条不是普通页面，而是鉴权与启动链路的一部分：
 * - `/login`：`beforeEach` 的 `loginPageUrl` 判定与未登录重定向终点；
 * - `/loading`：微应用路由未初始化完成时的等待页；
 * - `/unauthorized`：无权限兜底页。
 */
const SAFETY_CRITICAL_KEYS = new Set(['/login', '/loading', '/unauthorized']);

/** 告警前缀 */
const LOG_PREFIX = '[路由优先级]';

/**
 * 归一化路由 path：把动态段统一成占位符，便于比较「同形态」路径
 *
 * 例：`/Nodes/Node/:id`、`/Nodes/Node/:name`、`/Nodes/Node/:id(\\d+)` 归一化后同形态，
 * catch-all `/:pathMatch(.*)*` 同样归一化为 `/:__param__`。
 *
 * 注意：**大小写敏感**（vue-router 默认大小写敏感），避免把 `/Trace` 与 `/trace`
 * 误判为同一路由而过度跳过框架路由。
 *
 * @param path - 原始路由路径
 * @returns 归一化后的路径
 */
export function normalizeRoutePath(path: string): string {
  // 先统一分隔符，再处理尾部斜杠（顺序反了会漏掉 `\` 结尾这种写法）
  const unified = path.replace(/\\/g, '/');
  const trimmed = unified.length > 1 ? unified.replace(/\/+$/, '') : unified;
  return trimmed.replace(/:[^/()]+(\([^)]*\))?(\*|\+|\?)?/g, ':__param__');
}

/**
 * 绑定 router 实例（由 `core/router/index.ts` 在 createRouter 之后立即调用）
 *
 * @param router - vue-router 实例
 */
export function bindRoutePriority(router: Router): void {
  currentRouter = router;
}

/**
 * 是否已由外部声明接管（框架侧注册前查询）
 *
 * 命中判定：同形态 path **或** 同 name（name 为字符串时）。
 *
 * @param route - 待注册的路由
 * @returns true 表示框架应让位
 */
export function isDeclaredExternal(route: {
  path: string;
  name?: unknown;
}): boolean {
  if (externalByPath.has(normalizeRoutePath(route.path))) return true;
  return typeof route.name === 'string' && externalByName.has(route.name);
}

/**
 * 声明外部路由（只登记，不注册）
 *
 * 登记后框架侧立即让位；真正落盘由 `applyExternalRoutes` / `addExternalRoute` 完成。
 *
 * 同形态 path 只保留最后一次声明（避免两条外部路由互相清理）。
 *
 * @param routes - 外部路由列表
 */
export function declareExternalRoutes(routes: ConfigRoute[]): void {
  for (const route of routes) {
    if (!route?.path) {
      console.warn(`${LOG_PREFIX} 忽略无 path 的外部路由声明:`, route);
      continue;
    }
    externalByPath.set(normalizeRoutePath(route.path), route);
    if (typeof route.name === 'string') {
      externalByName.set(route.name, route);
    }
  }
}

/**
 * 登记一条路由记录
 *
 * @param record - 记录
 */
function track(record: RegisteredRecord): void {
  records.push(record);
}

/**
 * 注销一条已登记记录（从记录表移除，并尽量撤销已注册的路由）
 *
 * - 记录表必须同步移除，否则长期运行会残留失效记录、调试快照失真；
 * - 有 name 的用 `router.removeRoute(name)`；无名路由只能用 `addRoute` 返回的退订函数。
 *
 * @param record - 记录
 * @param router - router 实例
 */
function untrack(record: RegisteredRecord, router: Router): void {
  const index = records.indexOf(record);
  if (index > -1) records.splice(index, 1);

  if (record.name && router.hasRoute(record.name)) {
    router.removeRoute(record.name);
  } else {
    record.unregister?.();
  }
}

/**
 * 清理与目标路由冲突的已登记项（同 name 或同形态 path）
 *
 * @param route - 目标外部路由
 * @param router - router 实例
 * @returns 被清理掉的记录（用于告警输出）
 */
function purgeConflicts(route: ConfigRoute, router: Router): RegisteredRecord[] {
  const key = normalizeRoutePath(route.path);
  const name = typeof route.name === 'string' ? route.name : undefined;
  const conflicts = records.filter(
    (r) => r.key === key || (!!name && r.name === name),
  );

  for (const record of conflicts) {
    untrack(record, router);
  }

  // 注意：removeRoute 会连带移除子路由与别名，其记录可能残留在本表中；
  // 残留项在下次清理时会因 hasRoute 为 false 而走退订函数兜底（removeRoute 对已移除项是 no-op），
  // 不会造成重复注册。
  return conflicts;
}

/**
 * 生成「撤销函数」：撤销已注册路由 + 同步撤销外部声明
 *
 * 只移除路由而保留声明会造成隐性陷阱——框架侧仍以为该路径已被业务接管，
 * 从而永久让位（表现为「框架页面莫名 404」）。故两者必须一起撤销。
 *
 * @param route - 外部路由
 * @param router - router 实例
 * @returns 撤销函数
 */
function createExternalUnregister(route: ConfigRoute, router: Router): () => void {
  return () => {
    const key = normalizeRoutePath(route.path);
    const declaration = externalByPath.get(key);
    // 仅当当前声明仍是本条路由时才撤销声明，避免撤销旧路由误伤新声明
    if (declaration === route) externalByPath.delete(key);

    if (typeof route.name === 'string' && externalByName.get(route.name) === route) {
      externalByName.delete(route.name);
    }

    if (typeof route.name === 'string' && router.hasRoute(route.name)) {
      router.removeRoute(route.name);
    } else {
      const record = records.find((r) => r.source === 'external' && r.key === key);
      if (record) untrack(record, router);
    }
  };
}

/**
 * 覆盖守门（方案 A：**只告警，不拦截**）
 *
 * 覆盖权完全归外部，但安全关键路径、catch-all、无名、空实现这四类问题在运行时
 * 表现为白屏/死路由，报错点离改动点很远，因此在此处留线索。
 *
 * @param route - 外部路由
 * @param replaced - 被其清掉的框架记录
 */
function guardExternalRoute(
  route: ConfigRoute,
  replaced: RegisteredRecord[],
): void {
  const label = `${route.path}${typeof route.name === 'string' ? ` (${route.name})` : ''}`;

  if (SAFETY_CRITICAL_KEYS.has(normalizeRoutePath(route.path))) {
    console.warn(
      `${LOG_PREFIX} 外部路由 ${label} 覆盖了框架安全关键路径：` +
        '请确认自带 meta.auth=false / meta.layout=false，否则登录、加载、鉴权重定向链路可能异常',
    );
  }
  if (route.path.includes('*') || route.path.includes('(.*)')) {
    console.warn(
      `${LOG_PREFIX} 外部路由 ${label} 覆盖了 catch-all 兜底路由：未匹配路径将不再落入框架默认页`,
    );
  }
  if (typeof route.name !== 'string') {
    console.warn(
      `${LOG_PREFIX} 外部路由 ${label} 未设置字符串 name：后续无法被撤销，同名覆盖判定也不生效`,
    );
  }
  if (!route.component && !route.redirect && !route.children) {
    console.warn(
      `${LOG_PREFIX} 外部路由 ${label} 既无 component 也无 redirect / children：命中后会白屏`,
    );
  }
  if (replaced.length) {
    console.info(
      `${LOG_PREFIX} 外部路由 ${label} 已接管框架路由：${replaced
        .map((r) => r.name ?? r.key)
        .join(', ')}`,
    );
  }
}

/**
 * 框架侧统一收口：注册一条框架路由
 *
 * **框架侧所有注册点（静态表 / 微应用 / 后端菜单）都必须走此函数**，
 * 否则外部声明无法阻止晚注册的框架路由抢占同形态路径。
 *
 * @param route - 框架路由
 * @param router - router 实例，默认取已绑定实例
 * @returns 撤销函数；因命中外部声明而让位时返回 null
 */
export function addFrameworkRoute(
  route: ConfigRoute,
  router: Router | null = currentRouter,
): (() => void) | null {
  if (!router) {
    console.error(`${LOG_PREFIX} 未绑定 router，框架路由注册失败:`, route.path);
    return null;
  }

  if (isDeclaredExternal(route)) {
    console.warn(
      `${LOG_PREFIX} 框架路由 ${route.path}${
        typeof route.name === 'string' ? ` (${route.name})` : ''
      } 让位于外部声明的同形态路由`,
    );
    return null;
  }

  const unregister = router.addRoute(route as RouteRecordRaw);
  const record: RegisteredRecord = {
    source: 'framework',
    key: normalizeRoutePath(route.path),
    name: typeof route.name === 'string' ? route.name : undefined,
    unregister,
  };
  track(record);
  // 返回带记录同步的撤销函数：仅调 router 的退订会让本表残留失效记录
  return () => untrack(record, router);
}

/**
 * 外部侧注册：注册一条外部路由并强制覆盖框架同 name / 同形态 path 的已登记项
 *
 * @param route - 外部路由
 * @param router - router 实例，默认取已绑定实例
 * @returns 撤销函数（同时撤销外部声明，避免「框架永久让位」的隐性陷阱）
 */
export function addExternalRoute(
  route: ConfigRoute,
  router: Router | null = currentRouter,
): (() => void) | null {
  if (!router) {
    console.error(`${LOG_PREFIX} 未绑定 router，外部路由注册失败:`, route.path);
    return null;
  }
  if (!route?.path) {
    console.error(`${LOG_PREFIX} 外部路由缺少 path，注册失败:`, route);
    return null;
  }

  // 先登记声明：此后框架侧任何来源注册同形态路径都会自动让位
  declareExternalRoutes([route]);
  const replaced = purgeConflicts(route, router);
  guardExternalRoute(route, replaced);

  const unregister = router.addRoute(route as RouteRecordRaw);
  track({
    source: 'external',
    key: normalizeRoutePath(route.path),
    name: typeof route.name === 'string' ? route.name : undefined,
    unregister,
  });
  return createExternalUnregister(route, router);
}

/**
 * 把所有已声明（或本次传入）的外部路由落盘，幂等
 *
 * 应用入口应尽早调用（`initApp` 内部会在 `app.use(router)` 之前自动调用），
 * 因为 `app.use(router)` 会触发首次导航解析 —— 晚于它则首个 location 仍按框架静态路由解析。
 *
 * @param routes - 可选，追加声明并应用
 * @param router - router 实例，默认取已绑定实例
 */
export function applyExternalRoutes(
  routes?: ConfigRoute[],
  router: Router | null = currentRouter,
): void {
  if (routes?.length) declareExternalRoutes(routes);
  for (const route of Array.from(externalByPath.values())) {
    addExternalRoute(route, router);
  }
}

/**
 * 调试快照：外部声明与已登记记录
 *
 * @returns 快照对象
 */
export function getRoutePrioritySnapshot(): {
  external: string[];
  records: Array<{ source: RouteSource; name?: string; key: string }>;
} {
  return {
    external: Array.from(externalByPath.values()).map((r) => r.path),
    records: records.map((r) => ({
      source: r.source,
      name: r.name,
      key: r.key,
    })),
  };
}

/**
 * 重置路由优先级中心（仅供测试使用）
 */
export function resetRoutePriority(): void {
  externalByPath.clear();
  externalByName.clear();
  records = [];
  currentRouter = null;
}
