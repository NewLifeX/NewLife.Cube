/**
 * 地区地图模式（对齐 MVC Map.cshtml）
 *
 * 用 ECharts 中国地图渲染全国散点：省级（普通散点）、直辖市（涟漪 effectScatter）、
 * 城市（小散点）。数据来自后端 /api/Cube/Area/Map（省级 + 有经纬度城市）。
 * 中国地图 GeoJSON 为本地离线资源（src/assets/map/china.json），无需外网地图服务。
 */
import { useEffect, useRef, useState } from 'react';
import { Card, Spin } from 'antd';
import * as echarts from 'echarts';
import { api } from '@/api';
import chinaJson from '@/assets/map/china.json';

/** 地区点（后端 Map 接口返回） */
export interface AreaPoint {
  name: string;
  longitude: number;
  latitude: number;
  kind?: string;
}

/** 地图数据（后端 Map 接口返回） */
export interface MapData {
  provinces?: AreaPoint[];
  cities?: AreaPoint[];
}

/** 经纬度点 → ECharts 散点数据 */
export function toScatter(points: AreaPoint[]): Array<{ name: string; value: [number, number] }> {
  return (points ?? []).map((p) => ({ name: p.name, value: [p.longitude, p.latitude] as [number, number] }));
}

/** 拆分直辖市（涟漪）与普通省份 */
export function splitDirect(points: AreaPoint[]): { normal: AreaPoint[]; direct: AreaPoint[] } {
  const list = points ?? [];
  return {
    normal: list.filter((p) => p.kind !== '直辖市'),
    direct: list.filter((p) => p.kind === '直辖市'),
  };
}

/** 构建 ECharts 配置（纯函数，供单测） */
export function buildOption(data: MapData): echarts.EChartsOption {
  const provs = data.provinces ?? [];
  const cities = data.cities ?? [];
  const { normal, direct } = splitDirect(provs);

  return {
    tooltip: { trigger: 'item' },
    geo: {
      map: 'china',
      roam: true,
      zoom: 1.2,
      itemStyle: { areaColor: '#eef1f6', borderColor: '#c9d2e0' },
      emphasis: { itemStyle: { areaColor: '#dbe6f8' }, label: { show: false } },
    },
    series: [
      {
        name: '省份',
        type: 'scatter',
        coordinateSystem: 'geo',
        data: toScatter(normal),
        symbolSize: 11,
        label: { show: true, formatter: '{b}', position: 'right', fontSize: 10 },
        itemStyle: { color: '#3b7cf6' },
      },
      {
        name: '城市',
        type: 'scatter',
        coordinateSystem: 'geo',
        data: toScatter(cities),
        symbolSize: 4,
        itemStyle: { color: '#9ab7f8' },
      },
      {
        name: '直辖市',
        type: 'effectScatter',
        coordinateSystem: 'geo',
        data: toScatter(direct),
        symbolSize: 15,
        rippleEffect: { brushType: 'stroke' },
        label: { show: true, formatter: '{b}', position: 'right' },
        itemStyle: { color: '#f56c6c', shadowBlur: 10, shadowColor: '#333' },
        zlevel: 1,
      },
    ],
  };
}

/** 地图模式组件 */
export default function AreaMap({ type }: { type: string }) {
  const ref = useRef<HTMLDivElement>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let chart: echarts.ECharts | null = null;
    let cancelled = false;

    const load = async () => {
      try {
        echarts.registerMap('china', chinaJson as unknown as Parameters<typeof echarts.registerMap>[1]);
        const res = await api.client.get(`${type}/Map`);
        if (cancelled || !ref.current) return;
        chart ??= echarts.init(ref.current);
        chart.setOption(buildOption(res.data?.data ?? {}));
      } catch {
        // 数据加载失败时地图留空，不打断页面
      } finally {
        if (!cancelled) setLoading(false);
      }
    };
    void load();

    const onResize = () => chart?.resize();
    window.addEventListener('resize', onResize);
    return () => {
      cancelled = true;
      window.removeEventListener('resize', onResize);
      chart?.dispose();
    };
  }, [type]);

  return (
    <Card size="small">
      <Spin spinning={loading}>
        <div ref={ref} style={{ height: 560, width: '100%' }} data-testid="area-map" />
      </Spin>
    </Card>
  );
}
