/**
 * 工作台（Dashboard）业务逻辑与视图模型
 *
 * 后端 `Admin/Index/Dashboard` 返回弱类型的聚合数据（KPI + 内容卡 + 快捷入口 + 个人信息 + 系统信息），
 * `Admin/Index/MonitorData` 返回 CPU/内存快照。各皮肤只需按本模块输出的视图模型渲染，
 * 不再各自实现数据归一化，避免多皮肤口径漂移。
 */

import type { CubeApi } from '../api';

// ======================== 常量 ========================

/** 工作台后端控制器路径前缀 */
export const WORKBENCH_TYPE = 'Admin/Index';

/** 聚合数据动作名 */
export const WORKBENCH_DASHBOARD_ACTION = 'Dashboard';

/** 监控快照动作名 */
export const WORKBENCH_MONITOR_ACTION = 'MonitorData';

/** 监控曲线最多保留的点数。5 秒一轮，约保留最近 5 分钟 */
export const MAX_MONITOR_POINTS = 60;

/** 监控轮询间隔（毫秒） */
export const MONITOR_POLL_INTERVAL = 5000;

// ======================== 后端契约类型 ========================

/** 工作台 KPI 指标卡 */
export interface WorkbenchKpi {
  /** 部件名称，如 UserCount */
  name: string;
  /** 显示标题 */
  label: string;
  /** 主数值。后端可能返回数字或已格式化文本 */
  value: string | number;
  /** 趋势/说明文本 */
  trend?: string;
  /** 配色名：blue/green/cyan/orange/red/purple/grey */
  color?: string;
  /** 点击跳转地址 */
  url?: string;
}

/** 工作台内容卡 */
export interface WorkbenchWidget {
  /** 部件名称，如 Profile/Monitor/SysInfo */
  name: string;
  /** 卡片标题 */
  title: string;
  /** 图标名 */
  icon?: string;
  /** 12 栅格宽度 */
  cols?: number;
  /** 分类：系统/个人/通用/业务模块名 */
  category?: string;
  /** 卡片类型：Kpi/Content/Html/Kv/Chart */
  widgetType: string;
  /** 部件数据。Chart 卡由前端独立轮询 MonitorData，此处为 null */
  data: unknown;
}

/** 快捷入口 */
export interface WorkbenchQuickLink {
  name: string;
  url: string;
  icon?: string;
}

/** 工作台聚合响应 */
export interface WorkbenchData {
  user?: {
    name?: string;
    displayName?: string;
    roles?: string[] | string;
  };
  kpis: WorkbenchKpi[];
  hiddenKpis?: WorkbenchKpi[];
  widgets: WorkbenchWidget[];
  hiddenWidgets?: WorkbenchWidget[];
  quickLinks?: WorkbenchQuickLink[];
  profile?: {
    name?: string;
    displayName?: string;
    roleNames?: string | string[];
    online?: boolean;
    logins?: number;
    lastLogin?: string;
    lastLoginIP?: string;
    registerTime?: string;
  };
  sysInfo?: Record<string, unknown>;
}

/** `Admin/Index/MonitorData` 单次快照 */
export interface MonitorSnapshot {
  /** X 轴新点。约定为单元素数组 */
  xs?: string[];
  /** 各系列新数据。约定 [CPU 百分比, 内存百分比] */
  series?: number[][];
  /** 已用内存（MB） */
  memUsed?: number;
  /** 总内存（MB） */
  memTotal?: number;
}

/** 前端累计的监控点 */
export interface MonitorPoint {
  /** 时间标签 */
  label: string;
  /** CPU 使用率（0-100） */
  cpu: number;
  /** 内存使用率（0-100） */
  memory: number;
  /** 已用内存（MB） */
  memUsed?: number;
  /** 总内存（MB） */
  memTotal?: number;
}

// ======================== 视图模型 ========================

/** 键值行。链接型值已解析出 href/text，皮肤不得直接输出 HTML */
export interface WorkbenchRow {
  key: string;
  text: string;
  link: { href: string; text: string; external: boolean } | null;
}

/** 快捷入口项 */
export interface WorkbenchLink {
  name: string;
  url: string;
  icon?: string;
}

/** 登录/在线列表项 */
export interface WorkbenchLogItem {
  title: string;
  subtitle: string;
  time: string;
}

/** KPI 视图项。数值已转文本，配色已收敛为合法语义色 */
export interface WorkbenchKpiView {
  name: string;
  label: string;
  value: string;
  trend?: string;
  color: string;
  url?: string;
}

/** 内容卡视图项 */
export interface WorkbenchWidgetView {
  name: string;
  title: string;
  icon?: string;
  cols: number;
  /** 卡片呈现类型 */
  kind: 'profile' | 'quickLinks' | 'loginLog' | 'monitor' | 'kv' | 'text' | 'empty';
  /** 个人信息卡 */
  profile?: {
    name: string;
    displayName: string;
    initial: string;
    online: boolean;
    roles: string[];
    rows: WorkbenchRow[];
  };
  /** 快捷入口卡 */
  links?: WorkbenchLink[];
  /** 登录与在线卡 */
  logins?: WorkbenchLogItem[];
  onlines?: WorkbenchLogItem[];
  /** 键值卡 */
  rows?: WorkbenchRow[];
  /** 纯文本卡 */
  text?: string;
}

/** 工作台整体视图模型 */
export interface WorkbenchView {
  displayName: string;
  roles: string[];
  roleText: string;
  kpis: WorkbenchKpiView[];
  widgets: WorkbenchWidgetView[];
  quickLinks: WorkbenchLink[];
  sysInfoRows: WorkbenchRow[];
  /** 是否含需要轮询监控数据的图表卡 */
  hasChartWidget: boolean;
  /** 是否有可渲染内容 */
  hasContent: boolean;
}

/** 监控曲线 SVG 坐标 */
export interface MonitorChartGeometry {
  width: number;
  height: number;
  /** CPU 折线 points 值，空数据为空串 */
  cpuPoints: string;
  /** 内存折线 points 值，空数据为空串 */
  memPoints: string;
}

// ======================== 纯函数 ========================

/** 判断后端业务响应是否成功。兼容 code=0 与 code=200 两种历史约定 */
export function isSuccessCode(code: unknown): boolean {
  return code === 0 || code === 200;
}

/** 任意值转为可展示文本。对象转 JSON，HTML 标签剥离，空值显示 - */
export function formatCell(value: unknown): string {
  if (value === null || value === undefined || value === '') return '-';
  if (typeof value === 'string') {
    const text = value
      .replace(/<[^>]+>/g, '')
      .replace(/&nbsp;/g, ' ')
      .trim();
    return text || '-';
  }
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

/**
 * 从后端键值中提取链接
 *
 * SysInfo 等部件允许返回 `<a href="/Admin/Index/Main">查看完整服务器信息 »</a>`。
 * 皮肤不得把服务端字符串当 HTML 注入，只解析出 href/text 后用路由链接渲染。
 */
export function extractLink(value: unknown): { href: string; text: string } | null {
  if (typeof value !== 'string') return null;

  const matched = value.match(/<a\s+[^>]*href=["']([^"']+)["'][^>]*>([\s\S]*?)<\/a>/i);
  if (!matched) return null;

  const href = matched[1].trim();
  const text = formatCell(matched[2]);
  if (!href || text === '-') return null;

  return { href, text };
}

/** 角色字段可能是逗号分隔字符串或数组，统一为数组 */
export function normalizeRoles(value: string | string[] | undefined | null): string[] {
  if (!value) return [];
  if (Array.isArray(value)) return value.filter(Boolean);

  return value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);
}

/** 是否为站外链接 */
export function isExternalLink(href: string): boolean {
  return /^https?:\/\//i.test(href) || href.startsWith('//');
}

/** 把未知值收窄为普通对象。数组/原始值返回空对象，避免模板访问属性报错 */
export function asRecord(value: unknown): Record<string, unknown> {
  if (value && typeof value === 'object' && !Array.isArray(value)) return value as Record<string, unknown>;
  return {};
}

/** KPI 合法配色。后端 Color 属性只允许这些语义色 */
const KPI_COLORS = new Set(['blue', 'green', 'cyan', 'orange', 'red', 'purple', 'grey']);

/** 归一化 KPI 配色，未知值回退蓝色，避免把后端字符串直接拼进样式 */
export function normalizeKpiColor(color?: string | null): string {
  const value = (color || '').trim().toLowerCase();
  return KPI_COLORS.has(value) ? value : 'blue';
}

/** 归一化 12 栅格宽度。缺省 6，越界收敛到 1-12 */
export function normalizeCols(cols?: number | null): number {
  if (!Number.isFinite(cols)) return 6;
  return Math.min(12, Math.max(1, Math.round(cols as number)));
}

/** 数值收敛到 0-100，非法值返回 null */
function toPercent(value: unknown): number | null {
  const num = Number(value);
  if (!Number.isFinite(num)) return null;
  return Math.min(100, Math.max(0, num));
}

/** 从 MonitorData 响应提取一个监控点。数据缺失时返回 null，避免把脏数据画进曲线 */
export function toMonitorPoint(snapshot: MonitorSnapshot | null | undefined): MonitorPoint | null {
  if (!snapshot) return null;

  const cpu = toPercent(snapshot.series?.[0]?.[0]);
  const memory = toPercent(snapshot.series?.[1]?.[0]);
  if (cpu === null || memory === null) return null;

  const label = snapshot.xs?.[0] || new Date().toLocaleTimeString();
  return {
    label,
    cpu,
    memory,
    memUsed: snapshot.memUsed,
    memTotal: snapshot.memTotal,
  };
}

/** 追加监控点并限制最大长度。返回新数组，不修改入参 */
export function appendMonitorPoint(
  points: MonitorPoint[],
  point: MonitorPoint | null,
  max = MAX_MONITOR_POINTS,
): MonitorPoint[] {
  if (!point) return points;
  const limit = Number.isFinite(max) && max > 0 ? Math.floor(max) : MAX_MONITOR_POINTS;
  return [...points, point].slice(-limit);
}

/** 构建监控曲线 SVG 折线坐标。数据为空时返回空串 */
export function buildMonitorChart(points: MonitorPoint[], width = 320, height = 120): MonitorChartGeometry {
  const w = Number.isFinite(width) && width > 0 ? width : 320;
  const h = Number.isFinite(height) && height > 0 ? height : 120;
  if (!points || points.length === 0) return { width: w, height: h, cpuPoints: '', memPoints: '' };

  const toPoints = (pick: (item: MonitorPoint) => number) => {
    const step = points.length > 1 ? w / (points.length - 1) : 0;
    return points
      .map((item, index) => {
        const y = h - (toPercent(pick(item)) ?? 0) * (h / 100);
        return `${Math.round(index * step)},${Math.round(y)}`;
      })
      .join(' ');
  };

  return {
    width: w,
    height: h,
    cpuPoints: toPoints((item) => item.cpu),
    memPoints: toPoints((item) => item.memory),
  };
}

/** 键值对象转为视图行 */
function toRows(source: Record<string, unknown>): WorkbenchRow[] {
  return Object.entries(source).map(([key, value]) => {
    const link = extractLink(value);
    return {
      key,
      text: formatCell(value),
      link: link ? { ...link, external: isExternalLink(link.href) } : null,
    };
  });
}

/** 构建个人信息卡视图。部件数据为空时回退聚合响应中的 profile/user 节点 */
function buildProfile(
  data: WorkbenchData | null | undefined,
  payload: Record<string, unknown>,
): WorkbenchWidgetView['profile'] {
  const source = Object.keys(payload).length > 0 ? payload : asRecord(data?.profile);
  const name = String(source.name ?? data?.user?.name ?? '');
  const displayName = String(source.displayName ?? data?.user?.displayName ?? name ?? '');
  const roles = normalizeRoles(
    (source.roleNames as string | string[] | undefined) ?? data?.user?.roles ?? data?.profile?.roleNames,
  );

  return {
    name,
    displayName: displayName || '-',
    initial: Array.from(displayName || name || '?')[0] || '?',
    online: Boolean(source.online),
    roles,
    rows: [
      { key: 'logins', text: formatCell(source.logins ?? 0), link: null },
      { key: 'lastLogin', text: formatCell(source.lastLogin), link: null },
      { key: 'lastLoginIP', text: formatCell(source.lastLoginIP), link: null },
      { key: 'registerTime', text: formatCell(source.registerTime), link: null },
    ],
  };
}

/** 构建单个内容卡视图 */
function buildWidgetView(widget: WorkbenchWidget, data?: WorkbenchData | null): WorkbenchWidgetView {
  const payload = asRecord(widget.data);
  const base: WorkbenchWidgetView = {
    name: widget.name || '',
    title: widget.title || widget.name || '',
    icon: widget.icon,
    cols: normalizeCols(widget.cols),
    kind: 'empty',
  };

  if (widget.widgetType === 'Chart' || widget.name === 'Monitor') {
    return { ...base, kind: 'monitor' };
  }

  if (widget.name === 'Profile') {
    return { ...base, kind: 'profile', profile: buildProfile(data, payload) };
  }

  if (widget.name === 'QuickLink') {
    const links = Array.isArray(payload.links) ? (payload.links as WorkbenchQuickLink[]) : [];
    return {
      ...base,
      kind: 'quickLinks',
      links: links
        .filter((item) => item && item.url)
        .map((item) => ({ name: item.name || item.url, url: item.url, icon: item.icon })),
    };
  }

  if (widget.name === 'LoginLog') {
    const logins = Array.isArray(payload.logins) ? (payload.logins as Record<string, unknown>[]) : [];
    const onlines = Array.isArray(payload.onlines) ? (payload.onlines as Record<string, unknown>[]) : [];
    return {
      ...base,
      kind: 'loginLog',
      logins: logins.map((item) => ({
        title: formatCell(item.userName),
        subtitle: formatCell(item.action),
        time: formatCell(item.createTime),
      })),
      onlines: onlines.map((item) => ({
        title: formatCell(item.name),
        subtitle: formatCell(item.oAuthProvider),
        time: formatCell(item.createTime),
      })),
    };
  }

  if (payload && Object.keys(payload).length > 0) {
    return { ...base, kind: 'kv', rows: toRows(payload) };
  }

  if (widget.data === null || widget.data === undefined || widget.data === '') {
    return { ...base, kind: 'empty' };
  }
  if (typeof widget.data === 'object') {
    return Array.isArray(widget.data) ? { ...base, kind: 'text', text: formatCell(widget.data) } : { ...base, kind: 'empty' };
  }
  return { ...base, kind: 'text', text: formatCell(widget.data) };
}

/**
 * 构建工作台视图模型
 *
 * 输入后端聚合响应，输出皮肤可直接渲染的结构：KPI 数值已转文本、配色已收敛、
 * 内容卡已按呈现类型分类、快捷入口与系统信息行已归一化。
 */
export function buildWorkbenchView(data: WorkbenchData | null | undefined): WorkbenchView {
  const widgets = data?.widgets ?? [];
  const quickLinks = (data?.quickLinks ?? [])
    .filter((item) => item && item.url)
    .map((item) => ({ name: item.name || item.url, url: item.url, icon: item.icon }));

  return {
    displayName: String(data?.user?.displayName || data?.profile?.displayName || data?.user?.name || '-'),
    roles: normalizeRoles(data?.user?.roles ?? data?.profile?.roleNames),
    roleText: normalizeRoles(data?.user?.roles ?? data?.profile?.roleNames).join(' / '),
    kpis: (data?.kpis ?? []).map((kpi) => ({
      name: kpi.name || '',
      label: kpi.label || kpi.name || '',
      value: formatCell(kpi.value),
      trend: kpi.trend,
      color: normalizeKpiColor(kpi.color),
      url: kpi.url,
    })),
    widgets: widgets.map((widget) => buildWidgetView(widget, data)),
    quickLinks,
    sysInfoRows: toRows(asRecord(data?.sysInfo)),
    hasChartWidget: widgets.some((item) => item.widgetType === 'Chart' || item.name === 'Monitor'),
    hasContent: (data?.kpis?.length ?? 0) > 0 || widgets.length > 0,
  };
}

// ======================== 数据加载 ========================

/**
 * 创建工作台数据加载器
 *
 * 各皮肤自行管理响应式状态，这里只统一请求路径与成功判定：
 * `load()` 拉取聚合数据，`pollMonitor()` 拉取一次监控快照。
 */
export function createWorkbenchLoader(api: CubeApi) {
  return {
    /** 拉取工作台聚合数据 */
    async load(): Promise<{ data?: WorkbenchData; error?: string }> {
      const res = await api.page.getAction<WorkbenchData>(WORKBENCH_TYPE, WORKBENCH_DASHBOARD_ACTION);
      if (isSuccessCode(res?.code) && res.data) return { data: res.data };
      return { error: res?.message || 'Failed to load workbench data' };
    },

    /** 拉取一次监控快照 */
    async pollMonitor(): Promise<MonitorPoint | null> {
      const res = await api.page.getAction<MonitorSnapshot>(WORKBENCH_TYPE, WORKBENCH_MONITOR_ACTION);
      if (!isSuccessCode(res?.code)) return null;
      return toMonitorPoint(res.data);
    },
  };
}
