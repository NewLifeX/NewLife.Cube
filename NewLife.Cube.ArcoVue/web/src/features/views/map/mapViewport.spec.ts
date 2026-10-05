/** 视口筛选与分帧单测（OSC-261004d7f4） */
import { describe, expect, it } from 'vitest';
import { boundsOf, expandBounds, inBounds, pickViewportPoints } from './mapViewport';

describe('mapViewport', () => {
  const b = { minLng: 100, minLat: 20, maxLng: 110, maxLat: 30 };

  it('expandBounds 按比例外扩', () => {
    expect(expandBounds(b, 0.2)).toEqual({ minLng: 98, maxLng: 112, minLat: 18, maxLat: 32 });
  });

  it('inBounds 边界包含', () => {
    expect(inBounds({ lng: 100, lat: 20 }, b)).toBe(true);
    expect(inBounds({ lng: 110, lat: 30 }, b)).toBe(true);
    expect(inBounds({ lng: 110.1, lat: 25 }, b)).toBe(false);
    expect(inBounds({ lng: 105, lat: 19.9 }, b)).toBe(false);
  });

  it('boundsOf 空集返回 null', () => {
    expect(boundsOf([{ lng: 1, lat: 2 }, { lng: -1, lat: 5 }])).toEqual({
      minLng: -1,
      maxLng: 1,
      minLat: 2,
      maxLat: 5,
    });
    expect(boundsOf([])).toBeNull();
  });

  it('pickViewportPoints 跳过已绘制、限数、保序', () => {
    const pts = [
      { id: 'a', lng: 101, lat: 21 },
      { id: 'b', lng: 102, lat: 22 },
      { id: 'c', lng: 111, lat: 21 },
      { id: 'd', lng: 103, lat: 23 },
    ];
    expect(pickViewportPoints(pts, new Set(['a']), b, 2).map((p) => p.id)).toEqual(['b', 'd']);
    expect(pickViewportPoints(pts, new Set(), b, 0)).toEqual([]);
    expect(pickViewportPoints(pts, new Set(['a', 'b', 'c', 'd']), b, 5)).toEqual([]);
  });
});
