import type { DashboardConfig, WidgetInstance, WidgetSourceItem } from '@cube/api-core';
import type { ViewInsight } from '@/core/utils/viewProfile';
import { resolveStatEntries } from '@/core/utils/searchFilters';

function metricCard(
  id: string,
  title: string,
  typePath: string,
  order: number,
  extra?: Partial<WidgetInstance>,
): WidgetInstance {
  return {
    id,
    kind: 'metricCard',
    title,
    layout: { w: 3, order },
    source: { provider: 'entity.aggregate', typePath },
    query: { measure: { fn: 'count' } },
    ...extra,
  };
}

/**
 * 仅当个人+模板 DashboardJson 均未配置时合成旧 insight。
 * 双关不合成；showStat 且 stat 空 → 一张 count。
 */
export function synthesizeLegacyDashboard(
  insight: ViewInsight | null | undefined,
  statData: Record<string, unknown> | null,
  hasDeveloperChart: boolean,
  hostTypePath: string,
): DashboardConfig | null {
  const showStat = insight?.showStat === true;
  const showChart = insight?.showChart === true;
  if (!showStat && !showChart) return null;

  const widgets: WidgetInstance[] = [];
  let order = 0;
  if (showStat) {
    const entries = resolveStatEntries(statData);
    if (entries.length) {
      for (const e of entries) {
        widgets.push(
          metricCard(`legacy-stat-${e.key}`, String(e.key), hostTypePath, order, {
            query: { measure: { fn: 'count' } },
            style: { color: 'blue' },
            syntheticValue: e.value,
          }),
        );
        order += 1;
      }
    } else {
      widgets.push(metricCard('legacy-count', '记录数', hostTypePath, order));
      order += 1;
    }
  }
  if (showChart && (hasDeveloperChart || insight?.chartOption !== undefined)) {
    widgets.push({
      id: 'legacy-chart',
      kind: 'legacyChart',
      title: '来自旧图表',
      layout: { w: 12, h: 3, order },
      source: { provider: 'entity.aggregate', typePath: hostTypePath },
      query: {},
      style: { chartType: 'bar' },
      chartOption: insight?.chartOption,
    });
  }
  if (!widgets.length) return null;
  return { version: 1, widgets };
}

export function normalizeTypePath(path: string | null | undefined): string {
  return (path ?? '').replace(/^\/+/, '');
}

/** 将 Sources 接口响应归一为列表（兼容 ApiResponse / 已解包数组 / PascalCase） */
export function normalizeSourceRows(payload: unknown): WidgetSourceItem[] {
  let rows: unknown = payload;
  // 最多剥两层 data（兼容 ApiResponse / 误传的 AxiosResponse）
  for (let i = 0; i < 3 && rows && typeof rows === 'object' && !Array.isArray(rows); i++) {
    const o = rows as Record<string, unknown>;
    if (!('data' in o) && !('Data' in o)) break;
    rows = o.data ?? o.Data;
  }
  if (!Array.isArray(rows)) return [];
  const out: WidgetSourceItem[] = [];
  for (const row of rows) {
    if (row == null) continue;
    let typePath = '';
    let displayName = '';
    let name = '';
    if (Array.isArray(row)) {
      typePath = normalizeTypePath(String(row[0] ?? ''));
      displayName = String(row[1] ?? '');
      name = String(row[2] ?? '');
    } else if (typeof row === 'object') {
      const r = row as Record<string, unknown>;
      typePath = normalizeTypePath(String(r.typePath ?? r.TypePath ?? r.Item1 ?? ''));
      displayName = String(r.displayName ?? r.DisplayName ?? r.Item2 ?? '');
      name = String(r.name ?? r.Name ?? r.Item3 ?? '');
    } else continue;
    if (!typePath) continue;
    out.push({
      typePath,
      displayName: displayName || name || typePath,
      name: name || typePath,
    });
  }
  return out;
}

/** 跨实体且未声明 linkFilter → 未联动 */
export function isUnlinkedWidget(widget: WidgetInstance, hostTypePath: string | undefined): boolean {
  const src = normalizeTypePath(widget.source?.typePath);
  const host = normalizeTypePath(hostTypePath);
  if (!src || !host || src.toLowerCase() === host.toLowerCase()) return false;
  const links = widget.query?.linkFilter;
  return !links || links.length === 0;
}

export function newWidgetId(): string {
  return `w_${Date.now().toString(36)}${Math.random().toString(36).slice(2, 8)}`;
}
