import type { ElegantConstRoute } from '@elegant-router/types';
import type { MenuItem } from '@newlifex/api-core';
import { getGlobalMenusByAuthRoutes } from '@/store/modules/route/shared';
import { resolveIconName } from '@/utils/icon';

/** 工作台固定路由（默认首页，始终排在菜单第一位） */
export const WORKBENCH_PATH = '/workbench';

/** 菜单图标为 Cube 的 fa-* 简名，转换为 iconify 名称；无法识别时回退默认图标 */
const ICON_MAP: Record<string, string> = {
  'fa-tachometer': 'fa6-solid:gauge-high',
  'fa-desktop': 'fa6-solid:desktop',
  'fa-table': 'fa6-solid:table',
  'fa-home': 'fa6-solid:house',
  'fa-database': 'fa6-solid:database',
  'fa-file': 'fa6-solid:file',
  'fa-file-text': 'fa6-solid:file-lines',
  'fa-wrench': 'fa6-solid:wrench',
  'fa-history': 'fa6-solid:clock-rotate-left',
  'fa-navicon': 'fa6-solid:bars',
  'fa-user': 'fa6-solid:user',
  'fa-users': 'fa6-solid:users',
  'fa-user-plus': 'fa6-solid:user-plus',
  'fa-user-secret': 'fa6-solid:user-secret',
  'fa-user-circle': 'fa6-solid:circle-user',
  'fa-star': 'fa6-solid:star',
  'fa-bomb': 'fa6-solid:bomb',
  'fa-clock-o': 'fa6-solid:clock',
  'fa-area-chart': 'fa6-solid:chart-area'
};

function toIcon(icon?: string) {
  if (!icon) return undefined;
  const name = icon.trim();
  if (!name) return undefined;
  if (name.includes(':')) return name;
  if (ICON_MAP[name]) return ICON_MAP[name];
  // Element Plus 图标名（如 Operation）转 iconify ep 名称
  return /^[A-Z]/.test(name) ? resolveIconName(name) : 'mdi:menu';
}

/** 菜单 URL 转换为可注册的路由路径；外部链接与不可用菜单返回空串 */
export function toRoutePath(menu: MenuItem): string {
  const url = (menu.url || '').trim();
  if (!url || url.startsWith('~') || url === '#' || /^(https?:)?\/\//i.test(url)) return '';
  const path = url.split('?')[0];
  return path.startsWith('/') ? path : `/${path}`;
}

/** 由路由路径生成唯一路由名，如 /Projects/Product → Projects_Product */
export function toRouteName(path: string) {
  return path
    .split('/')
    .filter(Boolean)
    .map(segment => segment.replace(/[^\w-]/g, ''))
    .join('_');
}

function toLeafRoute(menu: MenuItem, path: string, order: number, singleLevel: boolean): ElegantConstRoute {
  return {
    name: toRouteName(path),
    path,
    component: singleLevel ? 'layout.base$view.dynamic' : 'view.dynamic',
    props: { menuPath: path },
    meta: {
      title: menu.displayName || menu.name,
      icon: toIcon(menu.icon),
      order,
      hideInMenu: !menu.visible,
      cubePath: path
    } as ElegantConstRoute['meta']
  };
}

/** 把任意深度的菜单树压平为两级，中间层级不生成独立页面 */
function collectLeafMenus(menu: MenuItem): MenuItem[] {
  const children = menu.children || [];
  if (!children.length) return [menu];
  return children.flatMap(child => collectLeafMenus(child));
}

/**
 * 将魔方菜单树转换为 soybean 的 ElegantConstRoute 列表
 *
 * - 有子节点的菜单生成一级分组路由（layout.base），子菜单为扁平叶子路由
 * - 工作台作为固定菜单置顶
 */
export function createCubeRoutes(tree: MenuItem[]) {
  const workbenchRoute: ElegantConstRoute = {
    name: 'workbench',
    path: WORKBENCH_PATH,
    component: 'layout.base$view.dynamic',
    props: { menuPath: WORKBENCH_PATH, workbench: true },
    meta: {
      title: '工作台',
      i18nKey: 'route.workbench',
      icon: 'mdi:view-dashboard-outline',
      order: 0,
      cubePath: WORKBENCH_PATH
    } as ElegantConstRoute['meta']
  };

  const routes: ElegantConstRoute[] = [workbenchRoute];
  const usedPaths = new Set<string>([WORKBENCH_PATH]);

  tree.forEach((menu, index) => {
    const path = toRoutePath(menu);
    if (!path || usedPaths.has(path)) return;

    const leaves = collectLeafMenus(menu);

    if (leaves.length === 1 && leaves[0] === menu) {
      usedPaths.add(path);
      routes.push(toLeafRoute(menu, path, index + 1, true));
      return;
    }

    const children = leaves
      .map((leaf, leafIndex) => {
        const leafPath = toRoutePath(leaf);
        if (!leafPath || usedPaths.has(leafPath)) return null;
        usedPaths.add(leafPath);
        return toLeafRoute(leaf, leafPath, leafIndex + 1, false);
      })
      .filter(Boolean) as ElegantConstRoute[];

    if (!children.length) return;

    routes.push({
      name: toRouteName(path),
      path,
      component: 'layout.base',
      meta: {
        title: menu.displayName || menu.name,
        icon: toIcon(menu.icon),
        order: index + 1,
        hideInMenu: !menu.visible
      } as ElegantConstRoute['meta'],
      children
    });
  });

  return { routes, home: WORKBENCH_PATH };
}

/** 由路由构建侧边栏菜单 */
export function createCubeMenus(routes: ElegantConstRoute[]) {
  return getGlobalMenusByAuthRoutes(routes);
}

/** 收集所有可访问路径，用于守卫判断 403/404 */
export function collectRoutePaths(routes: ElegantConstRoute[]) {
  const paths = new Set<string>();
  routes.forEach(route => {
    paths.add(route.path);
    route.children?.forEach(child => paths.add(child.path));
  });
  return paths;
}
