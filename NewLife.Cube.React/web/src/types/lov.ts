/**
 * LOV（List of Values）值集系统类型定义（对齐 Vue 皮肤 core/types/lov.ts）
 *
 * 对应后端 Admin LovController 的 Meta / ListData 接口。
 * 值集分两类：
 *   - ENUM（枚举型）：预定义选项列表，options 内联在 Meta 响应中
 *   - LIST（列表型）：通过代理接口动态查询，支持搜索、分页、列翻译
 */

/** 枚举值项（对应后端 LovEnumItem 实体） */
export interface LovEnumOption {
  /** 枚举值 */
  value: string;
  /** 显示文本 */
  label: string;
  /** 额外数据（可选） */
  extra?: string | null;
}

/** 列表型值集的数据源配置（对应后端 LovListConfig） */
export interface LovListConfig {
  /** 代理请求地址 */
  requestUrl: string;
  /** HTTP 方法 GET/POST */
  method: string;
  /** 是否支持分页 */
  pageable: boolean;
  /** 页码字段名 */
  pageNumField: string | null;
  /** 每页条数字段名 */
  pageSizeField: string | null;
  /** 数据路径（从响应中提取数组的 JSON 路径） */
  dataPath: string | null;
  /** 总数路径（从响应中提取总数的 JSON 路径） */
  totalPath: string | null;
  /** 固定参数字典 */
  fixedParams: Record<string, string> | null;
  /** 是否代理请求（true=后端 /Admin/Lov/ListData 转发；false=前端直连） */
  proxyRequest?: boolean;
}

/** 列表型值集的搜索字段配置（对应后端 LovSearchField） */
export interface LovSearchField {
  field: string;
  title: string;
  /** 控件类型: input / select / lov / datepicker */
  componentType: string;
  /** 传参方式: BODY / QUERY */
  paramType: string;
  required: boolean;
  defaultValue: string | null;
  refLovCode: string | null;
}

/** 列表型值集的表格列配置（对应后端 LovTableColumn） */
export interface LovTableColumn {
  field: string;
  title: string;
  width: number;
  /** left / center / right */
  align: string;
  sortable: boolean;
  /** 该列原始值需翻译为此值集的显示文本 */
  refLovCode: string | null;
  /** date / amount 等格式化，与 refLovCode 互斥 */
  formatType: string | null;
}

/** 枚举型值集元数据 */
export interface LovEnumMeta {
  lovCode: string;
  type: 'ENUM';
  name: string;
  options: LovEnumOption[];
}

/** 列表型值集元数据 */
export interface LovListMeta {
  lovCode: string;
  type: 'LIST';
  name: string;
  valueField: string | null;
  labelField: string | null;
  listConfig: LovListConfig | null;
  searchFields: LovSearchField[];
  tableColumns: LovTableColumn[];
}

/** Meta 接口返回的单条值集元数据（联合类型，根据 type 区分） */
export type LovMetaItem = LovEnumMeta | LovListMeta;

/** Meta 接口完整响应体 */
export interface LovMetaResponse {
  data: LovMetaItem[];
}

/** ListData 请求参数 */
export interface LovListDataRequest {
  lovCode: string;
  params?: Record<string, unknown>;
  pageNum?: number;
  pageSize?: number;
}

/** ListData 响应体 */
export interface LovListDataResponse<T = Record<string, unknown>> {
  data: T[];
  total: number;
}

/**
 * 解析值集编码的前缀，判断类型
 *
 * @example
 * resolveLovType('Enum.ProcessCard.Status') // => 'ENUM'
 * resolveLovType('List.User')              // => 'LIST'
 */
export function resolveLovType(lovCode: string): 'ENUM' | 'LIST' | null {
  const prefix = lovCode.split('.')[0];
  if (prefix === 'Enum') return 'ENUM';
  if (prefix === 'List') return 'LIST';
  return null;
}
