/**
 * 列表状态/枚举徽章：优先用 GetPage 物化的 dataSource，避免反复拉后端。
 * 样式：浅底色 + 矩形圆角（非胶囊/圆形）。
 */
import type { FieldMeta } from '@/core/types/field';
import { resolveListControl } from '@/core/utils/fieldControl';

export type BadgeTone =
  | 'success'
  | 'danger'
  | 'warning'
  | 'info'
  | 'purple'
  | 'cyan'
  | 'orange'
  | 'neutral';

export interface CellBadge {
  label: string;
  tone: BadgeTone;
  /** 按钮底色 */
  buttonColor: string;
  buttonBorderColor: string;
  textColor: string;
  /** 建议列宽（刚好放下徽章） */
  width: number;
}

/** 浅底 + 同色文字；边框与底色一致 */
const TONE_STYLE: Record<
  BadgeTone,
  { buttonColor: string; buttonBorderColor: string; textColor: string }
> = {
  success: { buttonColor: '#E8FFEA', buttonBorderColor: '#E8FFEA', textColor: '#00B42A' },
  danger: { buttonColor: '#FFECE8', buttonBorderColor: '#FFECE8', textColor: '#F53F3F' },
  warning: { buttonColor: '#FFF7E8', buttonBorderColor: '#FFF7E8', textColor: '#FF7D00' },
  info: { buttonColor: '#E8F3FF', buttonBorderColor: '#E8F3FF', textColor: '#165DFF' },
  purple: { buttonColor: '#F5E8FF', buttonBorderColor: '#F5E8FF', textColor: '#722ED1' },
  cyan: { buttonColor: '#E8FFFB', buttonBorderColor: '#E8FFFB', textColor: '#0FC6C2' },
  orange: { buttonColor: '#FFF3E8', buttonBorderColor: '#FFF3E8', textColor: '#D2691E' },
  neutral: { buttonColor: '#F2F3F5', buttonBorderColor: '#F2F3F5', textColor: '#4E5969' },
};

const HASH_PALETTE: BadgeTone[] = ['info', 'warning', 'success', 'purple', 'cyan', 'orange', 'neutral'];

/**
 * 矩形圆角（对齐 Arco Tag / 中后台常见状态块）。
 * 短文案（是/否）若用 999 会视觉成圆，故固定为小圆角。
 */
export const BADGE_BORDER_RADIUS = 4;
/** VTable buttonStyle 内边距 */
export const BADGE_PADDING = 6;

export function isTruthy(raw: unknown): boolean {
  return raw === true || raw === 1 || raw === '1' || raw === 'true' || raw === 'True';
}

export function isBadgeField(field: FieldMeta): boolean {
  const kind = resolveListControl(field);
  return kind === 'boolean' || kind === 'select' || kind === 'lov';
}

/** 从 dataSource / 布尔约定解析显示文案（不发起网络请求） */
export function resolveCellLabel(field: FieldMeta, raw: unknown): string {
  if (raw == null || raw === '') return '-';
  const key = String(raw);
  const ds = field.dataSource;
  if (ds && Object.keys(ds).length) {
    return (
      ds[key] ??
      ds[key.toLowerCase()] ??
      (isTruthy(raw) ? ds['1'] ?? ds['true'] ?? ds['True'] : ds['0'] ?? ds['false'] ?? ds['False']) ??
      key
    );
  }
  if (field.typeName === 'Boolean' || resolveListControl(field) === 'boolean') {
    return isTruthy(raw) ? '是' : '否';
  }
  return key;
}

export function resolveBadgeTone(field: FieldMeta, raw: unknown, label: string): BadgeTone {
  const kind = resolveListControl(field);
  if (kind === 'boolean' || field.typeName === 'Boolean') {
    return isTruthy(raw) ? 'success' : 'danger';
  }
  if (/禁用|停用|否|失败|关闭|无效|删除|维修/.test(label)) {
    if (/维修/.test(label)) return 'info';
    return 'danger';
  }
  if (/启用|是|成功|正常|通过|有效/.test(label)) return 'success';
  if (/保养|待|进行|警告|审核|使用中/.test(label)) return /保养/.test(label) ? 'orange' : 'warning';
  if (/闲置|草稿/.test(label)) return 'purple';
  let h = 0;
  for (let i = 0; i < label.length; i++) h = (h * 31 + label.charCodeAt(i)) | 0;
  return HASH_PALETTE[Math.abs(h) % HASH_PALETTE.length];
}

export function estimateBadgeWidth(label: string): number {
  let w = 24;
  for (const ch of label) {
    w += /[\u4e00-\u9fff]/.test(ch) ? 13 : 7.5;
  }
  return Math.max(56, Math.min(160, Math.ceil(w)));
}

export function resolveCellBadge(field: FieldMeta, raw: unknown): CellBadge | null {
  if (!isBadgeField(field)) return null;
  if (raw == null || raw === '') return null;
  const label = resolveCellLabel(field, raw);
  if (!label || label === '-') return null;
  const tone = resolveBadgeTone(field, raw, label);
  const style = TONE_STYLE[tone];
  return {
    label,
    tone,
    ...style,
    width: estimateBadgeWidth(label),
  };
}

/** 徽章列默认宽度（取各选项标签估算的最大宽度） */
export function defaultBadgeColumnWidth(field: FieldMeta): number {
  const ds = field.dataSource;
  if (ds && Object.keys(ds).length) {
    let max = 56;
    for (const label of Object.values(ds)) {
      max = Math.max(max, estimateBadgeWidth(String(label)));
    }
    return max;
  }
  if (field.typeName === 'Boolean' || resolveListControl(field) === 'boolean') {
    return estimateBadgeWidth('是');
  }
  return 88;
}
