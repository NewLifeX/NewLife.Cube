import { describe, expect, it } from 'vitest';
import {
  chartOptionTitle,
  isFixedDeveloperChart,
  isTallFixedChart,
  mergeDeveloperCharts,
  normalizeChartOptions,
  prepareFixedChartOption,
  resolveUserStatChartView,
  sortBoxplotDatum,
  stripChartOptionTitle,
  synthesizeDeveloperChartWidgets,
} from './chartOptions';

describe('normalizeChartOptions', () => {
  it('turns null into an empty array', () => {
    expect(normalizeChartOptions(null)).toEqual([]);
    expect(normalizeChartOptions(undefined)).toEqual([]);
    expect(normalizeChartOptions({})).toEqual([]);
  });

  it('returns an option array as-is', () => {
    const options = [{ title: { text: 'a' } }];
    expect(normalizeChartOptions(options)).toBe(options);
  });
});

describe('resolveUserStatChartView', () => {
  it('keeps a single option', () => {
    const options = [{ series: [] }];
    expect(resolveUserStatChartView(false, options)).toEqual({ charts: options, showAlert: false });
  });

  it('keeps only the first two options', () => {
    const options = [{ id: 1 }, { id: 2 }, { id: 3 }];
    expect(resolveUserStatChartView(false, options).charts).toEqual([{ id: 1 }, { id: 2 }]);
  });

  it('hides the chart when the array is empty', () => {
    expect(resolveUserStatChartView(false, [])).toEqual({ charts: [], showAlert: false });
  });

  it('shows an alert and no chart when the request failed', () => {
    expect(resolveUserStatChartView(true, [{ series: [] }])).toEqual({ charts: [], showAlert: true });
  });
});

describe('boxplot normalize', () => {
  it('sorts five-number boxplot rows', () => {
    expect(sortBoxplotDatum([0, 1, 0, 1, 0])).toEqual([0, 0, 0, 1, 1]);
    expect(sortBoxplotDatum([5, 1, 3, 2, 4])).toEqual([1, 2, 3, 4, 5]);
  });

  it('prepareFixedChartOption sorts boxplot series and hides legend/title', () => {
    const prepared = prepareFixedChartOption({
      title: { text: '箱线' },
      legend: { data: ['箱线'] },
      series: [{ type: 'boxplot', data: [[0, 1, 0, 1, 0]] }],
    }) as {
      title?: unknown;
      legend?: { show?: boolean };
      grid?: unknown;
      series: { data: number[][] }[];
    };
    expect(prepared.title).toBeUndefined();
    expect(prepared.legend?.show).toBe(false);
    expect(prepared.grid).toBeTruthy();
    expect(prepared.series[0].data[0]).toEqual([0, 0, 0, 1, 1]);
  });

  it('marks boxplot as tall', () => {
    expect(isTallFixedChart({ series: [{ type: 'boxplot' }] })).toBe(true);
    expect(isTallFixedChart({ series: [{ type: 'line' }] })).toBe(false);
  });
});

describe('synthesizeDeveloperChartWidgets', () => {
  it('builds full-width fixed widgets and strips option title', () => {
    const widgets = synthesizeDeveloperChartWidgets(
      [
        { title: { text: '日活' }, series: [{ type: 'line' }] },
        { title: '月活', series: [{ type: 'boxplot', data: [[0, 1, 0, 1, 0]] }] },
        { title: '年活' },
      ],
      'Admin/UserStat',
    );
    expect(widgets).toHaveLength(2);
    expect(widgets[0]).toMatchObject({
      kind: 'legacyChart',
      title: '日活',
      layout: { w: 12, h: 3, order: 0 },
      fixed: true,
      tall: false,
    });
    expect(widgets[1]).toMatchObject({
      title: '月活',
      layout: { w: 12, h: 4 },
      tall: true,
    });
    expect(isFixedDeveloperChart(widgets[0].id)).toBe(true);
    expect((widgets[0].chartOption as { title?: unknown }).title).toBeUndefined();
  });

  it('mergeDeveloperCharts prepends fixed charts and keeps custom widgets', () => {
    const merged = mergeDeveloperCharts(
      {
        version: 1,
        widgets: [
          {
            id: 'legacy-chart',
            kind: 'legacyChart',
            title: '旧',
            layout: { w: 12, order: 0 },
            source: { provider: 'entity.aggregate', typePath: 'Admin/UserStat' },
            query: {},
          },
          {
            id: 'm1',
            kind: 'metricCard',
            title: '数',
            layout: { w: 3, order: 1 },
            source: { provider: 'entity.aggregate', typePath: 'Admin/UserStat' },
            query: {},
          },
        ],
      },
      [{ title: 'A' }, { title: 'B' }],
      'Admin/UserStat',
    );
    expect(merged.widgets.map((w) => w.kind)).toEqual(['legacyChart', 'legacyChart', 'metricCard']);
    expect(merged.widgets[0].layout.w).toBe(12);
    expect(merged.widgets[2].layout.order).toBe(2);
  });
});

describe('chartOptionTitle / stripChartOptionTitle', () => {
  it('reads and strips title', () => {
    expect(chartOptionTitle({ title: { text: ' 日活 ' } }, 'x')).toBe('日活');
    expect(chartOptionTitle({ title: '月活' }, 'x')).toBe('月活');
    expect(chartOptionTitle(null, '图表 1')).toBe('图表 1');
    expect(stripChartOptionTitle({ title: { text: 't' }, series: [] })).toEqual({ series: [] });
  });
});
