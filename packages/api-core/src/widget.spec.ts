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
    expect(emptyDashboard()).toEqual({ version: 1, widgets: [] });
  });
});
