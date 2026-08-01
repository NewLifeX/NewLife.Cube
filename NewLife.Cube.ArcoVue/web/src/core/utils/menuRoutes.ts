import type { Router, RouteRecordRaw } from 'vue-router';
import type { MenuItem } from '@cube/api-core';
import { normalizeMenuUrl, routeToApiPrefix, toKebabCase } from './url';
import { withRouteComponentName } from './namedRouteComponent';

export type ComponentLoader = () => Promise<{ default: unknown }>;

const appViewModules = import.meta.glob('../../apps/*/src/views/**/index.vue');

const normalizedToOriginal = new Map<string, string>();
for (const key of Object.keys(appViewModules)) {
  normalizedToOriginal.set(key.replace(/\\/g, '/').toLowerCase(), key);
}

function getFallbackDynamicPage(): ComponentLoader {
  return () => import('@/views/dynamic/DynamicPage.vue');
}

/** 解析 apps 整页覆写；匹配菜单 path 段（kebab / 小写 / 原始） */
export function resolvePageComponent(path: string): ComponentLoader {
  const segments = path.replace(/^\/+/, '').split('/').filter(Boolean);
  if (segments.length === 0) return getFallbackDynamicPage();

  const candidates = new Set<string>();
  const add = (segs: string[]) => {
    if (!segs.length) return;
    candidates.add(segs.join('/'));
    candidates.add(segs.map((s) => toKebabCase(s)).join('/'));
    candidates.add(segs.map((s) => s.toLowerCase()).join('/'));
  };

  add(segments);
  if (segments.length >= 2) {
    add(segments.slice(1));
    add([segments[0], ...segments.slice(1)]);
  }

  for (const [norm, original] of normalizedToOriginal) {
    const viewsIdx = norm.indexOf('/views/');
    if (viewsIdx < 0) continue;
    const rel = norm.substring(viewsIdx + '/views/'.length).replace(/\/index\.vue$/, '');
    if (candidates.has(rel)) {
      return appViewModules[original] as ComponentLoader;
    }
  }

  return getFallbackDynamicPage();
}

/** 展平菜单树 */
export function flattenMenus(menus: MenuItem[]): MenuItem[] {
  const out: MenuItem[] = [];
  const walk = (items: MenuItem[]) => {
    for (const m of items) {
      out.push(m);
      if (m.children?.length) walk(m.children);
    }
  };
  walk(menus);
  return out;
}

/**
 * B3：有 url 的菜单节点扁平注册到 Layout；
 * 不把 children 嵌套进 route.children；无 url 的纯文件夹不注册。
 */
export function buildLeafRoutes(menus: MenuItem[]): RouteRecordRaw[] {
  const flat = flattenMenus(menus);
  const seen = new Set<string>();
  const routes: RouteRecordRaw[] = [];

  for (const item of flat) {
    if (!item.url || item.visible === false) continue;
    const path = normalizeMenuUrl(item.url, 'pascal');
    if (seen.has(path.toLowerCase())) continue;
    seen.add(path.toLowerCase());

    const typePath = routeToApiPrefix(path);
    const routeName = 'menu-' + (item.name || item.id);
    routes.push({
      path: path.replace(/^\//, ''),
      name: routeName,
      component: withRouteComponentName(resolvePageComponent(item.url), routeName),
      props: { type: typePath, authId: item.id },
      meta: {
        title: item.displayName || item.name,
        icon: item.icon,
        hidden: false,
        menuId: item.id,
        typePath,
      },
    });
  }

  return routes;
}

export interface RegisterLeafRoutesResult {
  registered: string[];
  currentPathNeedsRefresh: boolean;
}

/** 将叶路由挂到名为 Layout 的父路由下 */
export function registerLeafRoutes(
  router: Router,
  menus: MenuItem[],
  currentPath?: string,
): RegisterLeafRoutesResult {
  const routes = buildLeafRoutes(menus);
  const existing = new Set(router.getRoutes().map((r) => r.path.toLowerCase()));
  const registered: string[] = [];

  for (const r of routes) {
    const full = '/' + String(r.path).replace(/^\//, '');
    if (existing.has(full.toLowerCase()) || existing.has(String(r.path).toLowerCase())) {
      continue;
    }
    router.addRoute('Layout', r);
    registered.push(full);
  }

  const normalizedCurrent = currentPath ? normalizeMenuUrl(currentPath, 'pascal') : '';
  const currentPathNeedsRefresh = !!(
    normalizedCurrent && registered.some((p) => p.toLowerCase() === normalizedCurrent.toLowerCase())
  );

  return { registered, currentPathNeedsRefresh };
}
