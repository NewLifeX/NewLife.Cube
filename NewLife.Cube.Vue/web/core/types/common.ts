/** 基础实体接口 */
export interface BaseEntity {
  id: number;
  createTime?: string;
  updateTime?: string;
  createUser?: string;
  updateUser?: string;
  remark?: string;
}

/** 分页参数 */
export interface PageInfo {
  pageIndex: number;
  pageSize: number;
  total?: number;
}

/** 标准分页信息 - 与API返回格式一致 */
export interface ApiPageInfo {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  longTotalCount: string;
}

/* 分页请求模板 */
export const pageInfoDefault: PageInfo = {
  pageIndex: 1,
  pageSize: 10,
  total: 0,
};

/** 标准API响应结构 - 与后端返回格式完全一致 */
export interface ApiResponse<T = unknown> {
  /** 响应状态码，0或200表示成功 */
  code: number;
  /** 响应消息 */
  message: string | null;
  /** 响应数据 */
  data: T;
  /** 链路追踪ID */
  traceId?: string | null;
  /** 分页信息 */
  page?: ApiPageInfo | null;
  /** 统计信息 */
  stat?: Record<string, unknown> | null;
}

/** 列表响应结构 - 业务层使用的简化格式 */
export interface ListResponse<T = unknown> {
  list: T[];
  total: number;
  page?: ApiPageInfo;
}

/** 表单类型 */
export type FormType = 'add' | 'edit' | 'view';

/** 启用状态 */
export interface EnableStatus {
  enable: boolean;
}

/** 下拉选项 */
export interface SelectOption {
  value: number | string;
  label: string;
}
