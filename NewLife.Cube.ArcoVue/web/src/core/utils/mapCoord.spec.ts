/** 地图坐标检测与解析单测（OSC-261004d7f4） */
import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import {
  detectCoordinateFields,
  parseCoordText,
  readCoordFromRow,
  validLngLat,
} from './mapCoord';

function f(name: string, typeName = 'String', extra: Partial<FieldMeta> = {}): FieldMeta {
  return { name, typeName, ...extra };
}

describe('detectCoordinateFields', () => {
  it('分列经纬度（字段名）', () => {
    const r = detectCoordinateFields([f('Name'), f('Longitude', 'Double'), f('Latitude', 'Double')]);
    expect(r).toEqual({ mode: 'latlng', lngField: 'Longitude', latField: 'Latitude' });
  });

  it('分列经纬度（显示名经度/纬度与小写 lng/lat）', () => {
    const r = detectCoordinateFields([
      f('X1', 'Double', { displayName: '经度' }),
      f('Y1', 'Decimal', { displayName: '纬度' }),
    ]);
    expect(r).toEqual({ mode: 'latlng', lngField: 'X1', latField: 'Y1' });
    expect(detectCoordinateFields([f('lng', 'Double'), f('lat', 'Double')])?.mode).toBe('latlng');
  });

  it('数值类型不符时回落合并坐标', () => {
    const r = detectCoordinateFields([f('Longitude'), f('Latitude'), f('Location')]);
    expect(r).toEqual({ mode: 'merged', coordField: 'Location' });
  });

  it('合并坐标（显示名含坐标/位置）', () => {
    expect(detectCoordinateFields([f('Name'), f('Pos', 'String', { displayName: '坐标位置' })]))
      .toEqual({ mode: 'merged', coordField: 'Pos' });
  });

  it('排除主键；无坐标返回 null', () => {
    expect(detectCoordinateFields([f('Id', 'Int32', { primaryKey: true }), f('Name')])).toBeNull();
    expect(detectCoordinateFields([])).toBeNull();
  });
});

describe('parseCoordText', () => {
  it('逗号/空格/分号/全角分隔', () => {
    expect(parseCoordText('116.39,39.91')).toEqual({ lng: 116.39, lat: 39.91 });
    expect(parseCoordText('116.39 39.91')).toEqual({ lng: 116.39, lat: 39.91 });
    expect(parseCoordText('116.39;39.91')).toEqual({ lng: 116.39, lat: 39.91 });
    expect(parseCoordText('116.39，39.91')).toEqual({ lng: 116.39, lat: 39.91 });
  });

  it('括号与 JSON 数组', () => {
    expect(parseCoordText('(116.39,39.91)')).toEqual({ lng: 116.39, lat: 39.91 });
    expect(parseCoordText('[116.39, 39.91]')).toEqual({ lng: 116.39, lat: 39.91 });
    expect(parseCoordText([116.39, 39.91])).toEqual({ lng: 116.39, lat: 39.91 });
  });

  it('顺序可配置 latlng', () => {
    expect(parseCoordText('39.91,116.39', 'latlng')).toEqual({ lng: 116.39, lat: 39.91 });
  });

  it('非法/零点/越界返回 null', () => {
    expect(parseCoordText('abc')).toBeNull();
    expect(parseCoordText('0,0')).toBeNull();
    expect(parseCoordText('200,95')).toBeNull();
    expect(parseCoordText(null)).toBeNull();
    expect(parseCoordText('')).toBeNull();
  });
});

describe('readCoordFromRow 与 validLngLat', () => {
  it('字段名大小写容错读取（PascalCase 字段名，camelCase 行键）', () => {
    const r = readCoordFromRow(
      { longitude: 116.39, latitude: 39.91 },
      { mode: 'latlng', lngField: 'Longitude', latField: 'Latitude' },
    );
    expect(r).toEqual({ lng: 116.39, lat: 39.91 });
  });

  it('合并字段行读取', () => {
    const r = readCoordFromRow(
      { Location: '116.39,39.91' },
      { mode: 'merged', coordField: 'Location' },
    );
    expect(r).toEqual({ lng: 116.39, lat: 39.91 });
  });

  it('validLngLat 范围与零点', () => {
    expect(validLngLat(116.39, 39.91)).toBe(true);
    expect(validLngLat(0, 0)).toBe(false);
    expect(validLngLat(181, 39)).toBe(false);
    expect(validLngLat(Number.NaN, 39)).toBe(false);
  });
});
