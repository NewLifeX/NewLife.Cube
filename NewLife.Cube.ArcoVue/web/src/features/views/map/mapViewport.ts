/**
 * 视口筛选与分帧（OSC-261004d7f4）：已加载点 → 当前视口待绘制集合
 */
import type { LngLat } from '@/core/utils/mapCoord';

export interface LngLatBounds {
  minLng: number;
  minLat: number;
  maxLng: number;
  maxLat: number;
}

/** 外扩包围盒（ratio 为各方向的比例，如 0.2 = 外扩 20%） */
export function expandBounds(b: LngLatBounds, ratio: number): LngLatBounds {
  const dx = (b.maxLng - b.minLng) * ratio;
  const dy = (b.maxLat - b.minLat) * ratio;
  return {
    minLng: b.minLng - dx,
    maxLng: b.maxLng + dx,
    minLat: b.minLat - dy,
    maxLat: b.maxLat + dy,
  };
}

export function inBounds(p: LngLat, b: LngLatBounds): boolean {
  return p.lng >= b.minLng && p.lng <= b.maxLng && p.lat >= b.minLat && p.lat <= b.maxLat;
}

/** 点集包围盒；空集返回 null */
export function boundsOf(points: readonly LngLat[]): LngLatBounds | null {
  if (!points.length) return null;
  let minLng = points[0].lng;
  let maxLng = points[0].lng;
  let minLat = points[0].lat;
  let maxLat = points[0].lat;
  for (const p of points) {
    if (p.lng < minLng) minLng = p.lng;
    if (p.lng > maxLng) maxLng = p.lng;
    if (p.lat < minLat) minLat = p.lat;
    if (p.lat > maxLat) maxLat = p.lat;
  }
  return { minLng, minLat, maxLng, maxLat };
}

/** 从已加载点中挑出「未绘制且在视口内」的点，最多 limit 个（保序） */
export function pickViewportPoints<T extends LngLat & { id: string }>(
  points: readonly T[],
  drawnIds: ReadonlySet<string>,
  bounds: LngLatBounds,
  limit: number,
): T[] {
  const out: T[] = [];
  if (limit <= 0) return out;
  for (const p of points) {
    if (out.length >= limit) break;
    if (drawnIds.has(p.id)) continue;
    if (inBounds(p, bounds)) out.push(p);
  }
  return out;
}
