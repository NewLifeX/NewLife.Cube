// Svelte 皮肤用户 Store 单测：菜单路径权限解析与派生显示名
import { describe, expect, it, vi } from 'vitest';
import { get } from 'svelte/store';

const { fakeApi } = vi.hoisted(() => ({
  fakeApi: {
    tokenManager: { getToken: () => null, setToken: () => {}, clearToken: () => {} },
    user: {},
    menu: {},
  },
}));

vi.mock('$lib/api', () => ({ getApi: () => fakeApi }));

import { displayName, getMenuPermission, menus, user } from '$lib/stores/user';

describe('Svelte 用户 Store', () => {
  it('按路径解析菜单权限，未命中返回空对象', () => {
    menus.set([
      {
        id: 1,
        name: '系统',
        displayName: '系统',
        parentID: 0,
        url: '',
        visible: true,
        children: [
          { id: 2, name: '用户', displayName: '用户', parentID: 1, url: '/Admin/User', visible: true, permissions: { '2': '添加' }, children: [] },
        ],
      },
      { id: 3, name: '首页', displayName: '首页', parentID: 0, url: '/Admin/Index', visible: true, permissions: { '1': '查看' }, children: [] },
    ]);

    expect(getMenuPermission('/Admin/User')).toEqual({ '2': '添加' });
    expect(getMenuPermission('/Admin/Index')).toEqual({ '1': '查看' });
    expect(getMenuPermission('/Admin/Unknown')).toEqual({});
  });

  it('显示名优先取 DisplayName，缺省回退用户名', () => {
    user.set({ displayName: '大石头', name: 'stone' });
    expect(get(displayName)).toBe('大石头');

    user.set({ name: 'stone' });
    expect(get(displayName)).toBe('stone');

    user.set(null);
    expect(get(displayName)).toBe('');
  });
});
