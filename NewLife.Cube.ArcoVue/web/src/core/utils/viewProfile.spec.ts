import { describe, expect, it } from 'vitest';
import {
  applyChartData,
  buildFormJsonWire,
  buildSortPayload,
  CHART_OPTION_MAX_BYTES,
  clearFormModeLayout,
  clearSavedViewFilters,
  createNamedView,
  createTableView,
  emptyFormJson,
  emptyFormLayout,
  emptySavedFilters,
  emptySavedQueries,
  frozenLeftCount,
  getFormModeLayout,
  getSavedViewFilters,
  mergeColumns,
  normalizeFilter,
  normalizeGroup,
  normalizeInsight,
  normalizeSavedQuery,
  parseFormJson,
  parseQueriesWire,
  parseSavedFilters,
  rematchStateColumns,
  removeView,
  restoreNamedView,
  seedDefaultView,
  serializeFormJson,
  serializeNamedView,
  serializeQueriesWire,
  serializeSavedFilters,
  setFormModeLayout,
  setSavedViewFilters,
  stateFromWire,
  stateToWirePayload,
  stripChartData,
  type ColumnPref,
  type NamedView,
  type SavedQueriesWire,
} from './viewProfile';
import type { FieldMeta } from '@/core/types/field';

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

  it('seeds default view with allowDelete enabled (delete visible for permitted users)', () => {
    const v = seedDefaultView(['Name']);
    expect(v.chrome?.allowDelete).toBe(true);
  });

  it('normalizes legacy chrome allowDelete to true when unset', () => {
    // 旧数据 chrome 未含 allowDelete（或缺失）→ 归一化后默认允许删除
    const s = stateFromWire(
      {
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true }],
            chrome: { showPager: false },
          },
        ]),
        activeViewId: 'default',
      },
      ['Name'],
    );
    expect(s.views[0].chrome?.allowDelete).toBe(true);
  });

  it('keeps explicit allowDelete=false from user config', () => {
    const s = stateFromWire(
      {
        viewsJson: JSON.stringify([
          {
            id: 'default',
            name: '默认列表',
            view: 'table',
            columns: [{ key: 'Name', visible: true }],
            chrome: { allowDelete: false },
          },
        ]),
        activeViewId: 'default',
      },
      ['Name'],
    );
    expect(s.views[0].chrome?.allowDelete).toBe(false);
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

  it('createNamedView applies chromeOverride (allowDelete by permission)', () => {
    let s = stateFromWire(null, ['Name']);
    // 有删除权限：创建视图默认允许删除
    s = createNamedView(s, '可删', 'table', ['Name'], undefined, { allowDelete: true });
    expect(s.views.at(-1)?.chrome?.allowDelete).toBe(true);
    // 无删除权限：创建视图默认不允许删除
    s = createNamedView(s, '只读', 'card', ['Name'], undefined, { allowDelete: false });
    expect(s.views.at(-1)?.chrome?.allowDelete).toBe(false);
  });

  it('restoreNamedView resets view to creation-time defaults', () => {
    const fields: FieldMeta[] = [
      { name: 'Title', displayName: '标题', typeName: 'String', primaryKey: false } as FieldMeta,
      { name: 'Start', displayName: '开始', typeName: 'DateTime', primaryKey: false } as FieldMeta,
      { name: 'End', displayName: '结束', typeName: 'DateTime', primaryKey: false } as FieldMeta,
    ];
    let s = stateFromWire(null, ['Name', 'Code']);
    s = createNamedView(s, '甘特', 'gantt', ['Name', 'Code'], fields, { allowDelete: true });
    const gantt = s.views.at(-1)!;
    // 用户改乱了视图：列/排序/映射/筛选/洞察
    s = {
      ...s,
      views: s.views.map(
        (v): NamedView =>
          v.id === gantt.id
            ? ({
                ...v,
                columns: [{ key: 'Name', visible: false }] as ColumnPref[],
                sort: { field: 'Name', desc: true },
                mapping: {
                  kind: 'gantt',
                  titleField: 'Title',
                  plannedStartField: 'Start',
                  plannedEndField: 'End',
                  barColor: '#FF0000',
                },
                filter: { logic: 'and' as const, conditions: [] },
                insight: { showStat: true, showChart: false },
              } as unknown as NamedView)
            : v,
      ),
    };
    const restored = restoreNamedView(s, gantt.id, ['Name', 'Code'], fields);
    const rv = restored.views.find((v) => v.id === gantt.id)!;
    expect(rv.id).toBe(gantt.id);
    expect(rv.name).toBe('甘特');
    expect(rv.view).toBe('gantt');
    // 列重置为全量 meta
    expect(rv.columns.map((c) => c.key)).toEqual(['Name', 'Code']);
    expect(rv.sort).toBeNull();
    // mapping 重新 seed
    expect(rv.mapping?.kind).toBe('gantt');
    expect((rv.mapping as { barColor?: string }).barColor).toBeUndefined();
    // 删除权限保留，其余外观回默认
    expect(rv.chrome?.allowDelete).toBe(true);
    expect(rv.chrome?.showPager).toBe(true);
    // 筛选/分组/洞察重置
    expect(rv.filter).toBeUndefined();
    expect(rv.group).toBeUndefined();
    expect(rv.insight).toBeUndefined();
  });

  it('restoreNamedView throws for missing view', () => {
    const s = stateFromWire(null, ['Name']);
    expect(() => restoreNamedView(s, 'nope', ['Name'])).toThrow(/不存在/);
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

  it('P5 reads back chartOption object; non-object normalized away', () => {
    const opt = { xAxis: { type: 'category' }, series: [{ type: 'bar' }] };
    expect(normalizeInsight({ showChart: true, chartOption: opt })).toEqual({
      showStat: false,
      showChart: true,
      chartOption: opt,
    });
    // 非法：非对象 / 数组 / 字符串 → 归一化为缺省（不 round-trip）
    expect(normalizeInsight({ showChart: true, chartOption: 'str' })).toEqual({
      showStat: false,
      showChart: true,
    });
    expect(normalizeInsight({ showChart: true, chartOption: [1, 2] })).toEqual({
      showStat: false,
      showChart: true,
    });
    // mode=chart 草案同样读回 chartOption
    expect(normalizeInsight({ mode: 'chart', chartOption: opt })).toEqual({
      showStat: false,
      showChart: true,
      chartOption: opt,
    });
    // 无 chartOption 与今日一致（仅开关）
    expect(normalizeInsight({ showStat: true, showChart: true })).toEqual({
      showStat: true,
      showChart: true,
    });
  });
});

describe('P5 chartOption 清洗与填充（OSC-260819e483）', () => {
  it('stripChartData 剔除 dataset.source 与 series[].data，深拷贝不动原对象', () => {
    const opt = {
      dataset: { source: [[1, 2]], dimensions: ['a', 'b'] },
      series: [{ type: 'bar', data: [1, 2], name: 's' }],
      tooltip: { show: true },
    };
    const clean = stripChartData(opt) as Record<string, unknown>;
    // 原对象不被改动
    expect((opt.dataset as Record<string, unknown>).source).toEqual([[1, 2]]);
    // 清洗后数据键被剔除，非数据键保留
    expect(clean.dataset).toEqual({ dimensions: ['a', 'b'] });
    expect(clean.series).toEqual([{ type: 'bar', name: 's' }]);
    expect((clean.tooltip as Record<string, unknown>).show).toBe(true);
  });

  it('applyChartData 写入 dataset.source 为当前列表行；无 dataset 时补上', () => {
    const rows = [{ Name: 'a', Enable: true }, { Name: 'b', Enable: false }];
    const withDs = applyChartData({ dataset: { dimensions: ['Name'] }, series: [{ type: 'bar' }] }, rows);
    expect(withDs.dataset).toEqual({ dimensions: ['Name'], source: rows });
    const noDs = applyChartData({ series: [{ type: 'pie' }] }, rows);
    expect(noDs.dataset).toEqual({ source: rows });
    expect(noDs.series).toEqual([{ type: 'pie' }]);
  });

  it('serializeNamedView 保存前剔除数据；超 32KB 拒绝保存并抛错', () => {
    const created = createNamedView(stateFromWire(null, ['Name', 'Enable']), 'v', 'table', [
      'Name',
      'Enable',
    ]);
    const v = created.views[created.views.length - 1];
    v.insight = {
      showStat: false,
      showChart: true,
      chartOption: {
        dataset: { source: [[1, 2]], dimensions: ['a', 'b'] },
        series: [{ type: 'bar', data: [1, 2] }],
      },
    };
    const raw = serializeNamedView(v) as Record<string, unknown>;
    const saved = raw.insight as Record<string, unknown>;
    expect(saved.showChart).toBe(true);
    expect((saved.chartOption as Record<string, unknown>).dataset).toEqual({ dimensions: ['a', 'b'] });
    expect((saved.chartOption as Record<string, unknown>).series).toEqual([{ type: 'bar' }]);

    // 超 32KB：拒绝保存（data 会被剔除，故用非 data 大字段构造）
    const bigText = 'x'.repeat(CHART_OPTION_MAX_BYTES + 1);
    v.insight = { showStat: false, showChart: true, chartOption: { title: { text: bigText } } };
    expect(() => serializeNamedView(v)).toThrow(/32KB|32K|限制/);
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

describe('normalizeFilter / normalizeGroup (OSC-0015)', () => {
  it('normalizes missing/invalid filter to empty', () => {
    expect(normalizeFilter(undefined)).toEqual({ logic: 'all', conditions: [] });
    expect(normalizeFilter(null)).toEqual({ logic: 'all', conditions: [] });
    expect(normalizeFilter('x')).toEqual({ logic: 'all', conditions: [] });
    expect(normalizeFilter([])).toEqual({ logic: 'all', conditions: [] });
    expect(normalizeFilter({})).toEqual({ logic: 'all', conditions: [] });
  });

  it('normalizes logic and keeps valid conditions', () => {
    const f = normalizeFilter({
      logic: 'any',
      conditions: [
        { field: 'Status', op: 'eq', value: 1 },
        { field: 'Age', op: 'gte', value: 18 },
        { field: 'Name', op: 'contains', value: 'a' },
        { field: 'Enable', op: 'isNull' },
      ],
    });
    expect(f).toEqual({
      logic: 'any',
      conditions: [
        { field: 'Status', op: 'eq', value: 1 },
        { field: 'Age', op: 'gte', value: 18 },
        { field: 'Name', op: 'contains', value: 'a' },
        { field: 'Enable', op: 'isNull' },
      ],
    });
  });

  it('drops invalid logic/op/empty conditions but keeps false/0', () => {
    const f = normalizeFilter({
      logic: 'xor',
      conditions: [
        { field: '', op: 'eq', value: 1 },
        { field: 'B', op: 'between', value: 1, value2: 2 },
        { field: 'C', op: 'eq', value: undefined },
        { field: 'D', op: 'eq', value: false },
        { field: 'E', op: 'eq', value: 0 },
        { field: 'F', op: 'notNull' },
      ],
    });
    expect(f.logic).toBe('all');
    expect(f.conditions.map((c) => c.field)).toEqual(['D', 'E', 'F']);
  });

  it('round-trips filter via serializeNamedView', () => {
    const v = {
      id: 'v1',
      name: '视图',
      view: 'table' as const,
      columns: [],
      filter: normalizeFilter({
        logic: 'any',
        conditions: [{ field: 'Status', op: 'eq', value: 2 }],
      }),
    };
    const raw = serializeNamedView(v);
    expect(raw.filter).toEqual({
      logic: 'any',
      conditions: [{ field: 'Status', op: 'eq', value: 2 }],
    });
  });

  it('normalizeGroup dedupes, trims, caps at 3, drops non-strings', () => {
    expect(normalizeGroup(undefined)).toEqual([]);
    expect(normalizeGroup('x')).toEqual([]);
    expect(normalizeGroup(['Status', ' Status ', 'Status', 'Enable', 'Dept', 'Role', 1])).toEqual([
      'Status',
      'Enable',
      'Dept',
    ]);
  });
});

// ---------- OSC-0016：QueriesJson wire ----------
const qFields = [
  { name: 'Name', typeName: 'String' },
  { name: 'Status', typeName: 'Int32' },
] as FieldMeta[];

describe('QueriesJson wire (OSC-0016)', () => {
  it('parseQueriesWire normalizes null/bad JSON/non-object/version mismatch to empty', () => {
    expect(parseQueriesWire(null, qFields)).toEqual(emptySavedQueries());
    expect(parseQueriesWire('', qFields)).toEqual(emptySavedQueries());
    expect(parseQueriesWire('not json', qFields)).toEqual(emptySavedQueries());
    expect(parseQueriesWire('[]', qFields)).toEqual(emptySavedQueries());
    expect(parseQueriesWire(JSON.stringify({ version: 2, queries: [] }), qFields)).toEqual(
      emptySavedQueries(),
    );
  });

  it('parseQueriesWire drops invalid entries, truncates name, regenerates duplicate/bad id', () => {
    const raw = JSON.stringify({
      version: 1,
      queries: [
        { id: 'q_a', name: '昨日新增客户', params: { Name: '张三', Unknown: 'x' } },
        { id: 'q_b', name: '   ', params: { Name: 'a' } }, // 空 name 丢弃
        { id: 'q_b', name: '重名id', params: { Q: 'xx', dtStart: '2026-01-01' } }, // 重复 id 重新生成
        { id: 123, name: '坏id', params: { Status: 2 } }, // 非字符串 id 重新生成
        { name: '无id', params: { Name: 'b' } }, // 无 id 生成
        { id: 'q_empty', name: '空参数', params: {} }, // 空 params 丢弃
        { name: '超长' + '名'.repeat(100), params: { Name: 'c' } }, // name 截断 50
      ],
    });
    const wire = parseQueriesWire(raw, qFields);
    // 保留：q_a / 重名id / 坏id / 无id / 超长名 = 5 条；空 name 与空 params 丢弃
    expect(wire.queries.length).toBe(5);
    expect(wire.queries[0]).toEqual({ id: 'q_a', name: '昨日新增客户', params: { Name: '张三' } });
    // 未知字段 Unknown 被 cleanSearchParams 丢弃
    expect(Object.keys(wire.queries[0].params)).toEqual(['Name']);
    // 保留键 Q/dtStart 被接纳
    expect(wire.queries[1].params).toEqual({ Q: 'xx', dtStart: '2026-01-01' });
    expect(wire.queries[1].id).toMatch(/^q_/);
    // 重复 id 被重新生成，id 唯一
    const ids = wire.queries.map((q) => q.id);
    expect(new Set(ids).size).toBe(ids.length);
    expect(ids.every((id) => /^q_/.test(id))).toBe(true);
    // name 截断 50（超长名为最后一条）
    expect(wire.queries[4].name.length).toBe(50);
  });

  it('serializeQueriesWire round-trips and empty list keeps shell', () => {
    const wire: SavedQueriesWire = {
      version: 1,
      queries: [
        { id: 'q_x', name: '查询A', params: { Q: 'xx', Status: 1 } },
      ],
    };
    const json = serializeQueriesWire(wire);
    expect(parseQueriesWire(json, qFields)).toEqual(wire);
    expect(serializeQueriesWire(emptySavedQueries())).toBe('{"version":1,"queries":[]}');
  });

  it('normalizeSavedQuery regenerates id when usedIds collide', () => {
    const used = new Set(['q_same']);
    const a = normalizeSavedQuery({ id: 'q_same', name: 'A', params: { Name: '1' } }, qFields, used);
    const b = normalizeSavedQuery({ id: 'q_same', name: 'B', params: { Name: '2' } }, qFields, used);
    expect(a?.id).not.toBe(b?.id);
  });
});
