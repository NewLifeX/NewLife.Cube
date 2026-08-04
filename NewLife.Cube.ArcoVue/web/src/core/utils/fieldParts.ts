/**
 * 字段分区（GetPage list/addForm/editForm/detail/search）回退规则（OSC-0009）。
 * 表单、详情、搜索与多视图必须消费同一字段来源，避免「详情展示用 list、回填用 detail」等不一致。
 * 后端 GetPage 各分区为权威；分区缺失时按依赖顺序回退，禁止按字段名猜测。
 */
import type { FieldMeta } from '../types/field';

export type FieldKindKey = 'list' | 'add' | 'edit' | 'detail' | 'search';

export interface FieldParts {
  list: FieldMeta[];
  add: FieldMeta[];
  edit: FieldMeta[];
  detail: FieldMeta[];
  search: FieldMeta[];
}

/** 空分区列表 */
export function emptyFieldParts(): FieldParts {
  return { list: [], add: [], edit: [], detail: [], search: [] };
}

/**
 * 按视图种类解析字段集（唯一入口）：
 * - add：addForm → editForm
 * - edit：editForm → addForm
 * - detail：detail → editForm → list
 * - search：search（无回退，空即无搜索条件）
 * - list：list（无回退）
 */
export function resolveFieldsForKind(kind: FieldKindKey, parts: FieldParts): FieldMeta[] {
  switch (kind) {
    case 'add':
      return parts.add.length ? parts.add : parts.edit;
    case 'edit':
      return parts.edit.length ? parts.edit : parts.add;
    case 'detail':
      if (parts.detail.length) return parts.detail;
      if (parts.edit.length) return parts.edit;
      return parts.list;
    case 'search':
      return parts.search;
    case 'list':
      return parts.list;
    default:
      return [];
  }
}
