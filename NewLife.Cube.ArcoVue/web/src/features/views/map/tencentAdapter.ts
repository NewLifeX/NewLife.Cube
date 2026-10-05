/**
 * 腾讯地图适配（TMap GL JS API v1.exp，脚本 map.qq.com/api/gljs）
 *
 * 标记采用 TMap.DOMOverlay 自定义 DOM（IconPark 图标）。腾讯 GL 无官方 Marker content DOM，
 * DOMOverlay 是官方推荐的自定义覆盖物方式；若 SDK 缺少 DOMOverlay 则初始化报错（由 MapView 呈现）。
 */
import type { MapAdapter, MapAdapterOptions, MapAdapterPoint } from './mapAdapter';
import { CHINA_CENTER, MARKER_SIZE, clampZoom, viewForBounds } from './mapAdapter';
import { createMarkerElement } from './mapMarkerDom';
import type { MapMarkerStyle } from './mapPoints';
import { loadMapScript } from './mapScript';

type AnySdk = Record<string, any>;

export async function createTencentAdapter(opts: MapAdapterOptions = {}): Promise<MapAdapter> {
  await loadMapScript('tencent', opts.key ?? '', opts.scriptUrl);
  const TMap = (window as AnySdk).TMap;
  if (!TMap) throw new Error('腾讯地图 SDK 未就绪');
  const DOMOverlay = TMap.DOMOverlay;
  if (!DOMOverlay) throw new Error('腾讯地图 SDK 缺少 DOMOverlay 支持');

  let map: AnySdk | null = null;
  let host: HTMLElement | null = null;
  const overlays = new Map<string, AnySdk>();
  const viewportCbs = new Set<() => void>();
  const clickCbs = new Set<(id: string) => void>();
  const hoverCbs = new Set<(id: string | null, x: number, y: number) => void>();

  const emitViewport = () => {
    for (const cb of viewportCbs) cb();
  };

  class CubeMarker extends DOMOverlay {
    private id: string;
    private style: MapMarkerStyle;

    constructor(options: AnySdk) {
      super(options);
      this.id = options.id;
      this.style = options.style;
      this.position = options.position;
    }

    onInit(options: AnySdk) {
      this.position = options.position;
      const el = createMarkerElement(this.style);
      el.style.marginLeft = `${-MARKER_SIZE / 2}px`;
      el.style.marginTop = `${-MARKER_SIZE / 2}px`;
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
      // DOMOverlay 基类以 this.dom 作为定位元素
      this.dom = el;
    }

    update(options: AnySdk) {
      if (options?.position) this.position = options.position;
    }
  }

  return {
    mount(el) {
      host = el;
      const center = opts.center ?? CHINA_CENTER;
      const m = new TMap.Map(el, {
        center: new TMap.LatLng(center[1], center[0]),
        zoom: clampZoom(opts.zoom ?? 5),
        pitch: 0,
        rotation: 0,
      });
      map = m;
      // 事件名兼容：不同小版本可能只有其中一种；未知事件名仅订阅不触发
      m.on('moveend', emitViewport);
      m.on('zoomend', emitViewport);
      m.on('idle', emitViewport);
    },
    destroy() {
      map?.destroy();
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
        minLng: Number(sw.getLng()),
        minLat: Number(sw.getLat()),
        maxLng: Number(ne.getLng()),
        maxLat: Number(ne.getLat()),
      };
    },
    fit(bounds) {
      if (!map || !bounds) return;
      const v = viewForBounds(bounds);
      map.setCenter(new TMap.LatLng(v.center[1], v.center[0]));
      map.setZoom(v.zoom);
    },
    setCenter(lng, lat, zoom) {
      if (!map) return;
      map.setCenter(new TMap.LatLng(lat, lng));
      map.setZoom(clampZoom(zoom ?? Number(map.getZoom())));
    },
    setDark() {
      // 腾讯 JS API 无内置暗色底图（需自定义样式）；降级保持标准底图
    },
    setSatellite(on) {
      if (!map) return;
      try {
        map.setBaseMap({ type: on ? 'satellite' : 'vector' });
      } catch {
        // 低版本 SDK 不支持底图类型切换：降级保持标准底图
      }
    },
    onViewportChange(cb) {
      viewportCbs.add(cb);
      return () => viewportCbs.delete(cb);
    },
    onClick(cb) {
      clickCbs.add(cb);
      return () => clickCbs.delete(cb);
    },
    onPointHover(cb) {
      hoverCbs.add(cb);
      return () => hoverCbs.delete(cb);
    },
    addPoints(points: readonly MapAdapterPoint[]) {
      if (!map) return;
      for (const p of points) {
        if (overlays.has(p.id)) continue;
        const overlay = new CubeMarker({
          map,
          position: new TMap.LatLng(p.lat, p.lng),
          id: p.id,
          style: p.style,
          zIndex: 120,
        });
        overlays.set(p.id, overlay);
      }
    },
    removePoints(ids: readonly string[]) {
      for (const id of ids) {
        const overlay = overlays.get(id);
        if (!overlay) continue;
        overlay.setMap(null);
        overlays.delete(id);
      }
    },
  };
}
