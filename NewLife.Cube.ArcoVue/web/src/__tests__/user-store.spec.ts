// ArcoVue 皮肤用户 Store 单测：基于共享 Pinia 适配器的菜单加载与权限解析
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const { fakeApi } = vi.hoisted(() => {
  const menus = [
    { name: '系统', children: [{ name: '用户', url: '/Admin/User', permissions: { '2': '添加' } }] },
  ];
  let token: string | null = null;
  return {
    fakeApi: {
      client: { defaults: { baseURL: '' } },
      tokenManager: {
        getToken: () => token,
        setToken: (value: string) => {
          token = value;
        },
        clearToken: () => {
          token = null;
        },
      },
      user: {
        info: async () => ({ code: 0, data: { name: 'stone', displayName: '大石头', permission: '1,2' } }),
        logout: async () => ({ code: 0 }),
      },
      menu: { getMenuTree: async () => ({ code: 0, data: menus }) },
    },
  };
});

vi.mock('@/api', () => ({ default: fakeApi, api: fakeApi }));

import { useUserStore } from '@/stores/user';

describe('ArcoVue 用户 Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('加载菜单后按路径解析权限，未命中返回空对象', async () => {
    const store = useUserStore();

    await store.fetchMenus();

    expect(store.menus.length).toBe(1);
    expect(store.getMenuPermission('/Admin/User')).toEqual({ '2': '添加' });
    expect(store.getMenuPermission('/Admin/Unknown')).toEqual({});
  });

  it('获取用户资料后解析权限列表与显示名', async () => {
    const store = useUserStore();

    await store.fetchUserInfo();

    expect(store.permissions).toEqual(['1', '2']);
    expect(store.displayName).toBe('大石头');
  });
});
