/**
 * LOV（List of Values）值集系统类型定义
 *
 * 对应后端 Cube Admin LovController 的 Meta / ListData / BatchLabel 接口。
 * 值集分两类：
 *   - ENUM（枚举型）：预定义选项列表，options 内联在 Meta 响应中
 *   - LIST（列表型）：通过代理接口动态查询，支持搜索、分页、列翻译
 */

// ─── 枚举型值集 ───────────────────────────────────────────

/** 枚举值项（对应后端 LovEnumItem 实体） */
export interface LovEnumOption {
  /** 枚举值 */
  value: string;
  /** 显示文本 */
  label: string;
  /** 额外数据（可选） */
  extra?: string | null;
}

// ─── 列表型值集配置 ───────────────────────────────────────

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
}

/** 列表型值集的搜索字段配置（对应后端 LovSearchField） */
export interface LovSearchField {
  /** 字段名 */
  field: string;
  /** 显示标题 */
  title: string;
  /** 控件类型: input / select / lov / datepicker */
  componentType: string;
  /** 传参方式: BODY / QUERY */
  paramType: string;
  /** 是否必填 */
  required: boolean;
  /** 默认值 */
  defaultValue: string | null;
  /** 关联值集编码（该字段渲染为关联值集的选择控件） */
  refLovCode: string | null;
}

/** 列表型值集的表格列配置（对应后端 LovTableColumn） */
export interface LovTableColumn {
  /** 字段名 */
  field: string;
  /** 显示标题 */
  title: string;
  /** 列宽 */
  width: number;
  /** 对齐方式: left / center / right */
  align: string;
  /** 是否可排序 */
  sortable: boolean;
  /** 关联值集编码（该列原始值需翻译为此值集的显示文本） */
  refLovCode: string | null;
  /** 格式化类型: date / amount 等，与 refLovCode 互斥 */
  formatType: string | null;
}

// ─── Meta API 响应类型 ────────────────────────────────────

/** Meta 接口返回的单条值集元数据（联合类型，根据 type 区分） */
export type LovMetaItem =
  | LovEnumMeta
  | LovListMeta;

/** 枚举型值集元数据 */
export interface LovEnumMeta {
  lovCode: string;
  type: 'ENUM';
  name: string;
  /** 枚举选项列表 */
  options: LovEnumOption[];
}

/** 列表型值集元数据 */
export interface LovListMeta {
  lovCode: string;
  type: 'LIST';
  name: string;
  /** 值字段名 */
  valueField: string | null;
  /** 标签字段名 */
  labelField: string | null;
  /** 数据源配置 */
  listConfig: LovListConfig | null;
  /** 搜索字段配置 */
  searchFields: LovSearchField[];
  /** 表格列配置 */
  tableColumns: LovTableColumn[];
}

/** Meta 接口完整响应体 */
export interface LovMetaResponse {
  /** 值集元数据数组 */
  meta: LovMetaItem[];
  /** 内联的枚举型值集 options（key 为 LovCode） */
  inlineEnums?: Record<string, LovEnumOption[]> | null;
}

// ─── ListData API 类型 ────────────────────────────────────

/** ListData 接口请求参数 */
export interface LovListDataRequest {
  /** 值集编码 */
  lovCode: string;
  /** 搜索参数 */
  params?: Record<string, unknown>;
  /** 页码 */
  pageNum?: number;
  /** 每页条数 */
  pageSize?: number;
}

/** ListData 接口响应 */
export interface LovListDataResponse<T = Record<string, unknown>> {
  /** 数据列表 */
  data: T[];
  /** 总条数 */
  total: number;
}

// ─── BatchLabel API 类型 ──────────────────────────────────

/** BatchLabel 接口请求参数 */
export interface LovBatchLabelRequest {
  /** 值集编码 */
  lovCode: string;
  /** 需要翻译的原始值列表 */
  values: string[];
}

/** BatchLabel 接口响应（value → label 映射） */
export type LovBatchLabelResponse = Record<string, string>;

// ─── 前端 LovSelect 组件 Props 类型 ───────────────────────

/** LovSelect 组件属性 */
export interface LovSelectProps {
  /** 值集编码，如 "Enum.ProcessCard.Status" */
  code: string;
  /** 强制指定类型（一般从 code 前缀自动推断） */
  type?: string;
  /** v-model 值 */
  modelValue?: string | number;
  /** 占位文本 */
  placeholder?: string;
  /** 是否可清除 */
  clearable?: boolean;
  /** 是否禁用 */
  disabled?: boolean;
  /** 尺寸 */
  size?: 'large' | 'default' | 'small';
}

/** LovSelect 组件事件 */
export interface LovSelectEmits {
  (e: 'update:modelValue', value: string | number | undefined): void;
  (e: 'change', value: string | number | undefined): void;
}

// ─── LovSelectTable 组件 Props 类型 ────────────────────────

/** LovSelectTable 弹窗组件属性 */
export interface LovSelectTableProps {
  dialogVisible: boolean;
  lovCode: string;
  lovMeta: LovListMeta | null;
  inlineEnums: Record<string, LovEnumOption[]>;
  translateCache: Map<string, string>;
}

/** LovSelectTable 组件事件 */
export interface LovSelectTableEmits {
  (e: 'update:dialogVisible', value: boolean): void;
  (e: 'select', row: Record<string, unknown>): void;
}
