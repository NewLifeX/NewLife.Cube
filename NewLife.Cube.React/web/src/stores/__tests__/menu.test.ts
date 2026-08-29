/**
 * 菜单扁平转换单元测试
 */
import { describe, expect, it } from 'vitest';
import { flattenMenu, resolveMenuTitle } from '@/stores/menu';
import type { MenuItem } from '@cube/api-core';

const tree: MenuItem[] = [
  {
    id: 1,
    name: 'system',
    displayName: '系统管理',
    parentID: 0,
    url: '/Admin',
    icon: 'fa-cog',
    visible: true,
    children: [
      {
        id: 2,
        name: 'user',
        displayName: '用户管理',
        parentID: 1,
        url: '/Admin/User',
        icon: 'fa-user',
        visible: true,
        children: [],
      },
      {
        id: 3,
        name: 'hidden',
        displayName: '隐藏菜单',
        parentID: 1,
        url: '/Admin/Hidden',
        visible: false,
        children: [],
      },
    ],
  },
];

describe('flattenMenu 树转扁平', () => {
  it('扁平化并保留父子关系', () => {
    const flat = flattenMenu(tree);
    expect(flat).toHaveLength(2); // 隐藏节点被过滤
    expect(flat[0].path).toBe('/Admin');
    expect(flat[1].parentId).toBe(1);
  });

  it('过滤不可见节点', () => {
    const flat = flattenMenu(tree);
    expect(flat.some((m) => m.path === '/Admin/Hidden')).toBe(false);
  });
});

describe('resolveMenuTitle 菜单标题解析', () => {
  const flat = flattenMenu(tree);

  it('命中返回菜单显示名', () => {
    expect(resolveMenuTitle(flat, '/Admin/User')).toBe('用户管理');
  });

  it('大小写不敏感匹配', () => {
    expect(resolveMenuTitle(flat, '/admin/user')).toBe('用户管理');
  });

  it('未命中返回 fallback', () => {
    expect(resolveMenuTitle(flat, '/No/Such', '默认页面')).toBe('默认页面');
    expect(resolveMenuTitle(flat, '/No/Such')).toBe('');
  });
});
