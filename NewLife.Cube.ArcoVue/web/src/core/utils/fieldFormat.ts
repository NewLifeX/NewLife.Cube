/**
 * 列表 / 卡片 / 看板 共享的字段值格式化器。
 *
 * 集中处理：日期/时间/日期时间（壁钟时间，无时区漂移）、布尔、
 * GetPage 物化的 dataSource 字典、LIST/Enum LOV（BatchLabel 缓存）、
 * 地区/级联叶子值（缓存 label）。未命中则降级 String(raw)。
 */
import type { FieldMeta } from '../types/field';
import { resolveCellLabel } from './fieldBadge';
import { resolveListControl } from './fieldControl';
import { formatDate, formatDateTime, formatTime, inferDateKind } from './datetime';
import { getValueByKey } from './url';

export interface FormatFieldOptions {
  /** lovCode → { value: label } 缓存（由 DefaultList.hydrateLovLabels 维护） */
  labelCache?: Record<string, Record<string, string>>;
  /** 地区/级联字段叶子值 → label 缓存 */
  areaLabelCache?: Record<string, string>;
}

/**
 * 解析单字段在某行上的展示文案。
 * 入参 field 可为空（仅按原始值 String 化）。
 */
export function formatFieldValue(
  field: FieldMeta | undefined,
  record: Record<string, unknown>,
  options: FormatFieldOptions = {},
): string {
  const name = field?.name;
  const raw = name ? getValueByKey(record, name) : undefined;
  if (raw == null || raw === '') return '-';

  // 1. 日期 / 时间（优先于字典，避免 DateTime 被当枚举处理）
  if (field) {
    const kind = resolveListControl(field);
    if (kind === 'date' || field.typeName === 'DateTime') {
      const dk = inferDateKind(field);
      if (dk === 'date') return formatDate(raw);
      if (dk === 'time') return formatTime(raw);
      return formatDateTime(raw);
    }
    if (kind === 'time' || field.typeName === 'TimeSpan') {
      return formatTime(raw);
    }
  }

  // 2. dataSource（GetPage 物化的枚举/状态字典）
  if (field?.dataSource && Object.keys(field.dataSource).length) {
    const label = resolveCellLabel(field, raw);
    if (label && label !== '-') return label;
  }

  // 3. 布尔
  if (field && (field.typeName === 'Boolean' || resolveListControl(field) === 'boolean')) {
    return resolveCellLabel(field, raw);
  }

  // 4. LOV（LIST/Enum）— 使用 BatchLabel 缓存翻译
  if (field?.lovCode && resolveListControl(field) === 'lov') {
    const map = options.labelCache?.[field.lovCode];
    const cached = map?.[String(raw)];
    if (cached) return cached;
    // dataSource 已被 hydrateLovLabels 回写时也能命中
    const dsLabel = field.dataSource?.[String(raw)];
    if (dsLabel) return dsLabel;
    return String(raw);
  }

  // 5. 地区/级联叶子值
  if (field && (field.itemType ?? '').toLowerCase() !== '') {
    const it = (field.itemType ?? '').toLowerCase();
    if (it === 'area' || it === 'area4' || it === 'cascader') {
      const areaLabel = options.areaLabelCache?.[String(raw)];
      if (areaLabel) return areaLabel;
    }
  }

  return String(raw);
}
