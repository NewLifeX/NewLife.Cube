/**
 * DataField 元数据到 UI 组件的映射规则引擎
 *
 * 根据后端返回的 DataField 元数据，自动推断应使用的前端组件类型。
 * 各皮肤包基于此规则引擎，将抽象组件类型映射到具体 UI 库组件。
 */

import type { DataField } from '../api';

/** 抽象组件类型，各皮肤包自行映射到具体组件 */
export type WidgetType =
  | 'text'           // 单行文本
  | 'textarea'       // 多行文本
  | 'number'         // 数字输入
  | 'select'         // 下拉选择
  | 'checkbox'       // 复选框
  | 'switch'         // 开关
  | 'date'           // 日期选择
  | 'datetime'       // 日期时间
  | 'time'           // 时间选择
  | 'password'       // 密码
  | 'readonly'       // 只读文本
  | 'link'           // 链接
  | 'image'          // 图片/头像
  | 'file'           // 文件上传
  | 'email'          // 邮箱
  | 'phone'          // 手机号
  | 'url'            // URL
  | 'color'          // 颜色选择
  | 'code'           // 代码编辑器
  | 'html'           // 富文本
  | 'markdown'       // Markdown 富文本
  | 'json'           // JSON 编辑器
  | 'icon'           // 图标选择
  | 'lov'            // 值集单选（LOV，选项经 lovCode 拉取或 dataSource）
  | 'lovMulti';      // 值集多选（LOV，多选转逗号分隔）

/** 字段映射结果 */
export interface FieldMapping {
  /** 推断的组件类型 */
  widget: WidgetType;
  /** 原始字段元数据 */
  field: DataField;
  /** 附加属性（由推断逻辑填充） */
  props?: Record<string, unknown>;
}

/**
 * 根据 DataField 元数据推断组件类型
 *
 * 判断优先级：只读 → itemType → lovCode → 数据字典 → url 字段 → typeName → 名称模式 → 长度 → 默认
 *
 * @param field 字段元数据
 * @returns 映射结果
 */
export function resolveWidget(field: DataField): FieldMapping {
  // 1. 只读字段
  if (field.readOnly || field.primaryKey) {
    return { widget: 'readonly', field };
  }

  // 2. itemType 精确匹配（后端显式指定 UI 类型）
  const itemType = (field.itemType ?? '').toLowerCase();
  if (itemType) {
    const itemTypeMap: Record<string, WidgetType> = {
      mail: 'email',
      email: 'email',
      mobile: 'phone',
      phone: 'phone',
      fax: 'phone',
      image: 'image',
      avatar: 'image',
      file: 'file',
      attachment: 'file',
      url: 'url',
      link: 'link',
      color: 'color',
      code: 'code',
      html: 'html',
      richtext: 'html',
      markdown: 'markdown',
      json: 'json',
      icon: 'icon',
      password: 'password',
      textarea: 'textarea',
      time: 'time',
      singleselect: 'lov',
      lovtable: 'lov',
      multipleselect: 'lovMulti',
      lovtablemulti: 'lovMulti',
    };
    const mapped = itemTypeMap[itemType];
    if (mapped) {
      // 单选/多选 LOV 控件透传 lovCode
      if ((mapped === 'lov' || mapped === 'lovMulti') && field.lovCode) {
        return { widget: mapped, field, props: { lovCode: field.lovCode, multiple: mapped === 'lovMulti' } };
      }
      return { widget: mapped, field };
    }
  }

  // 3. lovCode 优先于 dataSource（后端静态构造下发的值集）
  if (field.lovCode) {
    const multiple = field.multiple || itemType === 'multipleselect' || itemType === 'lovtablemulti';
    return { widget: multiple ? 'lovMulti' : 'lov', field, props: { lovCode: field.lovCode, multiple } };
  }

  // 4. 有数据字典/枚举 → 下拉选择（字段名以 s 结尾或显式多选 → 多选，对齐 MVC _Form_Item 约定）
  if (field.dataSource && Object.keys(field.dataSource).length > 0) {
    const name = field.name ?? '';
    const multi =
      field.multiple ||
      itemType === 'multipleselect' ||
      itemType === 'lovtablemulti' ||
      /s$/i.test(name);
    return { widget: multi ? 'lovMulti' : 'select', field, props: { multiple: multi } };
  }

  // 5. 有 url 属性 → 链接
  if (field.url) {
    return { widget: 'link', field, props: { href: field.url, target: field.target } };
  }

  // 6. 按 typeName 推断
  const typeName = (field.typeName ?? '').toLowerCase();

  if (typeName === 'boolean') {
    return { widget: 'switch', field };
  }

  if (typeName === 'datetime' || typeName === 'datetimeoffset') {
    // 纯日期字段（ItemType=date 或字段名以 Date/day 结尾，无 itemType）→ 日期选择（对齐 MVC _Form_DateTime）
    const name = field.name ?? '';
    const isDateOnly = itemType === 'date' || (!itemType && /(date|day)$/i.test(name));
    return { widget: isDateOnly ? 'date' : 'datetime', field };
  }

  if (typeName === 'date') {
    return { widget: 'date', field };
  }

  if (typeName === 'timespan') {
    return { widget: 'time', field };
  }

  if (typeName === 'guid') {
    return { widget: 'readonly', field };
  }

  if (['int32', 'int64', 'int16', 'decimal', 'double', 'single', 'float', 'byte', 'uint32', 'uint64', 'sbyte'].includes(typeName)) {
    return { widget: 'number', field, props: { precision: field.scale } };
  }

  // 枚举类型名（非标准基元，无 lovCode 兜底时按 lov 渲染）
  // 真实枚举类型名（如 SexKinds）不在已知基元集合中，视为枚举 → 下拉（选项由后端 dataSource 下发）
  const PRIMITIVE_TYPES = new Set([
    'string', 'boolean', 'datetime', 'datetimeoffset', 'date', 'timespan', 'guid',
    'int32', 'int64', 'int16', 'decimal', 'double', 'single', 'float', 'byte',
    'sbyte', 'uint32', 'uint64', 'enum',
  ]);
  if (typeName === 'enum' || (typeName && !PRIMITIVE_TYPES.has(typeName))) {
    return { widget: 'lov', field };
  }

  // 7. 名称模式匹配
  const name = field.name ?? '';
  if (/password|pwd|secret/i.test(name)) {
    return { widget: 'password', field };
  }
  if (/e?mail/i.test(name)) {
    return { widget: 'email', field };
  }
  if (/mobile|phone|tel/i.test(name)) {
    return { widget: 'phone', field };
  }
  if (/avatar|logo|icon|photo|image/i.test(name)) {
    return { widget: 'image', field };
  }
  if (/url|href|link/i.test(name)) {
    return { widget: 'url', field };
  }

  // 8. 长文本（长度 > 200 或包含特定名称）
  if ((field.length && field.length > 200) || /remark|content|body|description|summary|memo/i.test(name)) {
    return { widget: 'textarea', field };
  }

  // 9. 默认单行文本
  return { widget: 'text', field };
}

/**
 * 批量推断字段映射
 * @param fields 字段元数据数组
 * @returns 映射结果数组
 */
export function resolveWidgets(fields: DataField[]): FieldMapping[] {
  return fields.map(resolveWidget);
}

/** 列宽规范：数值列最小宽度（像素） */
export const NUMBER_COLUMN_WIDTH = 100;

/** 列宽规范：时间列宽度（像素） */
export const TIME_COLUMN_WIDTH = 140;

/** 列宽规范：主键列宽度（像素） */
export const KEY_COLUMN_WIDTH = 80;

/** 列宽规范：图标/颜色/布尔等窄列宽度（像素） */
export const NARROW_COLUMN_WIDTH = 90;

/** 列宽规范：图片/文件列宽度（像素） */
export const MEDIA_COLUMN_WIDTH = 100;

/** 数值类型名集合（大小写不敏感） */
const NUMBER_TYPES = new Set([
  'int16',
  'int32',
  'int64',
  'uint16',
  'uint32',
  'uint64',
  'byte',
  'sbyte',
  'single',
  'double',
  'decimal',
  'intptr',
  'uintptr',
]);

/** 时间类型名集合（大小写不敏感） */
const TIME_TYPES = new Set(['datetime', 'datetimeoffset', 'dateonly', 'date', 'time', 'timespan']);

/**
 * 计算列表列宽（SKIN-12 列宽规范）
 *
 * 优先级：呈现配置 `width`（数字）→ 主键 → 图片/文件 → 图标/颜色/布尔 → 时间 → 数值 → 自适应（undefined）。
 * 字符串列不设固定宽度，交给表格自适应；`width: 'auto'` 同样视为自适应。
 *
 * @param field 字段元数据
 * @returns 像素宽度；undefined 表示自适应
 * @example resolveColumnWidth({ name: 'Id', primaryKey: true }) // 80
 * @example resolveColumnWidth({ name: 'CreateTime', typeName: 'DateTime' }) // 140
 * @example resolveColumnWidth({ name: 'Name', typeName: 'String' }) // undefined
 */
export function resolveColumnWidth(field: DataField): number | undefined {
  if (!field) return undefined;
  if (typeof field.width === 'number' && field.width > 0) return field.width;
  if (field.width === 'auto') return undefined;
  if (field.primaryKey) return KEY_COLUMN_WIDTH;

  const itemType = (field.itemType || '').toLowerCase();
  if (itemType === 'image' || itemType === 'avatar' || itemType === 'file' || itemType === 'upload') return MEDIA_COLUMN_WIDTH;
  if (itemType === 'color' || itemType === 'icon' || itemType === 'switch' || itemType === 'boolean' || itemType === 'checkbox') {
    return NARROW_COLUMN_WIDTH;
  }

  const typeName = (field.typeName || '').toLowerCase();
  if (TIME_TYPES.has(typeName)) return TIME_COLUMN_WIDTH;
  if (typeName === 'boolean' || typeName === 'bool') return NARROW_COLUMN_WIDTH;
  if (NUMBER_TYPES.has(typeName)) return NUMBER_COLUMN_WIDTH;

  return undefined;
}

/**
 * 计算列表列对齐（SKIN-12 列宽规范配套）
 *
 * 数值右对齐便于比较大小；主键与图标/颜色/布尔居中；其余左对齐。
 *
 * @param field 字段元数据
 * @returns 对齐方式
 * @example resolveColumnAlign({ name: 'Count', typeName: 'Int32' }) // 'right'
 */
export function resolveColumnAlign(field: DataField): 'left' | 'center' | 'right' {
  if (!field) return 'left';
  if (field.primaryKey) return 'center';

  const itemType = (field.itemType || '').toLowerCase();
  if (['image', 'avatar', 'file', 'upload', 'color', 'icon', 'switch', 'boolean', 'checkbox'].includes(itemType)) return 'center';

  const typeName = (field.typeName || '').toLowerCase();
  if (NUMBER_TYPES.has(typeName)) return 'right';
  if (typeName === 'boolean' || typeName === 'bool') return 'center';

  return 'left';
}

