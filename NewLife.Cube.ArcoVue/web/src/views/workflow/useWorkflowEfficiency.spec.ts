import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({ default: { workflow: { efficiency: vi.fn() } } }));

import {
  buildQuery,
  drillOf,
  formatDuration,
  formatRate,
  removeTagOf,
  rowsLookEmpty,
} from './useWorkflowEfficiency';

describe('formatDuration 耗时文案（design §6.4 三档）', () => {
  it('不足 1 小时写分钟', () => {
    expect(formatDuration(0.5)).toBe('30 分钟');
    expect(formatDuration(0.05)).toBe('3 分钟');
  });

  it('1 到不足 48 小时写小时', () => {
    expect(formatDuration(3.5)).toBe('3.5 小时');
    expect(formatDuration(47.94)).toBe('47.9 小时');
  });

  it('达 48 小时写天', () => {
    expect(formatDuration(48)).toBe('2.0 天');
    expect(formatDuration(60)).toBe('2.5 天');
  });

  it('全 0 的年行视为空表', () => {
    expect(rowsLookEmpty([])).toBe(true);
    expect(rowsLookEmpty([{ count: 0 }, { count: 0 }, { count: 0 }])).toBe(true);
    expect(rowsLookEmpty([{ count: 0 }, { count: 2 }])).toBe(false);
  });

  it('无样本 null 写「—」', () => {
    expect(formatDuration(null)).toBe('—');
    expect(formatDuration(undefined)).toBe('—');
  });
});

describe('formatRate 比例文案', () => {
  it('0~1 转百分比取整，null 写「—」', () => {
    expect(formatRate(0.8)).toBe('80%');
    expect(formatRate(0.256)).toBe('26%');
    expect(formatRate(null)).toBe('—');
  });
});

describe('drillOf 点行下钻（design §6.3）', () => {
  it('流程→节点（带 definitionId）', () => {
    expect(drillOf('process', '12')).toEqual({ groupBy: 'node', patch: { definitionId: '12' } });
  });

  it('部门→用户（带 departmentId）', () => {
    expect(drillOf('department', '3')).toEqual({ groupBy: 'user', patch: { departmentId: '3' } });
  });

  it('年→月（带 year）', () => {
    expect(drillOf('year', '2026')).toEqual({ groupBy: 'month', patch: { year: '2026' } });
  });

  it('用户：已有流程→节点，否则→流程', () => {
    expect(drillOf('user', '7', '12')).toEqual({ groupBy: 'node', patch: { userId: '7' } });
    expect(drillOf('user', '7')).toEqual({ groupBy: 'process', patch: { userId: '7' } });
  });

  it('月、节点行就地展开（返回 null）', () => {
    expect(drillOf('month', '2026-09')).toBeNull();
    expect(drillOf('node', 'n1')).toBeNull();
  });
});

describe('removeTagOf 去标签回上一级', () => {
  it('流程→流程聚合、部门→部门聚合、年→年聚合', () => {
    expect(removeTagOf('definitionId')).toBe('process');
    expect(removeTagOf('departmentId')).toBe('department');
    expect(removeTagOf('year')).toBe('year');
  });

  it('用户标签：有流程回节点，否则回流程', () => {
    expect(removeTagOf('userId', '12')).toBe('node');
    expect(removeTagOf('userId')).toBe('process');
  });
});

describe('buildQuery 参数组装', () => {
  it('缺省近 30 天，带筛选', () => {
    const q = buildQuery({
      groupBy: 'process',
      window: 'days30',
      yearValue: '2026',
      definitionId: '12',
    });
    expect(q).toEqual({ groupBy: 'process', days: 30, definitionId: '12' });
  });

  it('近 7/90 天', () => {
    expect(buildQuery({ groupBy: 'user', window: 'days7', yearValue: '2026' }).days).toBe(7);
    expect(buildQuery({ groupBy: 'user', window: 'days90', yearValue: '2026' }).days).toBe(90);
  });

  it('按年份时间窗传 year；groupBy=year 固定三年不带时间窗', () => {
    expect(buildQuery({ groupBy: 'process', window: 'year', yearValue: '2025' }).year).toBe('2025');
    const q = buildQuery({ groupBy: 'year', window: 'days30', yearValue: '2026' });
    expect(q.days).toBeUndefined();
    expect(q.year).toBeUndefined();
  });

  it('yearFilter（年下钻）优先于时间窗', () => {
    const q = buildQuery({ groupBy: 'month', window: 'days30', yearValue: '2026', yearFilter: '2025' });
    expect(q.year).toBe('2025');
    expect(q.days).toBeUndefined();
  });

  it('展开 slow：节点行带 nodeId，月行带 month', () => {
    const q = buildQuery({ groupBy: 'node', window: 'days30', yearValue: '2026', definitionId: '12', nodeId: 'n1' });
    expect(q.groupBy).toBe('node');
    expect(q.nodeId).toBe('n1');
    const m = buildQuery({ groupBy: 'month', window: 'days30', yearValue: '2026', month: '2026-09' });
    expect(m.month).toBe('2026-09');
  });
});
