import type { DataField } from '@cube/api-core';
import type { FieldMeta, FieldOption } from '../types/field';

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

/** DataField → FieldMeta（兼容后端 lovCode / dataSourceMap / multiple） */
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
  };
  const name = field.name || ext.Name || '';
  return {
    name,
    displayName: field.displayName || ext.DisplayName || name,
    category: field.category || ext.Category || undefined,
    typeName: field.typeName || 'String',
    itemType: field.itemType,
    length: field.length,
    precision: field.precision,
    scale: field.scale,
    nullable: field.nullable,
    primaryKey: field.primaryKey,
    readOnly: field.readOnly,
    required: field.required,
    visible: field.visible,
    description: field.description,
    lovCode: ext.lovCode || ext.LovCode,
    multiple: ext.multiple,
    options: ext.options,
    dataSource: pickDataSource(ext as DataField & Record<string, unknown>),
    maxWidth: field.maxWidth,
    url: field.url,
    target: field.target,
  };
}

export function toFieldMetas(fields: DataField[] | undefined | null): FieldMeta[] {
  return (fields ?? []).map(toFieldMeta);
}
