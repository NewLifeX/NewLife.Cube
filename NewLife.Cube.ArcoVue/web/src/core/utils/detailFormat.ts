/**
 * 详情值渲染纯函数（OSC-0009）。
 * 提供 dataSource 翻译、多选拆标签、Boolean、JSON 摘要、URL/图片/文件安全呈现判断。
 * 只做「值 → 展示」的派生计算，不修改原始模型；HTML/Markdown 一律以纯文本输出，禁止原样渲染。
 */
import type { FieldMeta } from '../types/field';
import { isTruthy } from './fieldBadge';

function itemTypeOf(field: FieldMeta): string {
  return (field.itemType ?? '').trim().toLowerCase();
}

/** 是否多选字段（multiple 或 itemType 大小写不敏感的 multipleselect） */
export function isMultipleValueField(field: FieldMeta): boolean {
  return !!field.multiple || itemTypeOf(field) === 'multipleselect';
}

/** 是否布尔类值（true/false/1/0），用于布尔别名字典的精确回退 */
function isBooleanLike(raw: unknown): boolean {
  return (
    raw === true ||
    raw === false ||
    raw === 1 ||
    raw === 0 ||
    raw === '1' ||
    raw === '0' ||
    raw === 'true' ||
    raw === 'false' ||
    raw === 'True' ||
    raw === 'False'
  );
}

/** 单值字段翻译：dataSource 命中返回标签；否则返回 null（交由调用方降级） */
export function lookupLabel(field: FieldMeta, raw: unknown): string | null {
  if (raw == null || raw === '') return null;
  const ds = field.dataSource;
  if (ds && Object.keys(ds).length) {
    const key = String(raw);
    const hit = ds[key] ?? ds[key.toLowerCase()] ?? ds[key.toUpperCase()];
    if (hit != null) return hit;
    // 仅当值为布尔类（true/false/1/0）时才做布尔别名互译，避免未知值误映射为布尔 label
    if (isBooleanLike(raw)) {
      if (isTruthy(raw)) return ds['1'] ?? ds['true'] ?? ds['True'] ?? null;
      return ds['0'] ?? ds['false'] ?? ds['False'] ?? null;
    }
    return null;
  }
  return null;
}

/** 多值字段按逗号/数组拆分为标签数组；单值返回单元素数组 */
export function detailLabels(field: FieldMeta, raw: unknown): string[] {
  if (raw == null || raw === '') return [];
  const values = isMultipleValueField(field)
    ? Array.isArray(raw)
      ? raw.map(String)
      : String(raw).split(',').map((s) => s.trim()).filter(Boolean)
    : [raw];
  const out: string[] = [];
  for (const v of values) {
    const label = lookupLabel(field, v);
    out.push(label ?? String(v));
  }
  return out;
}

/** JSON 摘要：超过阈值截断并追加省略号；解析失败返回原文 */
export function jsonPreview(raw: unknown, max = 200): string {
  if (raw == null || raw === '') return '';
  const s = typeof raw === 'string' ? raw : JSON.stringify(raw);
  if (s.length <= max) return s;
  return `${s.slice(0, max)}…`;
}

/** 详情纯文本（模板 <div> 直接输出，安全）：dataSource/多选/Boolean/JSON 摘要/常规字符串 */
export function detailText(field: FieldMeta, raw: unknown): string {
  if (raw == null || raw === '') return '-';
  const itemType = itemTypeOf(field);
  if (itemType === 'json') return jsonPreview(raw);
  if (isMultipleValueField(field)) {
    const labels = detailLabels(field, raw);
    return labels.length ? labels.join('、') : '-';
  }
  const label = lookupLabel(field, raw);
  if (label != null) return label;
  if (typeof raw === 'boolean') return raw ? '是' : '否';
  if (field.typeName === 'Boolean') return isTruthy(raw) ? '是' : '否';
  return String(raw);
}

export interface DetailLink {
  /** 原始值 */
  value: string;
  /** 安全链接（仅 http/https/ftp）；否则 # */
  href: string;
  /** 是否允许新窗口打开 */
  safe: boolean;
  /** 展示文本（文件名等） */
  text: string;
}

/** URL 字段：仅 http/https/ftp 视为安全链接；其它协议一律不渲染为可点链接 */
export function detailUrl(field: FieldMeta, raw: unknown): DetailLink | null {
  if (raw == null || raw === '') return null;
  const itemType = itemTypeOf(field);
  const isUrlType = itemType === 'url' || !!field.url;
  if (!isUrlType) return null;
  const value = String(raw);
  const safe = /^(https?|ftp):\/\//i.test(value);
  return { value, href: safe ? value : '#', safe, text: value };
}

/** 图片字段：返回图片地址（安全判断同 URL，仅 http/https 渲染 <img>） */
export function detailImage(field: FieldMeta, raw: unknown): DetailLink | null {
  if (raw == null || raw === '') return null;
  const itemType = itemTypeOf(field);
  if (itemType !== 'image') return null;
  const value = String(raw);
  const safe = /^(https?):\/\//i.test(value);
  return { value, href: safe ? value : '#', safe, text: value };
}

/** 文件字段：返回文件名与安全下载链接 */
export function detailFile(field: FieldMeta, raw: unknown): DetailLink | null {
  if (raw == null || raw === '') return null;
  const itemType = itemTypeOf(field);
  if (itemType !== 'file' && itemType !== 'upload') return null;
  const value = String(raw);
  const safe = /^(https?|ftp):\/\//i.test(value);
  const text = value.split(/[\\/]/).pop() || value;
  return { value, href: safe ? value : '#', safe, text };
}
