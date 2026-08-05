import type { FieldMeta } from '@/core/types/field';
import type { FormLayout } from '@/core/utils/viewProfile';

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

/**
 * 按字段集归一化表单布局（OSC-0013）：
 * order/hidden 去重且仅保留当前字段集的 canonical name；collapsedCategories 仅保留存在的非空 Category。
 */
export function normalizeFormLayout(
  layout: FormLayout | null | undefined,
  fields: FieldMeta[],
): FormLayout {
  if (!layout) return { order: [], hidden: [], collapsedCategories: [] };
  const names = new Set(fields.map((f) => f.name));
  const categories = new Set(
    fields.map((f) => (f.category ?? '').trim()).filter(Boolean),
  );
  return {
    order: [...new Set(layout.order.filter((n) => names.has(n)))],
    hidden: [...new Set(layout.hidden.filter((n) => names.has(n)))],
    collapsedCategories: [
      ...new Set(layout.collapsedCategories.filter((c) => categories.has(c))),
    ],
  };
}

/**
 * 应用表单布局到分组（OSC-0013）：hidden 过滤、order 排序（未列字段按元数据原序追加）、
 * 返回折叠的 Category 集合；空分组不显示。无布局时按元数据原样返回。
 */
export function applyFormLayout(
  groups: FieldGroup[],
  layout: FormLayout | null | undefined,
): { groups: FieldGroup[]; collapsed: string[] } {
  const norm = normalizeFormLayout(layout, groups.flatMap((g) => g.fields));
  const hidden = new Set(norm.hidden);
  const orderMap = new Map(norm.order.map((n, i) => [n, i]));
  const collapsed = norm.collapsedCategories.filter((c) =>
    groups.some((g) => g.category === c),
  );
  const next = groups
    .map((g) => ({
      ...g,
      fields: orderFields(
        g.fields.filter((f) => !hidden.has(f.name)),
        orderMap,
      ),
    }))
    .filter((g) => g.fields.length > 0);
  return { groups: next, collapsed };
}

/** 按 orderMap 排序；未列字段保持原序追加到末尾 */
function orderFields(fields: FieldMeta[], orderMap: Map<string, number>): FieldMeta[] {
  if (!orderMap.size) return fields;
  const ordered = fields
    .filter((f) => orderMap.has(f.name))
    .sort((a, b) => orderMap.get(a.name)! - orderMap.get(b.name)!);
  const rest = fields.filter((f) => !orderMap.has(f.name));
  return [...ordered, ...rest];
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
