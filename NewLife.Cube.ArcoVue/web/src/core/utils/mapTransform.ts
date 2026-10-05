/**
 * 坐标系换算（OSC-261004d7f4）：WGS-84 ↔ GCJ-02（火星坐标）↔ BD-09（百度坐标）
 *
 * 大陆范围内按公开偏移算法换算；境外坐标原样返回；NaN/非法输入原样透传。
 */
import type { CoordSystem, LngLat } from '@/core/utils/mapCoord';

const X_PI = (Math.PI * 3000.0) / 180.0;
const PI = Math.PI;
const A = 6378245.0;
const EE = 0.00669342162296594323;

/** 是否在中国大陆范围外（境外不做偏移） */
export function outOfChina(lng: number, lat: number): boolean {
  return !(lng > 73.66 && lng < 135.05 && lat > 3.86 && lat < 53.55);
}

function transformLat(x: number, y: number): number {
  let ret =
    -100.0 + 2.0 * x + 3.0 * y + 0.2 * y * y + 0.1 * x * y + 0.2 * Math.sqrt(Math.abs(x));
  ret += ((20.0 * Math.sin(6.0 * x * PI) + 20.0 * Math.sin(2.0 * x * PI)) * 2.0) / 3.0;
  ret += ((20.0 * Math.sin(y * PI) + 40.0 * Math.sin((y / 3.0) * PI)) * 2.0) / 3.0;
  ret += ((160.0 * Math.sin((y / 12.0) * PI) + 320 * Math.sin((y * PI) / 30.0)) * 2.0) / 3.0;
  return ret;
}

function transformLng(x: number, y: number): number {
  let ret = 300.0 + x + 2.0 * y + 0.1 * x * x + 0.1 * x * y + 0.1 * Math.sqrt(Math.abs(x));
  ret += ((20.0 * Math.sin(6.0 * x * PI) + 20.0 * Math.sin(2.0 * x * PI)) * 2.0) / 3.0;
  ret += ((20.0 * Math.sin(x * PI) + 40.0 * Math.sin((x / 3.0) * PI)) * 2.0) / 3.0;
  ret += ((150.0 * Math.sin((x / 12.0) * PI) + 300.0 * Math.sin((x / 30.0) * PI)) * 2.0) / 3.0;
  return ret;
}

function isFinitePair(lng: number, lat: number): boolean {
  return Number.isFinite(lng) && Number.isFinite(lat);
}

/** WGS-84 → GCJ-02 */
export function wgs84ToGcj02(lng: number, lat: number): LngLat {
  if (!isFinitePair(lng, lat) || outOfChina(lng, lat)) return { lng, lat };
  let dLat = transformLat(lng - 105.0, lat - 35.0);
  let dLng = transformLng(lng - 105.0, lat - 35.0);
  const radLat = (lat / 180.0) * PI;
  let magic = Math.sin(radLat);
  magic = 1 - EE * magic * magic;
  const sqrtMagic = Math.sqrt(magic);
  dLat = (dLat * 180.0) / (((A * (1 - EE)) / (magic * sqrtMagic)) * PI);
  dLng = (dLng * 180.0) / ((A / sqrtMagic) * Math.cos(radLat) * PI);
  return { lng: lng + dLng, lat: lat + dLat };
}

/** GCJ-02 → WGS-84（粗反解，误差约 1e-5 度级） */
export function gcj02ToWgs84(lng: number, lat: number): LngLat {
  if (!isFinitePair(lng, lat) || outOfChina(lng, lat)) return { lng, lat };
  const g = wgs84ToGcj02(lng, lat);
  return { lng: lng * 2 - g.lng, lat: lat * 2 - g.lat };
}

/** GCJ-02 → BD-09 */
export function gcj02ToBd09(lng: number, lat: number): LngLat {
  if (!isFinitePair(lng, lat)) return { lng, lat };
  const z = Math.sqrt(lng * lng + lat * lat) + 0.00002 * Math.sin(lat * X_PI);
  const theta = Math.atan2(lat, lng) + 0.000003 * Math.cos(lng * X_PI);
  return { lng: z * Math.cos(theta) + 0.0065, lat: z * Math.sin(theta) + 0.006 };
}

/** BD-09 → GCJ-02 */
export function bd09ToGcj02(lng: number, lat: number): LngLat {
  if (!isFinitePair(lng, lat)) return { lng, lat };
  const x = lng - 0.0065;
  const y = lat - 0.006;
  const z = Math.sqrt(x * x + y * y) - 0.00002 * Math.sin(y * X_PI);
  const theta = Math.atan2(y, x) - 0.000003 * Math.cos(x * X_PI);
  return { lng: z * Math.cos(theta), lat: z * Math.sin(theta) };
}

/** 通用换算：from === to 或非法输入时原样返回 */
export function convert(lng: number, lat: number, from: CoordSystem, to: CoordSystem): LngLat {
  if (from === to || !isFinitePair(lng, lat)) return { lng, lat };
  // 统一先到 GCJ-02
  let g: LngLat;
  if (from === 'wgs84') g = wgs84ToGcj02(lng, lat);
  else if (from === 'bd09') g = bd09ToGcj02(lng, lat);
  else g = { lng, lat };
  if (to === 'gcj02') return g;
  if (to === 'bd09') return gcj02ToBd09(g.lng, g.lat);
  return gcj02ToWgs84(g.lng, g.lat);
}
