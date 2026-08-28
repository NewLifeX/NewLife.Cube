import type { MenuItem } from '@cube/api-core';
import { flattenMenus } from './menuRoutes';
import { resolveWorkbenchIcon } from './workbench';

export interface QuickLinkPin {
  name: string;
  url: string;
  icon?: string;
}

function mapPin(it: unknown): QuickLinkPin | null {
  const o = (it && typeof it === 'object' ? it : {}) as Record<string, unknown>;
  const name = String(o.name ?? o.Name ?? '').trim();
  const url = String(o.url ?? o.Url ?? '').trim();
  if (!name || !url) return null;
  const icon = resolveWorkbenchIcon(String(o.icon ?? o.Icon ?? ''), '');
  return { name, url, icon };
}

/** 从部件 query.pins 读取用户自选入口 */
export function readQuickLinkPins(query: unknown): QuickLinkPin[] {
  if (!query || typeof query !== 'object') return [];
  const raw = (query as { pins?: unknown; Pins?: unknown }).pins
    ?? (query as { Pins?: unknown }).Pins;
  if (!Array.isArray(raw)) return [];
  return raw.map(mapPin).filter((x): x is QuickLinkPin => x != null);
}

/** Data 接口 links → 展示项 */
export function readQuickLinkServer(result: unknown): QuickLinkPin[] {
  const r = result as { links?: unknown; Links?: unknown } | undefined;
  const raw = r?.links ?? r?.Links;
  if (!Array.isArray(raw)) return [];
  return raw.map(mapPin).filter((x): x is QuickLinkPin => x != null);
}

function normUrl(url: string): string {
  return url.trim().replace(/\/+$/, '').toLowerCase() || '/';
}

/** 当前用户菜单中有 url 的可见项（与路由注册一致，含子节点但自带 url 的项） */
export function menuLeavesForPins(menus: MenuItem[] | null | undefined): QuickLinkPin[] {
  const flat = flattenMenus(menus ?? []);
  const out: QuickLinkPin[] = [];
  const seen = new Set<string>();
  for (const m of flat) {
    if (m.visible === false || !m.url?.trim()) continue;
    const url = m.url.trim().startsWith('/') ? m.url.trim() : `/${m.url.trim()}`;
    const key = normUrl(url);
    if (seen.has(key)) continue;
    seen.add(key);
    out.push({
      name: (m.displayName || m.name || url).trim(),
      url,
      icon: resolveWorkbenchIcon(m.icon, ''),
    });
  }
  return out;
}

/** 展示：有 pins 用 pins（并按仍有权限的菜单过滤）；否则用服务端默认链 */
export function resolveQuickLinksDisplay(
  pins: QuickLinkPin[],
  serverLinks: QuickLinkPin[],
  allowedMenus: QuickLinkPin[],
): QuickLinkPin[] {
  if (!pins.length) return serverLinks;
  const allow = new Set(allowedMenus.map((m) => normUrl(m.url)));
  const kept = pins.filter((p) => allow.has(normUrl(p.url)));
  return kept.length ? kept : serverLinks;
}
