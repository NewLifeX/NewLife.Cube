/**
 * 地图视图数据管线（OSC-261004d7f4）
 *
 * - 视口渐进加载：首批 1000 条（父级列表请求）→ 视口优先绘制（≤300/帧）
 * - 异步续页：默认 1000/批，串行直到总行数或 maxPoints 上限；查询变更中止并重置
 * - 拖动/缩放（≥150ms 节流）增量补绘；已绘制 >3000 时回收视口 2 倍范围外的点
 * - 逐层加载（地区实体）：检测到数值层级字段时，按缩放只拉取/绘制所需层级（省→市→区县→街道）
 * - 悬停卡片：150ms 显示 / 80ms 隐藏延迟；点击点位回调详情
 */
import { onBeforeUnmount, onMounted, ref, watch, type Ref } from 'vue';
import { useAppStore } from '@/stores/app';
import type { FieldMeta } from '@/core/types/field';
import { detectCoordinateFields, type CoordDetection } from '@/core/utils/mapCoord';
import type { MapProviderKind } from '@/core/utils/mapConfig';
import { getValueByKey } from '@/core/utils/url';
import type { MapMapping } from '@/core/utils/viewMapping';
import type { ViewFormatRule } from '@/core/utils/viewProfile';
import type { MapAdapter, MapAdapterPoint } from './mapAdapter';
import { clampZoom, isMapStubMode } from './mapAdapter';
import { createAmapAdapter } from './amapAdapter';
import { createBaiduAdapter } from './baiduAdapter';
import {
  buildCategoryContext,
  buildMapPoints,
  detectLevelField,
  readLevelOfRow,
  resolveMarkerStyle,
  targetSystemOf,
  type CategoryContext,
  type MapMarkerStyle,
  type MapPoint,
} from './mapPoints';
import { boundsOf, expandBounds, inBounds, pickViewportPoints } from './mapViewport';
import { createStubAdapter } from './stubAdapter';
import { createTencentAdapter } from './tencentAdapter';

export interface UseMapViewOptions {
  /** 地图容器元素 */
  container: Ref<HTMLElement | null>;
  /** 已加载行（父级首批数据） */
  records: () => readonly Record<string, unknown>[];
  /** 字段元数据（坐标检测回落 + 分类 dataSource） */
  fields: () => readonly FieldMeta[];
  mapping: () => MapMapping | null;
  rowKey: () => string;
  formatRules: () => readonly ViewFormatRule[];
  /** 后端总行数（0/未知则不再续页） */
  total: () => number;
  /** 续页加载（pageIndex 为 1 基；首批由父级列表提供，续页从 2 起；返回该页行；null=无更多） */
  loadPage?: (pageIndex: number) => Promise<readonly Record<string, unknown>[] | null>;
  /**
   * 分层加载（地区等层级实体）：按层级字段过滤的续页；level 从 1 起（1=省/自治区/直辖市/港澳台）；
   * pageIndex 为 1 基（从 1 开始逐页）。返回该页行；null=无更多。仅在自动检测到层级字段时使用。
   */
  loadLevelPage?: (
    levelField: string,
    level: number,
    pageIndex: number,
  ) => Promise<readonly Record<string, unknown>[] | null>;
  /** 父级列表算得的高度（px；>0 时优先于自测，保证与列表视图同款且不出现竖直滚动条） */
  height?: () => number;
  /** 定位查询（地图中心）：按标题字段搜索一行；未命中返回 null */
  loadLocateRow?: (field: string, value: string) => Promise<Record<string, unknown> | null>;
  /** 强制桩模式（E2E 注入；默认按 URL __mapStub=1） */
  stub?: () => boolean;
  onDetail: (row: Record<string, unknown>) => void;
}

const VIEWPORT_THROTTLE = 150;
const DRAW_CHUNK = 300;
const RECYCLE_THRESHOLD = 3000;
const HOVER_SHOW_DELAY = 150;
const HOVER_HIDE_DELAY = 80;
const DEFAULT_ICON = 'local';
const DEFAULT_COLOR = '#165DFF';
/** 分层加载每页行数（与父级列表请求 pageSize 一致） */
const LEVEL_PAGE_SIZE = 1000;
/** 显示第 N 层所需最小缩放：1=省(任意缩放)、2=市(≥7)、3=区县(≥9)、4=街道(≥11) */
const LEVEL_ZOOM_STEPS = [0, 7, 9, 11];
/** 「定位」按钮跳转缩放（区县/街镇级可见） */
const LOCATE_ZOOM = 10;
/** 赤道每像素米数（Web 墨卡托 zoom0，256px 瓦片） */
const MEAN_M_PER_PX_EQ = 156543.03392;
/** 比例尺目标条宽（px）与可取整距离（米） */
const SCALE_TARGET_W = 80;
const SCALE_STEPS = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000, 100000];
/** 自适应高度：视口下缘余量 / 最小 / 最大 */
const HEIGHT_GAP = 16;
const HEIGHT_MIN = 420;
const HEIGHT_MAX = 1600;

type AdapterSource = '' | 'stub' | MapProviderKind;

export function useMapView(opts: UseMapViewOptions) {
  const appStore = useAppStore();
  const ready = ref(false);
  const error = ref('');
  const initializing = ref(true);
  const zoom = ref(5);
  const drawnCount = ref(0);
  const loadedCount = ref(0);
  /** 坐标无效/缺失被跳过的行数（状态条展示） */
  const skippedCount = ref(0);
  const loadingMore = ref(false);
  const hover = ref<{ point: MapPoint; x: number; y: number } | null>(null);
  /** 容器尺寸（悬停卡片边缘翻转用） */
  const size = ref({ w: 0, h: 0 });
  /** 自适应高度（px；0=未测量，先用 CSS 默认） */
  const height = ref(0);
  /** 检测到的层级字段（空=常规模式） */
  const levelField = ref('');
  /** 当前缩放允许显示的最大层级（分层模式） */
  const activeLevel = ref(1);
  /** 已加载层级（分层模式，状态条展示） */
  const loadedLevels = ref<number[]>([]);
  /** 分层加载进行中（状态条展示） */
  const levelBusy = ref(false);
  /** 卫星底图开关（会话级；不影响数据管线） */
  const satellite = ref(false);
  /** 比例尺：条宽（px）与文案（如 20 公里） */
  const scaleWidth = ref(0);
  const scaleText = ref('');
  const centerLat = ref(35);

  let adapter: MapAdapter | null = null;
  let adapterSource: AdapterSource = '';
  let detachViewport: (() => void) | null = null;
  let detachClick: (() => void) | null = null;
  let detachHover: (() => void) | null = null;

  let detection: CoordDetection | null = null;
  let allRows: readonly Record<string, unknown>[] = [];
  /** 分层模式已加载行（各层汇总；常规模式为空） */
  let levelRows: readonly Record<string, unknown>[] = [];
  let levelFieldName = '';
  let levelOfId: Map<string, number> = new Map();
  let pointsByLevel: Map<number, MapPoint[]> = new Map();
  let levelLoading = new Set<number>();
  let levelSyncTask: Promise<void> | null = null;
  let visibleCache: MapPoint[] | null = null;
  let visibleCacheKey = '';
  let pointsVersion = 0;
  let points: MapPoint[] = [];
  let styles = new Map<string, MapMarkerStyle>();
  let pointById = new Map<string, MapPoint>();
  let drawnIds = new Set<string>();
  let generation = 0;
  let nextPage = 1;
  let disposed = false;
  let fitted = false;

  let viewportTimer = 0;
  let pumpHandle = 0;
  let showTimer = 0;
  let hideTimer = 0;
  let hoveredId: string | null = null;
  let mounted = false;
  let observer: ResizeObserver | null = null;
  /** 系统暗色主题监听（body[arco-theme] 变化→底图样式跟随） */
  let themeObserver: MutationObserver | null = null;

  /** 当前是否暗色主题 */
  function isDarkTheme(): boolean {
    return typeof document !== 'undefined' && document.body?.getAttribute('arco-theme') === 'dark';
  }

  /** 比例尺：按中心纬度与缩放算“目标宽度对应的整距离”，条宽取整到 1/2/5 序列 */
  function updateScale() {
    const z = zoom.value;
    const mPerPx = (MEAN_M_PER_PX_EQ * Math.cos((centerLat.value * Math.PI) / 180)) / 2 ** z;
    if (!Number.isFinite(mPerPx) || mPerPx <= 0) {
      scaleText.value = '';
      scaleWidth.value = 0;
      return;
    }
    const raw = mPerPx * SCALE_TARGET_W;
    let step = SCALE_STEPS[0];
    for (const s of SCALE_STEPS) {
      if (s <= raw) step = s;
      else break;
    }
    scaleWidth.value = Math.max(40, Math.min(120, Math.round(step / mPerPx)));
    scaleText.value = step >= 1000 ? `${step / 1000} 公里` : `${step} 米`;
  }

  /** 凭当前适配器视口更新中心纬度与比例尺 */
  function syncScale() {
    const b = adapter?.getBounds();
    if (b) centerLat.value = (b.minLat + b.maxLat) / 2;
    updateScale();
  }

  /** 卫星底图切换（标准↔卫星，会话级）；返回切换后的状态 */
  function toggleSatellite(): boolean {
    satellite.value = !satellite.value;
    adapter?.setSatellite(satellite.value);
    return satellite.value;
  }

  function sourceOf(): AdapterSource {
    if (opts.stub?.() ?? false) return 'stub';
    if (isMapStubMode(window.location.search)) return 'stub';
    return appStore.mapConfig.provider ?? '';
  }

  function maxPoints(): number {
    const m = opts.mapping();
    return m?.maxPoints && m.maxPoints > 0 ? m.maxPoints : 1000000;
  }

  /** 当前缩放可显示的最大层级（分层模式） */
  function maxLevelForZoom(z: number): number {
    let lv = 1;
    for (let i = 1; i < LEVEL_ZOOM_STEPS.length; i++) {
      if (z >= LEVEL_ZOOM_STEPS[i]) lv = i + 1;
    }
    return lv;
  }

  /** 分层模式下当前可绘制的点集（≤ activeLevel；缓存避免每帧拼接） */
  function visiblePoints(): MapPoint[] {
    if (!levelFieldName) return points;
    const key = `${activeLevel.value}:${pointsVersion}`;
    if (visibleCache && visibleCacheKey === key) return visibleCache;
    let out: MapPoint[];
    if (activeLevel.value <= 1) {
      out = pointsByLevel.get(1) ?? [];
    } else {
      out = [];
      for (let lv = 1; lv <= activeLevel.value; lv++) {
        const arr = pointsByLevel.get(lv);
        if (arr?.length) out.push(...arr);
      }
    }
    visibleCache = out;
    visibleCacheKey = key;
    return out;
  }

  /** 索引点位层级（分层模式回收与过滤用） */
  function indexPoint(p: MapPoint) {
    if (!levelFieldName) return;
    const lv = readLevelOfRow(p.row, levelFieldName);
    levelOfId.set(p.id, lv);
    let arr = pointsByLevel.get(lv);
    if (!arr) {
      arr = [];
      pointsByLevel.set(lv, arr);
    }
    arr.push(p);
  }

  /** 自适应高度：优先父级列表测高（同款不滚）；缺失时按容器顶部到视口底部自测 */
  function measureHeight() {
    const fixed = Number(opts.height?.() ?? 0);
    if (Number.isFinite(fixed) && fixed > 0) {
      height.value = fixed;
      return;
    }
    const el = opts.container.value;
    if (!el || !el.isConnected) return;
    const top = el.getBoundingClientRect().top;
    if (!Number.isFinite(top)) return;
    const avail = Math.round(window.innerHeight - top - HEIGHT_GAP);
    height.value = Math.max(HEIGHT_MIN, Math.min(avail, HEIGHT_MAX));
  }

  function expectedTotal(): number {
    const t = Number(opts.total?.() ?? 0);
    return Number.isFinite(t) && t > 0 ? t : points.length;
  }

  function detectionOf(): CoordDetection | null {
    const m = opts.mapping();
    if (m?.coordMode === 'latlng' && m.lngField && m.latField) {
      return { mode: 'latlng', lngField: m.lngField, latField: m.latField };
    }
    if (m?.coordMode === 'merged' && m.coordField) {
      return { mode: 'merged', coordField: m.coordField };
    }
    return detectCoordinateFields([...opts.fields()]);
  }

  function categoryCtx(): CategoryContext | null {
    const m = opts.mapping();
    if (!m) return null;
    const kindField = m.categoryField
      ? opts.fields().find((f) => f.name === m.categoryField)
      : undefined;
    return buildCategoryContext(m, kindField);
  }

  /** 全量重建点集（查询变更/视图重建时） */
  function rebuildPoints() {
    const m = opts.mapping();
    detection = detectionOf();
    if (!m || !detection) {
      points = [];
      styles = new Map();
      pointById = new Map();
      loadedCount.value = 0;
      return;
    }
    // 分层模式忽略父级首批行，改由各层级按需拉取（loadLevel）
    const sourceRows = levelFieldName ? levelRows : allRows;
    const built = buildMapPoints(sourceRows, detection, {
      coordOrder: m.coordOrder,
      coordSystem: m.coordSystem,
      targetSystem: targetSystemOf(sourceOf() === 'baidu' ? 'baidu' : 'amap'),
      rowKey: opts.rowKey(),
    });
    const ctx = categoryCtx();
    const rules = opts.formatRules().filter((r) => r.apply === 'cell');
    points = built.points;
    styles = new Map();
    pointById = new Map();
    levelOfId = new Map();
    pointsByLevel = new Map();
    for (const p of points) {
      if (ctx) styles.set(p.id, resolveMarkerStyle(p.row, m, ctx, rules));
      pointById.set(p.id, p);
      indexPoint(p);
    }
    visibleCache = null;
    pointsVersion++;
    skippedCount.value = built.skipped;
    loadedCount.value = points.length;
  }

  /** 追加续页行（增量构建，避免全量重算） */
  function appendRows(rows: readonly Record<string, unknown>[]) {
    const m = opts.mapping();
    if (!m || !detection) return;
    if (levelFieldName) levelRows = [...levelRows, ...rows];
    else allRows = [...allRows, ...rows];
    const built = buildMapPoints(rows, detection, {
      coordOrder: m.coordOrder,
      coordSystem: m.coordSystem,
      targetSystem: targetSystemOf(sourceOf() === 'baidu' ? 'baidu' : 'amap'),
      rowKey: opts.rowKey(),
    });
    const ctx = categoryCtx();
    const rules = opts.formatRules().filter((r) => r.apply === 'cell');
    for (const p of built.points) {
      // 同一 id 已存在则跳过（分层重试/续页重叠时去重，保证计数与内存正确）
      if (pointById.has(p.id)) continue;
      points.push(p);
      if (ctx) styles.set(p.id, resolveMarkerStyle(p.row, m, ctx, rules));
      pointById.set(p.id, p);
      indexPoint(p);
    }
    visibleCache = null;
    pointsVersion++;
    skippedCount.value += built.skipped;
    loadedCount.value = points.length;
  }

  function adapterPoint(p: MapPoint): MapAdapterPoint {
    return {
      id: p.id,
      lng: p.lng,
      lat: p.lat,
      style: styles.get(p.id) ?? { icon: DEFAULT_ICON, color: DEFAULT_COLOR },
    };
  }

  function schedulePump() {
    if (pumpHandle || disposed || !ready.value) return;
    pumpHandle = window.requestAnimationFrame(pumpStep);
  }

  function pumpStep() {
    pumpHandle = 0;
    if (disposed || !adapter) return;
    const viewport = adapter.getBounds();
    if (!viewport) return;
    const padded = expandBounds(viewport, 0.2);
    const candidates = pickViewportPoints(visiblePoints(), drawnIds, padded, DRAW_CHUNK);
    if (candidates.length) {
      adapter.addPoints(candidates.map(adapterPoint));
      const before = drawnCount.value;
      for (const p of candidates) drawnIds.add(p.id);
      drawnCount.value = drawnIds.size;
      // 每跨过千级阈值做一次回收尝试，避免每帧全量扫描
      if (Math.floor(drawnCount.value / 1000) > Math.floor(before / 1000)) recycle();
      schedulePump();
      return;
    }
    recycle();
    maybeFitInitial();
    if (levelFieldName) {
      void syncLevels();
    } else if (loadedCount.value < Math.min(expectedTotal(), maxPoints())) {
      void loadAll();
    }
  }

  /** 回收已绘制点：分层模式隐藏超过当前层级的点；常规模式回收视口 2 倍范围外（仅当 >3000） */
  function recycle() {
    if (!adapter) return;
    const viewport = adapter.getBounds();
    if (!viewport) return;
    const keep = expandBounds(viewport, 1);
    const maxLv = levelFieldName ? activeLevel.value : 0;
    const recycleOut = drawnIds.size > RECYCLE_THRESHOLD;
    const drop: string[] = [];
    for (const id of drawnIds) {
      const p = pointById.get(id);
      if (!p) {
        drop.push(id);
        continue;
      }
      if (maxLv && (levelOfId.get(id) ?? 1) > maxLv) {
        drop.push(id);
        continue;
      }
      if (recycleOut && !inBounds(p, keep)) drop.push(id);
    }
    if (!drop.length) return;
    adapter.removePoints(drop);
    for (const id of drop) drawnIds.delete(id);
    drawnCount.value = drawnIds.size;
  }

  /** 无默认中心时，首批绘制完成后 fit 到点集范围；配置地图中心/默认视野时对应处理 */
  function maybeFitInitial() {
    if (fitted || !adapter || !points.length) return;
    fitted = true;
    const m = opts.mapping();
    // 配置了地图中心（名称）→ 自动定位到该对象；配置了默认视野 → 保持 SDK 初始中心；否则适配全部点位
    if (m?.DefaultCenter) {
      void locate();
      return;
    }
    if (m?.DefaultLocation) return;
    const b = boundsOf(visiblePoints());
    if (b) adapter.fit(expandBounds(b, 0.05));
  }

  /** 行 → 地图点（复用坐标检测与换算；用于定位查询结果） */
  function pointOfRow(row: Record<string, unknown>): MapPoint | null {
    const m = opts.mapping();
    if (!m || !detection) return null;
    const built = buildMapPoints([row], detection, {
      coordOrder: m.coordOrder,
      coordSystem: m.coordSystem,
      targetSystem: targetSystemOf(sourceOf() === 'baidu' ? 'baidu' : 'amap'),
      rowKey: opts.rowKey(),
    });
    return built.points[0] ?? null;
  }

  /**
   * 定位到地图中心点（自定义配置的地图中心名称）：
   * 先在已加载点位内按标题字段精确查找；未命中时按标题字段搜索一次后端；均未命中返回 false。
   */
  async function locate(): Promise<boolean> {
    const m = opts.mapping();
    if (!m || !adapter) return false;
    const name = (m.DefaultCenter || '').trim();
    if (!name) return false;
    const tf = m.titleField;
    if (tf) {
      const hit = points.find((p) => String(getValueByKey(p.row, tf) ?? '').trim() === name);
      if (hit) {
        adapter.setCenter(hit.lng, hit.lat, LOCATE_ZOOM);
        return true;
      }
    }
    if (tf && opts.loadLocateRow) {
      const row = await opts.loadLocateRow(tf, name);
      if (disposed) return false;
      const pt = row ? pointOfRow(row) : null;
      if (pt) {
        adapter?.setCenter(pt.lng, pt.lat, LOCATE_ZOOM);
        return true;
      }
    }
    return false;
  }

  /** 异步续页加载（串行直到 total/maxPoints 上限；查询变更中止） */
  async function loadAll() {
    if (loadingMore.value || disposed || !adapter || !opts.loadPage) return;
    const cap = maxPoints();
    if (loadedCount.value >= Math.min(expectedTotal(), cap)) return;
    loadingMore.value = true;
    const gen = generation;
    try {
      while (!disposed && gen === generation && adapter) {
        if (allRows.length >= Math.min(expectedTotal(), cap)) break;
        const rows = await opts.loadPage(nextPage);
        if (disposed || gen !== generation) return;
        if (!rows || !rows.length) break;
        nextPage++;
        appendRows(rows);
        schedulePump();
      }
    } finally {
      if (gen === generation) loadingMore.value = false;
    }
  }

  /** 分层模式：按当前 activeLevel 串行补拉缺失层级（并发去重；缩放中动态追高） */
  function syncLevels(): Promise<void> | null {
    if (!levelFieldName || !opts.loadLevelPage || disposed) return null;
    if (levelSyncTask) return levelSyncTask;
    const startGen = generation;
    const task = (async () => {
      while (!disposed && startGen === generation) {
        const target = Math.max(1, activeLevel.value);
        let next = 0;
        for (let lv = 1; lv <= target; lv++) {
          if (!loadedLevels.value.includes(lv) && !levelLoading.has(lv)) {
            next = lv;
            break;
          }
        }
        if (!next) break;
        await loadLevel(next);
      }
    })().finally(() => {
      // 仅当前任务仍是注册任务时清除（reset 换代后不得误清新任务引用）
      if (levelSyncTask === task) levelSyncTask = null;
    });
    levelSyncTask = task;
    return task;
  }

  /** 拉取单个层级（分页直至该层取完或达 maxPoints 上限；generation 变更中止） */
  async function loadLevel(level: number) {
    if (!levelFieldName || !opts.loadLevelPage) return;
    if (levelLoading.has(level) || loadedLevels.value.includes(level)) return;
    // 捕获本世代集合：reset 换新集合后，本流程只清理自己的在途标记，不误删新世代
    const owner = levelLoading;
    owner.add(level);
    levelBusy.value = true;
    const gen = generation;
    const cap = maxPoints();
    try {
      // 后端 pageIndex 为 1 基（0 与 1 同为第 1 页）：从第 1 页起逐页拉取
      let pageIndex = 1;
      for (;;) {
        if (disposed || gen !== generation) return;
        const rows = await opts.loadLevelPage(levelFieldName, level, pageIndex);
        if (disposed || gen !== generation) return;
        if (!rows || !rows.length) break;
        appendRows(rows);
        if (points.length >= cap || rows.length < LEVEL_PAGE_SIZE) break;
        pageIndex++;
      }
      if (gen === generation) {
        loadedLevels.value = [...loadedLevels.value, level].sort((a, b) => a - b);
        schedulePump();
      }
    } finally {
      owner.delete(level);
      // 仅本世代复位忙标记，避免旧世代 finally 提前清除新世代的加载中状态
      if (gen === generation) levelBusy.value = false;
    }
  }

  function onViewport() {
    if (viewportTimer) return;
    viewportTimer = window.setTimeout(() => {
      viewportTimer = 0;
      if (disposed) return;
      if (adapter) {
        zoom.value = clampZoom(adapter.getZoom());
        activeLevel.value = maxLevelForZoom(zoom.value);
      }
      syncScale();
      // 缩小后先清扫超层级/越界点，再补绘新视口（分层模式立即隐藏细级）
      recycle();
      schedulePump();
      void syncLevels();
    }, VIEWPORT_THROTTLE);
  }

  function onPointClick(id: string) {
    const p = pointById.get(id);
    if (p) opts.onDetail(p.row);
  }

  function onPointHoverEvent(id: string | null, x: number, y: number) {
    if (id == null) {
      hoveredId = null;
      window.clearTimeout(showTimer);
      if (hover.value) {
        window.clearTimeout(hideTimer);
        hideTimer = window.setTimeout(() => {
          hover.value = null;
        }, HOVER_HIDE_DELAY);
      }
      return;
    }
    window.clearTimeout(hideTimer);
    if (hoveredId === id) {
      if (hover.value) hover.value = { point: hover.value.point, x, y };
      return;
    }
    hoveredId = id;
    window.clearTimeout(showTimer);
    const p = pointById.get(id);
    if (!p) return;
    showTimer = window.setTimeout(() => {
      if (hoveredId === id) hover.value = { point: p, x, y };
    }, HOVER_SHOW_DELAY);
  }

  function clearAdapterPoints() {
    if (adapter && drawnIds.size) adapter.removePoints([...drawnIds]);
    drawnIds = new Set();
    drawnCount.value = 0;
  }

  /** 数据/查询变更：中止续页、清空点位、重建并重新泵入 */
  function resetPipeline() {
    generation++;
    fitted = false;
    hoveredId = null;
    hover.value = null;
    levelSyncTask = null;
    // 世代切换：复位在途标志（旧任务被 gen 守卫丢弃，不会误清新世代状态；不复位则新续页被加载中标志死锁）
    loadingMore.value = false;
    levelBusy.value = false;
    // 分层模式：忽略父级首批行，按层级从第 1 层（省/直辖市/港澳台）重新加载
    levelFieldName = detectLevelField([...opts.fields()]);
    levelField.value = levelFieldName;
    if (levelFieldName) {
      allRows = [];
      levelRows = [];
      levelLoading = new Set();
      loadedLevels.value = [];
    } else {
      allRows = [...opts.records()];
    }
    // 续页从第 2 页起（首批由父级列表提供第 1 页；后端 pageIndex 为 1 基）
    nextPage = 2;
    clearAdapterPoints();
    rebuildPoints();
    activeLevel.value = maxLevelForZoom(zoom.value);
    schedulePump();
    if (levelFieldName) {
      void syncLevels();
    } else if (loadedCount.value < Math.min(expectedTotal(), maxPoints())) {
      void loadAll();
    }
  }

  function destroyAdapter() {
    detachViewport?.();
    detachClick?.();
    detachHover?.();
    detachViewport = null;
    detachClick = null;
    detachHover = null;
    adapter?.destroy();
    adapter = null;
    adapterSource = '';
    ready.value = false;
  }

  let buildSeq = 0;

  async function buildAdapter() {
    // 并发守卫：异步创建期间若再次进入（源/数据变更），旧构建完成后自我销毁，避免双实例挂载与 SDK 监听泄漏
    const seq = ++buildSeq;
    destroyAdapter();
    const m = opts.mapping();
    const src = sourceOf();
    try {
      let created: MapAdapter | null;
      if (src === 'stub') {
        created = createStubAdapter({ zoom: m?.zoom ?? 5, center: m?.DefaultLocation, dark: isDarkTheme() });
      } else if (src) {
        const cfg = appStore.mapConfig;
        if (!cfg.key) return;
        const maker =
          src === 'amap' ? createAmapAdapter : src === 'baidu' ? createBaiduAdapter : createTencentAdapter;
        created = await maker({
          key: cfg.key,
          scriptUrl: cfg.scriptUrl || undefined,
          zoom: m?.zoom ?? 5,
          center: m?.DefaultLocation,
          dark: isDarkTheme(),
        });
      } else {
        return;
      }
      if (disposed || seq !== buildSeq) {
        created.destroy();
        return;
      }
      adapter = created;
      const el = opts.container.value;
      if (!el) {
        adapter.destroy();
        adapter = null;
        return;
      }
      adapter.mount(el);
      adapterSource = src;
      detachViewport = adapter.onViewportChange(onViewport);
      detachClick = adapter.onClick(onPointClick);
      detachHover = adapter.onPointHover(onPointHoverEvent);
      ready.value = true;
      error.value = '';
      zoom.value = clampZoom(adapter.getZoom());
      satellite.value = false;
      syncScale();
      resetPipeline();
    } catch (e) {
      adapter?.destroy();
      ready.value = false;
      adapter = null;
      adapterSource = '';
      error.value = e instanceof Error ? e.message : '地图初始化失败';
    }
  }

  async function init() {
    // 地图依赖服务商/密钥：进入地图视图时强制刷新，保证魔方设置变更后立即生效（OSC-261004d7f4）
    await appStore.fetchMapConfig(true);
    await buildAdapter();
  }

  /** records/mapping 变化：适配源（服务商/桩）变化则重建，否则仅重置数据管线 */
  async function refresh() {
    if (!mounted) return;
    if (sourceOf() !== adapterSource) {
      await buildAdapter();
      return;
    }
    if (adapter) resetPipeline();
  }

  function destroy() {
    disposed = true;
    window.clearTimeout(viewportTimer);
    window.clearTimeout(showTimer);
    window.clearTimeout(hideTimer);
    if (pumpHandle) window.cancelAnimationFrame(pumpHandle);
    pumpHandle = 0;
    destroyAdapter();
  }

  function zoomIn() {
    adapter?.zoomIn();
    if (adapter) onViewportFlush();
  }

  function zoomOut() {
    adapter?.zoomOut();
    if (adapter) onViewportFlush();
  }

  /** 立即刷新可视状态（按钮点击后不等节流） */
  function onViewportFlush() {
    window.clearTimeout(viewportTimer);
    viewportTimer = 0;
    if (adapter) {
      zoom.value = clampZoom(adapter.getZoom());
      activeLevel.value = maxLevelForZoom(zoom.value);
    }
    syncScale();
    recycle();
    schedulePump();
    void syncLevels();
  }

  // 生命周期与数据监听均在 composable 内注册（SFC 构薄门禁：.vue 禁写 watch/onMounted）
  onMounted(async () => {
    mounted = true;
    measureHeight();
    window.addEventListener('resize', measureHeight);
    // 暗色主题切换（右上角外观按钮切换 body[arco-theme]）→ 底图样式跟随
    if (typeof MutationObserver !== 'undefined') {
      themeObserver = new MutationObserver(() => {
        adapter?.setDark(isDarkTheme());
      });
      themeObserver.observe(document.body, { attributes: true, attributeFilter: ['arco-theme'] });
    }
    const el = opts.container.value;
    if (el && typeof ResizeObserver !== 'undefined') {
      observer = new ResizeObserver(() => {
        const target = opts.container.value;
        if (target) size.value = { w: target.clientWidth, h: target.clientHeight };
        // 容器位置可能随布局变化（侧栏/抽屉/全屏），同步重测高度
        measureHeight();
      });
      observer.observe(el);
      size.value = { w: el.clientWidth, h: el.clientHeight };
    }
    try {
      await init();
    } finally {
      initializing.value = false;
    }
  });

  onBeforeUnmount(() => {
    mounted = false;
    window.removeEventListener('resize', measureHeight);
    themeObserver?.disconnect();
    themeObserver = null;
    observer?.disconnect();
    observer = null;
    destroy();
  });

  watch(
    () => [opts.records(), opts.mapping(), opts.fields()] as const,
    () => {
      void refresh();
    },
  );

  // 父级列表测高变化（窗口/布局/视图切换）→ 同步根高度
  watch(
    () => opts.height?.() ?? 0,
    () => {
      measureHeight();
    },
  );

  return {
    ready,
    error,
    initializing,
    zoom,
    drawnCount,
    loadedCount,
    skippedCount,
    loadingMore,
    hover,
    size,
    height,
    activeLevel,
    loadedLevels,
    levelField,
    levelBusy,
    init,
    refresh,
    destroy,
    zoomIn,
    zoomOut,
    locate,
    satellite,
    toggleSatellite,
    scaleWidth,
    scaleText,
  };
}
