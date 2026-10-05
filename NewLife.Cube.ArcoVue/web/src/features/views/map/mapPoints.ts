/**
 * 地图点位构建与样式解析（OSC-261004d7f4）
 *
 * - records → 点位：坐标读取、坐标系换算（数据坐标 → 目标底图坐标）
 * - 样式解析：填色规则命中颜色 > 分类规则（值 → 图标/颜色）> 默认样式
 */
import type { FieldMeta } from '@/core/types/field';
import {
  normalizeDataSource,
  type MapCategoryRule,
  type MapMapping,
} from '@/core/utils/viewMapping';
import {
  readCoordFromRow,
  type CoordDetection,
  type CoordOrder,
  type CoordSystem,
  type LngLat,
} from '@/core/utils/mapCoord';
import { convert } from '@/core/utils/mapTransform';
import type { ViewFormatRule } from '@/core/utils/viewProfile';
import { getValueByKey } from '@/core/utils/url';

export interface MapPoint extends LngLat {
  id: string;
  row: Record<string, unknown>;
}

export interface MapMarkerStyle {
  icon: string;
  color: string;
}

/** 目标底图坐标系：高德/腾讯 GCJ-02，百度 BD-09 */
export function targetSystemOf(provider: string): 'gcj02' | 'bd09' {
  return provider === 'baidu' ? 'bd09' : 'gcj02';
}

export interface BuildPointsOptions {
  coordOrder?: CoordOrder;
  coordSystem?: CoordSystem;
  targetSystem?: 'gcj02' | 'bd09';
  rowKey: string;
}

/** records → 点位；无效/缺失坐标计入 skipped */
export function buildMapPoints(
  rows: readonly Record<string, unknown>[],
  detection: CoordDetection,
  opts: BuildPointsOptions,
): { points: MapPoint[]; skipped: number } {
  const points: MapPoint[] = [];
  let skipped = 0;
  const from = opts.coordSystem ?? 'gcj02';
  const to = opts.targetSystem ?? 'gcj02';
  for (const row of rows) {
    const c = readCoordFromRow(row, detection, opts.coordOrder ?? 'lnglat');
    if (!c) {
      skipped++;
      continue;
    }
    const p = convert(c.lng, c.lat, from, to);
    points.push({
      id: String(getValueByKey(row, opts.rowKey) ?? ''),
      lng: p.lng,
      lat: p.lat,
      row,
    });
  }
  return { points, skipped };
}

export interface CategoryContext {
  rules?: MapCategoryRule[];
  /** 分类值键归一（dataSource 数字键优先），与看板分桶同源 */
  canonicalByKey: Map<string, string>;
  defaultIcon: string;
  defaultColor: string;
}

/** 构建分类解析上下文（供逐行解析样式；半纯函数，一次构建多次使用） */
export function buildCategoryContext(
  mapping: MapMapping,
  categoryField?: FieldMeta,
): CategoryContext {
  const ds = categoryField?.dataSource;
  return {
    rules: mapping.categoryRules,
    canonicalByKey: ds ? normalizeDataSource(ds).canonicalByKey : new Map<string, string>(),
    defaultIcon: mapping.DefaultIcon || 'local',
    defaultColor: mapping.defaultColor || '#165DFF',
  };
}

/** 单条填色规则是否命中（cell 范围；与筛选操作符语义对齐） */
export function matchFormatRule(row: Record<string, unknown>, rule: ViewFormatRule): boolean {
  if (!rule.field) return false;
  const v = getValueByKey(row, rule.field);
  const s = v == null ? '' : String(v);
  const target = rule.value == null ? '' : String(rule.value);
  switch (rule.op) {
    case 'eq':
      return Array.isArray(rule.value)
        ? (rule.value as unknown[]).some((x) => String(x) === s)
        : target === s;
    case 'neq':
      return target !== s;
    case 'contains':
      return s.includes(target);
    case 'notContains':
      return !s.includes(target);
    case 'startsWith':
      return s.startsWith(target);
    case 'endsWith':
      return s.endsWith(target);
    case 'isNull':
      return v == null || v === '';
    case 'notNull':
      return !(v == null || v === '');
    case 'gt':
    case 'gte':
    case 'lt':
    case 'lte': {
      const a = Number(v);
      const b = Number(rule.value);
      if (!Number.isFinite(a) || !Number.isFinite(b)) return false;
      if (rule.op === 'gt') return a > b;
      if (rule.op === 'gte') return a >= b;
      if (rule.op === 'lt') return a < b;
      return a <= b;
    }
    case 'after':
    case 'before': {
      const a = new Date(String(v)).getTime();
      const b = new Date(String(rule.value)).getTime();
      if (!Number.isFinite(a) || !Number.isFinite(b)) return false;
      return rule.op === 'after' ? a > b : a < b;
    }
    default:
      return false;
  }
}

/**
 * 解析点位样式：填色命中颜色 > 分类规则颜色 > 默认颜色；
 * 图标取分类规则图标，未命中用默认图标。
 */
export function resolveMarkerStyle(
  row: Record<string, unknown>,
  mapping: MapMapping,
  ctx: CategoryContext,
  formatRules: readonly ViewFormatRule[] = [],
): MapMarkerStyle {
  let icon = ctx.defaultIcon;
  let color = ctx.defaultColor;

  if (mapping.categoryField) {
    const raw = getValueByKey(row, mapping.categoryField);
    if (raw != null && raw !== '') {
      const key = ctx.canonicalByKey.get(String(raw)) || String(raw);
      const rule = ctx.rules?.find((r) => r.value === key);
      if (rule) {
        if (rule.icon) icon = rule.icon;
        if (rule.color) color = rule.color;
      }
    }
  }

  const fill = formatRules.find((r) => matchFormatRule(row, r));
  if (fill) color = fill.color;

  return { icon, color };
}

/** 层级字段名：地区实体 Level=1省/2市/3区县/4街道，用于地图逐层加载（OSC-261004d7f4） */
const LEVEL_FIELD_RE = /^(level|levelid|grade|depth|tier)$/i;
const LEVEL_NAME_RE = /层级|等级|级别/;
const LEVEL_NUMERIC_TYPES: ReadonlySet<String> = new Set([
  'Byte',
  'SByte',
  'Int16',
  'Int32',
  'Int64',
  'UInt16',
  'UInt32',
  'UInt64',
  'Single',
  'Double',
  'Decimal',
  'Short',
  'UShort',
]);

/**
 * 自动检测层级字段（按字段名/显示名 + 数值类型）；无则返回空串（常规视口渐进模式）。
 * 检测到后 MapView 按「低缩放只绘上层、放大逐层拉取与显示」运行。
 */
export function detectLevelField(fields: readonly FieldMeta[]): string {
  for (const f of fields) {
    if (!f.name || f.primaryKey) continue;
    if (!LEVEL_NUMERIC_TYPES.has(f.typeName || '')) continue;
    if (LEVEL_FIELD_RE.test(f.name) || LEVEL_NAME_RE.test((f.displayName || '').trim())) return f.name;
  }
  return '';
}

/** 读取行层级；缺失/非法按 1（最顶层）处理，保证低缩放仍可见 */
export function readLevelOfRow(row: Record<string, unknown>, field: string): number {
  if (!field) return 1;
  const n = Number(getValueByKey(row, field));
  return Number.isFinite(n) && n > 0 ? Math.trunc(n) : 1;
}
