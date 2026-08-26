import { describe, expect, it } from 'vitest';
import {
  AI_FAB_MARGIN,
  AI_PANEL_HEIGHT,
  AI_PANEL_MIN_H,
  AI_PANEL_MIN_W,
  AI_PANEL_WIDTH,
  applyAiResize,
  clampAiBoxPos,
  clampAiFabPos,
  defaultAiFabPos,
  defaultAiPanelPos,
  isAiResizeDir,
  normalizeAiFab,
  normalizeAiPanel,
  resolveAiPanelSize,
} from './aiFab';

describe('clampAiFabPos', () => {
  it('限制在视口内', () => {
    expect(clampAiFabPos(-10, -10, 800, 600, 36)).toEqual({ x: AI_FAB_MARGIN, y: AI_FAB_MARGIN });
    expect(clampAiFabPos(9999, 9999, 800, 600, 36)).toEqual({
      x: 800 - 36 - AI_FAB_MARGIN,
      y: 600 - 36 - AI_FAB_MARGIN,
    });
  });
});

describe('defaultAiFabPos', () => {
  it('默认右下', () => {
    expect(defaultAiFabPos(800, 600, 36)).toEqual({ x: 800 - 36 - 24, y: 600 - 36 - 24 });
  });
});

describe('resolveAiPanelSize', () => {
  it('宽屏用卡片尺寸', () => {
    expect(resolveAiPanelSize(1280, 800)).toEqual({ w: AI_PANEL_WIDTH, h: AI_PANEL_HEIGHT });
  });
  it('窄屏收进视口', () => {
    expect(resolveAiPanelSize(320, 500)).toEqual({
      w: 320 - AI_FAB_MARGIN * 2,
      h: Math.min(AI_PANEL_HEIGHT, 500 - AI_FAB_MARGIN * 2),
    });
  });
  it('保留用户拖出的宽高', () => {
    expect(resolveAiPanelSize(1280, 800, { w: 520, h: 620 })).toEqual({ w: 520, h: 620 });
  });
});

describe('defaultAiPanelPos', () => {
  it('默认右下卡片', () => {
    expect(defaultAiPanelPos(1280, 800)).toEqual({
      x: 1280 - AI_PANEL_WIDTH - 24,
      y: 800 - AI_PANEL_HEIGHT - 24,
    });
  });
  it('可贴悬浮球', () => {
    const near = { x: 100, y: 200 };
    const pos = defaultAiPanelPos(1280, 800, near);
    expect(pos.x).toBeGreaterThanOrEqual(AI_FAB_MARGIN);
    expect(pos.y).toBeGreaterThanOrEqual(AI_FAB_MARGIN);
  });
});

describe('clampAiBoxPos', () => {
  it('大盒子仍贴边', () => {
    expect(clampAiBoxPos(9999, 9999, 360, 480, 800, 600)).toEqual({
      x: 800 - 360 - AI_FAB_MARGIN,
      y: 600 - 480 - AI_FAB_MARGIN,
    });
  });
});

describe('normalizeAiFab', () => {
  it('合法坐标', () => {
    expect(normalizeAiFab({ x: 10.2, y: 20 })).toEqual({ x: 10.2, y: 20 });
  });
  it('非法丢弃', () => {
    expect(normalizeAiFab(null)).toBeUndefined();
    expect(normalizeAiFab({ x: 'a', y: 1 })).toBeUndefined();
    expect(normalizeAiFab({ x: 1 })).toBeUndefined();
  });
});

describe('normalizeAiPanel', () => {
  it('兼容旧版只有 xy', () => {
    expect(normalizeAiPanel({ x: 10, y: 20 })).toEqual({ x: 10, y: 20 });
  });
  it('带宽高', () => {
    expect(normalizeAiPanel({ x: 10, y: 20, w: 400, h: 500 })).toEqual({
      x: 10,
      y: 20,
      w: 400,
      h: 500,
    });
  });
});

describe('isAiResizeDir / applyAiResize', () => {
  it('识别方向', () => {
    expect(isAiResizeDir('se')).toBe(true);
    expect(isAiResizeDir('x')).toBe(false);
  });
  it('右下角放大', () => {
    const next = applyAiResize({ x: 100, y: 80, w: 360, h: 480 }, 'se', 40, 20, 1280, 800);
    expect(next).toEqual({ x: 100, y: 80, w: 400, h: 500 });
  });
  it('左边拉伸保持右缘', () => {
    const next = applyAiResize({ x: 200, y: 80, w: 360, h: 480 }, 'w', -40, 0, 1280, 800);
    expect(next.w).toBe(400);
    expect(next.x).toBe(160);
  });
  it('不低于最小尺寸', () => {
    const next = applyAiResize({ x: 100, y: 80, w: 360, h: 480 }, 'se', -400, -400, 1280, 800);
    expect(next.w).toBe(AI_PANEL_MIN_W);
    expect(next.h).toBe(AI_PANEL_MIN_H);
  });
});
