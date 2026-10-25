import { describe, expect, it } from 'vitest';
import { filterVisibleMenuTree } from '../menuHelpers';
import type { TreeMenuItem } from '@newlifex/cube-vue/core/stores/menu';

const menu = (id: string, visible?: boolean, children?: TreeMenuItem[]): TreeMenuItem =>
  ({ id, name: id, path: `/${id}`, visible, children }) as TreeMenuItem;

describe('filterVisibleMenuTree', () => {
  it('隐藏菜单不进入导航，但仍保留在原始数据中（用于注册路由）', () => {
    const tree = [menu('Admin', true, [menu('User', true), menu('MailConfig', false)])];

    const nav = filterVisibleMenuTree(tree);

    expect(nav.map((m) => m.id)).toEqual(['Admin']);
    expect(nav[0].children?.map((m) => m.id)).toEqual(['User']);
    // 入参不被修改：隐藏菜单仍在，路由注册依赖它
    expect(tree[0].children?.map((m) => m.id)).toEqual(['User', 'MailConfig']);
  });

  it('visible 缺省视为可见', () => {
    const tree = [menu('Test', undefined), menu('Hidden', false)];

    expect(filterVisibleMenuTree(tree).map((m) => m.id)).toEqual(['Test']);
  });

  it('隐藏父菜单同时隐藏其子树', () => {
    const tree = [menu('Root', false, [menu('Child', true)])];

    expect(filterVisibleMenuTree(tree)).toEqual([]);
  });
});
