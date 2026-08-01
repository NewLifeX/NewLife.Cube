/**
 * 字段 → 控件映射（移植自 Cube.Vue fieldControl，ArcoVue 本地真理源）
 */
import type {
  FieldMeta,
  ControlType,
  SearchControlType,
  ListControlType,
} from '../types/field';

const NUMERIC_TYPES: ReadonlySet<string> = new Set([
  'Int32',
  'Int64',
  'Decimal',
  'Double',
  'Single',
]);

const ITEM_TYPE_TO_CONTROL: Record<string, ControlType> = {
  file: 'upload',
  image: 'image',
  json: 'json',
  html: 'richHtml',
  markdown: 'richMarkdown',
  color: 'color',
  icon: 'icon',
  mail: 'email',
  mobile: 'tel',
  url: 'url',
  singleselect: 'lov',
  multipleselect: 'lovMulti',
};

function normalizeItemType(field: FieldMeta): string {
  return (field.itemType ?? '').trim().toLowerCase();
}

export function resolveControl(field: FieldMeta): ControlType {
  const itemType = normalizeItemType(field);
  if (itemType && ITEM_TYPE_TO_CONTROL[itemType]) {
    return ITEM_TYPE_TO_CONTROL[itemType];
  }

  const typeName = field.typeName;

  if (typeName === 'Guid') return 'readonly';

  if (field.lovCode) {
    return field.multiple || itemType === 'multipleselect' ? 'lovMulti' : 'lov';
  }

  if (field.dataSource && Object.keys(field.dataSource).length > 0) {
    return 'select';
  }

  if (typeName === 'Boolean') return 'switch';
  if (typeName === 'DateTime') return 'datePicker';
  if (typeName === 'TimeSpan') return 'timePicker';
  if (NUMERIC_TYPES.has(typeName)) return 'inputNumber';
  if (typeName === 'Enum') return 'lov';

  if (typeName === 'String') {
    const len = field.length ?? 0;
    if (len >= 300) return 'textarea';
    return 'input';
  }

  return 'input';
}

export function resolveSearchControl(field: FieldMeta): SearchControlType {
  const itemType = normalizeItemType(field);

  if (itemType === 'file' || itemType === 'image') return 'fileExists';
  if (itemType === 'singleselect') return 'lov';
  if (itemType === 'multipleselect') return 'lovMulti';

  const typeName = field.typeName;

  if (typeName === 'Guid') return 'text';

  if (field.lovCode) {
    return field.multiple || itemType === 'multipleselect' ? 'lovMulti' : 'lov';
  }

  if (field.dataSource && Object.keys(field.dataSource).length > 0) {
    return 'select';
  }

  if (typeName === 'Boolean') return 'switch';
  if (typeName === 'DateTime') return 'datetimeRange';
  if (typeName === 'TimeSpan') return 'timeRange';
  if (NUMERIC_TYPES.has(typeName)) return 'numberRange';

  return 'text';
}

export function resolveListControl(field: FieldMeta): ListControlType {
  const itemType = normalizeItemType(field);

  switch (itemType) {
    case 'image':
      return 'image';
    case 'file':
      return 'file';
    case 'color':
      return 'color';
    case 'icon':
      return 'icon';
    case 'json':
      return 'json';
    case 'html':
    case 'markdown':
      return 'html';
    case 'url':
      return 'url';
    case 'singleselect':
    case 'multipleselect':
      return 'lov';
  }

  const typeName = field.typeName;

  if (typeName === 'Guid') return 'readonly';
  if (field.lovCode || typeName === 'Enum') return 'lov';
  if (field.dataSource && Object.keys(field.dataSource).length > 0) return 'select';
  if (typeName === 'Boolean') return 'boolean';
  if (typeName === 'DateTime') return 'date';
  if (typeName === 'TimeSpan') return 'time';
  if (NUMERIC_TYPES.has(typeName)) return 'number';
  if (field.url) return 'url';

  return 'text';
}

const FULL_WIDTH_CONTROLS: ReadonlySet<ControlType> = new Set([
  'textarea',
  'json',
  'richHtml',
  'richMarkdown',
  'upload',
  'image',
  'lovMulti',
]);

export function isFullWidthControl(control: ControlType): boolean {
  return FULL_WIDTH_CONTROLS.has(control);
}

export function serializeSubmitModel(
  model: Record<string, unknown>,
  fields: FieldMeta[],
): Record<string, unknown> {
  const multiNames = new Set(
    fields
      .filter((f) => f.multiple || f.itemType === 'multipleSelect')
      .map((f) => f.name),
  );
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(model)) {
    if (multiNames.has(k) && Array.isArray(v)) {
      out[k] = (v as unknown[]).map(String).join(',');
    } else {
      out[k] = v;
    }
  }
  return out;
}

export function resolveNumberPrecision(field: FieldMeta): number | undefined {
  const scale = field.scale ?? 0;
  if (scale > 0) return scale;
  switch (field.typeName) {
    case 'Int32':
    case 'Int64':
      return 0;
    case 'Single':
      return 4;
    case 'Double':
      return 8;
    case 'Decimal':
      return 2;
    default:
      return undefined;
  }
}

export function resolveNumberStep(field: FieldMeta): number {
  const scale = field.scale ?? 0;
  if (scale > 0) return Math.pow(10, -scale);
  if (field.typeName === 'Single' || field.typeName === 'Double') return 0.1;
  if (field.typeName === 'Decimal') return 0.01;
  return 1;
}
