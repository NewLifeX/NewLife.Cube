/**
 * 时长换算纯函数测试（`ItemType=TimeSpan` 的单位口径）
 *
 * 运行：pnpm test:unit core/components/TimeSpanInput/__tests__/timeSpan.logic.spec.ts
 */
import { describe, expect, it } from 'vitest';
import {
  joinTimeSpan,
  secondsToTimeSpan,
  splitTimeSpan,
  timeSpanToSeconds,
  timeSpanUnitOfDescription,
} from '../timeSpan.logic';

describe('timeSpanUnitOfDescription', () => {
  it('应该按描述推断存储单位', () => {
    expect(timeSpanUnitOfDescription('累计在线总时间，单位秒')).toBe('seconds');
    expect(timeSpanUnitOfDescription('耗时，单位毫秒')).toBe('milliseconds');
    expect(timeSpanUnitOfDescription('时长(ms)')).toBe('milliseconds');
    expect(timeSpanUnitOfDescription('Timeout in milliseconds')).toBe('milliseconds');
    expect(timeSpanUnitOfDescription('时长，单位分钟')).toBe('minutes');
    expect(timeSpanUnitOfDescription('Duration in minute')).toBe('minutes');
    expect(timeSpanUnitOfDescription('时长，单位小时')).toBe('hours');
    expect(timeSpanUnitOfDescription('Duration in hour')).toBe('hours');
  });

  it('应该默认按秒 当描述缺失或没有单位线索时', () => {
    expect(timeSpanUnitOfDescription('')).toBe('seconds');
    expect(timeSpanUnitOfDescription(undefined)).toBe('seconds');
    expect(timeSpanUnitOfDescription('累计时长')).toBe('seconds');
  });

  it('应该毫秒优先于分钟 当描述同时含两种单位时', () => {
    expect(timeSpanUnitOfDescription('毫秒（不足 1 分钟不显示）')).toBe('milliseconds');
  });
});

describe('timeSpanToSeconds / secondsToTimeSpan', () => {
  it('应该按单位换算', () => {
    expect(timeSpanToSeconds(22515, 'seconds')).toBe(22515);
    expect(timeSpanToSeconds(1500, 'milliseconds')).toBe(1.5);
    expect(timeSpanToSeconds(90, 'minutes')).toBe(5400);
    expect(timeSpanToSeconds(2, 'hours')).toBe(7200);
  });

  it('应该保留负号 以便列表显示负时长', () => {
    expect(timeSpanToSeconds(-60, 'seconds')).toBe(-60);
    expect(timeSpanToSeconds('-60', 'seconds')).toBe(-60);
  });

  it('应该非法值按 0 处理', () => {
    expect(timeSpanToSeconds(undefined, 'seconds')).toBe(0);
    expect(timeSpanToSeconds('abc', 'seconds')).toBe(0);
  });

  it('应该按存储单位整数语义四舍五入 当由秒回写时', () => {
    expect(secondsToTimeSpan(90, 'seconds')).toBe(90);
    expect(secondsToTimeSpan(90, 'minutes')).toBe(2);
    expect(secondsToTimeSpan(5400, 'hours')).toBe(2);
    expect(secondsToTimeSpan(1500, 'milliseconds')).toBe(1500000);
  });
});

describe('splitTimeSpan', () => {
  it('应该拆成时/分/秒分段', () => {
    expect(splitTimeSpan(22515, 'seconds')).toEqual({ hours: 6, minutes: 15, seconds: 15 });
    expect(splitTimeSpan(0, 'seconds')).toEqual({ hours: 0, minutes: 0, seconds: 0 });
    expect(splitTimeSpan(null, 'seconds')).toEqual({ hours: 0, minutes: 0, seconds: 0 });
  });

  it('应该支持超过 24 小时的时长', () => {
    expect(splitTimeSpan(90061, 'seconds')).toEqual({ hours: 25, minutes: 1, seconds: 1 });
  });

  it('应该毫秒单位下秒保留 3 位小数', () => {
    expect(splitTimeSpan(1500, 'milliseconds')).toEqual({ hours: 0, minutes: 0, seconds: 1.5 });
    expect(splitTimeSpan(1, 'milliseconds')).toEqual({ hours: 0, minutes: 0, seconds: 0.001 });
  });

  it('应该毫秒四舍五入到 60 时进位 而不是显示 60 秒', () => {
    expect(splitTimeSpan(59999.6, 'milliseconds')).toEqual({ hours: 0, minutes: 1, seconds: 0 });
    expect(splitTimeSpan(3599999.6, 'milliseconds')).toEqual({ hours: 1, minutes: 0, seconds: 0 });
  });

  it('应该负值按 0 处理 因为输入框不接受负时长', () => {
    expect(splitTimeSpan(-90, 'seconds')).toEqual({ hours: 0, minutes: 0, seconds: 0 });
  });
});

describe('joinTimeSpan', () => {
  it('应该合成为秒', () => {
    expect(joinTimeSpan({ hours: 6, minutes: 15, seconds: 15 }, 'seconds')).toBe(22515);
  });

  it('应该合成时按存储单位换算', () => {
    expect(joinTimeSpan({ hours: 1, minutes: 30, seconds: 0 }, 'minutes')).toBe(90);
    expect(joinTimeSpan({ hours: 2, minutes: 0, seconds: 0 }, 'hours')).toBe(2);
    expect(joinTimeSpan({ hours: 0, minutes: 0, seconds: 1.5 }, 'milliseconds')).toBe(1500);
  });

  it('应该非法或负分段按 0 处理', () => {
    expect(joinTimeSpan({ hours: -1, minutes: 0, seconds: 0 }, 'seconds')).toBe(0);
    expect(joinTimeSpan({ hours: 1, minutes: Number.NaN, seconds: 0 }, 'seconds')).toBe(3600);
    expect(joinTimeSpan({ hours: 1, minutes: 0, seconds: undefined as unknown as number }, 'seconds')).toBe(3600);
  });

  it('应该分段与合成互为逆运算', () => {
    for (const total of [0, 59, 60, 3599, 3600, 22515, 90061]) {
      expect(joinTimeSpan(splitTimeSpan(total, 'seconds'), 'seconds')).toBe(total);
    }
  });
});
