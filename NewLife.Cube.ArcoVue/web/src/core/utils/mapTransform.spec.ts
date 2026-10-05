/** 坐标换算单测（OSC-261004d7f4） */
import { describe, expect, it } from 'vitest';
import {
  bd09ToGcj02,
  convert,
  gcj02ToBd09,
  gcj02ToWgs84,
  outOfChina,
  wgs84ToGcj02,
} from './mapTransform';

describe('mapTransform', () => {
  it('大陆 WGS84 → GCJ02（天安门偏移量级）', () => {
    const p = wgs84ToGcj02(116.39133, 39.90733);
    expect(p.lng).toBeCloseTo(116.3975, 2);
    expect(p.lat).toBeCloseTo(39.9087, 2);
  });

  it('境外原样返回', () => {
    expect(outOfChina(139.6917, 35.6895)).toBe(true);
    expect(outOfChina(116.39, 39.91)).toBe(false);
    expect(wgs84ToGcj02(139.6917, 35.6895)).toEqual({ lng: 139.6917, lat: 35.6895 });
  });

  it('GCJ02 → BD09（百度偏移量级）', () => {
    const p = gcj02ToBd09(116.3975, 39.9087);
    expect(p.lng).toBeCloseTo(116.4039, 2);
    expect(p.lat).toBeCloseTo(39.915, 2);
  });

  it('BD09 → GCJ02 往返', () => {
    const b = gcj02ToBd09(121.4737, 31.2304);
    const g = bd09ToGcj02(b.lng, b.lat);
    expect(g.lng).toBeCloseTo(121.4737, 4);
    expect(g.lat).toBeCloseTo(31.2304, 4);
  });

  it('WGS84 往返（粗反解容差）', () => {
    const g = wgs84ToGcj02(113.2644, 23.1291);
    const w = gcj02ToWgs84(g.lng, g.lat);
    expect(w.lng).toBeCloseTo(113.2644, 4);
    expect(w.lat).toBeCloseTo(23.1291, 4);
  });

  it('convert 同源原样返回；NaN 透传', () => {
    expect(convert(116.4, 39.9, 'gcj02', 'gcj02')).toEqual({ lng: 116.4, lat: 39.9 });
    const bad = convert(Number.NaN, 39.9, 'wgs84', 'gcj02');
    expect(Number.isNaN(bad.lng)).toBe(true);
    expect(bad.lat).toBe(39.9);
  });
});
