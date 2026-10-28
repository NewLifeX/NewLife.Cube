// NaiveUI 皮肤用户 Store 单测：菜单权限解析与用户显示名
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const { fakeApi } = vi.hoisted(() => ({
  fakeApi: {
    tokenManager: { getToken: () => null, setToken: () => {}, clearToken: () => {} },
    user: {},
    menu: {},
  },
}));

vi.mock('@/api', () => ({ default: fakeApi, api: fakeApi }));

import type { MenuItem } from '@newlifex/cube';
import { useUserStore } from '@/stores/user';

const menus: MenuItem[] = [
  {
    id: 1,
    name: '系统',
    displayName: '系统',
    parentID: 0,
    url: '',
    visible: true,
    children: [
      { id: 2, name: '用户', displayName: '用户', parentID: 1, url: '/Admin/User', visible: true, permissions: { '2': '添加', '4': '编辑' }, children: [] },
    ],
  },
  { id: 3, name: '首页', displayName: '首页', parentID: 0, url: '/Admin/Index', visible: true, permissions: { '1': '查看' }, children: [] },
];


describe('NaiveUI 用户 Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('按路径解析菜单权限，未命中返回空对象', () => {
    const store = useUserStore();
    store.menus = menus;

    expect(store.getMenuPermission('/Admin/User')).toEqual({ '2': '添加', '4': '编辑' });
    expect(store.getMenuPermission('/Admin/Unknown')).toEqual({});
  });

  it('显示名优先取 DisplayName，缺省回退用户名', () => {
    const store = useUserStore();

    store.userInfo = { id: 1, name: 'stone', displayName: '大石头' };
    expect(store.displayName).toBe('大石头');

    store.userInfo = { id: 1, name: 'stone' } as typeof store.userInfo;
    expect(store.displayName).toBe('stone');
  });
});
