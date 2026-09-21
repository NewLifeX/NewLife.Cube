import { describe, expect, it, vi } from 'vitest';
import { serializeDashboardJson } from '@newlifex/api-core';
import { isUnlinkedWidget, normalizeSourceRows, synthesizeLegacyDashboard } from './legacy';
import {
  fallbackKanbanDisplayFields,
  normalizeKanbanDisplayFields,
  resolveKanbanInteractive,
} from './useMiniKanbanWidget';
import { readChartItems, resolveChartDimKey } from './useMiniChartWidget';
import { normalizeQueryResult, shouldQueryWidget, unlinkedAfterQuery } from './useWidgetQuery';
import {
  buildMiniChartOption,
  isNumericTypeName,
  measureDisplayName,
  measureFnOptionsFor,
  resolveChartMeasures,
} from './chartTemplates';
import { getWidget, registerWidget } from './registry';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

describe('synthesizeLegacyDashboard', () => {
  it('does not synthesize when both switches off', () => {
    expect(
      synthesizeLegacyDashboard({ showStat: false, showChart: false }, { Total: 1 }, false, 'Admin/User'),
    ).toBeNull();
  });

  it('showStat with empty stat → one count card', () => {
    const d = synthesizeLegacyDashboard({ showStat: true, showChart: false }, null, false, 'Admin/User');
    expect(d?.widgets).toHaveLength(1);
    expect(d?.widgets[0].title).toBe('记录数');
    expect(d?.widgets[0].query.measure?.fn).toBe('count');
  });

  it('showChart with option → legacyChart', () => {
    const d = synthesizeLegacyDashboard(
      { showStat: false, showChart: true, chartOption: { series: [] } },
      null,
      false,
      'Admin/User',
    );
    expect(d?.widgets.some((w) => w.kind === 'legacyChart')).toBe(true);
  });
});

describe('normalizeSourceRows', () => {
  it('unwraps ApiResponse and PascalCase / Item1 rows', () => {
    const rows = normalizeSourceRows({
      code: 0,
      data: [
        { TypePath: '/Admin/User', DisplayName: '用户', Name: 'User' },
        { Item1: 'Admin/Role', Item2: '角色', Item3: 'Role' },
        ['Admin/Menu', '菜单', 'Menu'],
      ],
    });
    expect(rows).toHaveLength(3);
    expect(rows[0]).toMatchObject({ typePath: 'Admin/User', displayName: '用户', name: 'User' });
    expect(rows[1].typePath).toBe('Admin/Role');
    expect(rows[2].displayName).toBe('菜单');
  });

  it('accepts already-unwrapped array', () => {
    expect(normalizeSourceRows([{ typePath: 'Admin/User', displayName: '用户', name: 'User' }])).toHaveLength(1);
  });
});

describe('readChartItems', () => {
  it('reads camelCase and PascalCase items', () => {
    expect(
      readChartItems({
        Items: [{ Key: '男', Label: '男', Value: 3 }],
      }),
    ).toEqual([{ key: '男', label: '男', value: 3 }]);
    expect(readChartItems({ items: [{ key: 'a', label: 'A', value: 1 }] })).toHaveLength(1);
  });

  it('bar shows category axis but hides left value ticks', () => {
    const opt = buildMiniChartOption('bar', [
      { key: '0', label: '未知', value: 1 },
      { key: '1', label: '男', value: 4 },
    ]);
    const series = (opt.series as { data: number[]; label?: { show?: boolean } }[])[0];
    expect(series.data).toEqual([1, 4]);
    expect(series.label?.show).toBe(true);
    expect((opt.xAxis as { type?: string }).type).toBe('category');
    expect((opt.yAxis as { axisLabel?: { show?: boolean } }).axisLabel?.show).toBe(false);
  });

  it('pie has no legend', () => {
    const opt = buildMiniChartOption('pie', [{ key: 'a', label: 'A', value: 2 }]);
    expect((opt.legend as { show?: boolean }).show).toBe(false);
  });

  it('hbar is horizontal category axis', () => {
    const opt = buildMiniChartOption('hbar', [{ key: 'a', label: 'A', value: 2 }]);
    expect((opt.yAxis as { type?: string }).type).toBe('category');
    expect((opt.xAxis as { type?: string }).type).toBe('value');
  });
});

describe('buildMiniChartOption 多度量与显示选项（OSC-260920）', () => {
  const items = [
    { key: 'a', label: 'A', value: 2, values: [2, 21] },
    { key: 'b', label: 'B', value: 1, values: [1, 20] },
  ];
  const measures = [
    { fn: 'count' as const, label: '记录数' },
    { fn: 'sum' as const, field: 'Amount', label: '金额（求和）' },
  ];

  it('每个度量一条序列，数据按 values 对齐', () => {
    const opt = buildMiniChartOption('bar', items, undefined, { measures });
    const series = opt.series as { name?: string; data: number[]; label?: { show?: boolean } }[];
    expect(series).toHaveLength(2);
    expect(series[0].data).toEqual([2, 1]);
    expect(series[1].data).toEqual([21, 20]);
    expect(series[1].name).toBe('金额（求和）');
    expect(series[0].label?.show).toBe(true);
  });

  it('图例 / 数据标签 / 坐标轴 / 网格线开关生效', () => {
    const opt = buildMiniChartOption('bar', items, undefined, {
      measures,
      chartOptions: { legend: true, dataLabel: false, axis: false, gridLine: true },
    });
    expect((opt.legend as { show?: boolean }).show).toBe(true);
    const series = opt.series as { label?: { show?: boolean } }[];
    expect(series[0].label?.show).toBe(false);
    expect((opt.xAxis as { show?: boolean }).show).toBe(false);
    expect((opt.yAxis as { splitLine?: { show?: boolean } }).splitLine?.show).toBe(true);
  });

  it('缺省无 measures 时保持单序列旧行为', () => {
    const opt = buildMiniChartOption('bar', [{ key: 'a', label: 'A', value: 5 }]);
    const series = opt.series as { data: number[] }[];
    expect(series).toHaveLength(1);
    expect(series[0].data).toEqual([5]);
    expect((opt.legend as { show?: boolean }).show).toBe(false);
    expect((opt.yAxis as { splitLine?: { show?: boolean } }).splitLine?.show).toBe(false);
  });

  it('resolveChartMeasures 优先 measures，其次 measure，缺省 Count(*)', () => {
    expect(resolveChartMeasures({ measures })).toHaveLength(2);
    expect(resolveChartMeasures({ measure: { fn: 'avg', field: 'Amount' } })).toEqual([
      { fn: 'avg', field: 'Amount' },
    ]);
    expect(resolveChartMeasures(null)).toEqual([{ fn: 'count' }]);
  });

  it('measureDisplayName 优先 label，其次字段（函数）', () => {
    expect(measureDisplayName({ fn: 'sum', field: 'Amount', label: '金额（求和）' })).toBe('金额（求和）');
    expect(measureDisplayName({ fn: 'sum', field: 'Amount' })).toBe('Amount（求和）');
    expect(measureDisplayName({ fn: 'count' })).toBe('记录数');
  });

  it('readChartItems 读取 values / Values', () => {
    expect(
      readChartItems({ items: [{ key: 'a', label: 'A', value: 1, values: [1, 2] }] })[0].values,
    ).toEqual([1, 2]);
    expect(
      readChartItems({ Items: [{ Key: 'a', Label: 'A', Value: 1, Values: [3, 4] }] })[0].values,
    ).toEqual([3, 4]);
  });
});

describe('measureFnOptionsFor 按字段类型限定统计方式（OSC-260921）', () => {
  it('数值且非主键字段可用全部方式', () => {
    expect(measureFnOptionsFor({ typeName: 'Int32' })).toEqual(['count', 'sum', 'max', 'min', 'avg']);
    expect(measureFnOptionsFor({ typeName: 'Decimal' })).toContain('sum');
    expect(measureFnOptionsFor({ typeName: 'Double' })).toContain('avg');
  });

  it('文本 / 日期 / 时间 / 布尔 / Guid 字段仅计数', () => {
    expect(measureFnOptionsFor({ typeName: 'String' })).toEqual(['count']);
    expect(measureFnOptionsFor({ typeName: 'DateTime' })).toEqual(['count']);
    expect(measureFnOptionsFor({ typeName: 'TimeSpan' })).toEqual(['count']);
    expect(measureFnOptionsFor({ typeName: 'Boolean' })).toEqual(['count']);
    expect(measureFnOptionsFor({ typeName: 'Guid' })).toEqual(['count']);
  });

  it('编号（主键/身份列）即使为数值也仅计数', () => {
    expect(measureFnOptionsFor({ typeName: 'Int32', primaryKey: true })).toEqual(['count']);
    expect(measureFnOptionsFor({ typeName: 'Int64', primaryKey: true })).toEqual(['count']);
  });

  it('未选字段与空类型仅计数；isNumericTypeName 与后端类型集合对齐', () => {
    expect(measureFnOptionsFor(null)).toEqual(['count']);
    expect(measureFnOptionsFor({})).toEqual(['count']);
    expect(isNumericTypeName('Int16')).toBe(true);
    expect(isNumericTypeName('Byte')).toBe(true);
    expect(isNumericTypeName('Double')).toBe(true);
    expect(isNumericTypeName('String')).toBe(false);
    expect(isNumericTypeName(undefined)).toBe(false);
  });
});

describe('resolveChartDimKey', () => {
  const items = [
    { key: '1', label: '部门', value: 4 },
    { key: '2', label: '公司', value: 2 },
  ];

  it('uses dataIndex key', () => {
    expect(resolveChartDimKey({ dataIndex: 0 }, items)).toBe('1');
    expect(resolveChartDimKey({ dataIndex: 1 }, items)).toBe('2');
  });

  it('falls back to name match', () => {
    expect(resolveChartDimKey({ name: '公司' }, items)).toBe('2');
  });
});

describe('normalizeQueryResult', () => {
  it('unwraps ApiResponse envelope', () => {
    const r = normalizeQueryResult({
      code: 0,
      data: { value: null, items: [{ key: '1', label: '男', value: 3 }] },
    });
    expect(r?.items).toHaveLength(1);
    expect(r?.items?.[0].label).toBe('男');
  });

  it('reads Trend/trend and Url/url', () => {
    expect(normalizeQueryResult({ Trend: '注册用户', Value: '12', Url: '/Admin/User' })).toMatchObject({
      trend: '注册用户',
      value: '12',
      url: '/Admin/User',
    });
    expect(normalizeQueryResult({ trend: 'x', value: 1, url: '/a' })?.trend).toBe('x');
  });
});

describe('share expire helpers', () => {
  it('resolves presets and custom days to seconds', async () => {
    const { resolveExpireSeconds, SHARE_LONG_SECONDS } = await import(
      '@/views/crud/useShareViewPopover'
    );
    expect(resolveExpireSeconds('1h', 7)).toBe(3600);
    expect(resolveExpireSeconds('1d', 7)).toBe(86400);
    expect(resolveExpireSeconds('7d', 7)).toBe(604800);
    expect(resolveExpireSeconds('long', 7)).toBe(SHARE_LONG_SECONDS);
    expect(resolveExpireSeconds('custom', 3)).toBe(3 * 86400);
    expect(resolveExpireSeconds('custom', 0)).toBe(86400);
  });

  it('buildSharePageUrl 拼 embed 与 token，工作台无 viewId', async () => {
    const origin = 'http://localhost:5183';
    vi.stubGlobal('window', { location: { origin } });
    const { buildSharePageUrl } = await import('@/views/crud/useShareViewPopover');
    expect(buildSharePageUrl('Admin/User', 'default', 'tok')).toBe(
      `${origin}/Admin/User?viewId=default&embed=1&token=tok`,
    );
    expect(buildSharePageUrl('home', '', 'tok')).toBe(`${origin}/home?embed=1&token=tok`);
    expect(buildSharePageUrl('Workbench/ops', '', 'tok')).toBe(
      `${origin}/Workbench/ops?embed=1&token=tok`,
    );
  });

  it('normalizeShareData 能从 data 嵌套解出 token', async () => {
    const { normalizeShareData } = await import('@/views/crud/useShareViewPopover');
    expect(normalizeShareData({ token: 'abc', expire: 'x' })?.token).toBe('abc');
    expect(normalizeShareData({ data: { Token: 'xyz', Path: '/home' } })).toMatchObject({
      token: 'xyz',
      path: '/home',
    });
  });
});

describe('isUnlinkedWidget', () => {
  it('same entity is linked; cross-entity without mapping is unlinked', () => {
    const w = {
      id: 'a',
      kind: 'metricCard',
      title: 't',
      layout: { w: 3 as const, order: 0 },
      source: { provider: 'entity.aggregate' as const, typePath: 'Admin/Role' },
      query: {},
    };
    expect(isUnlinkedWidget(w, 'Admin/User')).toBe(true);
    expect(isUnlinkedWidget({ ...w, source: { ...w.source, typePath: 'Admin/User' } }, 'Admin/User')).toBe(false);
    expect(
      isUnlinkedWidget(
        { ...w, query: { linkFilter: [{ hostField: 'RoleId', sourceField: 'Id' }] } },
        'Admin/User',
      ),
    ).toBe(false);
  });

  it('cross-entity declaring $host extraFilter counts as linked (OSC-260903e2a4)', () => {
    const w = {
      id: 'a',
      kind: 'dataList',
      title: 't',
      layout: { w: 6 as const, order: 0 },
      source: { provider: 'entity.list' as const, typePath: 'Admin/User' },
      query: {
        extraFilter: {
          logic: 'all' as const,
          conditions: [{ field: 'RoleId', op: 'eq', value: { $host: 'RoleId' } }],
        },
      },
    };
    expect(isUnlinkedWidget(w, 'Admin/Role')).toBe(false);
  });
});

describe('unlinkedAfterQuery (OSC-260903e2a4)', () => {
  const w = (cross: boolean) => ({
    id: 'a',
    kind: 'metricCard',
    title: 't',
    layout: { w: 3 as const, order: 0 },
    source: { provider: 'entity.aggregate' as const, typePath: cross ? 'Admin/Role' : 'Admin/User' },
    query: {},
  });

  it('same source never unlinked regardless hostFilterApplied', () => {
    expect(unlinkedAfterQuery(w(false), 'Admin/User', false, true)).toBe(false);
    expect(unlinkedAfterQuery(w(false), 'Admin/User', false, false)).toBe(false);
  });

  it('cross undeclared stays unlinked', () => {
    expect(unlinkedAfterQuery(w(true), 'Admin/User', true, true)).toBe(true);
    expect(unlinkedAfterQuery(w(true), 'Admin/User', true, false)).toBe(true);
  });

  it('cross declared-link (linkFilter/$host) follows hostFilterApplied', () => {
    const linked = {
      ...w(true),
      query: { extraFilter: { logic: 'all' as const, conditions: [{ field: 'Id', op: 'eq', value: { $host: 'Id' } }] } },
    };
    expect(unlinkedAfterQuery(linked, 'Admin/User', false, true)).toBe(false);
    expect(unlinkedAfterQuery(linked, 'Admin/User', false, false)).toBe(true);
  });
});

describe('normalizeKanbanDisplayFields', () => {
  it('dedupes, excludes title/group, caps at 8', () => {
    expect(
      normalizeKanbanDisplayFields(
        ['Name', 'name', 'Code', 'Enable', 'Title', 'A', 'B', 'C', 'D', 'E', 'F'],
        { groupField: 'Enable', titleField: 'Title' },
      ),
    ).toEqual(['Name', 'Code', 'A', 'B', 'C', 'D', 'E', 'F']);
  });

  it('empty mapping falls back to list data columns minus title/group', () => {
    expect(
      fallbackKanbanDisplayFields(
        [
          { name: 'Name', displayName: '名称', typeName: 'String' },
          { name: 'RoleID', displayName: '角色', typeName: 'Int32' },
          { name: 'Code', displayName: '编码', typeName: 'String' },
          { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
        ],
        { groupField: 'RoleID', titleField: 'Name' },
      ),
    ).toEqual(['Code', 'Enable']);
  });
});

describe('compact kanban', () => {
  it('compact disables edit/delete/detail', () => {
    expect(resolveKanbanInteractive(true)).toEqual({
      canEdit: false,
      canDelete: false,
      canViewDetail: false,
      enableTableDoubleClick: false,
    });
    expect(resolveKanbanInteractive(false).canEdit).toBe(true);
  });
});

describe('serialize order', () => {
  it('rewrites order 0..n-1', () => {
    const json = serializeDashboardJson({
      version: 1,
      widgets: [
        {
          id: 'b',
          kind: 'metricCard',
          title: 'B',
          layout: { w: 3, order: 9 },
          source: { provider: 'entity.aggregate', typePath: 'Admin/User' },
          query: {},
        },
        {
          id: 'a',
          kind: 'metricCard',
          title: 'A',
          layout: { w: 4, order: 1 },
          source: { provider: 'entity.aggregate', typePath: 'Admin/User' },
          query: {},
        },
      ],
    });
    const parsed = JSON.parse(json) as { widgets: { id: string; layout: { order: number } }[] };
    expect(parsed.widgets.map((w) => w.id)).toEqual(['a', 'b']);
    expect(parsed.widgets.map((w) => w.layout.order)).toEqual([0, 1]);
  });
});

describe('unknown kind skips Query', () => {
  it('unregistered kind shouldQueryWidget false', () => {
    expect(getWidget('not-installed')).toBeUndefined();
    expect(
      shouldQueryWidget({
        id: 'x',
        kind: 'not-installed',
        title: 'x',
        layout: { w: 3, order: 0 },
        source: { provider: 'entity.aggregate', typePath: 'Admin/User' },
        query: {},
      }),
    ).toBe(false);
  });

  it('registered metricCard should query', () => {
    registerWidget({
      kind: 'metricCard',
      title: '指标卡',
      providers: ['entity.aggregate'],
      defaultW: 3,
      component: {},
    });
    expect(
      shouldQueryWidget({
        id: 'x',
        kind: 'metricCard',
        title: 'x',
        layout: { w: 3, order: 0 },
        source: { provider: 'entity.aggregate', typePath: 'Admin/User' },
        query: {},
      }),
    ).toBe(true);
  });

  it('named provider with widgetName should query Data', () => {
    registerWidget({
      kind: 'metricCard',
      title: '指标卡',
      providers: ['named'],
      defaultW: 3,
      component: {},
    });
    expect(
      shouldQueryWidget({
        id: 'n',
        kind: 'metricCard',
        title: 'n',
        layout: { w: 3, order: 0 },
        source: { provider: 'named', widgetName: 'DemoNamed' },
        query: {},
      }),
    ).toBe(true);
    expect(
      shouldQueryWidget({
        id: 'n2',
        kind: 'metricCard',
        title: 'n2',
        layout: { w: 3, order: 0 },
        source: { provider: 'named' },
        query: {},
      }),
    ).toBe(false);
  });
});

describe('Host isolation', () => {
  it('useWidgetHost does not import useListQuery', () => {
    const p = resolve(fileURLToPath(import.meta.url), '..', 'useWidgetHost.ts');
    const src = readFileSync(p, 'utf8');
    expect(src).not.toMatch(/useListQuery/);
  });
});

describe('workbench grid', () => {
  it('spanOf accepts 2 and 8; narrow is 12', async () => {
    const { spanOf, fallbackHeightOf, minHeightOf } = await import('./useWidgetGrid');
    expect(spanOf(2, false)).toBe(2);
    expect(spanOf(8, false)).toBe(8);
    expect(spanOf(5, false)).toBe(3);
    expect(spanOf(2, true)).toBe(12);
    expect(fallbackHeightOf('monitorChart')).toBe(3);
    expect(fallbackHeightOf('quickLinks')).toBe(3);
    expect(fallbackHeightOf('dataList')).toBe(4);
    expect(fallbackHeightOf('miniKanban')).toBe(3);
    expect(fallbackHeightOf('dataCard')).toBe(3);
    expect(minHeightOf(4)).toBe(260);
    expect(minHeightOf(3)).toBe(180);
  });
});

describe('添加部件入口（OSC-260921）', () => {
  const dir = resolve(fileURLToPath(import.meta.url), '..');
  const hostSrc = () => readFileSync(resolve(dir, 'WidgetHost.vue'), 'utf8');
  const workbenchSrc = () => readFileSync(resolve(dir, '../../views/home/Workbench.vue'), 'utf8');
  const useWorkbenchSrc = () => readFileSync(resolve(dir, '../../views/home/useWorkbench.ts'), 'utf8');

  it('顶部菜单「添加部件…」排在「发布…」之前', () => {
    const src = workbenchSrc();
    const add = src.indexOf('添加部件…');
    const publish = src.indexOf('发布…');
    expect(add).toBeGreaterThan(-1);
    expect(publish).toBeGreaterThan(-1);
    expect(add).toBeLessThan(publish);
    expect(src).toMatch(/value="__addWidget"/);
    expect(src).toMatch(/openAdd\(\)/);
  });

  it('部件行内「+」仅按 showInlineAdd 条件渲染', () => {
    const src = hostSrc();
    expect(src).toMatch(/v-if="showInlineAdd\(widget\)"/);
    // 多维视图：仅最后一个部件；工作台：顶部菜单不可用时退回最后一个部件
    expect(src).toMatch(/ctx\?\.surface === 'insight'/);
    expect(src).toMatch(/!ctx\.topAddEntry/);
  });

  it('工作台把顶部菜单可用性注入部件表面上下文', () => {
    const src = useWorkbenchSrc();
    expect(src).toMatch(/surface\.topAddEntry = v/);
  });

  it('部件下拉菜单项为「编辑…」', () => {
    expect(hostSrc()).toMatch(/编辑…/);
  });
});
