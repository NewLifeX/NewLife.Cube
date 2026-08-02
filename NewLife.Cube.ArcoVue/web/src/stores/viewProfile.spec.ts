import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const { getViewProfile, putViewProfile, deleteViewProfile, messageError } = vi.hoisted(() => ({
  getViewProfile: vi.fn(),
  putViewProfile: vi.fn(),
  deleteViewProfile: vi.fn(),
  messageError: vi.fn(),
}));

vi.mock('@/api', () => ({
  default: {
    profile: {
      getViewProfile,
      putViewProfile,
      deleteViewProfile,
    },
  },
}));

vi.mock('@arco-design/web-vue', () => ({
  Message: {
    error: messageError,
    warning: vi.fn(),
  },
}));

import { useViewProfileStore } from './viewProfile';

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