import { describe, expect, it } from 'vitest';
import { buildWidgetLayout, mergeWidgetsWithLayout, moveWidget } from '../pages/workbench/layout';
import type { WorkbenchWidget } from '../types/workbench';

const widget = (name: string): WorkbenchWidget => ({
  name,
  title: name,
  widgetType: 'Content',
  data: null,
});

describe('mergeWidgetsWithLayout', () => {
  it('未登记布局时保持后端顺序', () => {
    const result = mergeWidgetsWithLayout([widget('A'), widget('B'), widget('C')]);
    expect(result.visible.map((item) => item.name)).toEqual(['A', 'B', 'C']);
    expect(result.hidden).toEqual([]);
  });

  it('按 sort 排序，未登记卡片排在已登记之后', () => {
    const result = mergeWidgetsWithLayout([widget('A'), widget('B'), widget('C')], {
      B: { sort: 0 },
      A: { sort: 1 },
    });
    expect(result.visible.map((item) => item.name)).toEqual(['B', 'A', 'C']);
  });

  it('hide 卡片进入 hidden，并按 sort 排序', () => {
    const result = mergeWidgetsWithLayout([widget('A'), widget('B'), widget('C')], {
      A: { sort: 2, hide: true },
      C: { sort: 0, hide: true },
      B: { sort: 1 },
    });
    expect(result.visible.map((item) => item.name)).toEqual(['B']);
    expect(result.hidden.map((item) => item.name)).toEqual(['C', 'A']);
  });

  it('空列表与空布局安全返回', () => {
    expect(mergeWidgetsWithLayout([], null)).toEqual({ visible: [], hidden: [] });
  });
});

describe('buildWidgetLayout', () => {
  it('可见卡片 sort 从 0 递增，隐藏卡片排在后面并标记 hide', () => {
    const layout = buildWidgetLayout([widget('B'), widget('A')], [widget('C')]);
    expect(layout).toEqual({
      B: { sort: 0, hide: false },
      A: { sort: 1, hide: false },
      C: { sort: 2, hide: true },
    });
  });
});

describe('moveWidget', () => {
  it('把卡片移动到目标位置并保持其余顺序', () => {
    const result = moveWidget([widget('A'), widget('B'), widget('C')], 'C', 'A');
    expect(result.map((item) => item.name)).toEqual(['C', 'A', 'B']);
  });

  it('未知卡片或同位置时原样返回', () => {
    const source = [widget('A'), widget('B')];
    expect(moveWidget(source, 'X', 'A').map((item) => item.name)).toEqual(['A', 'B']);
    expect(moveWidget(source, 'A', 'A').map((item) => item.name)).toEqual(['A', 'B']);
  });

  it('不修改入参数组', () => {
    const source = [widget('A'), widget('B')];
    moveWidget(source, 'B', 'A');
    expect(source.map((item) => item.name)).toEqual(['A', 'B']);
  });
});
