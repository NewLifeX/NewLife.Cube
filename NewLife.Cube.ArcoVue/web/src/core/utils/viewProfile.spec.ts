import { describe, expect, it } from 'vitest';
import {
  buildSortPayload,
  createTableView,
  frozenLeftCount,
  mergeColumns,
  rematchStateColumns,
  removeView,
  seedDefaultView,
  stateFromWire,
  stateToWirePayload,
} from './viewProfile';

describe('mergeColumns', () => {
  it('keeps pref order and appends new meta keys', () => {
    const m = mergeColumns(['A', 'B', 'C'], [
      { key: 'B', visible: false },
      { key: 'A', visible: true, width: 120 },
      { key: 'Z', visible: true },
    ]);
    expect(m.map((x) => x.key)).toEqual(['B', 'A', 'C']);
    expect(m.find((x) => x.key === 'B')?.visible).toBe(false);
    expect(m.find((x) => x.key === 'A')?.width).toBe(120);
  });

  it('rematch fills empty dirty columns from meta', () => {
    const s = rematchStateColumns(
      {
        activeViewId: 'default',
        view: 'table',
        views: [
          { id: 'default', name: '默认列表', view: 'table', columns: [], sort: null },
        ],
      },
      ['Name', 'Code'],
    );
    expect(s.views[0].columns.map((c) => c.key)).toEqual(['Name', 'Code']);
  });
});

describe('namedViews', () => {
  it('seeds 默认列表 default', () => {
    const v = seedDefaultView(['Name']);
    expect(v.id).toBe('default');
    expect(v.name).toBe('默认列表');
    expect(v.view).toBe('table');
  });

  it('uses workspace defaultView when seeding empty state', () => {
    const s = stateFromWire(null, ['Name'], { defaultView: 'card' });
    expect(s.view).toBe('card');
    expect(s.views[0].view).toBe('card');
  });

  it('migrates legacy 列表 name on default view', () => {
    const s = stateFromWire(
      {
        viewsJson: JSON.stringify([
          { id: 'default', name: '列表', view: 'table', columns: [{ key: 'Name', visible: true }] },
        ]),
        activeViewId: 'default',
      },
      ['Name'],
    );
    expect(s.views[0].name).toBe('默认列表');
  });

  it('create/remove table views', () => {
    let s = stateFromWire(null, ['Name']);
    s = createTableView(s, '精简', ['Name']);
    expect(s.views).toHaveLength(2);
    expect(s.views[1].view).toBe('table');
    s = removeView(s, s.views[1].id);
    expect(s.views).toHaveLength(1);
    expect(() => removeView(s, s.views[0].id)).toThrow(/至少保留/);
  });

  it('wire round-trip includes viewsJson', () => {
    const s = stateFromWire(
      {
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true, frozen: 'left', title: '姓名' }],
            sort: { field: 'Name', desc: true },
          },
        ]),
        activeViewId: 'default',
      },
      ['Name', 'Code'],
    );
    expect(s.views[0].columns[0].title).toBe('姓名');
    const payload = stateToWirePayload('Admin/User', s);
    expect(payload.typePath).toBe('Admin/User');
    expect(payload.activeViewId).toBe('default');
    expect(JSON.parse(payload.viewsJson!).length).toBe(1);
    expect(JSON.parse(payload.columnsJson!)[0].key).toBe('Name');
  });

  it('keeps saved named views and active view id from wire', () => {
    const s = stateFromWire(
      {
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
      ['Name'],
      { defaultView: 'table' },
    );
    expect(s.views).toHaveLength(2);
    expect(s.activeViewId).toBe('v-card');
    expect(s.view).toBe('card');
    expect(s.views[1].name).toBe('卡片视图');
  });
});

describe('buildSortPayload / frozenLeftCount', () => {
  it('sort shape', () => {
    expect(buildSortPayload({ field: 'Name', desc: true })).toEqual({
      sort: 'Name',
      desc: true,
    });
    expect(buildSortPayload(null)).toEqual({});
  });

  it('counts leading left-frozen visible cols', () => {
    expect(
      frozenLeftCount([
        { key: 'a', visible: true, frozen: 'left' },
        { key: 'b', visible: true, frozen: 'left' },
        { key: 'c', visible: true, frozen: false },
      ]),
    ).toBe(2);
  });
});
