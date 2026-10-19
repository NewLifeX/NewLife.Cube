/**
 * 工作台 SVG 图表辅助函数单元测试
 *
 * 覆盖坐标生成、单点兜底、百分比与内存格式化。
 * 运行：pnpm test:unit core/__tests__/workbenchChart.spec.ts
 */
import { describe, it, expect } from 'vitest';
import { buildSparkPoints, clamp, formatMemory, formatPercent } from '../pages/workbench/chart';

describe('buildSparkPoints — SVG polyline 坐标', () => {
  it('没有数据时返回空串', () => {
    expect(buildSparkPoints([], 100, 100)).toBe('');
    expect(buildSparkPoints([1], 0, 100)).toBe('');
  });

  it('把 0-100 映射到画布高度，0 在底部、100 在顶部', () => {
    expect(buildSparkPoints([0, 100], 100, 100)).toBe('0.0,100.0 100.0,0.0');
  });

  it('单点复制为水平线，保证首帧可见', () => {
    expect(buildSparkPoints([50], 100, 100)).toBe('0.0,50.0 100.0,50.0');
  });

  it('越界值收敛到 0-100 区间', () => {
    expect(buildSparkPoints([-10, 150], 100, 100)).toBe('0.0,100.0 100.0,0.0');
  });
});

describe('格式化函数', () => {
  it('clamp 处理非数字并收敛区间', () => {
    expect(clamp(Number.NaN, 0, 100)).toBe(0);
    expect(clamp(-1, 0, 100)).toBe(0);
    expect(clamp(101, 0, 100)).toBe(100);
  });

  it('百分比保留一位小数并去掉 .0', () => {
    expect(formatPercent(33.33)).toBe('33.3%');
    expect(formatPercent(50)).toBe('50%');
    expect(formatPercent(Number.NaN)).toBe('-');
  });

  it('内存由 MB 换算为 GB', () => {
    expect(formatMemory(512, 1024)).toBe('0.5 / 1.0 GB');
    expect(formatMemory(undefined, 1024)).toBe('-');
    expect(formatMemory(1, 0)).toBe('-');
  });
});
