import { describe, expect, it } from 'vitest';
import {
  emptyDashboard,
  hasDashboardDomain,
  parseDashboardJson,
  serializeDashboardJson,
  validateDashboardForPut,
} from './widget';

describe('parseDashboardJson / serializeDashboardJson', () => {
  it('returns null for illegal version or blank', () => {
    expect(parseDashboardJson(null)).toBeNull();
    expect(parseDashboardJson('')).toBeNull();
    expect(parseDashboardJson('{"version":2,"widgets":[]}')).toBeNull();
    expect(parseDashboardJson('not-json')).toBeNull();
  });

  it('parses empty widgets', () => {
    const cfg = parseDashboardJson('{"version":1,"widgets":[]}');
    expect(cfg).toEqual({ version: 1, widgets: [] });
    expect(hasDashboardDomain('{"version":1,"widgets":[]}')).toBe(true);
    expect(hasDashboardDomain('  ')).toBe(false);
  });

  it('keeps unknown keys and strips query result keys', () => {
    const cfg = parseDashboardJson(
      JSON.stringify({
        version: 1,
        extraFlag: true,
        widgets: [
          {
            id: 'b',
            kind: 'metricCard',
            title: 't',
            layout: { w: 9, order: 5 },
            source: { provider: 'entity.aggregate', typePath: '/Admin/User' },
            query: { measure: { fn: 'count' } },
            data: 1,
            future: 'keep',
          },
        ],
      }),
    );
    expect(cfg?.extraFlag).toBe(true);
    expect(cfg?.widgets[0].future).toBe('keep');
    expect(cfg?.widgets[0].data).toBeUndefined();
    expect(cfg?.widgets[0].source.typePath).toBe('Admin/User');
    expect(cfg?.widgets[0].layout.w).toBe(3);
  });

  it('serialize rewrites order to 0..n-1', () => {
    const json = serializeDashboardJson({
      version: 1,
      widgets: [
        {
          id: 'b',
          kind: 'metricCard',
          title: 'B',
          layout: { w: 3, order: 8 },
          source: { provider: 'entity.aggregate', typePath: 'Admin/User' },
          query: { measure: { fn: 'count' } },
        },
        {
          id: 'a',
          kind: 'metricCard',
          title: 'A',
          layout: { w: 6, order: 1 },
          source: { provider: 'entity.aggregate', typePath: 'Admin/User' },
          query: { measure: { fn: 'count' } },
        },
      ],
    });
    const parsed = JSON.parse(json) as { widgets: { id: string; layout: { order: number } }[] };
    expect(parsed.widgets.map((w) => w.id)).toEqual(['a', 'b']);
    expect(parsed.widgets.map((w) => w.layout.order)).toEqual([0, 1]);
  });

  it('workbench surface keeps w=8 and maps illegal w=5 to 3', () => {
    const keep = parseDashboardJson(
      '{"version":1,"widgets":[{"id":"a","kind":"monitorChart","title":"m","layout":{"w":8,"order":0},"source":{"provider":"named","widgetName":"Monitor"}}]}',
      'workbench',
    );
    expect(keep?.widgets[0].layout.w).toBe(8);
    const bad = parseDashboardJson(
      '{"version":1,"widgets":[{"id":"a","kind":"metricCard","title":"t","layout":{"w":5,"order":0},"source":{"provider":"named","widgetName":"MyLogins"}}]}',
      'workbench',
    );
    expect(bad?.widgets[0].layout.w).toBe(3);
    const insight = parseDashboardJson(
      '{"version":1,"widgets":[{"id":"a","kind":"metricCard","title":"t","layout":{"w":2,"order":0},"source":{"provider":"named","widgetName":"UserCount"}}]}',
    );
    expect(insight?.widgets[0].layout.w).toBe(3);
  });

  it('validateDashboardForPut rejects duplicate id, legacyChart, oversize', () => {
    expect(validateDashboardForPut('')).toEqual({ ok: true, json: '' });
    const dup = serializeDashboardJson({
      version: 1,
      widgets: [
        {
          id: 'a',
          kind: 'metricCard',
          title: 't',
          layout: { w: 3, order: 0 },
          source: { provider: 'entity.aggregate', typePath: 'Admin/User' },
          query: {},
        },
        {
          id: 'a',
          kind: 'metricCard',
          title: 't2',
          layout: { w: 3, order: 1 },
          source: { provider: 'entity.aggregate', typePath: 'Admin/User' },
          query: {},
        },
      ],
    });
    const r = validateDashboardForPut(dup);
    expect(r.ok).toBe(false);
    const legacy = '{"version":1,"widgets":[{"id":"l","kind":"legacyChart","title":"t","layout":{"w":3,"order":0},"source":{"provider":"entity.aggregate","typePath":"Admin/User"},"query":{}}]}';
    expect(validateDashboardForPut(legacy).ok).toBe(false);
    const kanban = '{"version":1,"widgets":[{"id":"k","kind":"miniKanban","title":"t","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/User"},"query":{}}]}';
    expect(validateDashboardForPut(kanban).ok).toBe(false);
    expect(validateDashboardForPut(kanban, 'workbench').ok).toBe(true);
    const dataList = '{"version":1,"widgets":[{"id":"d","kind":"dataList","title":"t","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/User"},"query":{}}]}';
    expect(validateDashboardForPut(dataList).ok).toBe(false);
    expect(validateDashboardForPut(dataList, 'workbench').ok).toBe(true);
    const dataCard = '{"version":1,"widgets":[{"id":"c","kind":"dataCard","title":"t","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/User"},"query":{}}]}';
    expect(validateDashboardForPut(dataCard).ok).toBe(false);
    expect(validateDashboardForPut(dataCard, 'workbench').ok).toBe(true);
    expect(emptyDashboard()).toEqual({ version: 1, widgets: [] });
  });
});
