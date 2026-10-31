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
  | 'lovMulti'       // 值集多选（LOV，多选转逗号分隔）
  | 'area'           // 地区级联（ItemType=area/area1~4）
  | 'timeSpan'       // 时长（时:分:秒，值仍是整数存储）
  | 'mapSelect'      // 映射字段下拉（RoleName→RoleID，提交关联键）
  | 'mapInput';      // 映射字段键值输入（关联表过大、无数据源时退回）

/** 时长存储单位（由字段描述推断） */
export type TimeSpanUnit = 'milliseconds' | 'seconds' | 'minutes' | 'hours';

/** 时长分段（时/分/秒）。毫秒单位下秒可带 3 位小数 */
export interface TimeSpanParts {
  hours: number;
  minutes: number;
  seconds: number;
}

/** 各存储单位相对秒的换算系数（1 存储单位 = factor 秒） */
const TIME_SPAN_UNIT_SECONDS: Record<TimeSpanUnit, number> = {
  milliseconds: 0.001,
  seconds: 1,
  minutes: 60,
  hours: 3600,
};

/** 数值类型名集合（大小写不敏感） */
const NUMERIC_TYPE_NAMES: ReadonlySet<string> = new Set([
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
  'float',
]);

/** 字段映射结果 */
export interface FieldMapping {
  /** 推断的组件类型 */
  widget: WidgetType;
  /** 原始字段元数据 */
  field: DataField;
  /** 附加属性（由推断逻辑填充） */
  props?: Record<string, unknown>;
}

/** 数值归一：非法值按 0 处理，保留正负号（列表可显示负时长） */
function toFiniteNumber(value: unknown): number {
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

/** 非负数值归一：非法值或负值按 0 处理（时长输入不接受负数） */
function toNonNegativeNumber(value: unknown): number {
  const n = toFiniteNumber(value);
  return n > 0 ? n : 0;
}

/**
 * 从字段描述推断时长存储单位（与后端 `ViewHelper.FormatValue` 同口径）。
 *
 * 顺序：毫秒/ms → 小时/hour → 分/minute → 默认秒。
 *
 * @param description 字段描述，如「累计在线总时间，单位秒」
 * @returns 存储单位
 * @example timeSpanUnitOfDescription('耗时，单位毫秒') // 'milliseconds'
 */
export function timeSpanUnitOfDescription(description?: string): TimeSpanUnit {
  const text = (description ?? '').toLowerCase();
  if (/毫秒|ms|millisecond/.test(text)) return 'milliseconds';
  if (/小时|hour/.test(text)) return 'hours';
  if (/分|minute/.test(text)) return 'minutes';
  return 'seconds';
}

/**
 * 时长存储值 → 秒（保留负号，供列表格式化使用）。
 *
 * @param value 存储值
 * @param unit 存储单位
 * @returns 总秒数
 */
export function timeSpanToSeconds(value: unknown, unit: TimeSpanUnit): number {
  return toFiniteNumber(value) * TIME_SPAN_UNIT_SECONDS[unit];
}

/**
 * 秒 → 时长存储值，按存储单位的整数语义四舍五入。
 *
 * @param seconds 总秒数
 * @param unit 存储单位
 * @returns 存储值
 */
export function secondsToTimeSpan(seconds: number, unit: TimeSpanUnit): number {
  return Math.round(toFiniteNumber(seconds) / TIME_SPAN_UNIT_SECONDS[unit]);
}

/**
 * 时长存储值 → 时/分/秒分段（供输入组件回显）。负值按 0 处理。
 *
 * @param value 存储值
 * @param unit 存储单位
 * @returns 时/分/秒分段；毫秒单位下秒保留 3 位小数
 * @example splitTimeSpan(22515, 'seconds') // { hours: 6, minutes: 15, seconds: 15 }
 */
export function splitTimeSpan(value: unknown, unit: TimeSpanUnit): TimeSpanParts {
  const total = Math.max(0, timeSpanToSeconds(value, unit));
  let hours = Math.floor(total / 3600);
  let minutes = Math.floor((total % 3600) / 60);
  const factor = unit === 'milliseconds' ? 1000 : 1;
  let seconds = Math.round((total % 60) * factor) / factor;

  // 毫秒单位下 59.9996 会四舍五入成 60.000，需进位，避免出现「秒 = 60」这种非法分段
  if (seconds >= 60) {
    seconds = 0;
    minutes += 1;
  }
  if (minutes >= 60) {
    minutes = 0;
    hours += 1;
  }

  return { hours, minutes, seconds };
}

/**
 * 时/分/秒分段 → 时长存储值。各段非法或负数按 0 处理。
 *
 * @param parts 时/分/秒分段
 * @param unit 存储单位
 * @returns 存储值
 */
export function joinTimeSpan(parts: TimeSpanParts, unit: TimeSpanUnit): number {
  const hours = Math.floor(toNonNegativeNumber(parts.hours));
  const minutes = Math.floor(toNonNegativeNumber(parts.minutes));
  const seconds = toNonNegativeNumber(parts.seconds);

  return secondsToTimeSpan(hours * 3600 + minutes * 60 + seconds, unit);
}

/**
 * 解析 `decimal(p,s)` 形式的 ItemType。
 *
 * @param itemType 字段 ItemType
 * @returns 精度与小数位；非该形式返回 null
 */
export function parseDecimalItemType(itemType?: string): { precision: number; scale: number } | null {
  const match = /^decimal\(\s*(\d+)\s*,\s*(\d+)\s*\)$/i.exec((itemType ?? '').trim());
  return match ? { precision: Number(match[1]), scale: Number(match[2]) } : null;
}

/**
 * 地区字段层级：`area` 默认三级（省市区分到底），`area1~area4` 对应省/市/区/街道。
 *
 * @param field 字段元数据
 * @returns 层级 1~4
 */
export function areaLevelOf(field: DataField): number {
  const match = /^area([1-4])$/i.exec((field.itemType ?? '').trim());
  return match ? Number(match[1]) : 3;
}

/**
 * 表单实际读写的模型键。映射字段（`RoleName`通过 Map 指向 `RoleID`）要提交的是关联键。
 *
 * @param field 字段元数据
 * @returns 模型键
 */
export function mapValueKey(field: DataField): string {
  return field.mapField || field.name;
}

/**
 * 解析模型里实际存在的键（大小写不敏感兜底）。
 *
 * 后端元数据字段名为 PascalCase（`RoleID`），行数据/表单模型多为 camelCase（`roleID`），
 * 直接按元数据名读写会出现「同字段大小写双键共存」，提交时两个键都可能被绑定。
 *
 * @param key 期望的键
 * @param model 模型对象（可为空）
 * @returns 模型中实际存在的键；不存在时返回原键
 */
export function resolveExistingKey(key: string, model?: Record<string, unknown> | null): string {
  if (!model || !key) return key;
  if (Object.prototype.hasOwnProperty.call(model, key)) return key;
  const camel = key.charAt(0).toLowerCase() + key.slice(1);
  if (camel !== key && Object.prototype.hasOwnProperty.call(model, camel)) return camel;
  const pascal = key.charAt(0).toUpperCase() + key.slice(1);
  if (pascal !== key && Object.prototype.hasOwnProperty.call(model, pascal)) return pascal;

  // 「ID」「IP」这类全大写缩写首尾转换后仍是自身，再按大小写不敏感扫描兜底（行数据键为 id/ip）
  const lowerKey = key.toLowerCase();
  for (const k in model) {
    if (k.toLowerCase() === lowerKey && Object.prototype.hasOwnProperty.call(model, k)) return k;
  }
  return key;
}

/**
 * 表单实际读写的模型键：映射字段取关联键（`RoleName` → `RoleID`），并按模型大小写归一。
 *
 * @param field 字段元数据
 * @param model 当前表单/行模型
 * @returns 模型键
 * @example resolveModelKey({ name: 'RoleName', mapField: 'RoleID' }, { roleID: 3 }) // 'roleID'
 */
export function resolveModelKey(field: DataField, model?: Record<string, unknown> | null): string {
  return resolveExistingKey(mapValueKey(field), model);
}

/**
 * 映射键是否为数值类型（无数据源时据此退回数字输入而非只读文本）。
 *
 * @param field 字段元数据
 * @returns 是否数值键
 */
export function isNumericMapKey(field: DataField): boolean {
  return NUMERIC_TYPE_NAMES.has((field.mapTypeName ?? '').trim().toLowerCase());
}

/** 后端是否下发了取值字典（枚举 / 关联表） */
export function hasDataSource(field: DataField): boolean {
  return !!field.dataSource && Object.keys(field.dataSource).length > 0;
}

/**
 * 根据 DataField 元数据推断组件类型
 *
 * 判断优先级（对齐 MVC `_Form_Item` / `_Form_String` / `_Form_Editor`）：
 * itemType 精确/前缀 → 映射字段(Map) → lovCode → dataSource → typeName → 名称模式 → 长度 → 默认。
 *
 * @param field 字段元数据
 * @returns 映射结果
 * @example resolveWidget({ name: 'OnlineTime', typeName: 'Int32', itemType: 'TimeSpan' }).widget // 'timeSpan'
 * @example resolveWidget({ name: 'RoleName', mapField: 'RoleID', dataSource: { '1': '管理员' } }).widget // 'mapSelect'
 */
export function resolveWidget(field: DataField): FieldMapping {
  const itemType = (field.itemType ?? '').trim().toLowerCase();
  const rawItemType = (field.itemType ?? '').trim();
  const typeName = (field.typeName ?? '').trim().toLowerCase();
  const name = field.name ?? '';

  // 1. 映射字段（RoleName→RoleID 等）：虚拟属性本身只读，必须在只读判定之前处理。
  //    有 lovCode 走远程值集，有 dataSource 走本地下拉，整型键无数据源时退回键值输入，其余只读展示。
  if (field.mapField) {
    const base = { field, props: { mapField: field.mapField, mapTypeName: field.mapTypeName, multiple: !!field.multiple } };
    if (field.lovCode) {
      return {
        widget: field.multiple ? 'lovMulti' : 'lov',
        field,
        props: { ...base.props, lovCode: field.lovCode, dataSource: field.dataSource },
      };
    }
    if (hasDataSource(field)) return { widget: 'mapSelect', ...base };
    return { widget: isNumericMapKey(field) ? 'mapInput' : 'readonly', ...base };
  }

  // 2. 只读字段与主键
  if (field.readOnly || field.primaryKey) {
    return { widget: 'readonly', field };
  }

  // 3. itemType 前缀形式（image-jpg / file-zip / TimeSpan:format 等）
  if (itemType.startsWith('image')) return { widget: 'image', field };
  if (itemType.startsWith('file')) return { widget: 'file', field };
  if (itemType.startsWith('timespan')) {
    return {
      widget: 'timeSpan',
      field,
      props: {
        unit: timeSpanUnitOfDescription(field.description),
        format: rawItemType.includes(':') ? rawItemType.slice(rawItemType.indexOf(':') + 1).trim() : undefined,
      },
    };
  }

  // 4. itemType 精确匹配（后端显式指定 UI 类型）
  if (itemType) {
    const itemTypeMap: Record<string, WidgetType> = {
      mail: 'email',
      email: 'email',
      mobile: 'phone',
      phone: 'phone',
      tel: 'phone',
      fax: 'phone',
      image: 'image',
      avatar: 'image',
      photo: 'image',
      file: 'file',
      files: 'file',
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
      text: 'text',
      number: 'number',
      time: 'time',
      datetime: 'datetime',
      date: 'date',
      singleselect: 'lov',
      select: 'select',
      radio: 'select',
      lovtable: 'lov',
      multipleselect: 'lovMulti',
      checkbox: 'lovMulti',
      checkboxes: 'lovMulti',
      lovtablemulti: 'lovMulti',
      area: 'area',
      area1: 'area',
      area2: 'area',
      area3: 'area',
      area4: 'area',
      gmk: 'number',
      percent: 'number',
      percentage: 'number',
      money: 'number',
    };
    const mapped = itemTypeMap[itemType];
    if (mapped) {
      if (mapped === 'area') {
        return { widget: mapped, field, props: { areaLevel: areaLevelOf(field) } };
      }
      if (mapped === 'number') {
        return { widget: mapped, field, props: numberProps(field, itemType) };
      }
      // 单选/多选透传 lovCode，并附带 dataSource 供没有远程 LOV 的皮肤直接渲染本地选项
      if (mapped === 'lov' || mapped === 'lovMulti' || mapped === 'select') {
        const multiple =
          mapped === 'lovMulti' || !!field.multiple || itemType === 'multipleselect' || itemType === 'lovtablemulti';
        return {
          widget: mapped === 'select' ? 'select' : multiple ? 'lovMulti' : 'lov',
          field,
          props: { lovCode: field.lovCode, multiple, dataSource: field.dataSource },
        };
      }
      return { widget: mapped, field };
    }
  }

  // 5. 带参数的 decimal(p,s)
  const decimalType = parseDecimalItemType(itemType);
  if (decimalType) return { widget: 'number', field, props: decimalType };

  // 6. lovCode 优先于 dataSource（后端静态构造下发的值集）
  if (field.lovCode) {
    const multiple = field.multiple || itemType === 'multipleselect' || itemType === 'lovtablemulti';
    return {
      widget: multiple ? 'lovMulti' : 'lov',
      field,
      props: { lovCode: field.lovCode, multiple, dataSource: field.dataSource },
    };
  }

  // 7. 有数据字典/枚举 → 下拉选择（字段名以 s 结尾或显式多选 → 多选，对齐 MVC _Form_Item 约定）
  if (hasDataSource(field)) {
    const multi =
      field.multiple ||
      itemType === 'multipleselect' ||
      itemType === 'lovtablemulti' ||
      /s$/i.test(name);
    return { widget: multi ? 'lovMulti' : 'select', field, props: { multiple: multi, dataSource: field.dataSource } };
  }

  // 8. 有 url 属性 → 链接
  if (field.url) {
    return { widget: 'link', field, props: { href: field.url, target: field.target } };
  }

  // 9. 按 typeName 推断
  if (typeName === 'boolean') {
    return { widget: 'switch', field };
  }

  if (typeName === 'datetime' || typeName === 'datetimeoffset') {
    // 纯日期字段（ItemType=date 或字段名以 Date/day 结尾，无 itemType）→ 日期选择（对齐 MVC _Form_DateTime）
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

  if (NUMERIC_TYPE_NAMES.has(typeName) || typeName === 'intptr' || typeName === 'uintptr') {
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

  // 10. 名称模式匹配
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

  // 11. 长文本（对齐 MVC _Form_String：长度 < 0 或 >= 300，备注类 >= 200 也按长文本）
  if (
    (typeof field.length === 'number' && (field.length < 0 || field.length >= 300)) ||
    (typeof field.length === 'number' && field.length >= 200 && /remark|description|comment|content|body|summary|memo/i.test(name)) ||
    /remark|content|body|description|summary|memo/i.test(name)
  ) {
    return { widget: 'textarea', field };
  }

  // 12. 默认单行文本
  return { widget: 'text', field };
}

/** 数值类 ItemType 的附加属性（金额/百分比/精度） */
function numberProps(field: DataField, itemType: string): Record<string, unknown> {
  const props: Record<string, unknown> = { precision: field.scale };
  if (itemType === 'percent' || itemType === 'percentage') props.percent = true;
  if (itemType === 'money') props.money = true;
  if (itemType === 'gmk') props.gmk = true;
  return props;
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

