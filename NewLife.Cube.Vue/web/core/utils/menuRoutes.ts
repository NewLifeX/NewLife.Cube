import type { Router } from 'vue-router';
import type { FlatMenuItem } from '@newlifex/cube-vue/core/stores/menu';
import { normalizeMenuUrl, toKebabCase, type RouteNamingStyle } from './url';
import { getConfig } from '../configure';

/** 视图组件异步加载器 */
type ComponentLoader = () => Promise<{ default: unknown; }>;

/**
 * 视图组件缓存
 * key: 视图目录路径（如 '/apps/iot/src/views/device/product'）
 * value: 懒加载的 index.vue 组件
 */
const viewComponentCache = new Map<string, ComponentLoader>();

/**
 * 预加载所有框架项目视图目录
 * 路径格式：
 *   /apps/&#42;/src/views/xxx/yyy/index.vue
 *   /@newlifex/cube-vue/core/apps/&#42;/src/views/xxx/yyy/index.vue
 *
 * Vite import.meta.glob 要求使用字面量，不能拼接变量，
 * 所以这里预先 glob 所有视图，再在运行时按需查找。
 */
const allViewModules = import.meta.glob([
  '/apps/*/src/views/**/index.vue',
  '/@newlifex/cube-vue/core/apps/*/src/views/**/index.vue',
], { eager: false });

/**
 * 归一化路径数组（统一小写、反斜杠转正斜杠）
 */
const normalizedModulePaths = Object.keys(allViewModules).map((p) =>
  p.replace(/\\/g, '/').toLowerCase(),
);

/** 归一化路径 → 原始路径 映射（用于从 allViewModules 取组件） */
const normalizedToOriginal = new Map<string, string>();
for (const originalKey of Object.keys(allViewModules)) {
  normalizedToOriginal.set(originalKey.replace(/\\/g, '/').toLowerCase(), originalKey);
}

/** 从模块路径中提取所有已知应用名（小写） */
function extractKnownAppNames(): string[] {
  const names = new Set<string>();
  for (const p of normalizedModulePaths) {
    // 匹配 /apps/{name}/ 或 /@newlifex\/cube-vue\/core\/apps/{name}/
    const m = p.match(/\/(?:apps|@newlifex\/cube-vue\/core\/apps)\/([^/]+)\//);
    if (m) names.add(m[1]);
  }
  return Array.from(names);
}
const knownAppNames = extractKnownAppNames();

/** 生成缓存 key */
function buildCacheKey(appName: string, viewSegments: string[]): string {
  return `/apps/${appName.toLowerCase()}/src/views/${viewSegments.map((s) => toKebabCase(s)).join('/')}`;
}

/** 从缓存取或解析并缓存 */
function fromCacheOrResolve(
  cacheKey: string,
  resolver: () => ComponentLoader,
): ComponentLoader {
  if (viewComponentCache.has(cacheKey)) return viewComponentCache.get(cacheKey)!;
  const loader = resolver();
  viewComponentCache.set(cacheKey, loader);
  return loader;
}

/**
 * 根据路由路径解析对应的视图组件。
 *
 * 约定规则：
 *   - 3段及以上：第一段作为应用名，其余映射到 views 文件夹
 *   - 1~2段：第一段未必是应用名，遍历所有已知应用匹配
 *   - 匹配优先级（同级内）：
 *     1. 短横线风格: /apps/iot/src/views/my-device/my-logs/index.vue
 *     2. 原始风格:   /apps/IoT/src/views/MyDevice/MyLogs/index.vue
 *   - 路径优先级：
 *     1. /apps/&#42;/src/views/** （框架项目）
 *     2. /@newlifex/cube-vue/core/apps/&#42;/src/views/** （核心应用）
 *
 * @param path 路由路径，如 /IoT/Device/Product 或 /ProcessCard/ProcessCard
 */
function resolvePageComponent(path: string): ComponentLoader {
  // 解析路径：去掉开头的 /，分割各层级
  const segments = path.replace(/^\/+/, '').split('/').filter(Boolean);
  if (segments.length === 0) return getFallbackComponent('form');

  if (segments.length >= 3) {
    // 3段以上：第一段是应用名，其余是视图路径
    const [appName, ...viewPathSegments] = segments;
    const cacheKey = buildCacheKey(appName, viewPathSegments);
    return fromCacheOrResolve(cacheKey, () => findMatchingView(appName, viewPathSegments));
  }

  // 1~2段：无应用名，遍历所有已知应用匹配
  const cacheKey = buildCacheKey('_', segments);
  return fromCacheOrResolve(cacheKey, () => resolveViewComponent(segments));
}

/**
 * 匹配结果
 */
interface MatchResult {
  loader: ComponentLoader;
  priority: number; // 1=框架项目短横线, 2=框架项目原始, 3=核心应用短横线, 4=核心应用原始
}

/**
 * 生成所有候选视图目录（多种段组合 × 多种命名风格）
 */
function buildViewDirCandidates(
  combinations: string[][],
): Set<string> {
  const candidates = new Set<string>();
  for (const segments of combinations) {
    if (segments.length === 0) continue;
    candidates.add(segments.join('/'));                            // 原始风格
    candidates.add(segments.map((s) => toKebabCase(s)).join('/')); // 短横线风格
    candidates.add(segments.map((s) => s.toLowerCase()).join('/'));// 全小写风格
  }
  return candidates;
}

/**
 * 扫描模块路径，返回匹配结果列表
 */
function scanViewModules(
  viewDirCandidates: Set<string>,
): MatchResult[] {
  const matches: MatchResult[] = [];

  for (const normalizedPath of normalizedModulePaths) {
    let isFrameworkProject = false;

    if (normalizedPath.includes('/apps/')) {
      isFrameworkProject = true;
    } else if (!normalizedPath.includes('/@newlifex/cube-vue/core/apps/')) {
      continue;
    }

    const viewsIndex = normalizedPath.indexOf('/views/');
    if (viewsIndex === -1) continue;

    const viewRelativeDir = normalizedPath
      .substring(viewsIndex + '/views/'.length)
      .replace('/index.vue', '');

    if (viewDirCandidates.has(viewRelativeDir)) {
      // 仅仅检查是否含有短横线来区分命名风格
      const isKebab = viewRelativeDir.includes('-');
      const priority = isFrameworkProject ? (isKebab ? 1 : 2) : (isKebab ? 3 : 4);
      const originalKey = normalizedToOriginal.get(normalizedPath);
      if (originalKey) {
        matches.push({
          loader: allViewModules[originalKey] as ComponentLoader,
          priority,
        });
      }
    }
  }

  return matches;
}

/**
 * 从匹配结果中选最优（有应用名）
 */
function pickBestMatch(
  matches: MatchResult[],
  appName: string,
  viewPathSegments: string[],
): ComponentLoader {
  if (matches.length > 0) {
    matches.sort((a, b) => a.priority - b.priority);
    const best = matches[0];
    console.log(`[ViewResolver] Matched: ${appName}/${viewPathSegments.join('/')}`);
    return best.loader;
  }

  console.log(`[ViewResolver] No match for ${appName}/${viewPathSegments.join('/')}, using fallback`);
  return getFallbackComponent('index');
}

/**
 * 从匹配结果中选最优（无应用名）
 */
function pickDirectMatch(
  matches: MatchResult[],
  segments: string[],
): ComponentLoader {
  if (matches.length > 0) {
    matches.sort((a, b) => a.priority - b.priority);
    const best = matches[0];
    console.log(`[ViewResolver] Matched: ${segments.join('/')}`);
    return best.loader;
  }

  console.log(`[ViewResolver] No match for ${segments.join('/')}, using fallback`);
  return getFallbackComponent('index');
}

/**
 * 在指定应用下匹配视图
 *
 * @param appName           应用名，如 ProcessCard
 * @param viewPathSegments  视图路径分段，如 ['ProcessCard']
 */
function findMatchingView(
  appName: string,
  viewPathSegments: string[],
): ComponentLoader {
  const candidates = buildViewDirCandidates([
    viewPathSegments,
    [appName, ...viewPathSegments],
  ]);
  const matches = scanViewModules(candidates);
  return pickBestMatch(matches, appName, viewPathSegments);
}

/**
 * 1~2段路径：无应用名，遍历所有已知应用，找最佳匹配
 */
function resolveViewComponent(
  segments: string[],
): ComponentLoader {
  // 先试 segments 本身作为视图目录
  const directCandidates = buildViewDirCandidates([segments]);
  let matches = scanViewModules(directCandidates);
  if (matches.length > 0) return pickDirectMatch(matches, segments);

  // 再试 {knownApp} + segments 组合
  const allCombinations: string[][] = knownAppNames.map(
    (app) => [app, ...segments],
  );
  const comboCandidates = buildViewDirCandidates(allCombinations);
  matches = scanViewModules(comboCandidates);
  return pickDirectMatch(matches, segments);
}

/**
 * 获取后备组件（无匹配视图时使用）
 */
function getFallbackComponent(type: 'index' | 'form'): ComponentLoader {
  return () => import(`@newlifex/cube-vue/core/views/${type}.vue`);
}

/**
 * 注册菜单路由的返回结果
 */
export interface RegisterMenuRoutesResult {
  /** 当前路径是否需要重新导航（路由刚注册且正在访问） */
  currentPathNeedsRefresh: boolean;
}

/**
 * 将菜单叶子节点批量注册为动态路由。
 *
 * 优先级说明：
 *   - 已存在路径（应用级预注册）不会被覆盖
 *   - 每个菜单叶子节点默认使用框架 index.vue / form.vue
 *
 * @param router        Vue Router 实例
 * @param menus         已拍平的菜单列表（来自 menuStore.flatMenus）
 * @param currentPath   可选：当前访问的路径，用于判断是否需要重新导航
 * @returns 注册结果，包含是否需要重新导航
 */
export function registerMenuRoutes(
  router: Router,
  menus: FlatMenuItem[],
  currentPath?: string,
): RegisterMenuRoutesResult {
  console.log('Registering menu routes...');

  // 读取路由命名风格配置
  const { router: { routeNamingStyle } } = getConfig();
  const toStyle: RouteNamingStyle = routeNamingStyle === 'kebab' ? 'kebab' : 'pascal';

  // 获取已注册路径，保护应用级预注册路由
  const existingPaths = new Set(router.getRoutes().map((r) => r.path));

  // 筛选叶子节点：没有其他 menu 以其 id 为 parentId 的即为叶子
  const parentIds = new Set(menus.map((m) => m.parentId).filter(Boolean));
  const leafMenus = menus.filter((m) => !parentIds.has(m.id) && m.path);

  // 需要重新导航的路径（刚添加的动态路由）
  const pendingNavigations: string[] = [];

  for (const menu of leafMenus) {
    // 根据配置风格转换路径
    const normalizedPath = normalizeMenuUrl(menu.path, toStyle);
    if (existingPaths.has(normalizedPath)) continue; // 应用级路由优先，跳过

    router.addRoute({
      path: normalizedPath,
      name: `menu-${menu.name || menu.id}`,
      component: resolvePageComponent(menu.path),
      meta: {
        auth: true,
        menuId: menu.id,
        title: menu.title ?? menu.name,
        originalPath: menu.path, // 保留原始路径用于调试
      },
    });

    // 记录刚添加的动态路由
    pendingNavigations.push(normalizedPath);
  }

  // 判断当前路径是否需要重新导航（使用相同的命名风格转换）
  const currentPathNeedsRefresh = !!(
    currentPath && pendingNavigations.includes(normalizeMenuUrl(currentPath, toStyle))
  );
  if (currentPathNeedsRefresh) {
    console.log(`Current path needs refresh: ${currentPath}`);
  }

  return { currentPathNeedsRefresh };
}
