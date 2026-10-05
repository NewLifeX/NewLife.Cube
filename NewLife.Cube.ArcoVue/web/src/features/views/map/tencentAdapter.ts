/**
 * 腾讯地图适配（TMap GL JS API v1.exp，脚本 map.qq.com/api/gljs）
 *
 * 标记采用 TMap.DOMOverlay 自定义 DOM（IconPark 图标）。腾讯 GL 无 Marker content DOM，
 * DOMOverlay 是官方推荐的自定义覆盖物方式。
 *
 * SDK 内部契约（v1.exp 源码核对）：基类构造依次调用 `onInit(options)` 与 `this.dom = this.createDOM()`——
 * onInit 中直接赋值的 this.dom 会被 createDOM 的返回值覆盖，自定义 DOM 必须由 createDOM() 返回；
 * 定位钩子 updateDOM() 基类为空实现，必须子类实现（用 projectToContainer 换算容器像素）。
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
  const mapClickCbs = new Set<(lng: number, lat: number) => void>();
  const hoverCbs = new Set<(id: string | null, x: number, y: number) => void>();

  const emitViewport = () => {
    for (const cb of viewportCbs) cb();
  };

  class CubeMarker extends DOMOverlay {
    // 字段值首次由 super() 内部的 onInit 写入，构造体再赋一次（useDefineForClassFields 会把声明字段重置为 undefined）
    private id!: string;
    private markerStyle!: MapMarkerStyle;

    constructor(options: AnySdk) {
      super(options);
      this.id = options.id;
      this.markerStyle = options.style;
    }

    onInit(options: AnySdk) {
      // super() 内部先于 createDOM 回调：构造参数转存实例字段供 createDOM 使用
      this.position = options.position;
      this.id = options.id;
      this.markerStyle = options.style;
    }

    createDOM() {
      const el = createMarkerElement(this.markerStyle);
      el.style.position = 'absolute';
      el.style.left = '0';
      el.style.top = '0';
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
      return el;
    }

    updateDOM() {
      const m = this.map;
      if (!m || !this.dom || !this.position) return;
      const p = m.projectToContainer(this.position);
      const x = Number(p?.x ?? p?.getX?.());
      const y = Number(p?.y ?? p?.getY?.());
      if (!Number.isFinite(x) || !Number.isFinite(y)) return;
      // 元素中心对齐经纬度点（对标高德 offset / 百度 anchor 的居中语义）
      this.dom.style.transform = `translate(${x - MARKER_SIZE / 2}px, ${y - MARKER_SIZE / 2}px)`;
    }

    update(options: AnySdk) {
      if (options?.position) this.position = options.position;
      this.updateDOM();
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
        // 关闭腾讯默认控件（缩放按钮 TOP_RIGHT / 比例尺 BOTTOM_LEFT）：项目自绘工具栏缩放键与实时比例尺，避免重复
        showControl: false,
      });
      map = m;
      // 事件名兼容：不同小版本可能只有其中一种；未知事件名仅订阅不触发
      m.on('moveend', emitViewport);
      m.on('zoomend', emitViewport);
      m.on('idle', emitViewport);
      // 空白点击（拾取模式）：DOMOverlay 点击已 stopPropagation，不会冒泡到此
      m.on('click', (evt: AnySdk) => {
        const lng = Number(evt?.latLng?.getLng?.());
        const lat = Number(evt?.latLng?.getLat?.());
        if (!Number.isFinite(lng) || !Number.isFinite(lat)) return;
        for (const cb of mapClickCbs) cb(lng, lat);
      });
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
