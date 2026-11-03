import { describe, expect, it } from 'vitest';
import {
  getMenuPath,
  getRecentMenus,
  getSearchTokens,
  highlightSegments,
  isSearchableMenu,
  searchMenus,
} from '../menuSearch';
import type { FlatMenuItem } from '@newlifex/cube-vue/core/stores/menu';

const menu = (
  id: string,
  name: string,
  title?: string,
  path?: string,
  parentId?: string | null,
  visible?: boolean,
  pinYin?: string,
  initials?: string,
): FlatMenuItem =>
  ({ id, name, title, path, parentId, visible, pinYin, initials }) as FlatMenuItem;

const menus: FlatMenuItem[] = [
  menu('1', 'Admin', 'System management', '/Admin'),
  menu('2', 'User', 'Users', '/Admin/User', '1'),
  menu('3', 'Role', 'Roles', '/Admin/Role', '1'),
  menu('4', 'Hidden', 'Hidden page', '/Admin/Hidden', '1', false),
];

describe('menuSearch', () => {
  it('隐藏菜单参与搜索，无路径菜单不参与搜索', () => {
    expect(isSearchableMenu(menu('1', 'A', 'A', '/A'))).toBe(true);
    expect(isSearchableMenu(menu('1', 'A', 'A', '/A', null, false))).toBe(true);
    expect(isSearchableMenu(menu('1', 'A', 'A'))).toBe(false);

    const results = searchMenus(menus, 'hidden');
    expect(results.map((e) => e.menu.id)).toEqual(['4']);
  });

  it('隐藏父菜单下的子菜单仍参与搜索', () => {
    const list = [
      menu('1', 'HiddenGroup', 'Hidden group', '/HiddenGroup', null, false),
      menu('2', 'VisibleChild', 'Visible child', '/HiddenGroup/Child', '1'),
    ];

    expect(searchMenus(list, 'visible').map((e) => e.menu.id)).toEqual(['2']);
  });

  it('按显示名、菜单名、URL 匹配且忽略大小写', () => {
    expect(searchMenus(menus, 'users').map((e) => e.menu.id)).toEqual(['2']);
    expect(searchMenus(menus, 'role').map((e) => e.menu.id)).toEqual(['3']);
    expect(searchMenus(menus, 'admin/user').map((e) => e.menu.id)).toEqual(['2']);
  });

  it('空格分隔多关键字要求全部命中', () => {
    expect(searchMenus(menus, 'system users').map((e) => e.menu.id)).toEqual(['2']);
    expect(searchMenus(menus, 'system roles').map((e) => e.menu.id)).toEqual(['3']);
    expect(searchMenus(menus, 'system missing')).toEqual([]);
  });

  it('支持拼音全拼与首字母搜索', () => {
    const list = [
      menu('1', 'Admin', '系统管理', '/Admin', null, true, 'XiTongGuanLi', 'XTGL'),
      menu('2', 'User', '用户管理', '/Admin/User', '1', true, 'YongHuGuanLi', 'YHGL'),
    ];

    expect(searchMenus(list, 'yonghu').map((e) => e.menu.id)).toEqual(['2']);
    expect(searchMenus(list, 'yhgl').map((e) => e.menu.id)).toEqual(['2']);
    // 父级模块拼音参与匹配：xitong + yh
    expect(searchMenus(list, 'xitong yh').map((e) => e.menu.id)).toEqual(['2']);
    expect(searchMenus(list, 'xtgl').map((e) => e.menu.id)).toEqual(['1', '2']);
  });

  it('最近使用菜单优先于匹配质量', () => {
    const list = [
      menu('1', 'Admin', '系统管理', '/Admin'),
      menu('2', 'User', '用户管理', '/Admin/User', '1'),
      menu('3', 'Role', '角色管理', '/Admin/Role', '1'),
    ];

    const results = searchMenus(list, 'admin', 20, ['/Admin/Role']);
    expect(results.map((e) => e.menu.id)).toEqual(['3', '1', '2']);
  });

  it('按最近使用顺序返回菜单并跳过失效地址', () => {
    const list = [
      menu('1', 'Admin', '系统管理', '/Admin'),
      menu('2', 'User', '用户管理', '/Admin/User', '1'),
    ];

    const results = getRecentMenus(list, ['/Admin/User', '/missing', '/Admin', '/Admin/User']);
    expect(results.map((e) => e.menu.id)).toEqual(['2', '1']);
  });

  it('显示名前缀优先于路径包含', () => {
    const list = [
      menu('a', 'Report', 'Yearly report', '/Report/Yearly'),
      menu('b', 'Report', 'Report center', '/Report'),
    ];

    const results = searchMenus(list, 'report');
    expect(results.map((e) => e.menu.id)).toEqual(['b', 'a']);
  });

  it('返回层级路径', () => {
    expect(getMenuPath(menus, menus[1])).toBe('System management › Users');
    expect(searchMenus(menus, 'users')[0].path).toBe('System management › Users');
  });

  it('限制返回条数', () => {
    const list = Array.from({ length: 30 }, (_, i) =>
      menu(String(i), `Page${i}`, `Page ${i}`, `/Page/${i}`),
    );

    expect(searchMenus(list, 'page', 10)).toHaveLength(10);
  });

  it('切分关键字与高亮分段', () => {
    expect(getSearchTokens('  System   Users ')).toEqual(['system', 'users']);
    expect(highlightSegments('Users', 'us')).toEqual([
      { text: 'Us', match: true },
      { text: 'ers', match: false },
    ]);
    expect(highlightSegments('Users', 'zz')).toEqual([{ text: 'Users', match: false }]);
    expect(highlightSegments('', 'us')).toEqual([]);
  });
});
