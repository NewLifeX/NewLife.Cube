/**
 * 字段映射共享类型定义（全项目唯一契约来源）
 *
 * 后端 GetPage 下发的字段元数据（NewLife.Cube `DataField`）经前端归一为 `FieldMeta`，
 * 由 `web/core/utils/fieldControl.ts` 统一解析为控件类型，驱动
 * 列表渲染 / 动态搜索 / 表单编辑 三处视图。
 *
 * 后端 `DataField` 序列化为 camelCase：name / displayName / typeName / itemType /
 * length / nullable / primaryKey / readOnly / lovCode / description。
 */

/** 表单 / 编辑控件类型（全项目唯一枚举） */
export type ControlType =
  | 'input' // 普通文本输入
  | 'textarea' // 大文本多行
  | 'inputNumber' // 数值输入
  | 'switch' // 布尔开关
  | 'datePicker' // 日期时间选择
  | 'timePicker' // 时间选择（HH:mm:ss）
  | 'lov' // 值集单选（枚举 / 单选）
  | 'lovMulti' // 值集多选
  | 'upload' // 文件上传
  | 'image' // 图片上传 + 预览
  | 'json' // Json 编辑器
  | 'richHtml' // 富文本（html）
  | 'richMarkdown' // 富文本（markdown）
  | 'color' // 颜色选择器
  | 'icon' // 图标选择器
  | 'email' // 邮箱（带校验）
  | 'tel' // 手机（带校验）
  | 'url' // 网址（带校验）
  | 'readonly'; // 只读文本（Guid 等）

/** 动态搜索控件类型（搜索区子集） */
export type SearchControlType =
  | 'text' // 模糊文本
  | 'numberRange' // 数值范围
  | 'dateRange' // 日期范围
  | 'datetimeRange' // 日期时间范围
  | 'timeRange' // 时间范围
  | 'lov' // 值集单选
  | 'lovMulti' // 值集多选
  | 'switch' // 布尔（是 / 否）
  | 'fileExists'; // 附件存在性（有 / 无）

/** 列表单元格渲染类型 */
export type ListControlType =
  | 'text' // 普通文本
  | 'number' // 数值（右对齐）
  | 'boolean' // 布尔（开关 / 标签）
  | 'date' // 日期
  | 'time' // 时间（HH:mm:ss）
  | 'color' // 颜色色块
  | 'icon' // 图标预览
  | 'image' // 图片缩略图
  | 'json' // Json 折叠展示
  | 'html' // 富文本摘要
  | 'lov' // 值集翻译（标签）
  | 'file' // 文件链接
  | 'readonly'; // 只读文本（Guid）

/** 选项（保留字段，LOV 模式下前端经 lovCode 从接口拉取，不再前端硬编码） */
export interface FieldOption {
  value: string | number;
  label: string;
}

/**
 * 统一字段元数据。后端 `DataField` 前端归一结构。
 */
export interface FieldMeta {
  /** 字段名（与后端属性名一致，如 Id / Name / Kind） */
  name: string;
  /** 显示名（中文标签） */
  displayName?: string;
  /** CLR 类型名：String / Int32 / Int64 / Decimal / Double / Single / Boolean / DateTime / TimeSpan / Enum / Guid / 枚举真实类型名 */
  typeName: string;
  /** 后端 ItemType：image / file / json / html / markdown / color / icon / mail / mobile / url / singleSelect / multipleSelect / date / time 等 */
  itemType?: string;
  /** 字段长度（String 大文本判定依据，Length >= 300 视为大文本） */
  length?: number;
  /** 数值精度（后端 BindColumn.Precision，总位数） */
  precision?: number;
  /** 小数位数（后端 BindColumn.Scale，用于 el-input-number 的 step/precision） */
  scale?: number;
  /** 是否允许空 */
  nullable?: boolean;
  /** 是否主键 */
  primaryKey?: boolean;
  /** 是否只读 */
  readOnly?: boolean;
  /** 描述（用作表单占位提示） */
  description?: string;
  /** 值集编码（枚举 / singleSelect / multipleSelect 由后端静态构造下发，前端绝不硬编码） */
  lovCode?: string;
  /** 是否多选（multipleSelect 为 true；亦可由前端在映射时推导） */
  multiple?: boolean;
  /** 选项（保留，LOV 模式一般留空） */
  options?: FieldOption[];
}

/** 搜索字段（列表搜索区消费，继承 FieldMeta 并附带解析后的搜索控件类型） */
export interface SearchFieldMeta extends FieldMeta {
  /** 搜索控件类型（由 resolveSearchControl 解析） */
  searchType: SearchControlType;
}
