/**
 * 百度地图适配（BMap JS API 3.0，脚本 api.map.baidu.com/api?v=3.0）
 *
 * 标记采用自定义覆盖物（BMap.Overlay 子类）：initialize 返回 IconPark DOM、
 * draw 时由 pointToOverlayPixel 定位。注意：本适配层坐标系为 BD-09。
 */
import type { MapAdapter, MapAdapterOptions, MapAdapterPoint } from './mapAdapter';
import { CHINA_CENTER, MARKER_SIZE, clampZoom, viewForBounds } from './mapAdapter';
import { createMarkerElement } from './mapMarkerDom';
import type { MapMarkerStyle } from './mapPoints';
import { loadMapScript } from './mapScript';

type AnySdk = Record<string, any>;

export async function createBaiduAdapter(opts: MapAdapterOptions = {}): Promise<MapAdapter> {
  await loadMapScript('baidu', opts.key ?? '', opts.scriptUrl);
  const BMap = (window as AnySdk).BMap;
  if (!BMap) throw new Error('百度地图 SDK 未就绪');

  let map: AnySdk | null = null;
  let host: HTMLElement | null = null;
  const overlays = new Map<string, AnySdk>();
  const viewportCbs = new Set<() => void>();
  const clickCbs = new Set<(id: string) => void>();
  const mapClickCbs = new Set<(lng: number, lat: number) => void>();
  const hoverCbs = new Set<(id: string | null, x: number, y: number) => void>();

  const emitViewport = () => {
    for (const cb of viewportCbs) cb();
  };

  /** 自定义覆盖物：DOM 由 IconPark 渲染，位置由 SDK 调 draw() 定位 */
  class MarkerOverlay extends BMap.Overlay {
    private point: AnySdk;
    private id: string;
    private style: MapMarkerStyle;
    private el: HTMLElement | null = null;

    constructor(point: AnySdk, id: string, style: MapMarkerStyle) {
      super();
      this.point = point;
      this.id = id;
      this.style = style;
    }

    initialize(m: AnySdk) {
      const el = createMarkerElement(this.style);
      el.style.position = 'absolute';
      el.style.cursor = 'pointer';
      el.addEventListener('click', (e) => {
        e.stopPropagation();
        for (const cb of clickCbs) cb(this.id);
      });
      const report = (e: MouseEvent) => {
        const rect = host?.getBoundingClientRect();
        if (!rect) return;
        for (const cb of hoverCbs) cb(this.id, e.clientX - rect.left, e.clientY - rect.top);
      };
      el.addEventListener('mouseenter', report);
      el.addEventListener('mousemove', report);
      el.addEventListener('mouseleave', () => {
        for (const cb of hoverCbs) cb(null, 0, 0);
      });
      this.el = el;
      m.getPanes().markerPane.appendChild(el);
      return el;
    }

    draw() {
      if (!this.el || !map) return;
      const pixel = map.pointToOverlayPixel(this.point);
      this.el.style.left = `${Number(pixel.x) - MARKER_SIZE / 2}px`;
      this.el.style.top = `${Number(pixel.y) - MARKER_SIZE / 2}px`;
    }

    remove() {
      this.el?.remove();
      this.el = null;
    }
  }

  return {
    mount(el) {
      host = el;
      const center = opts.center ?? CHINA_CENTER;
      const m = new BMap.Map(el);
      map = m;
      m.centerAndZoom(new BMap.Point(center[0], center[1]), clampZoom(opts.zoom ?? 5));
      m.enableScrollWheelZoom(true);
      m.addEventListener('moveend', emitViewport);
      m.addEventListener('zoomend', emitViewport);
      // 空白点击（拾取模式）：overlay 点击已 stopPropagation，不会冒泡到此
      m.addEventListener('click', (e: AnySdk) => {
        const pt = e?.point ?? e?.latlng;
        const lng = Number(pt?.lng ?? pt?.getLng?.());
        const lat = Number(pt?.lat ?? pt?.getLat?.());
        if (!Number.isFinite(lng) || !Number.isFinite(lat)) return;
        for (const cb of mapClickCbs) cb(lng, lat);
      });
    },
    destroy() {
      map?.clearOverlays();
      map = null;
      host = null;
      overlays.clear();
    },
    zoomIn() {
      if (map) map.setZoom(clampZoom(Number(map.getZoom()) + 1));
    },
    zoomOut() {
      if (map) map.setZoom(clampZoom(Number(map.getZoom()) - 1));
    },
    getZoom() {
      return map ? Number(map.getZoom()) : 0;
    },
    getBounds() {
      if (!map) return null;
      const b = map.getBounds();
      if (!b) return null;
      const sw = b.getSouthWest();
      const ne = b.getNorthEast();
      return {
        minLng: Number(sw.lng),
        minLat: Number(sw.lat),
        maxLng: Number(ne.lng),
        maxLat: Number(ne.lat),
      };
    },
    fit(bounds) {
      if (!map || !bounds) return;
      const v = viewForBounds(bounds);
      map.setCenter(new BMap.Point(v.center[0], v.center[1]));
      map.setZoom(v.zoom);
    },
    setCenter(lng, lat, zoom) {
      if (!map) return;
      map.setCenter(new BMap.Point(lng, lat));
      map.setZoom(clampZoom(zoom ?? Number(map.getZoom())));
    },
    setDark() {
      // BMap 3.0 无内置暗色底图（setMapStyleV2 需完整 styleJson）；降级保持标准底图
    },
    setSatellite(on) {
      if (!map) return;
      // BMap 的地图类型常量为全局字符串（normal/satellite），缺失时用字符串兜底
      const w = window as AnySdk;
      const normal = w.BMAP_NORMAL_MAP ?? 'normal';
      const satellite = w.BMAP_SATELLITE_MAP ?? 'satellite';
      map.setMapType(on ? satellite : normal);
    },
    onViewportChange(cb) {
      viewportCbs.add(cb);
      return () => viewportCbs.delete(cb);
    },
    onClick(cb) {
      clickCbs.add(cb);
      return () => clickCbs.delete(cb);
    },
    onMapClick(cb) {
      mapClickCbs.add(cb);
      return () => mapClickCbs.delete(cb);
    },
    onPointHover(cb) {
      hoverCbs.add(cb);
      return () => hoverCbs.delete(cb);
    },
    addPoints(points: readonly MapAdapterPoint[]) {
      if (!map) return;
      for (const p of points) {
        if (overlays.has(p.id)) continue;
        const overlay = new MarkerOverlay(new BMap.Point(p.lng, p.lat), p.id, p.style);
        map.addOverlay(overlay);
        overlays.set(p.id, overlay);
      }
    },
    removePoints(ids: readonly string[]) {
      if (!map) return;
      for (const id of ids) {
        const overlay = overlays.get(id);
        if (!overlay) continue;
        map.removeOverlay(overlay);
        overlays.delete(id);
      }
    },
  };
}
