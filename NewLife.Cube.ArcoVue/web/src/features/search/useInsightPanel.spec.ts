import { describe, expect, it } from 'vitest';
import { insightPanelShouldOccupyLayout } from './useInsightPanel';

describe('insightPanelShouldOccupyLayout', () => {
  it('hides when dashboard is unconfigured (no widgets)', () => {
    expect(insightPanelShouldOccupyLayout(0, '', false)).toBe(false);
  });

  it('shows when dashboard has widgets', () => {
    expect(insightPanelShouldOccupyLayout(1, '', false)).toBe(true);
  });

  it('shows developer-chart error only when there are no widgets', () => {
    expect(insightPanelShouldOccupyLayout(0, '图表加载失败', false)).toBe(true);
    expect(insightPanelShouldOccupyLayout(2, '图表加载失败', true)).toBe(true);
  });
});
