/**
 * 地图坐标字段检测与合并坐标值解析（OSC-261004d7f4）
 *
 * 支持两种形态：
 * - 分列经纬度：Longitude/Lng/Lat 等字段名或「经度/纬度」显示名，且为数值类型
 * - 合并坐标字段：Location/Coord/坐标 等单字段，值形如 "116.39,39.91"、"(116.39 39.91)"、"[116.39,39.91]"、数组
 */
import type { FieldMeta } from '@/core/types/field';
import { getValueByKey } from '@/core/utils/url';

/** 坐标来源形态 */
export type CoordMode = 'latlng' | 'merged';
/** 合并坐标解析顺序 */
export type CoordOrder = 'lnglat' | 'latlng';
/** 数据坐标系 */
export type CoordSystem = 'gcj02' | 'wgs84' | 'bd09';

/** 坐标检测结果；null 表示未命中 */
export type CoordDetection =
  | { mode: 'latlng'; lngField: string; latField: string }
  | { mode: 'merged'; coordField: string };

export interface LngLat {
  lng: number;
  lat: number;
}

const LNG_NAMES = ['longitude', 'lng', 'lon', 'gpslng'];
const LAT_NAMES = ['latitude', 'lat', 'gpslat'];
const MERGED_NAMES = [
  'location',
  'coord',
  'coords',
  'coordinate',
  'coordinates',
  'point',
  'geo',
  'geopoint',
  'lnglat',
  'latlng',
  'position',
];
const MERGED_LABELS = ['坐标', '经纬度', '位置'];

const NUMBER_TYPES = new Set([
  'Int16',
  'Int32',
  'Int64',
  'UInt16',
  'UInt32',
  'UInt64',
  'Byte',
  'SByte',
  'Double',
  'Single',
  'Decimal',
]);

function isNumberField(f: FieldMeta): boolean {
  return NUMBER_TYPES.has(f.typeName || '');
}

function findByNames(fields: FieldMeta[], names: string[], label: string): FieldMeta | undefined {
  return fields.find(
    (f) =>
      !!f.name &&
      !f.primaryKey &&
      (names.includes(f.name.toLowerCase()) || (f.displayName || '').includes(label)),
  );
}

/**
 * 检测坐标字段。分列优先（两者齐全且数值类型）；否则尝试合并坐标字段（String/未知类型）。
 * 仅看字段存在性，不做取值抽样（首屏数据未加载时也可判定）。
 */
export function detectCoordinateFields(fields: FieldMeta[]): CoordDetection | null {
  const lng = findByNames(fields, LNG_NAMES, '经度');
  const lat = findByNames(fields, LAT_NAMES, '纬度');
  if (lng && lat && isNumberField(lng) && isNumberField(lat)) {
    return { mode: 'latlng', lngField: lng.name, latField: lat.name };
  }

  const merged = fields.find(
    (f) =>
      !!f.name &&
      !f.primaryKey &&
      (MERGED_NAMES.includes(f.name.toLowerCase()) ||
        MERGED_LABELS.some((k) => (f.displayName || '').includes(k))) &&
      (!f.typeName || f.typeName === 'String'),
  );
  if (merged) return { mode: 'merged', coordField: merged.name };

  return null;
}

/** 经纬度有效性：范围合法且非 (0,0)（旧数据空值约定） */
export function validLngLat(lng: number, lat: number): boolean {
  if (!Number.isFinite(lng) || !Number.isFinite(lat)) return false;
  if (lng < -180 || lng > 180 || lat < -90 || lat > 90) return false;
  if (Math.abs(lng) < 1e-6 && Math.abs(lat) < 1e-6) return false;
  return true;
}

/**
 * 解析合并坐标值。支持逗号/空格/分号/全角分隔、括号包裹、JSON 数组；
 * order 缺省 lnglat（先经度后纬度）。
 */
export function parseCoordText(raw: unknown, order: CoordOrder = 'lnglat'): LngLat | null {
  let a: number | null = null;
  let b: number | null = null;

  if (Array.isArray(raw) && raw.length >= 2) {
    a = Number(raw[0]);
    b = Number(raw[1]);
  } else if (raw != null && raw !== '') {
    const text = String(raw).trim();
    if (text) {
      const parts = text.replace(/[()[\]{}]/g, ' ').split(/[,，;；\s]+/).filter((s) => s.length > 0);
      if (parts.length >= 2) {
        a = Number(parts[0]);
        b = Number(parts[1]);
      }
    }
  }

  if (a == null || b == null || !Number.isFinite(a) || !Number.isFinite(b)) return null;
  const lng = order === 'lnglat' ? a : b;
  const lat = order === 'lnglat' ? b : a;
  return validLngLat(lng, lat) ? { lng, lat } : null;
}

/** 从数据行读取坐标（按检测结果），无效/缺失返回 null */
export function readCoordFromRow(
  row: Record<string, unknown>,
  detection: CoordDetection,
  order: CoordOrder = 'lnglat',
): LngLat | null {
  if (detection.mode === 'latlng') {
    const lng = Number(getValueByKey(row, detection.lngField));
    const lat = Number(getValueByKey(row, detection.latField));
    return validLngLat(lng, lat) ? { lng, lat } : null;
  }
  return parseCoordText(getValueByKey(row, detection.coordField), order);
}
