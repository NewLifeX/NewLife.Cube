import type { Router } from 'vue-router';
import type { FlatMenuItem } from 'cube-front/core/stores/menu';
import { normalizeMenuUrl, toKebabCase } from './url';

/**
 * 视图组件缓存
 * key: 视图目录路径（如 '/apps/iot/src/views/device/product'）
 * value: 懒加载的 index.vue 组件
 */
const viewComponentCache = new Map<string, () => Promise<any>>();

/**
 * 预加载所有框架项目视图目录
 * 路径格式：
 *   /apps/&#42;/src/views/xxx/yyy/index.vue
 *   /cube-front/core/apps/&#42;/src/views/xxx/yyy/index.vue
 *
 * Vite import.meta.glob 要求使用字面量，不能拼接变量，
 * 所以这里预先 glob 所有视图，再在运行时按需查找。
 */
const allViewModules = import.meta.glob([
  '/apps/*/src/views/**/index.vue',
  '/cube-front/core/apps/*/src/views/**/index.vue',
], { eager: false });

/**
 * 归一化路径数组（统一小写、反斜杠转正斜杠）
 */
const normalizedModulePaths = Object.keys(allViewModules).map((p) =>
  p.replace(/\\/g, '/').toLowerCase(),
);

/**
 * 根据路由路径解析对应的视图组件。
 *
 * 约定规则：
 *   - 路径第一层作为应用名，后面的部分映射到 views 文件夹结构
 *   - 匹配优先级：
 *     1. 短横线风格: /apps/iot/src/views/my-device/my-logs/index.vue
 *     2. 原始风格:   /apps/IoT/src/views/MyDevice/MyLogs/index.vue
 *   - 路径优先级：
 *     1. /apps/&#42;/src/views/** （框架项目，如 IoT、EMS 等）
 *     2. /cube-front/core/apps/&#42;/src/views/** （cube-admin 等核心应用）
 *   - 逐级匹配：先匹配 apps/xxx，再匹配 src/views/xxx，逐级构建路径
 *
 * @param path 路由路径，如 /IoT/Device/Product
 */
function resolvePageComponent(path: string) {
  // 解析路径：去掉开头的 /，分割各层级
  const segments = path.replace(/^\/+/, '').split('/').filter(Boolean);
  if (segments.length === 0) {
    return getFallbackComponent('form');
  }

  const [appName, ...viewPathSegments] = segments;

  // 缓存 key 使用短横线风格的路径
  const cacheKey = `/apps/${appName.toLowerCase()}/src/views/${viewPathSegments
    .map((s) => toKebabCase(s))
    .join('/')}`;

  // 检查缓存
  if (viewComponentCache.has(cacheKey)) {
    return viewComponentCache.get(cacheKey)!;
  }

  // 尝试匹配目标视图
  const componentLoader = findMatchingView(appName, viewPathSegments);

  // 缓存并返回
  viewComponentCache.set(cacheKey, componentLoader);
  return componentLoader;
}

/**
 * 匹配结果
 */
interface MatchResult {
  loader: () => Promise<any>;
  priority: number; // 1=框架项目短横线, 2=框架项目原始, 3=核心应用短横线, 4=核心应用原始
}

/**
 * 逐级匹配视图路径
 *
 * @param appName           应用名，如 IoT
 * @param viewPathSegments  视图路径分段，如 ['Device', 'Product']
 */
function findMatchingView(
  appName: string,
  viewPathSegments: string[],
): () => Promise<any> {
  // 构建目标路径风格
  const kebabViewDir = viewPathSegments.map((s) => toKebabCase(s)).join('/');
  const originalViewDir = viewPathSegments.join('/');

  const matches: MatchResult[] = [];

  // 遍历所有归一化的模块路径，寻找匹配
  for (const normalizedPath of normalizedModulePaths) {
    let isFrameworkProject = false;

    // 检查路径类型
    if (normalizedPath.includes('/apps/')) {
      isFrameworkProject = true;
    } else if (!normalizedPath.includes('/cube-front/core/apps/')) {
      continue; // 不匹配任何已知路径格式
    }

    // 提取相对于 views/ 的路径
    const viewsIndex = normalizedPath.indexOf('/views/');
    if (viewsIndex === -1) continue;

    const viewRelativeDir = normalizedPath
      .substring(viewsIndex + '/views/'.length)
      .replace('/index.vue', '');

    // 检查是否匹配
    let matched = false;
    let priority = 0;

    if (viewRelativeDir === kebabViewDir) {
      matched = true;
      priority = isFrameworkProject ? 1 : 3;
    } else if (viewRelativeDir === originalViewDir) {
      matched = true;
      priority = isFrameworkProject ? 2 : 4;
    }

    if (matched) {
      matches.push({
        loader: allViewModules[normalizedPath] as () => Promise<any>,
        priority,
      });
    }
  }

  // 按优先级排序，返回最高优先级匹配
  if (matches.length > 0) {
    matches.sort((a, b) => a.priority - b.priority);
    const best = matches[0];
    console.log(`[ViewResolver] Matched (priority ${best.priority}): ${appName}/${viewPathSegments.join('/')}`);
    return best.loader;
  }

  // 无匹配，使用后备组件
  console.log(`[ViewResolver] No match for /${appName}/${viewPathSegments.join('/')}, using fallback`);
  return getFallbackComponent('index');
}

/**
 * 获取后备组件（无匹配视图时使用）
 */
function getFallbackComponent(type: 'index' | 'form') {
  return () => import(`cube-front/core/views/${type}.vue`);
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

  // 获取已注册路径，保护应用级预注册路由
  const existingPaths = new Set(router.getRoutes().map((r) => r.path));

  // 筛选叶子节点：没有其他 menu 以其 id 为 parentId 的即为叶子
  const parentIds = new Set(menus.map((m) => m.parentId).filter(Boolean));
  const leafMenus = menus.filter((m) => !parentIds.has(m.id) && m.path);

  // 需要重新导航的路径（刚添加的动态路由）
  const pendingNavigations: string[] = [];

  for (const menu of leafMenus) {
    // 转换路径为短横线风格
    const normalizedPath = normalizeMenuUrl(menu.path);
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

    // 记录刚添加的动态路由（用 normalizedPath，因为实际注册的路径已转换）
    pendingNavigations.push(normalizedPath);
  }

  // 判断当前路径是否需要重新导航（需要将 currentPath 也转为 normalizedPath 格式）
  const currentPathNeedsRefresh = !!(
    currentPath && pendingNavigations.includes(normalizeMenuUrl(currentPath))
  );
  if (currentPathNeedsRefresh) {
    console.log(`Current path needs refresh: ${currentPath}`);
  }

  return { currentPathNeedsRefresh };
}
