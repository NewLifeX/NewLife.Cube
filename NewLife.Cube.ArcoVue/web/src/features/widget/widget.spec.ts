import { describe, expect, it } from 'vitest';
import { serializeDashboardJson } from '@cube/api-core';
import { isUnlinkedWidget, normalizeSourceRows, synthesizeLegacyDashboard } from './legacy';
import { resolveKanbanInteractive } from './useMiniKanbanWidget';
import { readChartItems } from './useMiniChartWidget';
import { normalizeQueryResult, shouldQueryWidget } from './useWidgetQuery';
import { buildMiniChartOption } from './chartTemplates';
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

describe('normalizeQueryResult', () => {
  it('unwraps ApiResponse envelope', () => {
    const r = normalizeQueryResult({
      code: 0,
      data: { value: null, items: [{ key: '1', label: '男', value: 3 }] },
    });
    expect(r?.items).toHaveLength(1);
    expect(r?.items?.[0].label).toBe('男');
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
