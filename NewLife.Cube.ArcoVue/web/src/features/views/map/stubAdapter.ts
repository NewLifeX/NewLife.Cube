/**
 * 离线桩适配（OSC-261004d7f4）：URL 带 __mapStub=1 时启用（E2E / 无 Key 环境）
 *
 * - 不加载真实 SDK；按当前视口把点位线性投影到容器（无瓦片底图）
 * - 维护 `window.__mapStubMarkers`（当前已绘制点）与 `window.__mapStubApi`
 *   （setViewport/pan/getViewport/getMarkerIds）供测试断言
 * - 点击/悬停行为与真实适配层一致，支撑「点击散点打开详情」「悬停卡片」用例
 */
import type { MapAdapter, MapAdapterOptions, MapAdapterPoint } from './mapAdapter';
import { MARKER_SIZE, clampZoom } from './mapAdapter';
import { createMarkerElement } from './mapMarkerDom';
import type { LngLatBounds } from './mapViewport';

type AnySdk = Record<string, any>;

/** 桩默认视口：中国大区 */
const DEFAULT_BOUNDS: LngLatBounds = { minLng: 73, maxLng: 135, minLat: 18, maxLat: 54 };

export function createStubAdapter(opts: MapAdapterOptions = {}): MapAdapter {
  let root: HTMLElement | null = null;
  let bounds: LngLatBounds = { ...DEFAULT_BOUNDS };
  let zoom = clampZoom(opts.zoom ?? 5);
  const points = new Map<string, MapAdapterPoint>();
  const nodes = new Map<string, HTMLElement>();
  const viewportCbs = new Set<() => void>();
  const clickCbs = new Set<(id: string) => void>();
  const mapClickCbs = new Set<(lng: number, lat: number) => void>();
  const hoverCbs = new Set<(id: string | null, x: number, y: number) => void>();

  const project = (lng: number, lat: number): { x: number; y: number } => {
    const w = root?.clientWidth || 800;
    const h = root?.clientHeight || 600;
    return {
      x: ((lng - bounds.minLng) / (bounds.maxLng - bounds.minLng)) * w,
      y: ((bounds.maxLat - lat) / (bounds.maxLat - bounds.minLat)) * h,
    };
  };

  const place = (id: string) => {
    const node = nodes.get(id);
    const p = points.get(id);
    if (!node || !p) return;
    const { x, y } = project(p.lng, p.lat);
    node.style.left = `${x - MARKER_SIZE / 2}px`;
    node.style.top = `${y - MARKER_SIZE / 2}px`;
  };

  const publish = () => {
    (window as AnySdk).__mapStubMarkers = [...points.values()].map((p) => ({
      id: p.id,
      lng: p.lng,
      lat: p.lat,
    }));
  };

  const emitViewport = () => {
    for (const cb of viewportCbs) cb();
  };

  /** 测试控制：设置/更新视口并触发视口变化事件 */
  const setViewport = (next?: Partial<LngLatBounds>) => {
    bounds = { ...bounds, ...(next ?? {}) };
    for (const id of nodes.keys()) place(id);
    publish();
    emitViewport();
  };

  return {
    mount(el) {
      root = document.createElement('div');
      root.className = 'cube-map-stub';
      root.style.position = 'absolute';
      root.style.inset = '0';
      root.style.overflow = 'hidden';
      el.appendChild(root);
      // 空白点击（拾取模式）：按点击像素位置在当前视口内反算经纬度（marker 点击已 stopPropagation）
      root.addEventListener('click', (e) => {
        const rect = root?.getBoundingClientRect();
        if (!rect || !rect.width || !rect.height) return;
        const lng = bounds.minLng + ((e.clientX - rect.left) / rect.width) * (bounds.maxLng - bounds.minLng);
        const lat = bounds.maxLat - ((e.clientY - rect.top) / rect.height) * (bounds.maxLat - bounds.minLat);
        for (const cb of mapClickCbs) cb(lng, lat);
      });
      // 初始中心/缩放与真实适配器语义对齐（视口记忆恢复依赖 center 参数生效）
      if (opts.center) {
        const span = 360 / 2 ** zoom;
        bounds = {
          minLng: Math.max(-180, opts.center[0] - span / 2),
          maxLng: Math.min(180, opts.center[0] + span / 2),
          minLat: Math.max(-90, opts.center[1] - span / 2),
          maxLat: Math.min(90, opts.center[1] + span / 2),
        };
      }
      (window as AnySdk).__mapStubApi = {
        ...((window as AnySdk).__mapStubApi ?? {}),
        getViewport: () => ({ ...bounds }),
        getZoom: () => zoom,
        setViewport,
        pan: (dLng: number, dLat: number) =>
          setViewport({
            minLng: bounds.minLng + dLng,
            maxLng: bounds.maxLng + dLng,
            minLat: bounds.minLat + dLat,
            maxLat: bounds.maxLat + dLat,
          }),
        getMarkerIds: () => [...points.keys()],
      };
      publish();
    },
    destroy() {
      root?.remove();
      root = null;
      nodes.clear();
      points.clear();
      delete (window as AnySdk).__mapStubMarkers;
      delete (window as AnySdk).__mapStubApi;
    },
    zoomIn() {
      zoom = clampZoom(zoom + 1);
      setViewport();
    },
    zoomOut() {
      zoom = clampZoom(zoom - 1);
      setViewport();
    },
    getZoom() {
      return zoom;
    },
    getBounds() {
      return { ...bounds };
    },
    fit(b) {
      if (b) setViewport(b);
    },
    setCenter(lng, lat, z) {
      zoom = clampZoom(z ?? zoom);
      const span = 360 / 2 ** zoom;
      setViewport({
        minLng: Math.max(-180, lng - span / 2),
        maxLng: Math.min(180, lng + span / 2),
        minLat: Math.max(-90, lat - span / 2),
        maxLat: Math.min(90, lat + span / 2),
      });
    },
    setDark(dark) {
      root?.classList.toggle('cube-map-stub--dark', dark);
    },
    setSatellite(on) {
      root?.classList.toggle('cube-map-stub--satellite', on);
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
    addPoints(list: readonly MapAdapterPoint[]) {
      for (const p of list) {
        if (points.has(p.id)) continue;
        points.set(p.id, p);
        const node = createMarkerElement(p.style);
        node.style.position = 'absolute';
        node.style.cursor = 'pointer';
        node.addEventListener('click', (e) => {
          e.stopPropagation();
          for (const cb of clickCbs) cb(p.id);
        });
        const report = (e: MouseEvent) => {
          const rect = root?.getBoundingClientRect();
          if (!rect) return;
          for (const cb of hoverCbs) cb(p.id, e.clientX - rect.left, e.clientY - rect.top);
        };
        node.addEventListener('mouseenter', report);
        node.addEventListener('mousemove', report);
        node.addEventListener('mouseleave', () => {
          for (const cb of hoverCbs) cb(null, 0, 0);
        });
        root?.appendChild(node);
        nodes.set(p.id, node);
        place(p.id);
      }
      publish();
    },
    removePoints(ids: readonly string[]) {
      for (const id of ids) {
        nodes.get(id)?.remove();
        nodes.delete(id);
        points.delete(id);
      }
      publish();
    },
  };
}
