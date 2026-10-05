/**
 * 地图适配层接口（OSC-261004d7f4）
 *
 * 目标：MapView 只依赖本接口，不感知 高德/百度/腾讯 SDK 差异。
 * 坐标约定：进入适配层的经纬度均为「目标底图坐标系」（高德/腾讯 GCJ-02、百度 BD-09），
 * 由 mapPoints.buildMapPoints 完成数据坐标系 → 目标坐标系的换算。
 */
import type { MapMarkerStyle } from './mapPoints';
import type { LngLatBounds } from './mapViewport';

export interface MapAdapterPoint {
  id: string;
  lng: number;
  lat: number;
  style: MapMarkerStyle;
}

export interface MapAdapterOptions {
  /** JS API Key（桩模式忽略） */
  key?: string;
  /** 自定义脚本地址；留空用服务商默认 */
  scriptUrl?: string;
  /** 初始缩放，默认 5 */
  zoom?: number;
  /** 初始中心（目标坐标系），默认中国中部 */
  center?: [number, number];
  /** 深色底图（跟随系统暗色主题；不支持的端降级为标准底图） */
  dark?: boolean;
}

export interface MapAdapter {
  /** 挂载到容器（SDK 初始化同步完成） */
  mount(el: HTMLElement): void;
  destroy(): void;
  zoomIn(): void;
  zoomOut(): void;
  getZoom(): number;
  getBounds(): LngLatBounds | null;
  fit(bounds: LngLatBounds | null): void;
  /** 视口定位到指定中心（可选缩放；缺省保持当前缩放） */
  setCenter(lng: number, lat: number, zoom?: number): void;
  /** 深色底图开关（跟随系统暗色主题；不支持的端降级为标准并保持视图） */
  setDark(dark: boolean): void;
  /** 卫星底图开关（标准↔卫星；不支持的端降级为标准） */
  setSatellite(on: boolean): void;
  /** 视口变化（拖动/缩放结束、程序化跳转）；返回退订函数 */
  onViewportChange(cb: () => void): () => void;
  /** 点位点击；返回退订函数 */
  onClick(cb: (id: string) => void): () => void;
  /** 地图空白处点击（拾取模式用）；lng/lat 为目标底图坐标系；返回退订函数 */
  onMapClick(cb: (lng: number, lat: number) => void): () => void;
  /** 点位悬停/离开（id=null 表示离开）；x/y 为相对地图容器像素；返回退订函数 */
  onPointHover(cb: (id: string | null, x: number, y: number) => void): () => void;
  /** 增量添加点位（幂等：已存在的 id 忽略） */
  addPoints(points: readonly MapAdapterPoint[]): void;
  removePoints(ids: readonly string[]): void;
}

export const ZOOM_MIN = 3;
export const ZOOM_MAX = 18;
/** 标记 DOM 尺寸（px） */
export const MARKER_SIZE = 32;
/** 中国中部（默认视图中心） */
export const CHINA_CENTER: [number, number] = [104.2, 35.9];

export function clampZoom(z: number): number {
  const n = Math.round(Number.isFinite(z) ? z : 5);
  return Math.min(ZOOM_MAX, Math.max(ZOOM_MIN, n));
}

/** 由包围盒推算中心与缩放（fit 的三端统一实现，避免各 SDK 适配差异） */
export function viewForBounds(b: LngLatBounds): { center: [number, number]; zoom: number } {
  const center: [number, number] = [(b.minLng + b.maxLng) / 2, (b.minLat + b.maxLat) / 2];
  const span = Math.max(b.maxLng - b.minLng, (b.maxLat - b.minLat) * 1.4, 0.01);
  return { center, zoom: clampZoom(Math.log2((360 / span) * 0.6)) };
}

/** E2E/离线桩模式：URL 带 __mapStub=1 时启用（不加载真实 SDK、不消耗 Key） */
export function isMapStubMode(search: string): boolean {
  return /(?:^|[?&])__mapStub=1(?:&|$)/.test(search);
}
