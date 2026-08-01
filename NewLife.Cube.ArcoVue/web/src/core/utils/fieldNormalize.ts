import type { DataField } from '@cube/api-core';
import type { FieldMeta, FieldOption } from '../types/field';

/** DataField → FieldMeta（兼容后端 lovCode / multiple 扩展字段） */
export function toFieldMeta(field: DataField): FieldMeta {
  const ext = field as DataField & {
    lovCode?: string;
    multiple?: boolean;
    options?: FieldOption[];
  };
  return {
    name: field.name,
    displayName: field.displayName,
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
    lovCode: ext.lovCode,
    multiple: ext.multiple,
    options: ext.options,
    dataSource: field.dataSource,
    maxWidth: field.maxWidth,
    url: field.url,
    target: field.target,
  };
}

export function toFieldMetas(fields: DataField[] | undefined | null): FieldMeta[] {
  return (fields ?? []).map(toFieldMeta);
}
