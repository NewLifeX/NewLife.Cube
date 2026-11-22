import dayjs from 'dayjs';
import type { DataField } from '@newlifex/api-core';
import type { FieldMapping } from '@newlifex/field-mapping';
import { resolveUrl, toCamelCase } from '@newlifex/page-utils';

/** 行数据取值：元数据为 PascalCase，后端 JSON 为 camelCase，双向兼容 */
export function getRowValue(row: Record<string, unknown>, name: string) {
  if (name in row) return row[name];
  const camel = toCamelCase(name);
  if (camel in row) return row[camel];
  const lower = name.toLowerCase();
  const key = Object.keys(row).find(k => k.toLowerCase() === lower);
  return key ? row[key] : undefined;
}

function isDateField(field: DataField) {
  const typeName = (field.typeName || '').toLowerCase();
  return typeName === 'datetime' || typeName === 'date' || typeName === 'datetimeoffset';
}

/** 单元格展示文本 */
export function formatCell(field: DataField, row: Record<string, unknown>) {
  const value = getRowValue(row, field.name);
  if (value === null || value === undefined || value === '') return '';

  if (typeof value === 'boolean') return value ? '是' : '否';

  if (isDateField(field)) {
    const text = String(value);
    // 后端 GetValue 可能下发仅含时间的展示值（如审计日志今天只显示 HH:mm:ss），无法解析时原样展示
    const date = dayjs(text);
    if (!date.isValid()) return text;

    const itemType = (field.itemType || '').toLowerCase();
    const isDateOnly = itemType === 'date' || /(date|day)$/i.test(field.name);
    return date.format(isDateOnly ? 'YYYY-MM-DD' : 'YYYY-MM-DD HH:mm:ss');
  }

  // 有数据字典时按字典翻译
  if (field.dataSource) {
    const hit = field.dataSource[String(value)];
    if (hit !== undefined) return hit;
  }

  if (Array.isArray(value)) return value.join(', ');

  return String(value);
}

/** 链接列地址（支持 {Id} 等占位符），无 url 时返回空串 */
export function getCellLink(field: DataField, row: Record<string, unknown>) {
  if (!field.url) return '';
  return resolveUrl(field.url, row);
}

/** 图片地址：仅当值像图片地址（URL / 站点绝对路径 / data:image / 图片扩展名）时返回，否则返回空串 */
export function resolveImageSrc(value: unknown) {
  if (typeof value !== 'string') return '';
  const src = value.trim();
  if (!src) return '';
  const isUrlLike = /^(https?:\/\/|\/|data:image\/)/i.test(src) || /\.(png|jpe?g|gif|webp|svg|bmp|ico)(\?.*)?$/i.test(src);
  return isUrlLike ? src : '';
}

/** 搜索控件类型 */
export type SearchKind = 'keyword' | 'range' | 'date' | 'datetime' | 'select' | 'text' | 'number';

export interface SearchControl {
  field: DataField;
  kind: SearchKind;
  /** 起始参数名 */
  key: string;
  /** 结束参数名（区间） */
  endKey?: string;
}

/** 根据字段元数据推断搜索控件 */
export function buildSearchControl(field: DataField): SearchControl {
  if (field.kind === 'range') {
    return { field, kind: 'range', key: field.name, endKey: field.end || `${field.name}End` };
  }
  if (field.kind === 'keyword' || field.name === 'q' || field.name.toLowerCase() === 'q') {
    return { field, kind: 'keyword', key: 'q' };
  }

  const typeName = (field.typeName || '').toLowerCase();
  const itemType = (field.itemType || '').toLowerCase();

  if (typeName === 'datetime' || typeName === 'datetimeoffset' || typeName === 'date') {
    const dateOnly = typeName === 'date' || itemType === 'date' || /(date|day)$/i.test(field.name);
    return { field, kind: dateOnly ? 'date' : 'datetime', key: field.name };
  }

  if (typeName === 'boolean' || field.dataSource) {
    return { field, kind: 'select', key: field.name };
  }

  if (['int32', 'int64', 'int16', 'decimal', 'double', 'single', 'float'].includes(typeName)) {
    return { field, kind: 'number', key: field.name };
  }

  return { field, kind: 'text', key: field.name };
}

export interface SearchValues {
  [key: string]: unknown;
}

/** 收集搜索参数，剔除空值 */
export function buildSearchParams(values: SearchValues) {
  const params: Record<string, unknown> = {};

  Object.entries(values).forEach(([key, value]) => {
    if (value === null || value === undefined || value === '') return;
    if (Array.isArray(value)) {
      if (!value.length) return;
      params[key] = value;
      return;
    }
    params[key] = value;
  });

  return params;
}

/** 字典值转换为后端可识别的标量 */
export function dictValueOf(key: string) {
  return /^-?\d+$/.test(key) ? Number(key) : key;
}

/** 字段排序是否允许（虚拟列与链接列不参与后端排序） */
export function canSort(field: FieldMapping) {
  return !field.field.virtual && !field.field.url;
}

/** 父字段候选名：后端 JSON 为 camelCase，元数据为 PascalCase */
const TREE_PARENT_KEYS = ['parentID', 'parentId', 'ParentID', 'ParentId'];

/**
 * 把扁平列表按父字段组装成树（`setting.tree` 为真的列表用）。
 * 找不到父节点的行按根节点处理，保持同层原顺序；入参不被修改。
 */
export function buildTreeRows(rows: Record<string, unknown>[], pkField = 'id') {
  if (!rows.length) return [];

  const parentKey = TREE_PARENT_KEYS.find(key => rows.some(row => key in row));
  if (!parentKey) return rows;

  const idOf = (row: Record<string, unknown>) => String(getRowValue(row, pkField) ?? '');

  const nodes = new Map<string, Record<string, unknown>>();
  rows.forEach(row => nodes.set(idOf(row), { ...row }));

  const roots: Record<string, unknown>[] = [];
  rows.forEach(row => {
    const node = nodes.get(idOf(row))!;
    const parentId = row[parentKey];
    const parent = parentId === null || parentId === undefined ? undefined : nodes.get(String(parentId));

    if (!parent || parent === node) {
      roots.push(node);
      return;
    }

    const children = (parent.children as Record<string, unknown>[] | undefined) ?? [];
    children.push(node);
    parent.children = children;
  });

  return roots;
}
