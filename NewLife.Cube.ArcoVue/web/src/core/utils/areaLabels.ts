/**
 * 地区/级联标签缓存纯函数（OSC-2608139feb）。
 *
 * 列表/卡片/详情中地区叶子值默认只显示原始 ID；本模块提供
 * 「收集叶子 ID」与「合并标签缓存」两个纯函数，供
 * useListQuery.hydrateAreaLabels / useRecordDrawer 批量补标签使用。
 */
import type { FieldMeta } from '../types/field';
import { isCascaderField } from './fieldControl';
import { getValueByKey } from './url';

/**
 * 合并单个地区标签到缓存。id 或 name 为空时忽略。
 * @param cache 叶子值 → 标签缓存
 * @param id 叶子 ID
 * @param name 地区名称
 */
export function mergeAreaLabel(cache: Record<string, string>, id: unknown, name: unknown): void {
  if (id == null || id === '' || name == null || name === '') return;
  cache[String(id)] = String(name);
}

/**
 * 收集多行记录中所有级联字段的叶子 ID（去重）。
 * @param fields 字段元数据集合
 * @param rows 记录行
 * @returns 去重后的叶子 ID 列表
 */
export function collectCascaderIds(
  fields: FieldMeta[],
  rows: Record<string, unknown>[],
): (number | string)[] {
  const ids = new Set<string>();
  const cascaders = fields.filter(isCascaderField);
  for (const f of cascaders) {
    for (const row of rows) {
      const v = getValueByKey(row, f.name);
      if (v != null && v !== '') ids.add(String(v));
    }
  }
  return [...ids];
}

/** 地区名称接口单次上限（与后端 NormalizeNameIds 一致） */
export const AREA_NAME_CHUNK = 200;

/** 去掉缓存里已有的叶子 ID。 */
export function pendingAreaIds(ids: (number | string)[], cache: Record<string, string>): string[] {
  const out: string[] = [];
  const seen = new Set<string>();
  for (const id of ids) {
    const key = String(id);
    if (!key || cache[key] || seen.has(key)) continue;
    seen.add(key);
    out.push(key);
  }
  return out;
}

/** 按固定片大小切分 ID。空列表返回空数组。 */
export function chunkAreaIds(ids: string[], size = AREA_NAME_CHUNK): string[][] {
  if (!ids.length) return [];
  const step = size > 0 ? size : ids.length;
  const chunks: string[][] = [];
  for (let i = 0; i < ids.length; i += step) chunks.push(ids.slice(i, i + step));
  return chunks;
}
