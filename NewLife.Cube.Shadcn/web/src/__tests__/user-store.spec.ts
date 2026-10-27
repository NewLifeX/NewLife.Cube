// Shadcn 皮肤用户 Store 单测：菜单路径权限解析与登出清理
import { beforeEach, describe, expect, it, vi } from 'vitest';

const { fakeApi } = vi.hoisted(() => {
  let token: string | null = 'T0';
  return {
    fakeApi: {
      tokenManager: {
        getToken: () => token,
        setToken: (value: string) => {
          token = value;
        },
        clearToken: () => {
          token = null;
        },
      },
      user: { logout: async () => ({ code: 0 }) },
      menu: {},
    },
  };
});

vi.mock('@/api', () => ({ default: fakeApi, api: fakeApi }));

import { useUserStore } from '@/stores/user';
import type { MenuItem } from '@newlifex/api-core';

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


describe('Shadcn 用户 Store', () => {
  beforeEach(() => {
    useUserStore.setState({ userInfo: null, permissions: [], menus: [] });
  });

  it('按路径解析菜单权限，未命中返回空对象', () => {
    useUserStore.setState({ menus });

    expect(useUserStore.getState().getMenuPermission('/Admin/User')).toEqual({ '2': '添加', '4': '编辑' });
    expect(useUserStore.getState().getMenuPermission('/Admin/Index')).toEqual({ '1': '查看' });
    expect(useUserStore.getState().getMenuPermission('/Admin/Unknown')).toEqual({});
  });

  it('登出后清空用户资料、权限与菜单', async () => {
    useUserStore.setState({ userInfo: { id: 1, name: 'stone', displayName: 'stone' }, permissions: ['1'], menus });

    await useUserStore.getState().logout();

    const state = useUserStore.getState();
    expect(state.userInfo).toBeNull();
    expect(state.permissions).toEqual([]);
    expect(state.menus).toEqual([]);
    expect(fakeApi.tokenManager.getToken()).toBeNull();
  });
});
