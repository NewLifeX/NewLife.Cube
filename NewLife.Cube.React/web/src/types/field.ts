/**
 * 字段映射共享类型定义（对齐 Vue 皮肤 core/types/field.ts）
 *
 * 后端 GetPage 下发的字段元数据（NewLife.Cube `DataField`）经归一为 `FieldMeta`，
 * 由 `utils/fieldControl.ts` 统一解析为控件类型，驱动列表渲染 / 动态搜索 / 表单编辑三处视图。
 */
import type { DataField } from '@newlifex/api-core';

/** 表单 / 编辑控件类型（全项目唯一枚举，对齐 Vue ControlType） */
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

/** 动态搜索控件类型（搜索区子集，对齐 Vue SearchControlType） */
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

/** 列表单元格渲染类型（对齐 Vue ListControlType） */
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

/** 选项 */
export interface FieldOption {
  value: string | number;
  label: string;
}

/** 统一字段元数据（对齐 Vue FieldMeta；后端 DataField 前端归一结构） */
export interface FieldMeta {
  /** 字段名（与后端属性名一致，如 Id / Name / Kind） */
  name: string;
  /** 显示名（中文标签） */
  displayName?: string;
  /** 分类（配置页表单分组，对应后端 [Category]） */
  category?: string;
  /** CLR 类型名：String / Int32 / Int64 / Decimal / Double / Single / Boolean / DateTime / TimeSpan / Enum / Guid / 枚举真实类型名 */
  typeName: string;
  /** 后端 ItemType：image / file / json / html / markdown / color / icon / mail / mobile / url / singleSelect / multipleSelect / date / time 等 */
  itemType?: string;
  /** 字段长度（String 大文本判定依据，Length >= 300 视为大文本） */
  length?: number;
  /** 数值精度（总位数） */
  precision?: number;
  /** 小数位数（用于 inputNumber 的 step/precision） */
  scale?: number;
  /** 是否允许空 */
  nullable?: boolean;
  /** 是否必填 */
  required?: boolean;
  /** 是否主键 */
  primaryKey?: boolean;
  /** 是否只读 */
  readOnly?: boolean;
  /** 描述（用作表单占位提示） */
  description?: string;
  /** 值集编码（枚举 / singleSelect / multipleSelect 由后端静态构造下发，前端绝不硬编码） */
  lovCode?: string;
  /** 是否多选（multipleSelect 为 true） */
  multiple?: boolean;
  /** 数据字典（键值对列表） */
  dataSource?: Record<string, string>;
  /** 选项（LOV 模式一般留空，由接口拉取） */
  options?: FieldOption[];
  /** 链接 URL（支持变量替换 {Id} 等） */
  url?: string;
  /** 链接目标 */
  target?: string;
}

/**
 * 后端 DataField → 前端 FieldMeta（透传字段）
 *
 * @param field 后端字段元数据
 * @returns 前端统一字段元数据
 */
export function toFieldMeta(field: DataField): FieldMeta {
  return {
    name: field.name,
    displayName: field.displayName,
    category: field.category,
    typeName: field.typeName ?? 'String',
    itemType: field.itemType,
    length: field.length,
    precision: field.precision,
    scale: field.scale,
    nullable: field.nullable,
    required: field.required,
    primaryKey: field.primaryKey,
    readOnly: field.readOnly,
    description: field.description,
    lovCode: field.lovCode,
    multiple: field.multiple,
    dataSource: field.dataSource,
    url: field.url,
    target: field.target,
  };
}
