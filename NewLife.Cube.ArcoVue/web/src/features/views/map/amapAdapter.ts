/**
 * 高德地图适配（AMap JS API 2.0，脚本 webapi.amap.com/maps?v=2.0）
 *
 * 标记采用 AMap.Marker 的 content 自定义 DOM（IconPark 图标），anchor 居中。
 */
import type { MapAdapter, MapAdapterOptions, MapAdapterPoint } from './mapAdapter';
import { CHINA_CENTER, clampZoom, viewForBounds } from './mapAdapter';
import { createMarkerElement } from './mapMarkerDom';
import { loadMapScript } from './mapScript';

type AnySdk = Record<string, any>;

export async function createAmapAdapter(opts: MapAdapterOptions = {}): Promise<MapAdapter> {
  await loadMapScript('amap', opts.key ?? '', opts.scriptUrl);
  const AMap = (window as AnySdk).AMap;
  if (!AMap) throw new Error('高德地图 SDK 未就绪');

  let map: AnySdk | null = null;
  const markers = new Map<string, AnySdk>();
  /** 卫星图层（卫星= TileLayer.Satellite + RoadNet 叠加；标准=移除） */
  let satLayers: AnySdk[] = [];
  let darkOn = opts.dark === true;
  const viewportCbs = new Set<() => void>();
  const clickCbs = new Set<(id: string) => void>();
  const hoverCbs = new Set<(id: string | null, x: number, y: number) => void>();

  const emitViewport = () => {
    for (const cb of viewportCbs) cb();
  };

  return {
    mount(el) {
      const center = opts.center ?? CHINA_CENTER;
      const m = new AMap.Map(el, {
        viewMode: '2D',
        zoom: clampZoom(opts.zoom ?? 5),
        center,
        resizeEnable: true,
        mapStyle: darkOn ? 'amap://styles/dark' : 'amap://styles/normal',
      });
      map = m;
      m.on('moveend', emitViewport);
      m.on('zoomend', emitViewport);
    },
    destroy() {
      map?.destroy();
      map = null;
      markers.clear();
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
      map.setZoomAndCenter(v.zoom, v.center);
    },
    setCenter(lng, lat, zoom) {
      if (!map) return;
      map.setZoomAndCenter(clampZoom(zoom ?? Number(map.getZoom())), [lng, lat]);
    },
    setDark(dark) {
      darkOn = dark;
      if (!map) return;
      map.setMapStyle(dark ? 'amap://styles/dark' : 'amap://styles/normal');
    },
    setSatellite(on) {
      if (!map) return;
      if (on) {
        if (satLayers.length) return;
        satLayers = [new AMap.TileLayer.Satellite(), new AMap.TileLayer.RoadNet()];
        map.add(satLayers);
      } else if (satLayers.length) {
        map.remove(satLayers);
        satLayers = [];
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
        if (markers.has(p.id)) continue;
        const marker = new AMap.Marker({
          position: [p.lng, p.lat],
          anchor: 'center',
          content: createMarkerElement(p.style),
          extData: { id: p.id },
        });
        marker.on('click', () => {
          for (const cb of clickCbs) cb(p.id);
        });
        const report = (e: AnySdk) => {
          const pixel = e?.pixel ?? {};
          for (const cb of hoverCbs) cb(p.id, Number(pixel.x ?? 0), Number(pixel.y ?? 0));
        };
        marker.on('mouseover', report);
        marker.on('mousemove', report);
        marker.on('mouseout', () => {
          for (const cb of hoverCbs) cb(null, 0, 0);
        });
        map.add(marker);
        markers.set(p.id, marker);
      }
    },
    removePoints(ids: readonly string[]) {
      if (!map) return;
      for (const id of ids) {
        const marker = markers.get(id);
        if (!marker) continue;
        map.remove(marker);
        markers.delete(id);
      }
    },
  };
}
