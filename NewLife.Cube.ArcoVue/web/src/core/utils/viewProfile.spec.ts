import { describe, expect, it } from 'vitest';
import {
  buildFormJsonWire,
  buildSortPayload,
  clearFormModeLayout,
  clearSavedViewFilters,
  createTableView,
  emptyFormJson,
  emptyFormLayout,
  emptySavedFilters,
  frozenLeftCount,
  getFormModeLayout,
  getSavedViewFilters,
  mergeColumns,
  normalizeInsight,
  parseFormJson,
  parseSavedFilters,
  rematchStateColumns,
  removeView,
  seedDefaultView,
  serializeFormJson,
  serializeNamedView,
  serializeSavedFilters,
  setFormModeLayout,
  setSavedViewFilters,
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

describe('normalizeInsight', () => {
  it('normalizes missing/invalid to both off', () => {
    expect(normalizeInsight(undefined)).toEqual({ showStat: false, showChart: false });
    expect(normalizeInsight(null)).toEqual({ showStat: false, showChart: false });
    expect(normalizeInsight('x')).toEqual({ showStat: false, showChart: false });
    expect(normalizeInsight({ showStat: 'yes' })).toEqual({ showStat: false, showChart: false });
  });

  it('keeps independent booleans', () => {
    expect(normalizeInsight({ showStat: true })).toEqual({ showStat: true, showChart: false });
    expect(normalizeInsight({ showStat: false, showChart: true })).toEqual({
      showStat: false,
      showChart: true,
    });
    expect(normalizeInsight({ showStat: true, showChart: true })).toEqual({
      showStat: true,
      showChart: true,
    });
  });

  it('migrates legacy mode draft', () => {
    expect(normalizeInsight({ mode: 'stat' })).toEqual({ showStat: true, showChart: false });
    expect(normalizeInsight({ mode: 'chart' })).toEqual({ showStat: false, showChart: true });
    expect(normalizeInsight({ mode: 'none' })).toEqual({ showStat: false, showChart: false });
    expect(normalizeInsight({ mode: 'x' })).toEqual({ showStat: false, showChart: false });
  });
});

describe('SavedFiltersWire', () => {
  it('parses valid wire and round-trips', () => {
    const wire = parseSavedFilters('{"version":1,"views":{"v1":{"Name":"a","Enable":false}}}');
    expect(wire.version).toBe(1);
    expect(wire.views.v1).toEqual({ Name: 'a', Enable: false });
    expect(JSON.parse(serializeSavedFilters(wire))).toEqual({
      version: 1,
      views: { v1: { Name: 'a', Enable: false } },
    });
  });

  it('normalizes missing/corrupt/unknown-version to empty', () => {
    expect(parseSavedFilters(undefined)).toEqual(emptySavedFilters());
    expect(parseSavedFilters('')).toEqual(emptySavedFilters());
    expect(parseSavedFilters('not-json')).toEqual(emptySavedFilters());
    expect(parseSavedFilters('[]')).toEqual(emptySavedFilters());
    expect(parseSavedFilters('{"version":2,"views":{}}')).toEqual(emptySavedFilters());
    expect(parseSavedFilters('{"version":1,"views":[]}')).toEqual(emptySavedFilters());
  });

  it('drops non-object view entries, keeps valid ones', () => {
    const wire = parseSavedFilters(
      '{"version":1,"views":{"v1":{"Name":"a"},"v2":"bad","v3":null,"v4":[1]}}',
    );
    expect(Object.keys(wire.views).sort()).toEqual(['v1']);
  });

  it('set/clear only affects target view', () => {
    let wire = emptySavedFilters();
    wire = setSavedViewFilters(wire, 'v1', { Name: 'a' });
    wire = setSavedViewFilters(wire, 'v2', { Enable: true });
    expect(getSavedViewFilters(wire, 'v1')).toEqual({ Name: 'a' });
    expect(getSavedViewFilters(wire, 'v2')).toEqual({ Enable: true });

    wire = setSavedViewFilters(wire, 'v1', { Age: 0 });
    expect(getSavedViewFilters(wire, 'v1')).toEqual({ Age: 0 });
    expect(getSavedViewFilters(wire, 'v2')).toEqual({ Enable: true });

    wire = clearSavedViewFilters(wire, 'v1');
    expect(getSavedViewFilters(wire, 'v1')).toBeUndefined();
    expect(getSavedViewFilters(wire, 'v2')).toEqual({ Enable: true });
  });
});

describe('named view round-trip keeps unknown fields', () => {
  it('serializeNamedView preserves unknown top-level keys and insight extensions', () => {
    const s = stateFromWire(
      {
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true }],
            future: { a: 1 },
            insight: { showStat: true, showChart: false, futureChart: 'x' },
          },
        ]),
        activeViewId: 'default',
      },
      ['Name'],
    );
    const raw = serializeNamedView(s.views[0]);
    expect(raw.future).toEqual({ a: 1 });
    expect((raw.insight as Record<string, unknown>).futureChart).toBe('x');
    expect((raw.insight as Record<string, unknown>).showStat).toBe(true);

    // stateToWirePayload 同样保留未知字段
    const payload = stateToWirePayload('Admin/User', s);
    const views = JSON.parse(payload.viewsJson!) as Record<string, unknown>[];
    expect(views[0].future).toEqual({ a: 1 });
    expect((views[0].insight as Record<string, unknown>).futureChart).toBe('x');
  });

  it('mode draft migrates on parse and serializes as booleans', () => {
    const s = stateFromWire(
      {
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true }],
            insight: { mode: 'chart' },
          },
        ]),
        activeViewId: 'default',
      },
      ['Name'],
    );
    expect(s.views[0].insight).toEqual({ showStat: false, showChart: true });
    const views = JSON.parse(
      stateToWirePayload('Admin/User', s).viewsJson!,
    ) as Record<string, unknown>[];
    expect((views[0].insight as Record<string, unknown>).mode).toBeUndefined();
    expect((views[0].insight as Record<string, unknown>).showChart).toBe(true);
  });
});

describe('FormJson (OSC-0013)', () => {
  it('parses valid wire with independent modes', () => {
    const wire = parseFormJson(
      '{"version":1,"add":{"order":["A"],"hidden":[],"collapsedCategories":[]},"edit":{"order":[],"hidden":["B"],"collapsedCategories":["扩展"]}}',
    );
    expect(wire.add?.order).toEqual(['A']);
    expect(wire.edit?.hidden).toEqual(['B']);
    expect(wire.detail).toBeUndefined();
  });

  it('normalizes corrupt/empty/unknown-version to empty wire', () => {
    expect(parseFormJson(undefined)).toEqual(emptyFormJson());
    expect(parseFormJson('')).toEqual(emptyFormJson());
    expect(parseFormJson('[]')).toEqual(emptyFormJson());
    expect(parseFormJson('{"version":2}')).toEqual(emptyFormJson());
    expect(parseFormJson('{"version":1,"add":"bad"}')).toEqual(emptyFormJson());
  });

  it('set/clear only affects target mode and round-trips', () => {
    let wire = emptyFormJson();
    wire = setFormModeLayout(wire, 'add', {
      order: ['A'],
      hidden: [],
      collapsedCategories: [],
    });
    wire = setFormModeLayout(wire, 'edit', {
      order: [],
      hidden: ['B'],
      collapsedCategories: [],
    });
    expect(getFormModeLayout(wire, 'add')?.order).toEqual(['A']);
    expect(getFormModeLayout(wire, 'edit')?.hidden).toEqual(['B']);
    expect(getFormModeLayout(wire, 'detail')).toBeNull();

    wire = clearFormModeLayout(wire, 'add');
    expect(getFormModeLayout(wire, 'add')).toBeNull();
    expect(getFormModeLayout(wire, 'edit')?.hidden).toEqual(['B']);

    expect(JSON.parse(serializeFormJson(wire))).toEqual({
      version: 1,
      edit: { order: [], hidden: ['B'], collapsedCategories: [] },
    });
  });

  it('buildFormJsonWire keeps only non-empty modes (OSC-0013 manual save)', () => {
    const wire = buildFormJsonWire({
      add: { order: ['A'], hidden: [], collapsedCategories: [] },
      edit: emptyFormLayout(),
      detail: { order: [], hidden: ['C'], collapsedCategories: ['扩展'] },
    });
    expect(wire.version).toBe(1);
    expect(wire.add?.order).toEqual(['A']);
    expect(wire.edit).toBeUndefined();
    expect(wire.detail?.hidden).toEqual(['C']);
    expect(wire.detail?.collapsedCategories).toEqual(['扩展']);
  });

  it('buildFormJsonWire returns empty wire when all modes empty', () => {
    const wire = buildFormJsonWire({
      add: emptyFormLayout(),
      edit: emptyFormLayout(),
      detail: emptyFormLayout(),
    });
    expect(wire).toEqual({ version: 1 });
  });
});
