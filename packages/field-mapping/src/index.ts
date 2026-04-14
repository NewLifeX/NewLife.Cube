/**
 * @cube/field-mapping — DataField 元数据到 UI 组件的映射规则引擎
 *
 * 根据后端返回的 DataField 元数据，自动推断应使用的前端组件类型。
 * 各皮肤包基于此规则引擎，将抽象组件类型映射到具体 UI 库组件。
 */

import type { DataField } from '@cube/api-core';

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
  | 'html';          // 富文本

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
 * 判断优先级：只读 → itemType → 数据字典 → url 字段 → typeName → 名称模式 → 长度 → 默认
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
      password: 'password',
    };
    const mapped = itemTypeMap[itemType];
    if (mapped) return { widget: mapped, field };
  }

  // 3. 有数据字典/枚举 → 下拉选择
  if (field.dataSource && Object.keys(field.dataSource).length > 0) {
    return { widget: 'select', field };
  }

  // 4. 有 url 属性 → 链接
  if (field.url) {
    return { widget: 'link', field, props: { href: field.url, target: field.target } };
  }

  // 5. 按 typeName 推断
  const typeName = (field.typeName ?? '').toLowerCase();

  if (typeName === 'boolean') {
    return { widget: 'switch', field };
  }

  if (typeName === 'datetime' || typeName === 'datetimeoffset') {
    return { widget: 'datetime', field };
  }

  if (typeName === 'date') {
    return { widget: 'date', field };
  }

  if (['int32', 'int64', 'int16', 'decimal', 'double', 'single', 'float', 'byte', 'uint32', 'uint64', 'sbyte'].includes(typeName)) {
    return { widget: 'number', field, props: { precision: field.scale } };
  }

  // 6. 名称模式匹配
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

  // 7. 长文本（长度 > 200 或包含特定名称）
  if ((field.length && field.length > 200) || /remark|content|body|description|summary|memo/i.test(name)) {
    return { widget: 'textarea', field };
  }

  // 8. 默认单行文本
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

/**
 * 分解 2 的幂位数组（用于解析权限码）
 *
 * @example decomposePowerOfTwo(15) → [1, 2, 4, 8]
 *
 * @param num 权限合成码
 * @returns 各独立权限值
 */
export function decomposePowerOfTwo(num: number): number[] {
  const result: number[] = [];
  let bit = 1;
  while (bit <= num) {
    if ((num & bit) !== 0) {
      result.push(bit);
    }
    bit <<= 1;
  }
  return result;
}

/**
 * 字符串转小驼峰（对齐 Cube.Vue 中的 toCamelCase）
 *
 * @example toCamelCase("User_Name") → "userName"
 * @example toCamelCase("UserName") → "userName"
 */
export function toCamelCase(str: string): string {
  if (!str) return '';
  // 下划线/连字符分隔
  const s = str.replace(/[-_](\w)/g, (_, c) => (c ? c.toUpperCase() : ''));
  return s.charAt(0).toLowerCase() + s.slice(1);
}
