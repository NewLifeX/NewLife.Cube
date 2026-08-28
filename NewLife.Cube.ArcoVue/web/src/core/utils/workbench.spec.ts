import { describe, expect, it } from 'vitest';
import {
  alignWorkbenchSeedLayout,
  greetingPeriod,
  greetingText,
  resolveWorkbenchIcon,
  WORKBENCH_NAMED_ICONS,
} from './workbench';

describe('greetingPeriod', () => {
  it('maps hour ranges', () => {
    expect(greetingPeriod(new Date(2026, 0, 1, 5, 0))).toBe('上午');
    expect(greetingPeriod(new Date(2026, 0, 1, 10, 59))).toBe('上午');
    expect(greetingPeriod(new Date(2026, 0, 1, 11, 0))).toBe('中午');
    expect(greetingPeriod(new Date(2026, 0, 1, 12, 30))).toBe('中午');
    expect(greetingPeriod(new Date(2026, 0, 1, 13, 0))).toBe('下午');
    expect(greetingPeriod(new Date(2026, 0, 1, 17, 59))).toBe('下午');
    expect(greetingPeriod(new Date(2026, 0, 1, 18, 0))).toBe('晚上');
    expect(greetingPeriod(new Date(2026, 0, 1, 4, 0))).toBe('晚上');
  });
});

describe('greetingText', () => {
  it('uses displayName', () => {
    expect(greetingText('张三', new Date(2026, 0, 1, 9, 0))).toBe('上午好，张三');
  });
});

describe('resolveWorkbenchIcon', () => {
  it('maps fa and named fallback', () => {
    expect(resolveWorkbenchIcon('fa-users')).toBe('peoples');
    expect(resolveWorkbenchIcon('fa-sign-in')).toBe('login');
    expect(resolveWorkbenchIcon('fa-exclamation-triangle')).toBe('attention');
    expect(resolveWorkbenchIcon('fa-th-large')).toBe('application-menu');
    expect(resolveWorkbenchIcon('', 'Monitor')).toBe('chart-line');
    expect(WORKBENCH_NAMED_ICONS.Inbox).toBe('remind');
  });
});

describe('alignWorkbenchSeedLayout', () => {
  it('aligns monitor/quickLink height and content half row', () => {
    const cfg = alignWorkbenchSeedLayout({
      version: 1,
      widgets: [
        {
          id: 'seed-Monitor',
          kind: 'monitorChart',
          title: 'm',
          layout: { w: 8, h: 4, order: 0 },
          source: { provider: 'named', widgetName: 'Monitor' },
          query: {},
        },
        {
          id: 'seed-QuickLink',
          kind: 'quickLinks',
          title: 'q',
          layout: { w: 4, h: 2, order: 1 },
          source: { provider: 'named', widgetName: 'QuickLink' },
          query: {},
        },
        {
          id: 'seed-Inbox',
          kind: 'inbox',
          title: 'i',
          layout: { w: 6, h: 2, order: 2 },
          source: { provider: 'named', widgetName: 'Inbox' },
          query: {},
        },
        {
          id: 'custom-1',
          kind: 'metricCard',
          title: 'c',
          layout: { w: 3, order: 3 },
          source: { provider: 'named', widgetName: 'MyDays' },
          query: {},
        },
      ],
    });
    expect(cfg.widgets[0].layout).toMatchObject({ w: 8, h: 3 });
    expect(cfg.widgets[1].layout).toMatchObject({ w: 4, h: 3 });
    expect(cfg.widgets[2].layout.w).toBe(6);
    expect(cfg.widgets[3].layout.w).toBe(3);
  });
});
