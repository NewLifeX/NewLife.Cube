import type { FieldMeta } from '@/core/types/field';

export interface FieldGroup {
  /** 空字符串表示未分组 */
  category: string;
  /** 分组标题；空则不渲染分组头 */
  title: string;
  fields: FieldMeta[];
}

/** 分类显示名：扩展 → 扩展属性 */
export function resolveCategoryTitle(category: string): string {
  const c = (category ?? '').trim();
  if (!c) return '默认属性';
  if (c === '扩展') return '扩展属性';
  return c;
}

/**
 * 按 GetFields/GetPage 返回的 Category 分组，保持字段原有顺序；
 * 同分类合并，首次出现顺序即分组顺序。
 * 若全部字段均无 Category，则单组且不显示分组标题。
 * 有分类时未归类字段归入「默认属性」。
 */
export function groupFieldsByCategory(fields: FieldMeta[]): FieldGroup[] {
  const order: string[] = [];
  const map = new Map<string, FieldMeta[]>();
  for (const f of fields) {
    const key = (f.category ?? '').trim();
    if (!map.has(key)) {
      map.set(key, []);
      order.push(key);
    }
    map.get(key)!.push(f);
  }
  const hasAny = order.some((c) => !!c);
  if (!hasAny) {
    return [{ category: '', title: '', fields: [...fields] }];
  }
  return order.map((category) => ({
    category,
    title: resolveCategoryTitle(category),
    fields: map.get(category)!,
  }));
}

/** 估算详情标签列统一宽度（px），按整页最宽标签 */
export function estimateDetailLabelWidth(fields: FieldMeta[]): number {
  let max = 0;
  for (const f of fields) {
    const t = f.displayName || f.name || '';
    let w = 0;
    for (const ch of t) {
      w += /[\u4e00-\u9fff]/.test(ch) ? 14 : 8;
    }
    max = Math.max(max, w);
  }
  return Math.max(96, Math.min(220, max + 24));
}
