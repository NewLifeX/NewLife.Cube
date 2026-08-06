import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const {
  getViewProfile,
  putViewProfile,
  deleteViewProfile,
  getViewProfileTemplate,
  messageError,
  mockUserStore,
} = vi.hoisted(
  () => ({
    getViewProfile: vi.fn(),
    putViewProfile: vi.fn(),
    deleteViewProfile: vi.fn(),
    getViewProfileTemplate: vi.fn(),
    messageError: vi.fn(),
    mockUserStore: vi.fn(),
  }),
);

vi.mock('@/api', () => ({
  default: {
    profile: {
      getViewProfile,
      putViewProfile,
      deleteViewProfile,
      getViewProfileTemplate,
    },
  },
}));

vi.mock('@arco-design/web-vue', () => ({
  Message: {
    error: messageError,
    warning: vi.fn(),
  },
}));

// 表单布局为系统全局配置：saveNow 仅管理员提交 formJson，非管理员不发送
vi.mock('./user', () => ({
  useUserStore: () => mockUserStore(),
}));

import { useViewProfileStore } from './viewProfile';
import type { ViewFilter } from '@/core/utils/viewProfile';

// 默认模拟管理员身份（全局布局的唯一可写者），各 describe 的 beforeEach 会重置 API mock 但不重置身份
beforeEach(() => {
  mockUserStore.mockReset();
  mockUserStore.mockReturnValue({ userInfo: { isSystem: true } });
  // 默认无全局模板（视图/筛选域回落 system），保证既有测试语义不变
  getViewProfileTemplate.mockReset();
  getViewProfileTemplate.mockResolvedValue({ data: null });
});

describe('viewProfile store', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    getViewProfile.mockReset();
    putViewProfile.mockReset();
    deleteViewProfile.mockReset();
    messageError.mockReset();
  });

  it('reverts local columns when save fails', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/Department',
        view: 'table',
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true }],
          },
        ]),
        activeViewId: 'default',
        columnsJson: JSON.stringify([{ key: 'Name', visible: true }]),
      },
    });
    putViewProfile.mockRejectedValue(new Error('405'));

    const store = useViewProfileStore();
    await store.load('Admin/Department', ['Name', 'Code']);
    const committedColumns = store
      .getActive('Admin/Department')
      ?.columns.map((item) => ({ ...item }));

    store.updateColumns(
      'Admin/Department',
      [
        { key: 'Code', visible: true },
        { key: 'Name', visible: true },
      ],
      false,
    );

    expect(store.getActive('Admin/Department')?.columns.map((item) => item.key)).toEqual([
      'Code',
      'Name',
    ]);

    await store.saveNow('Admin/Department');

    expect(putViewProfile).toHaveBeenCalledOnce();
    expect(store.getActive('Admin/Department')?.columns).toEqual(committedColumns);
    expect(messageError).toHaveBeenCalledOnce();
  });

  it('restores saved named views and active view on reload', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/Department',
        view: 'card',
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true }],
          },
          {
            id: 'v-card',
            name: '卡片视图',
            view: 'card',
            columns: [{ key: 'Name', visible: true }],
          },
        ]),
        activeViewId: 'v-card',
      },
    });

    const store = useViewProfileStore();
    const state = await store.load('Admin/Department', ['Name'], undefined, {
      defaultView: 'table',
    });

    expect(state.views).toHaveLength(2);
    expect(state.activeViewId).toBe('v-card');
    expect(store.getActive('Admin/Department')?.view).toBe('card');
    expect(store.getActive('Admin/Department')?.name).toBe('卡片视图');
  });
});

describe('viewProfile store filters & pageSize (OSC-0012)', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    getViewProfile.mockReset();
    putViewProfile.mockReset();
    deleteViewProfile.mockReset();
    messageError.mockReset();
  });

  it('loads filtersJson and pageSize from wire', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        viewsJson: JSON.stringify([
          { id: 'default', name: '默认列表', view: 'table', columns: [{ key: 'Name', visible: true }] },
        ]),
        activeViewId: 'default',
        filtersJson: JSON.stringify({ version: 1, views: { default: { Name: 'a' } } }),
        pageSize: 100,
      },
    });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    expect(store.getViewFilters('Admin/User', 'default')).toEqual({ Name: 'a' });
    expect(store.getPageSize('Admin/User')).toBe(100);
  });

  it('saveViewFilters persists only target view with filtersJson', async () => {
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.saveViewFilters('Admin/User', 'default', { Name: 'a' }, true);
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({
        typePath: 'Admin/User',
        filtersJson: JSON.stringify({ version: 1, views: { default: { Name: 'a' } } }),
      }),
    );
  });

  it('clearViewFilters removes only target key', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        filtersJson: JSON.stringify({
          version: 1,
          views: { default: { Name: 'a' }, other: { Enable: true } },
        }),
      },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.clearViewFilters('Admin/User', 'default', true);
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({
        filtersJson: JSON.stringify({ version: 1, views: { other: { Enable: true } } }),
      }),
    );
  });

  it('setPageSize normalizes and persists; invalid becomes 0', async () => {
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.setPageSize('Admin/User', 50, true);
    expect(putViewProfile).toHaveBeenCalledWith(expect.objectContaining({ pageSize: 50 }));
    store.setPageSize('Admin/User', 30, true);
    expect(putViewProfile).toHaveBeenLastCalledWith(expect.objectContaining({ pageSize: 0 }));
  });

  it('reverts filters and pageSize when save fails', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        filtersJson: JSON.stringify({ version: 1, views: {} }),
        pageSize: 0,
      },
    });
    putViewProfile.mockRejectedValue(new Error('405'));
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.saveViewFilters('Admin/User', 'default', { Name: 'a' }, true);
    store.setPageSize('Admin/User', 100, true);
    await store.saveNow('Admin/User');
    expect(store.getViewFilters('Admin/User', 'default')).toBeUndefined();
    expect(store.getPageSize('Admin/User')).toBe(0);
    expect(messageError).toHaveBeenCalled();
  });
});

describe('viewProfile store formJson (OSC-0013)', () => {


  it('loads formJson from wire and exposes mode layouts', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        formJson: JSON.stringify({
          version: 1,
          edit: { order: ['Name'], hidden: [], collapsedCategories: [] },
        }),
      },
    });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    expect(store.getFormModeLayout('Admin/User', 'edit')).toEqual({
      order: ['Name'],
      hidden: [],
      collapsedCategories: [],
    });
    expect(store.getFormModeLayout('Admin/User', 'add')).toBeNull();
  });

  it('updateFormLayout persists only target mode', async () => {
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.updateFormLayout(
      'Admin/User',
      'edit',
      { order: ['Name'], hidden: [], collapsedCategories: [] },
      true,
    );
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({
        formJson: JSON.stringify({
          version: 1,
          edit: { order: ['Name'], hidden: [], collapsedCategories: [] },
        }),
      }),
    );
  });

  it('resetFormLayout removes only target mode key', async () => {
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.updateFormLayout(
      'Admin/User',
      'edit',
      { order: ['Name'], hidden: [], collapsedCategories: [] },
      true,
    );
    store.resetFormLayout('Admin/User', 'edit', true);
    expect(putViewProfile).toHaveBeenLastCalledWith(
      expect.objectContaining({
        formJson: JSON.stringify({ version: 1 }),
      }),
    );
  });

  it('reverts formJson when save fails', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        formJson: JSON.stringify({ version: 1 }),
      },
    });
    putViewProfile.mockRejectedValue(new Error('405'));
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.updateFormLayout(
      'Admin/User',
      'edit',
      { order: ['Name'], hidden: [], collapsedCategories: [] },
      true,
    );
    await store.saveNow('Admin/User');
    expect(store.getFormModeLayout('Admin/User', 'edit')).toBeNull();
    expect(messageError).toHaveBeenCalled();
  });

  it('setFormJson replaces whole wire once (manual save of three modes)', async () => {
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.setFormJson(
      'Admin/User',
      {
        version: 1,
        edit: { order: ['Name'], hidden: ['Remark'], collapsedCategories: [] },
      },
      true,
    );
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({
        formJson: JSON.stringify({
          version: 1,
          edit: {
            order: ['Name'],
            hidden: ['Remark'],
            collapsedCategories: [],
          },
        }),
      }),
    );
    expect(store.getFormModeLayout('Admin/User', 'edit')?.hidden).toEqual([
      'Remark',
    ]);
    expect(store.getFormModeLayout('Admin/User', 'add')).toBeNull();
  });

  it('non-admin saveNow omits formJson (global layout is admin-only)', async () => {
    mockUserStore.mockReturnValue({ userInfo: { isSystem: false } });
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    store.updateFormLayout(
      'Admin/User',
      'edit',
      { order: ['Name'], hidden: [], collapsedCategories: [] },
      true,
    );
    // 非管理员即使本地修改布局，也不得把 formJson 提交到服务端（全局配置仅管理员可写）
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.not.objectContaining({ formJson: expect.any(String) }),
    );
  });
});

describe('viewProfile store filter/group (OSC-0015)', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    getViewProfile.mockReset();
    putViewProfile.mockReset();
    deleteViewProfile.mockReset();
    messageError.mockReset();
  });

  it('updateFilter/getFilter round-trip 并持久化到 viewsJson', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        viewsJson: JSON.stringify([
          { id: 'default', name: '默认列表', view: 'table', columns: [{ key: 'Name', visible: true }] },
        ]),
      },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    const filter: ViewFilter = {
      logic: 'all',
      conditions: [
        { field: 'Name', op: 'eq', value: 'a' },
        { field: 'Enable', op: 'gte', value: 1 },
      ],
    };
    store.updateFilter('Admin/User', filter, true);
    expect(store.getFilter('Admin/User')).toEqual(filter);
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({
        typePath: 'Admin/User',
        viewsJson: expect.stringContaining('"filter"'),
      }),
    );
  });

  it('updateFilter 空方案等价清除（filter 字段被移除）', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true }],
            filter: { logic: 'all', conditions: [{ field: 'Name', op: 'eq', value: 'a' }] },
          },
        ]),
      },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    expect(store.getFilter('Admin/User').conditions).toHaveLength(1);
    store.updateFilter('Admin/User', { logic: 'all', conditions: [] }, true);
    expect(store.getFilter('Admin/User').conditions).toEqual([]);
  });

  it('updateGroup/getGroup round-trip 并持久化到 viewsJson', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        viewsJson: JSON.stringify([
          { id: 'default', name: '默认列表', view: 'table', columns: [{ key: 'Name', visible: true }] },
        ]),
      },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    const group = ['DepartmentId', 'RoleId'];
    store.updateGroup('Admin/User', group, true);
    expect(store.getGroup('Admin/User')).toEqual(group);
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({
        viewsJson: expect.stringContaining('"group"'),
      }),
    );
  });

  it('updateGroup 空数组清除分组', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true }],
            group: ['DepartmentId'],
          },
        ]),
      },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    expect(store.getGroup('Admin/User')).toEqual(['DepartmentId']);
    store.updateGroup('Admin/User', [], true);
    expect(store.getGroup('Admin/User')).toEqual([]);
  });

  it('getFilter/getGroup 未加载时返回安全空值', () => {
    const store = useViewProfileStore();
    expect(store.getFilter('Admin/None')).toEqual({ logic: 'all', conditions: [] });
    expect(store.getGroup('Admin/None')).toEqual([]);
  });
});

describe('viewProfile store template domains (OSC-0014)', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    getViewProfile.mockReset();
    putViewProfile.mockReset();
    deleteViewProfile.mockReset();
    messageError.mockReset();
  });
  const viewsTemplate = JSON.stringify([
    {
      id: 'default',
      name: '默认列表',
      view: 'table',
      columns: [{ key: 'Name', visible: true }, { key: 'Code', visible: true }],
    },
  ]);
  const filtersTemplate = JSON.stringify({ version: 1, views: { default: { Enable: true } } });

  beforeEach(() => {
    setActivePinia(createPinia());
    getViewProfile.mockReset();
    putViewProfile.mockReset();
    deleteViewProfile.mockReset();
    getViewProfileTemplate.mockReset();
    messageError.mockReset();
  });

  it('resolves views/filters from template when personal domain absent', async () => {
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    getViewProfileTemplate.mockResolvedValue({
      data: { typePath: 'Admin/User', viewsJson: viewsTemplate, filtersJson: filtersTemplate },
    });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name', 'Code']);
    expect(store.getViewsSource('Admin/User')).toBe('template');
    expect(store.getFiltersSource('Admin/User')).toBe('template');
    // 视图列来自模板
    expect(store.getActive('Admin/User')?.columns.map((c) => c.key)).toEqual(['Name', 'Code']);
    // 筛选来自模板
    expect(store.getViewFilters('Admin/User', 'default')).toEqual({ Enable: true });
  });

  it('first view save materializes personal copy and promotes source to personal', async () => {
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    getViewProfileTemplate.mockResolvedValue({
      data: { typePath: 'Admin/User', viewsJson: viewsTemplate, filtersJson: filtersTemplate },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name', 'Code']);
    store.updateColumns('Admin/User', [{ key: 'Code', visible: true }], false);
    await store.saveNow('Admin/User');
    expect(store.getViewsSource('Admin/User')).toBe('personal');
    // materialize：携带视图域（resolved 列 + 模板视图）
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({ viewsJson: expect.stringContaining('"Code"') }),
    );
  });

  it('template-only save without view change does not carry viewsJson', async () => {
    getViewProfile.mockResolvedValue({
      data: { typePath: 'Admin/User', view: 'table', activeViewId: 'default' },
    });
    getViewProfileTemplate.mockResolvedValue({
      data: { typePath: 'Admin/User', viewsJson: viewsTemplate, filtersJson: filtersTemplate },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name', 'Code']);
    // 仅改 PageSize（视图/筛选域不动）
    store.setPageSize('Admin/User', 50, true);
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.not.objectContaining({ viewsJson: expect.any(String) }),
    );
    expect(store.getViewsSource('Admin/User')).toBe('template');
  });

  it('restoreViewDomain falls back to template or system', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        viewsJson: JSON.stringify([
          { id: 'default', name: '默认列表', view: 'table', columns: [{ key: 'Code', visible: true }] },
        ]),
      },
    });
    getViewProfileTemplate.mockResolvedValue({
      data: { typePath: 'Admin/User', viewsJson: viewsTemplate },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name', 'Code']);
    expect(store.getViewsSource('Admin/User')).toBe('personal');
    await store.restoreViewDomain('Admin/User');
    // 删除个人视图域（空串），回落模板
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({ viewsJson: '' }),
    );
    expect(store.getViewsSource('Admin/User')).toBe('template');
    expect(store.getActive('Admin/User')?.columns.map((c) => c.key)).toEqual(['Name', 'Code']);
  });

  it('restoreFilterDomain falls back to template filters', async () => {
    getViewProfile.mockResolvedValue({
      data: {
        typePath: 'Admin/User',
        view: 'table',
        activeViewId: 'default',
        filtersJson: JSON.stringify({ version: 1, views: { default: { Name: 'a' } } }),
      },
    });
    getViewProfileTemplate.mockResolvedValue({
      data: { typePath: 'Admin/User', filtersJson: filtersTemplate },
    });
    putViewProfile.mockResolvedValue({ data: {} });
    const store = useViewProfileStore();
    await store.load('Admin/User', ['Name']);
    expect(store.getFiltersSource('Admin/User')).toBe('personal');
    await store.restoreFilterDomain('Admin/User');
    expect(putViewProfile).toHaveBeenCalledWith(
      expect.objectContaining({ filtersJson: '' }),
    );
    expect(store.getFiltersSource('Admin/User')).toBe('template');
    expect(store.getViewFilters('Admin/User', 'default')).toEqual({ Enable: true });
  });
});