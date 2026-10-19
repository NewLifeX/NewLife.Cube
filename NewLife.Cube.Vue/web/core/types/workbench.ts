/**
 * 工作台（Dashboard）数据契约
 *
 * 对应后端 `Admin/Index/Dashboard` 聚合端点返回的 data 结构（camelCase）。
 * KPI 卡片由 WidgetManager 的 Kpi 部件驱动，内容卡由 Content/Kv/Chart 部件驱动；
 * 前端只消费聚合结果，不自行拼装统计口径。
 */

/** 工作台 KPI 指标卡 */
export interface WorkbenchKpi {
  /** 部件名称，如 UserCount */
  name: string;
  /** 显示标题，如 用户总数 */
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

/** 工作台内容卡。data 结构由各 Widget 决定，前端按 widgetType/name 渲染 */
export interface WorkbenchWidget {
  /** 部件名称，如 Profile/Monitor/SysInfo */
  name: string;
  /** 卡片标题 */
  title: string;
  /** 图标名。FontAwesome 名或 Element Plus 图标名 */
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

/** 当前登录用户摘要 */
export interface WorkbenchUser {
  name?: string;
  displayName?: string;
  roles?: string[];
}

/** 工作台个人信息卡数据 */
export interface WorkbenchProfile {
  name?: string;
  displayName?: string;
  roleNames?: string | string[];
  online?: boolean;
  logins?: number;
  lastLogin?: string;
  lastLoginIP?: string;
  registerTime?: string;
}

/** 快捷入口 */
export interface WorkbenchQuickLink {
  name: string;
  url: string;
  icon?: string;
}

/** 工作台聚合响应 */
export interface WorkbenchData {
  /** 当前用户摘要 */
  user?: WorkbenchUser;
  /** 可见 KPI 指标 */
  kpis: WorkbenchKpi[];
  /** 已隐藏 KPI 指标（供后续布局恢复使用） */
  hiddenKpis?: WorkbenchKpi[];
  /** 可见内容卡 */
  widgets: WorkbenchWidget[];
  /** 已隐藏内容卡（供后续布局恢复使用） */
  hiddenWidgets?: WorkbenchWidget[];
  /** 快捷入口。最近访问优先，菜单补足 */
  quickLinks?: WorkbenchQuickLink[];
  /** 个人信息。与 Profile 部件数据一致 */
  profile?: WorkbenchProfile;
  /** 系统信息键值表 */
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

/** 部件数据中的快捷链接结构 */
export interface QuickLinkWidgetData {
  links?: WorkbenchQuickLink[];
}

/** 登录与在线部件数据 */
export interface LoginLogWidgetData {
  logins?: Array<{
    createTime?: string;
    userName?: string;
    action?: string;
    createIP?: string;
  }>;
  onlines?: Array<{
    name?: string;
    createTime?: string;
    oAuthProvider?: string;
  }>;
}
