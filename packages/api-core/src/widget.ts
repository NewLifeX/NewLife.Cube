/** 页面仪表盘 Widget 协议（OSC-2608280e9e / OSC-26082815a1）。前后端共用形状。 */

export type WidgetKind =
  | 'metricCard'
  | 'miniChart'
  | 'miniKanban'
  | 'dataList'
  | 'dataCard'
  | 'legacyChart'
  | 'quickLinks'
  | 'profile'
  | 'kvList'
  | 'loginLog'
  | 'monitorChart'
  | 'inbox'
  | string;
export type WidgetProvider = 'entity.aggregate' | 'entity.list' | 'named';
export type MeasureFn = 'count' | 'sum' | 'avg' | 'min' | 'max';
export type ChartType = 'sparkline' | 'line' | 'bar' | 'hbar' | 'pie';
export type WidgetSurface = 'insight' | 'workbench';
export type WidgetWidth = 2 | 3 | 4 | 6 | 8 | 12;

export interface WidgetLayout {
  w: WidgetWidth;
  h?: 1 | 2 | 3 | 4;
  order: number;
}

export interface WidgetLinkFilter {
  hostField: string;
  sourceField: string;
}

export interface WidgetFilterCondition {
  field: string;
  op: string;
  value?: unknown;
}

export interface WidgetFilter {
  logic: 'all' | 'any';
  conditions: WidgetFilterCondition[];
}

export interface WidgetInstance {
  id: string;
  kind: WidgetKind;
  title: string;
  layout: WidgetLayout;
  source: {
    provider: WidgetProvider;
    typePath?: string;
    widgetName?: string;
  };
  query: {
    measure?: { fn: MeasureFn; field?: string };
    groupBy?: string;
    timeField?: string;
    buckets?: number;
    limit?: number;
    mapping?: {
      groupField?: string;
      titleField?: string;
      imageField?: string;
      /** 迷你看板卡片正文显示字段（有序，不含标题/分组） */
      fields?: string[];
    };
    linkFilter?: WidgetLinkFilter[];
    extraFilter?: WidgetFilter;
  };
  style?: {
    icon?: string;
    color?: string;
    chartType?: ChartType;
    clickUrl?: string;
    /** 指标卡等自定义角标文案 */
    badge?: string;
  };
  [key: string]: unknown;
}

export interface DashboardConfig {
  version: 1;
  widgets: WidgetInstance[];
  [key: string]: unknown;
}

export interface WidgetQueryBody {
  mode?: 'aggregate' | 'list';
  typePath: string;
  measure?: { fn: MeasureFn; field?: string };
  groupBy?: string;
  timeField?: string;
  buckets?: number;
  limit?: number;
  extraFilter?: WidgetFilter;
  hostTypePath?: string;
  hostFilter?: WidgetFilter;
  linkFilter?: WidgetLinkFilter[];
  hostValues?: Record<string, unknown>;
}

export interface WidgetQueryResult {
  value?: unknown;
  trend?: unknown;
  url?: string;
  items?: { key: string; label: string; value: unknown; [key: string]: unknown }[];
  rows?: Record<string, unknown>[];
  hostFilterApplied?: boolean;
  links?: unknown;
  logins?: unknown;
  onlines?: unknown;
  unread?: unknown;
  cpu?: unknown;
  mem?: unknown;
  time?: unknown;
  name?: unknown;
  displayName?: unknown;
  [key: string]: unknown;
}

export interface WidgetSourceItem {
  typePath: string;
  displayName: string;
  name: string;
}

export interface WidgetKindMeta {
  kind: string;
  title: string;
  providers: string[];
  defaultW: number;
}

export interface WidgetNamedMeta {
  name: string;
  title: string;
  kind: string;
  cols: number;
  adminOnly: boolean;
  surfaces?: string;
  color?: string;
  icon?: string;
}

export interface WidgetCatalog {
  kinds: WidgetKindMeta[];
  named: WidgetNamedMeta[];
}

export interface WorkbenchResolveResult {
  source: 'user' | 'role' | 'system' | string;
  roleId: number;
  config: DashboardConfig | Record<string, unknown> | null;
}

const WIDTHS_INSIGHT = new Set([3, 4, 6, 12]);
const WIDTHS_WORKBENCH = new Set([2, 3, 4, 6, 8, 12]);
const MAX_WIDGETS_INSIGHT = 12;
const MAX_WIDGETS_WORKBENCH = 16;
const MAX_BYTES = 64 * 1024;
const STRIP_KEYS = new Set(['data', 'value', 'items', 'rows']);

export function widthsFor(surface?: WidgetSurface | string | null): Set<number> {
  return surface === 'workbench' ? WIDTHS_WORKBENCH : WIDTHS_INSIGHT;
}

export function maxWidgetsFor(surface?: WidgetSurface | string | null): number {
  return surface === 'workbench' ? MAX_WIDGETS_WORKBENCH : MAX_WIDGETS_INSIGHT;
}

function asRecord(v: unknown): Record<string, unknown> | null {
  return v && typeof v === 'object' && !Array.isArray(v) ? (v as Record<string, unknown>) : null;
}

export function emptyDashboard(): DashboardConfig {
  return { version: 1, widgets: [] };
}

/** 个人域是否有效（含显式空 widgets，空白/非法 JSON 不算） */
export function hasDashboardDomain(json: string | null | undefined): boolean {
  if (!json || !json.trim()) return false;
  try {
    return asRecord(JSON.parse(json)) != null;
  } catch {
    return false;
  }
}

export function parseDashboardJson(
  json: string | null | undefined,
  surface?: WidgetSurface | string | null,
): DashboardConfig | null {
  if (!json || !json.trim()) return null;
  try {
    const obj = asRecord(JSON.parse(json));
    if (!obj || obj.version !== 1) return null;
    const widgetsRaw = obj.widgets;
    if (widgetsRaw != null && !Array.isArray(widgetsRaw)) return null;
    const widths = widthsFor(surface);
    const widgets = (Array.isArray(widgetsRaw) ? widgetsRaw : [])
      .map((w, i) => normalizeWidget(w, i, widths))
      .filter((w): w is WidgetInstance => w != null)
      .sort((a, b) => a.layout.order - b.layout.order);
    const extra: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(obj)) {
      if (k !== 'version' && k !== 'widgets') extra[k] = v;
    }
    return { version: 1, widgets, ...extra };
  } catch {
    return null;
  }
}

function normalizeWidget(raw: unknown, index: number, widths: Set<number>): WidgetInstance | null {
  const o = asRecord(raw);
  if (!o) return null;
  const id = String(o.id ?? '').trim();
  if (!id) return null;
  const layoutObj = asRecord(o.layout) ?? {};
  let w = Number(layoutObj.w);
  if (!widths.has(w)) w = 3;
  let order = Number(layoutObj.order);
  if (!Number.isFinite(order)) order = index;
  const hN = Number(layoutObj.h);
  const layout: WidgetLayout = { w: w as WidgetWidth, order };
  if (hN >= 1 && hN <= 4) layout.h = hN as 1 | 2 | 3 | 4;
  const source = asRecord(o.source) ?? {};
  const query = asRecord(o.query) ?? {};
  const style = asRecord(o.style);
  const inst: WidgetInstance = {
    id,
    kind: String(o.kind ?? 'metricCard'),
    title: String(o.title ?? '').slice(0, 40),
    layout,
    source: {
      provider: String(source.provider ?? 'entity.aggregate') as WidgetProvider,
      typePath: source.typePath ? String(source.typePath).replace(/^\/+/, '') : undefined,
      widgetName: source.widgetName ? String(source.widgetName) : undefined,
    },
    query: { ...query } as WidgetInstance['query'],
  };
  if (style) inst.style = style as WidgetInstance['style'];
  for (const [k, v] of Object.entries(o)) {
    if (['id', 'kind', 'title', 'layout', 'source', 'query', 'style', ...STRIP_KEYS].includes(k)) continue;
    inst[k] = v;
  }
  return inst;
}

export function serializeDashboardJson(cfg: DashboardConfig, surface?: WidgetSurface | string | null): string {
  const widths = widthsFor(surface);
  const widgets = [...(cfg.widgets ?? [])]
    .sort((a, b) => a.layout.order - b.layout.order)
    .map((w, i) => {
      const rest: Record<string, unknown> = { ...w };
      for (const k of STRIP_KEYS) delete rest[k];
      const lw = Number(w.layout?.w);
      return {
        ...rest,
        layout: {
          ...w.layout,
          w: (widths.has(lw) ? lw : 3) as WidgetWidth,
          order: i,
        },
      };
    });
  const extra: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(cfg)) {
    if (k !== 'version' && k !== 'widgets') extra[k] = v;
  }
  return JSON.stringify({ version: 1, widgets, ...extra });
}

export function validateDashboardForPut(
  json: string,
  surface?: WidgetSurface | string | null,
): { ok: true; json: string } | { ok: false; error: string } {
  if (!json.trim()) return { ok: true, json: '' };
  if (new TextEncoder().encode(json).length > MAX_BYTES) return { ok: false, error: '仪表盘配置过大' };
  const parsed = parseDashboardJson(json, surface);
  if (!parsed) return { ok: false, error: '仪表盘配置无效' };
  const max = maxWidgetsFor(surface);
  if (parsed.widgets.length > max) return { ok: false, error: `部件数量不能超过 ${max}` };
  const ids = new Set<string>();
  for (const w of parsed.widgets) {
    if (ids.has(w.id)) return { ok: false, error: '部件 id 不能为空或重复' };
    ids.add(w.id);
    if (w.kind === 'legacyChart') return { ok: false, error: '禁止保存 legacyChart' };
    if (surface !== 'workbench' && w.kind === 'miniKanban') {
      return { ok: false, error: '页面仪表盘不支持数据看板' };
    }
    if (surface !== 'workbench' && w.kind === 'dataList') {
      return { ok: false, error: '页面仪表盘不支持数据列表' };
    }
    if (surface !== 'workbench' && w.kind === 'dataCard') {
      return { ok: false, error: '页面仪表盘不支持数据卡片' };
    }
  }
  return { ok: true, json: serializeDashboardJson(parsed, surface) };
}

/** 将 GET /Cube/Workbench 的 config 解成 DashboardConfig */
export function parseWorkbenchConfig(raw: unknown): DashboardConfig | null {
  if (raw == null) return null;
  if (typeof raw === 'string') return parseDashboardJson(raw, 'workbench');
  try {
    return parseDashboardJson(JSON.stringify(raw), 'workbench');
  } catch {
    return null;
  }
}
