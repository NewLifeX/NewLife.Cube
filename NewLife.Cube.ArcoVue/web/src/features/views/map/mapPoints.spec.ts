/** 地图点位与样式解析单测（OSC-261004d7f4） */
import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import type { MapMapping } from '@/core/utils/viewMapping';
import type { ViewFormatRule } from '@/core/utils/viewProfile';
import {
  buildCategoryContext,
  buildMapPoints,
  detectLevelField,
  matchFormatRule,
  pickDataCoord,
  readLevelOfRow,
  resolveMarkerStyle,
  targetSystemOf,
} from './mapPoints';
import { convert } from '@/core/utils/mapTransform';

const mapping: MapMapping = {
  kind: 'map',
  coordMode: 'latlng',
  lngField: 'Longitude',
  latField: 'Latitude',
  titleField: 'Name',
  categoryField: 'Kind',
  categoryRules: [
    { value: '直辖市', icon: 'flag', color: '#F53F3F' },
    { value: '2', icon: 'bank', color: '#165DFF' },
  ],
  DefaultIcon: 'local',
  defaultColor: '#123456',
};

const kindField: FieldMeta = {
  name: 'Kind',
  typeName: 'String',
  dataSource: { '2': '省', '3': '市' },
};

describe('buildMapPoints', () => {
  const detection = { mode: 'latlng', lngField: 'Longitude', latField: 'Latitude' } as const;

  it('分列坐标 → 点位；无效/零点/越界计入 skipped', () => {
    const rows = [
      { id: 1, Name: '甲', Longitude: 116.39, Latitude: 39.91 },
      { id: 2, Name: '乙', Longitude: 0, Latitude: 0 },
      { id: 3, Name: '丙', Longitude: 200, Latitude: 39.91 },
    ];
    const r = buildMapPoints(rows, detection, { rowKey: 'id' });
    expect(r.points.length).toBe(1);
    expect(r.skipped).toBe(2);
    expect(r.points[0].id).toBe('1');
    expect(r.points[0].row).toBe(rows[0]);
  });

  it('GCJ02 → BD09 目标换算', () => {
    const rows = [{ id: 1, Longitude: 116.3975, Latitude: 39.9087 }];
    const r = buildMapPoints(rows, detection, {
      rowKey: 'id',
      coordSystem: 'gcj02',
      targetSystem: 'bd09',
    });
    expect(r.points[0].lng).toBeCloseTo(116.4039, 2);
    expect(r.points[0].lat).toBeCloseTo(39.915, 2);
  });

  it('合并坐标字段解析', () => {
    const rows = [{ id: 'p1', Location: '(116.39,39.91)' }];
    const r = buildMapPoints(rows, { mode: 'merged', coordField: 'Location' }, { rowKey: 'id' });
    expect(r.points.length).toBe(1);
    expect(r.points[0].id).toBe('p1');
  });
});

describe('resolveMarkerStyle（分类/填色优先级）', () => {
  const ctx = buildCategoryContext(mapping, kindField);

  it('分类规则命中（含 dataSource 数字键归一）', () => {
    expect(resolveMarkerStyle({ Kind: '直辖市' }, mapping, ctx)).toEqual({
      icon: 'flag',
      color: '#F53F3F',
    });
    expect(resolveMarkerStyle({ Kind: 2 }, mapping, ctx)).toEqual({
      icon: 'bank',
      color: '#165DFF',
    });
  });

  it('未命中分类时用默认样式', () => {
    expect(resolveMarkerStyle({}, mapping, ctx)).toEqual({ icon: 'local', color: '#123456' });
  });

  it('填色命中覆盖颜色、保留分类图标', () => {
    const rule: ViewFormatRule = {
      id: 'f1',
      apply: 'cell',
      color: '#FF0000',
      field: 'Name',
      op: 'eq',
      value: '甲',
    };
    expect(resolveMarkerStyle({ Name: '甲', Kind: '直辖市' }, mapping, ctx, [rule])).toEqual({
      icon: 'flag',
      color: '#FF0000',
    });
    expect(resolveMarkerStyle({ Name: '乙' }, mapping, ctx, [rule])).toEqual({
      icon: 'local',
      color: '#123456',
    });
  });
});

describe('matchFormatRule', () => {
  it('eq/contains/数值比较/空值', () => {
    expect(matchFormatRule({ Name: '甲' }, { id: '1', apply: 'cell', color: '#000000', field: 'Name', op: 'eq', value: '甲' })).toBe(true);
    expect(matchFormatRule({ Name: '甲乙' }, { id: '2', apply: 'cell', color: '#000000', field: 'Name', op: 'contains', value: '乙' })).toBe(true);
    expect(matchFormatRule({ Age: 20 }, { id: '3', apply: 'cell', color: '#000000', field: 'Age', op: 'gte', value: 18 })).toBe(true);
    expect(matchFormatRule({ Age: 15 }, { id: '4', apply: 'cell', color: '#000000', field: 'Age', op: 'gte', value: 18 })).toBe(false);
    expect(matchFormatRule({}, { id: '5', apply: 'cell', color: '#000000', field: 'Name', op: 'isNull' })).toBe(true);
  });
});

describe('targetSystemOf', () => {
  it('高德/腾讯 GCJ02，百度 BD09', () => {
    expect(targetSystemOf('amap')).toBe('gcj02');
    expect(targetSystemOf('tencent')).toBe('gcj02');
    expect(targetSystemOf('baidu')).toBe('bd09');
  });
});

describe('pickDataCoord（地图拾取换算）', () => {
  it('同坐标系原样返回并圆整 6 位', () => {
    const m: MapMapping = { ...mapping, coordSystem: 'gcj02' };
    expect(pickDataCoord(114.50000049, 23.50000049, m, 'gcj02')).toEqual({ lng: 114.5, lat: 23.5 });
  });

  it('跨坐标系换算：GCJ-02 拾取 → WGS-84 写库，往返近似还原', () => {
    const m: MapMapping = { ...mapping, coordSystem: 'wgs84' };
    const wgs = pickDataCoord(114.5, 23.5, m, 'gcj02');
    expect(wgs.lng).not.toBe(114.5);
    const back = convert(wgs.lng, wgs.lat, 'wgs84', 'gcj02');
    // GCJ-02 ⇄ WGS-84 为近似变换，往返允许 ~1e-5 误差
    expect(back.lng).toBeCloseTo(114.5, 4);
    expect(back.lat).toBeCloseTo(23.5, 4);
  });

  it('百度底图（BD-09）拾取 → GCJ-02 数据坐标系', () => {
    const m: MapMapping = { ...mapping, coordSystem: 'gcj02' };
    const gcj = pickDataCoord(114.5, 23.5, m, 'bd09');
    const back = convert(gcj.lng, gcj.lat, 'gcj02', 'bd09');
    expect(back.lng).toBeCloseTo(114.5, 4);
    expect(back.lat).toBeCloseTo(23.5, 4);
  });
});

describe('detectLevelField / readLevelOfRow（逐层加载）', () => {
  it('按字段名/显示名 + 数值类型检测层级字段', () => {
    const fields: FieldMeta[] = [
      { name: 'Name', typeName: 'String' },
      { name: 'Level', displayName: '层级', typeName: 'Int32' },
    ];
    expect(detectLevelField(fields)).toBe('Level');
    // 名称命中但非数值类型不采用
    expect(detectLevelField([{ name: 'Grade', typeName: 'String' }])).toBe('');
    // 显示名命中（层级/等级/级别）
    expect(
      detectLevelField([{ name: 'T', displayName: '层级', typeName: 'Int32' }]),
    ).toBe('T');
    expect(detectLevelField([])).toBe('');
  });

  it('读取层级：非法/缺失/零按 1（顶层）处理', () => {
    expect(readLevelOfRow({ Level: 3 }, 'Level')).toBe(3);
    expect(readLevelOfRow({ level: 2 }, 'Level')).toBe(2);
    expect(readLevelOfRow({}, 'Level')).toBe(1);
    expect(readLevelOfRow({ Level: 0 }, 'Level')).toBe(1);
  });
});
