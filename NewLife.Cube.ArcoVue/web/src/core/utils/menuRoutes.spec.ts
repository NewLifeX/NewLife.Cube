import { describe, expect, it } from 'vitest';
import type { MenuItem } from '@newlifex/api-core';
import { buildLeafRoutes, flattenMenus } from './menuRoutes';

function menu(partial: Partial<MenuItem> & Pick<MenuItem, 'id' | 'name'>): MenuItem {
  return {
    displayName: partial.displayName || partial.name,
    parentID: partial.parentID ?? 0,
    url: partial.url ?? '',
    visible: partial.visible ?? true,
    children: partial.children ?? [],
    permissions: partial.permissions,
    ...partial,
  };
}

describe('menuRoutes B3', () => {
  it('flattens tree', () => {
    const tree = [
      menu({
        id: 1,
        name: 'Admin',
        children: [menu({ id: 2, name: 'User', url: '/Admin/User' })],
      }),
    ];
    expect(flattenMenus(tree)).toHaveLength(2);
  });

  it('registers url nodes flat; skips folder without url', () => {
    const tree = [
      menu({
        id: 1,
        name: 'Admin',
        displayName: '系统',
        children: [
          menu({ id: 2, name: 'User', url: '/Admin/User', displayName: '用户' }),
          menu({ id: 3, name: 'Role', url: '/Admin/Role', displayName: '角色' }),
        ],
      }),
    ];
    const routes = buildLeafRoutes(tree);
    expect(routes.map((r) => r.path)).toEqual(['Admin/User', 'Admin/Role']);
    expect(routes.every((r) => !r.children?.length)).toBe(true);
    expect(routes[0].props).toMatchObject({ type: '/Admin/User', authId: 2 });
  });

  it('skips invisible', () => {
    const tree = [menu({ id: 1, name: 'X', url: '/Admin/X', visible: false })];
    expect(buildLeafRoutes(tree)).toHaveLength(0);
  });

  it('同名菜单（不同 Area）路由名唯一，避免 addRoute 覆盖（OSC-2608139feb）', () => {
    const tree = [
      menu({ id: 11, name: 'Cube', url: '/vTest1/Cube' }),
      menu({ id: 22, name: 'Cube', url: '/Admin/Cube' }),
    ];
    const routes = buildLeafRoutes(tree);
    const names = routes.map((r) => r.name);
    expect(names).toEqual(['menu-11', 'menu-22']);
    expect(new Set(names).size).toBe(2);
    expect(routes.map((r) => r.path)).toEqual(['vTest1/Cube', 'Admin/Cube']);
  });

  it('/Workbench/{slug} 分流到 WorkbenchPage（slug prop）；单段与普通菜单不误分流（OSC-260902ef43）', () => {
    const tree = [
      menu({ id: 41, name: 'Ops', url: '/Workbench/ops', displayName: '运营看板' }),
      menu({ id: 42, name: 'Only', url: '/Workbench', displayName: '工作台' }),
      menu({ id: 43, name: 'User', url: '/Admin/User', displayName: '用户' }),
    ];
    const routes = buildLeafRoutes(tree);
    const slugRoutes = routes.filter(
      (r) => r.props && 'slug' in (r.props as Record<string, unknown>),
    );
    expect(slugRoutes).toHaveLength(1);
    expect(slugRoutes[0].path).toBe('Workbench/ops');
    expect(slugRoutes[0].name).toBe('menu-41');
    expect(slugRoutes[0].props).toMatchObject({ slug: 'ops', authId: 41 });
    expect(routes.map((r) => r.path).sort()).toEqual(['Admin/User', 'Workbench', 'Workbench/ops']);
  });

  it('1 位合法 slug（/Workbench/a）同样分流（与 isValidNamedSlug 一致，OSC-260902ef43）', () => {
    const tree = [menu({ id: 51, name: 'A', url: '/Workbench/a', displayName: '单字符' })];
    const routes = buildLeafRoutes(tree);
    const slugRoutes = routes.filter(
      (r) => r.props && 'slug' in (r.props as Record<string, unknown>),
    );
    expect(slugRoutes).toHaveLength(1);
    expect(slugRoutes[0].props).toMatchObject({ slug: 'a' });
  });
});
