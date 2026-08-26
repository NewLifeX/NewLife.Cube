import type { DataField } from '@cube/api-core';
import type { FieldMeta, FieldOption } from '../types/field';
import { isAreaFieldName, isBooleanDataSource, parseFkName } from './fieldControl';
import { applyDescriptionDataSourceIfNeeded } from './descriptionDataSource';

function pickDataSource(field: DataField & Record<string, unknown>): Record<string, string> | undefined {
  const raw =
    field.dataSource ??
    field.dataSourceMap ??
    field.DataSourceMap ??
    field.DataSource;
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return undefined;
  // 委托不会出现在 JSON；这里只处理已物化的字典
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(raw as Record<string, unknown>)) {
    if (typeof v === 'function') continue;
    out[String(k)] = v == null ? '' : String(v);
  }
  return Object.keys(out).length ? out : undefined;
}

function pickTypeName(
  field: DataField & Record<string, unknown>,
  dataSource?: Record<string, string>,
): string {
  const raw =
    (field.typeName as string | undefined) ||
    (field.TypeName as string | undefined) ||
    '';
  if (raw) return raw;
  // GetFields 偶发丢失 TypeName：布尔字典回落为 Boolean，便于开关控件
  if (isBooleanDataSource(dataSource)) return 'Boolean';
  return 'String';
}

function pickDescription(field: DataField & Record<string, unknown>): string | undefined {
  const raw =
    (field.description as string | undefined) ||
    (field.Description as string | undefined) ||
    '';
  const t = raw.trim();
  return t || undefined;
}

/** DataField → FieldMeta（兼容后端 lovCode / dataSourceMap / multiple / PascalCase） */
export function toFieldMeta(field: DataField): FieldMeta {
  const ext = field as DataField & {
    lovCode?: string;
    LovCode?: string;
    category?: string;
    Category?: string;
    multiple?: boolean;
    options?: FieldOption[];
    Name?: string;
    DisplayName?: string;
    ItemType?: string;
    TypeName?: string;
    Description?: string;
  };
  const name = field.name || ext.Name || '';
  const dataSource = pickDataSource(ext as DataField & Record<string, unknown>);
  const rawTypeName =
    (field.typeName as string | undefined) ||
    (ext.TypeName as string | undefined) ||
    '';
  const hasTypeName = !!String(rawTypeName).trim();
  const typeName = pickTypeName(ext as DataField & Record<string, unknown>, dataSource);
  const description = pickDescription(ext as DataField & Record<string, unknown>);
  const dataActionRaw =
    (field.dataAction as string | undefined) ||
    ((ext as { DataAction?: string }).DataAction as string | undefined) ||
    '';
  const dataAction = String(dataActionRaw).trim() || undefined;

  const meta: FieldMeta = {
    name,
    displayName: field.displayName || ext.DisplayName || name,
    category: field.category || ext.Category || undefined,
    typeName,
    itemType: field.itemType || ext.ItemType || (isAreaFieldName(name) ? 'area4' : undefined),
    length: field.length,
    precision: field.precision,
    scale: field.scale,
    nullable: field.nullable,
    primaryKey: field.primaryKey,
    readOnly: field.readOnly,
    required: field.required,
    visible: field.visible,
    description,
    lovCode: ext.lovCode || ext.LovCode,
    multiple: ext.multiple || !!parseFkName(name)?.multi,
    options: ext.options,
    dataSource,
    maxWidth: field.maxWidth,
    url: field.url,
    target: field.target,
    dataAction,
    hasTypeName,
  };

  // Int32 + description 明显键值对 → 补齐 dataSource，供 resolveControl 渲染下拉
  applyDescriptionDataSourceIfNeeded(meta);
  return meta;
}

export function toFieldMetas(fields: DataField[] | undefined | null): FieldMeta[] {
  return (fields ?? []).map(toFieldMeta);
}
