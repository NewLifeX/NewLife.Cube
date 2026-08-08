/**
 * 字段 → 控件映射
 *
 * 对齐 NewLife.Cube.Vue：
 * - `web/core/utils/fieldControl.ts`（LOV / ItemType / SearchControl）
 * - `web/src/utils/other.ts` 的 `getComponentBaseField`（未知 typeName → 枚举下拉）
 */
import type {
  FieldMeta,
  ControlType,
  SearchControlType,
  ListControlType,
} from '../types/field';

/** 已知 CLR 数值类型（搜索用范围、表单用数字框） */
const NUMERIC_TYPES: ReadonlySet<string> = new Set([
  'Int32',
  'Int64',
  'Int16',
  'UInt32',
  'UInt64',
  'Byte',
  'SByte',
  'Decimal',
  'Double',
  'Single',
  'Short',
  'UShort',
]);

/**
 * Cube.Vue `getComponentBaseField` 中的系统类型表。
 * 不在此表内的 typeName 视为枚举/自定义类型 → select + Lookup。
 */
const KNOWN_SYSTEM_TYPES: ReadonlySet<string> = new Set([
  'Int32',
  'Int64',
  'Int16',
  'UInt32',
  'UInt64',
  'Byte',
  'SByte',
  'Short',
  'UShort',
  'Decimal',
  'Double',
  'Single',
  'String',
  'Boolean',
  'DateTime',
  'TimeSpan',
  'Guid',
  'Enum',
]);

/** Cube.Vue contents 映射：已知 ItemType 不当作枚举 Lookup */
const KNOWN_CONTENT_ITEM_TYPES: ReadonlySet<string> = new Set([
  'mail',
  'mobile',
  'image',
  'file',
  'json',
  'html',
  'markdown',
  'color',
  'icon',
  'url',
  'singleselect',
  'multipleselect',
  'area',
  'area4',
  'cascader',
  'date',
  'datetime',
  'time',
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
  area: 'cascader',
  area4: 'cascader',
  cascader: 'cascader',
  date: 'datePicker',
  datetime: 'datePicker',
  time: 'timePicker',
};

/** 地区/级联 ItemType：后端 User.AreaId 使用 area4，地区实体为系统内置 Area */
const CASCADER_ITEM_TYPES: ReadonlySet<string> = new Set(['area', 'area4', 'cascader']);

/** 是否为地区/级联字段（用于表单 Cascader 与列表/搜索适配） */
export function isCascaderField(field: Pick<FieldMeta, 'itemType'>): boolean {
  return CASCADER_ITEM_TYPES.has((field.itemType ?? '').trim().toLowerCase());
}

function normalizeItemType(field: FieldMeta): string {
  return (field.itemType ?? '').trim().toLowerCase();
}

/**
 * 是否为 Cube.Vue 语义下的「枚举类 typeName」
 *（如 SexKinds、DepartmentKinds：非系统 CLR 名，走 select / Lookup）
 */
export function isEnumLikeTypeName(field: FieldMeta): boolean {
  const typeName = (field.typeName ?? '').trim();
  if (!typeName || KNOWN_SYSTEM_TYPES.has(typeName)) return false;
  const itemType = normalizeItemType(field);
  if (itemType && KNOWN_CONTENT_ITEM_TYPES.has(itemType)) return false;
  return true;
}

export function resolveControl(field: FieldMeta): ControlType {
  const itemType = normalizeItemType(field);
  if (itemType && ITEM_TYPE_TO_CONTROL[itemType]) {
    return ITEM_TYPE_TO_CONTROL[itemType];
  }

  const typeName = field.typeName;

  if (typeName === 'Guid') return 'readonly';

  // 布尔优先开关（即使已物化 dataSourceMap）
  if (typeName === 'Boolean') return 'switch';

  // 静态字典（GetPage 已物化 dataSource）优先本地下拉，避免自动 Enum lovCode 抢占导致多余 Meta 请求
  if (field.dataSource && Object.keys(field.dataSource).length > 0) {
    return 'select';
  }

  if (field.lovCode) {
    return field.multiple || itemType === 'multipleselect' ? 'lovMulti' : 'lov';
  }

  if (typeName === 'DateTime') return 'datePicker';
  if (typeName === 'TimeSpan') return 'timePicker';
  if (NUMERIC_TYPES.has(typeName)) return 'inputNumber';
  if (typeName === 'Enum') return 'lov';

  // Cube.Vue getComponentBaseField：未知 typeName → select（枚举）
  if (isEnumLikeTypeName(field)) return 'select';

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
  // 地区/级联：搜索也用 Cascader，便于按省市区逐级过滤
  if (CASCADER_ITEM_TYPES.has(itemType)) return 'cascader';
  if (itemType === 'singleselect') {
    // GetPage 已物化 dataSource 时优先本地下拉，避免再走 Lov Meta
    if (field.dataSource && Object.keys(field.dataSource).length > 0) return 'select';
    return 'lov';
  }
  if (itemType === 'multipleselect') {
    if (field.dataSource && Object.keys(field.dataSource).length > 0) return 'select';
    return 'lovMulti';
  }

  const typeName = field.typeName;

  if (typeName === 'Guid') return 'text';
  if (typeName === 'Boolean') return 'switch';

  // 搜索优先使用 GetPage 物化的 dataSource（枚举/委托字典），直接展示可读标签
  if (field.dataSource && Object.keys(field.dataSource).length > 0) {
    return 'select';
  }

  if (field.lovCode) {
    return field.multiple || itemType === 'multipleselect' ? 'lovMulti' : 'lov';
  }

  // Cube.Vue：未知 typeName（SexKinds 等）→ 下拉，由 Lookup / PrepareForApi 灌选项
  if (isEnumLikeTypeName(field)) return 'select';

  // 日期 / 时间 / 数值（OSC-0016）：单值等值控件，提交字段名=值（不再产生 _min/_max 假范围）
  if (itemType === 'date') return 'date';
  if (itemType === 'datetime') return 'datetime';
  if (itemType === 'time') return 'time';
  if (typeName === 'DateTime') return 'datetime';
  if (typeName === 'TimeSpan') return 'time';
  if (NUMERIC_TYPES.has(typeName)) return 'number';

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
    case 'area':
    case 'area4':
    case 'cascader':
      // 地区/级联列暂以文本展示叶子值；如有 BatchLabel 缓存 label 则由 formatFieldValue 翻译
      return 'text';
    case 'date':
    case 'datetime':
      return 'date';
    case 'time':
      return 'time';
  }

  const typeName = field.typeName;

  if (typeName === 'Guid') return 'readonly';
  if (typeName === 'Boolean') return 'boolean';
  // 静态字典优先（列表显示直接用 label，避免走 LOV/BatchLabel 链路）
  if (field.dataSource && Object.keys(field.dataSource).length > 0) return 'select';
  if (field.lovCode || typeName === 'Enum') return 'lov';
  if (isEnumLikeTypeName(field)) return 'select';
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

/** 审计字段名集合：创建/更新用户、IP、时间；新增/编辑表单不展示 */
const AUDIT_FIELD_NAMES = new Set([
  'createuser',
  'createuserid',
  'createip',
  'createtime',
  'updateuser',
  'updateuserid',
  'updateip',
  'updatetime',
]);

/** 是否为审计字段（创建/更新的用户、IP、时间），按字段名小写匹配 */
export function isAuditField(field: Pick<FieldMeta, 'name'>): boolean {
  return AUDIT_FIELD_NAMES.has((field.name || '').toLowerCase());
}

/**
 * 提交值类型归一化（OSC-0008）：
 * 枚举/Lov/select 控件的值来自 dataSource 字符串 key（如 "1"），
 * MVC 版后端 System.Text.Json 反序列化时拒绝 JSON 字符串绑定到 Int32/Enum 属性，
 * 这里按字段元数据把字符串数字转回原生类型。空值原样交给上层处理。
 */
/** 64 位整数：安全范围内转 number（保证后端 System.Text.Json 绑定成功）；超安全整数保留字符串避免精度丢失 */
function normalizeInt64(value: unknown): unknown {
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string' && /^-?\d+$/.test(value.trim())) {
    const n = Number(value);
    return Number.isSafeInteger(n) ? n : value;
  }
  return value;
}

export function normalizeSubmitValue(field: FieldMeta | undefined, value: unknown): unknown {
  if (value == null || value === '') return value;
  const typeName = (field?.typeName ?? '').trim();
  // Int64/UInt64 可能承载雪花 ID 等超安全整数；无条件 Number 会丢精度
  if (typeName === 'Int64' || typeName === 'UInt64') return normalizeInt64(value);
  if (NUMERIC_TYPES.has(typeName)) {
    if (typeof value === 'number' && Number.isFinite(value)) return value;
    if (typeof value === 'string' && /^-?\d+(\.\d+)?$/.test(value.trim())) return Number(value);
    return value;
  }
  if (typeName === 'Boolean') {
    if (typeof value === 'boolean') return value;
    if (value === 'true' || value === '1') return true;
    if (value === 'false' || value === '0') return false;
    return value;
  }
  if (typeName === 'Enum') {
    if (typeof value === 'string' && /^-?\d+$/.test(value.trim())) return Number(value);
  }
  return value;
}

export function serializeSubmitModel(
  model: Record<string, unknown>,
  fields: FieldMeta[],
): Record<string, unknown> {
  const fieldMap = new Map(fields.map((f) => [f.name, f]));
  const multiNames = new Set(
    fields
      // itemType 大小写不敏感（MultipleSelect / multipleselect 等价）
      .filter((f) => f.multiple || normalizeItemType(f) === 'multipleselect')
      .map((f) => f.name),
  );
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(model)) {
    if (multiNames.has(k) && Array.isArray(v)) {
      // XCode 多选约定：逗号分隔字符串
      out[k] = (v as unknown[]).map(String).join(',');
    } else {
      out[k] = normalizeSubmitValue(fieldMap.get(k), v);
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
