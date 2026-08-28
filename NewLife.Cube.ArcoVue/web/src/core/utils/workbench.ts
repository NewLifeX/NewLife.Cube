import type { DashboardConfig, WidgetInstance, WidgetWidth } from '@cube/api-core';
import { FA_ICON_MAP } from './iconRegistry';

/** 欢迎时段：5–11 上午、11–13 中午、13–18 下午，其余晚上 */
export function greetingPeriod(d: Date = new Date()): string {
  const h = d.getHours() + d.getMinutes() / 60;
  if (h >= 5 && h < 11) return '上午';
  if (h >= 11 && h < 13) return '中午';
  if (h >= 13 && h < 18) return '下午';
  return '晚上';
}

export function greetingText(displayName: string, d: Date = new Date()): string {
  const name = (displayName || '').trim() || '同学';
  return `${greetingPeriod(d)}好，${name}`;
}

/** CubeNC named → IconPark */
export const WORKBENCH_NAMED_ICONS: Record<string, string> = {
  UserCount: 'peoples',
  TodayLogin: 'login',
  OnlineCount: 'user',
  Log24h: 'file-text',
  Error24h: 'attention',
  CpuRate: 'dashboard',
  MyLogins: 'login',
  MyDays: 'calendar',
  QuickLink: 'application-menu',
  Profile: 'user',
  SysInfo: 'computer',
  LoginLog: 'peoples',
  Monitor: 'chart-line',
  Inbox: 'remind',
};

/** fa-* 或已是 IconPark type → 可渲染 type */
export function resolveWorkbenchIcon(icon?: string | null, widgetName?: string): string {
  const raw = (icon || '').trim();
  if (raw) {
    const key = raw.toLowerCase().replace(/^fa fa-/, 'fa-');
    if (FA_ICON_MAP[key]) return FA_ICON_MAP[key];
    if (key.startsWith('fa-') && FA_ICON_MAP[key]) return FA_ICON_MAP[key];
    if (!key.startsWith('fa-')) return raw;
  }
  if (widgetName && WORKBENCH_NAMED_ICONS[widgetName]) return WORKBENCH_NAMED_ICONS[widgetName];
  return 'application';
}

/** 内容卡半行配对（不占满整行） */
const HALF_ROW_SEEDS = new Set([
  'seed-Inbox',
  'seed-SysInfo',
  'seed-LoginLog',
  'seed-Profile',
]);

/**
 * 把系统种子 id 的布局对齐到美化后的默认栅格（不改用户自增部件）。
 * Monitor/QuickLink 同高；Inbox/SysInfo/LoginLog/Profile 半行（w=6）。
 */
export function alignWorkbenchSeedLayout(cfg: DashboardConfig): DashboardConfig {
  const widgets = (cfg.widgets ?? []).map((w: WidgetInstance) => {
    if (w.id === 'seed-Monitor') {
      return { ...w, layout: { ...w.layout, w: 8 as WidgetWidth, h: 3 as const } };
    }
    if (w.id === 'seed-QuickLink') {
      const span = (w.layout.w === 4 ? 4 : 12) as WidgetWidth;
      return { ...w, layout: { ...w.layout, w: span, h: 3 as const } };
    }
    if (HALF_ROW_SEEDS.has(w.id)) {
      return { ...w, layout: { ...w.layout, w: 6 as WidgetWidth } };
    }
    return w;
  });
  return { ...cfg, widgets };
}
