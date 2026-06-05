// 魔方系统相关类型定义

// 应用管理
export interface App {
  id: number;
  name: string;
  displayName: string;
  category: string;
  version: string;
  fileName: string;
  arguments: string;
  workingDirectory: string;
  userName: string;
  autoStart: boolean;
  enable: boolean;
  description: string;
  createTime: string;
  updateTime: string;
  createUser: string;
  updateUser: string;
}

// 应用日志
export interface AppLog {
  id: number;
  appId: number;
  appName: string;
  action: string;
  success: boolean;
  remark: string;
  createTime: string;
  createUser: string;
  createIP: string;
  updateTime: string;
}

// 地区管理
export interface Area {
  id: number;
  name: string;
  fullName: string;
  code: string;
  parentId: number;
  level: number;
  pinyin: string;
  jianpin: string;
  longitude: number;
  latitude: number;
  enable: boolean;
  createTime: string;
  updateTime: string;
  children?: Area[];
}

// 附件管理
export interface Attachment {
  id: number;
  fileName: string;
  displayName: string;
  category: string;
  size: number;
  contentType: string;
  hash: string;
  downloads: number;
  enable: boolean;
  description: string;
  createTime: string;
  createUser: string;
  updateTime: string;
  updateUser: string;
}

// 定时任务
export interface CronJob {
  id: number;
  name: string;
  displayName: string;
  cron: string;
  server: string;
  mode: string;
  data: string;
  start: string;
  end: string;
  enable: boolean;
  times: number;
  maxTimes: number;
  success: number;
  error: number;
  maxError: number;
  lastTime: string;
  nextTime: string;
  description: string;
  createTime: string;
  updateTime: string;
  createUser: string;
  updateUser: string;
}

// 订单管理
export interface OrderManager {
  id: number;
  code: string;
  name: string;
  amount: number;
  status: number;
  customerName: string;
  customerPhone: string;
  customerAddress: string;
  remark: string;
  createTime: string;
  updateTime: string;
  createUser: string;
  updateUser: string;
}

// 主体代理
export interface PrincipalAgent {
  id: number;
  name: string;
  displayName: string;
  type: string;
  category: string;
  contactPerson: string;
  contactPhone: string;
  contactEmail: string;
  address: string;
  enable: boolean;
  remark: string;
  createTime: string;
  updateTime: string;
  createUser: string;
  updateUser: string;
}

// API响应类型
export interface ApiResponse<T> {
  code: number;
  message: string;
  data: T;
  success: boolean;
}

// 列表响应类型
export interface ApiListResponse<T> {
  code: number;
  message: string;
  data: {
    rows: T[];
    total: number;
    count: number;
    list: T[];
  };
  success: boolean;
}

// 分页参数
export interface PageParams {
  page: number;
  pageSize: number;
  pageIndex: number;
}

// 搜索参数基类
export interface BaseSearchParams extends PageParams {
  q?: string;
}
