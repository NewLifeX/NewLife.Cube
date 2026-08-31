/**
 * 地区地图模式单测：数据转换纯函数 + 配置构建
 */
import { describe, expect, it } from 'vitest';
import { buildOption, splitDirect, toScatter, type AreaPoint, type MapData } from '../AreaMap';

describe('地区地图模式', () => {
  const beijing: AreaPoint = { name: '北京', longitude: 116.4, latitude: 39.9, kind: '直辖市' };
  const guangdong: AreaPoint = { name: '广东', longitude: 113.3, latitude: 23.1, kind: '省' };
  const shenzhen: AreaPoint = { name: '深圳', longitude: 114.06, latitude: 22.55 };

  it('toScatter 转换经纬度点', () => {
    const out = toScatter([beijing, shenzhen]);
    expect(out).toEqual([
      { name: '北京', value: [116.4, 39.9] },
      { name: '深圳', value: [114.06, 22.55] },
    ]);
  });

  it('toScatter 空数组安全', () => {
    expect(toScatter([])).toEqual([]);
    expect(toScatter(undefined as unknown as AreaPoint[])).toEqual([]);
  });

  it('splitDirect 拆分直辖市与普通省份', () => {
    const { normal, direct } = splitDirect([beijing, guangdong]);
    expect(normal.map((p) => p.name)).toEqual(['广东']);
    expect(direct.map((p) => p.name)).toEqual(['北京']);
  });

  it('buildOption 生成中国地图 geo + 三类散点', () => {
    const data: MapData = { provinces: [beijing, guangdong], cities: [shenzhen] };
    const option = buildOption(data) as Record<string, unknown>;
    const series = option.series as Array<Record<string, unknown>>;

    // geo 组件使用 china 地图
    expect((option.geo as Record<string, unknown>).map).toBe('china');
    // 三类系列：省份 scatter / 城市 scatter / 直辖市 effectScatter
    expect(series).toHaveLength(3);
    expect(series[0]).toMatchObject({ name: '省份', type: 'scatter', coordinateSystem: 'geo' });
    expect((series[0].data as unknown[]).length).toBe(1); // 仅广东（北京是直辖市）
    expect(series[1]).toMatchObject({ name: '城市', type: 'scatter' });
    expect((series[1].data as unknown[]).length).toBe(1);
    expect(series[2]).toMatchObject({ name: '直辖市', type: 'effectScatter' });
    expect((series[2].data as unknown[]).length).toBe(1);
  });

  it('buildOption 无数据时三类系列为空', () => {
    const option = buildOption({}) as Record<string, unknown>;
    const series = option.series as Array<{ data: unknown[] }>;
    expect(series.every((s) => s.data.length === 0)).toBe(true);
  });
});
