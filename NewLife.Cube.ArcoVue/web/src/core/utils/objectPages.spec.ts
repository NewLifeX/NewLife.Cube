import { describe, expect, it } from 'vitest';
import type { MenuItem } from '@cube/api-core';
import { collectObjectCandidates } from './objectPages';

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

describe('objectPages', () => {
  it('只收集两层可见菜单，排除当前页并去重', () => {
    const tree = [
      menu({
        id: 1,
        name: 'Admin',
        children: [
          menu({ id: 2, name: 'Cube', displayName: '魔方设置', url: '/Admin/Cube' }),
          menu({ id: 3, name: 'User', displayName: '用户', url: '/Admin/User' }),
          menu({ id: 4, name: 'Sys', displayName: '系统设置', url: '/Admin/Sys', visible: false }),
          menu({ id: 5, name: 'Star', displayName: '星尘设置', url: '/Admin/Star' }),
        ],
      }),
      menu({ id: 6, name: 'Cube', url: '/Cube' }), // 一层，跳过
      menu({ id: 7, name: 'AppLog', url: '/Cube/AppLog/Detail' }), // 三层，跳过
    ];
    const pages = collectObjectCandidates(tree, '/Admin/Cube');
    expect(pages.map((p) => p.type)).toEqual(['/Admin/User', '/Admin/Star']);
    expect(pages[0].name).toBe('用户');
  });

  it('重复 url 去重（不同 id 同名菜单）', () => {
    const tree = [
      menu({ id: 11, name: 'Cube', url: '/vTest1/Cube' }),
      menu({ id: 22, name: 'Cube', url: '/vTest1/Cube' }),
    ];
    expect(collectObjectCandidates(tree)).toHaveLength(1);
  });
});
