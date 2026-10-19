/**
 * 后端字段元数据 → 前端 `FieldMeta` 归一（唯一实现处）
 *
 * 这段逻辑原先内联在 `core/views/index.vue:153-173`，只有默认列表页能用。
 * 本文件把它提为公共纯函数，供 `core/engine/createCubeEngine.ts` 与业务页共用，
 * **不改动 `index.vue`**（保持既有页面行为零变化）。
 *
 * 契约要点：
 * - 字段 → 控件的映射一律走 `core/utils/fieldControl.ts`（唯一真理源），本文件不做控件判定；
 * - 搜索字段过滤口径与 `index.vue:192-197` 完全一致：主键与 `Guid` 永不进搜索；
 * - 表单字段过滤口径与 `index.vue:171-173` 一致：主键与只读字段不进可编辑表单。
 */
import type { FieldMeta } from '../types/field';
import type {
  BackendField,
  CubePageMeta,
  NormalizedPageFields,
  PageSetting,
} from '../engine/types';

/** 后端字段 → 统一 `FieldMeta` */
export function toFieldMeta(field: BackendField): FieldMeta {
  return {
    name: field.name,
    displayName: field.displayName,
    description: field.description,
    typeName: field.typeName,
    itemType: field.itemType,
    length: field.length,
    precision: field.precision,
    scale: field.scale,
    nullable: field.nullable,
    primaryKey: field.primaryKey,
    readOnly: field.readOnly,
    lovCode: field.lovCode,
    multiple: field.multiple,
    dataSource: field.dataSource,
    // 级联子字典：漏了这一项，表单里的 LovSelect 就拿不到 :parent，「父变过滤子」静默失效
    parentField: field.parentField,
  };
}

/** 后端字段 → 表单字段（过滤主键与只读） */
export function backendFieldsToFormFields(fields: BackendField[]): FieldMeta[] {
  return fields.filter((f) => !f.primaryKey && !f.readOnly).map(toFieldMeta);
}

/** 后端字段 → 搜索字段（过滤主键与 Guid） */
export function backendFieldsToSearchFields(fields: BackendField[]): FieldMeta[] {
  return fields
    .filter((f) => !f.primaryKey && f.typeName !== 'Guid')
    .map(toFieldMeta);
}

/** 后端字段 → 列表字段（保留全部，Guid/主键按只读文本渲染，是否展示由 `pageSetting.enableKey` 决定） */
export function backendFieldsToListFields(fields: BackendField[]): FieldMeta[] {
  return fields.map(toFieldMeta);
}

/** 把前端手写的 `FieldMeta[]` 当作覆盖字段时，补齐搜索/表单的过滤口径 */
export function normalizeOverrideFields(
  fields: FieldMeta[],
  kind: 'list' | 'search' | 'form',
): FieldMeta[] {
  if (kind === 'search') {
    return fields.filter((f) => !f.primaryKey && f.typeName !== 'Guid');
  }
  if (kind === 'form') {
    return fields.filter((f) => !f.primaryKey && !f.readOnly);
  }
  return fields;
}

/**
 * `GetPage` 元数据 → 归一字段集合。
 * 缺组按空数组处理，`setting` 缺省为 `{}`，保证视图侧可无条件消费。
 */
export function normalizePageMeta(meta?: CubePageMeta | null): NormalizedPageFields {
  const setting: PageSetting = meta?.setting ?? {};
  return {
    setting,
    listFields: backendFieldsToListFields(meta?.list ?? []),
    searchFields: backendFieldsToSearchFields(meta?.search ?? []),
    addFields: backendFieldsToFormFields(meta?.addForm ?? []),
    editFields: backendFieldsToFormFields(meta?.editForm ?? meta?.addForm ?? []),
    detailFields: backendFieldsToFormFields(meta?.detail ?? []),
  };
}
