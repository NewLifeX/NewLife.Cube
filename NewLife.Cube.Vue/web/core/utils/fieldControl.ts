/**
 * 字段 → 控件 统一映射（全项目唯一真理源）
 *
 * 规则（优先级）：
 *   1. ItemType 优先于 TypeName（后端显式声明想要的控件，如 image / json / color / mail …）
 *   2. Guid → readonly，且永不进搜索（主键 Id 仍走数值编辑）
 *   3. 枚举 / singleSelect / multipleSelect → 一律走 LOV（lovCode 由后端下发），单选 lov / 多选 lovMulti
 *   4. 数值（Int32/Int64/Decimal/Double/Single）→ inputNumber
 *   5. 大文本（String 且 Length >= 300）→ textarea
 *   6. 未知类型兜底 input，永不白屏
 *
 * 本模块是 index.vue / form.vue / FormContent / ListTableContent / ListSearchBar
 * 共用的唯一映射函数，禁止在以上组件中再写本地 TYPE_TO_*_TYPE 映射。
 */

import type {
  FieldMeta,
  ControlType,
  SearchControlType,
  ListControlType,
} from '../types/field';

/** 数值类型集合 */
const NUMERIC_TYPES: ReadonlySet<string> = new Set([
  'Int32',
  'Int64',
  'Decimal',
  'Double',
  'Single',
]);

/** ItemType（小写） → 表单控件类型 的精确映射表（唯一真理表） */
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

/** 解析小写 ItemType */
function normalizeItemType(field: FieldMeta): string {
  return (field.itemType ?? '').trim().toLowerCase();
}

/**
 * 解析表单 / 编辑控件类型。
 *
 * @param field 字段元数据
 * @returns 控件类型（ControlType）
 */
export function resolveControl(field: FieldMeta): ControlType {
  // 1. ItemType 优先
  const itemType = normalizeItemType(field);
  if (itemType && ITEM_TYPE_TO_CONTROL[itemType]) {
    return ITEM_TYPE_TO_CONTROL[itemType];
  }

  const typeName = field.typeName;

  // 2. Guid → 只读（主键 Id 不在此列，仍走数值/主键编辑）
  if (typeName === 'Guid') {
    return 'readonly';
  }

  // 3. 枚举 / 单选 / 多选：lovCode 由后端下发，统一走 LOV
  if (field.lovCode) {
    return field.multiple || itemType === 'multipleselect' ? 'lovMulti' : 'lov';
  }

  // 4. 已知 CLR 类型
  if (typeName === 'Boolean') return 'switch';
  if (typeName === 'DateTime') return 'datePicker';
  if (typeName === 'TimeSpan') return 'timePicker';
  if (NUMERIC_TYPES.has(typeName)) return 'inputNumber';

  // 枚举类型名（非标准基元，且无 lovCode 兜底）按 lov 渲染
  if (typeName === 'Enum') return 'lov';

  // 5. 字符串：大文本 → textarea
  if (typeName === 'String') {
    const len = field.length ?? 0;
    if (len >= 300) return 'textarea';
    return 'input';
  }

  // 6. 兜底：普通文本输入，永不白屏
  return 'input';
}

/**
 * 解析动态搜索控件类型。
 *
 * @param field 字段元数据（Guid / 主键已在上游过滤，不会进入搜索）
 * @returns 搜索控件类型（SearchControlType）
 */
export function resolveSearchControl(field: FieldMeta): SearchControlType {
  const itemType = normalizeItemType(field);

  // ItemType 优先
  if (itemType === 'file' || itemType === 'image') return 'fileExists';
  if (itemType === 'singleselect') return 'lov';
  if (itemType === 'multipleselect') return 'lovMulti';

  const typeName = field.typeName;

  // Guid / 主键：上游已过滤，这里兜底不进搜索
  if (typeName === 'Guid') return 'text';

  // LOV 驱动
  if (field.lovCode) {
    return field.multiple || itemType === 'multipleselect' ? 'lovMulti' : 'lov';
  }

  // 已知 CLR 类型
  if (typeName === 'Boolean') return 'switch';
  if (typeName === 'DateTime') return 'datetimeRange';
  if (typeName === 'TimeSpan') return 'timeRange';
  if (NUMERIC_TYPES.has(typeName)) return 'numberRange';

  // 默认模糊文本
  return 'text';
}

/**
 * 解析列表单元格渲染类型。
 *
 * @param field 字段元数据
 * @returns 列表渲染类型（ListControlType）
 */
export function resolveListControl(field: FieldMeta): ListControlType {
  const itemType = normalizeItemType(field);

  // ItemType 优先
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
    case 'mail':
    case 'mobile':
    case 'url':
      return 'text';
    case 'singleselect':
    case 'multipleselect':
      return 'lov';
  }

  const typeName = field.typeName;

  if (typeName === 'Guid') return 'readonly';
  if (typeName === 'Boolean') return 'boolean';
  if (typeName === 'DateTime') return 'date';
  if (typeName === 'TimeSpan') return 'time';
  if (NUMERIC_TYPES.has(typeName)) return 'number';

  // 枚举 / LOV
  if (field.lovCode || typeName === 'Enum') return 'lov';

  // 默认文本
  return 'text';
}

/** 表单全宽控件（占满整行） */
const FULL_WIDTH_CONTROLS: ReadonlySet<ControlType> = new Set([
  'textarea',
  'json',
  'richHtml',
  'richMarkdown',
  'upload',
  'image',
  'lovMulti',
]);

/** 判断某控件是否应占满整行 */
export function isFullWidthControl(control: ControlType): boolean {
  return FULL_WIDTH_CONTROLS.has(control);
}

/**
 * 提交前序列化：将多选字段（后端以 String 列存储）的 string[] 合并为逗号分隔字符串，
 * 避免 System.Text.Json 将数组绑定到 String 属性时报错。
 *
 * 判定多选：field.multiple 为 true，或 itemType 为 'multipleSelect'（lovMulti 控件来源）。
 * 仅当值为数组时才合并，其它类型原样透传。
 *
 * @param model 表单数据对象
 * @param fields 字段元数据（用于判定哪些字段是多选）
 * @returns 序列化后的提交对象（多选字段已转为逗号分隔字符串）
 */
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

/**
 * 推导数值控件的小数精度（传给 el-input-number 的 :precision）。
 *
 * 规则（与后端约定一致）：
 *   - 后端返回了有效精度（scale > 0，如 Decimal Scale=2）→ 直接用返回的精度；
 *   - 后端返回 0 精度或根本没下发 scale（即「该有精度却没配」）→ 按 CLR 类型给默认精度：
 *       · 单精度 Single  → 4 位
 *       · 双精度 Double  → 8 位
 *       · 十进制 Decimal → 2 位
 *       · 整数 Int32/Int64 → 0 位（本就无小数）
 *   - 未识别类型兜底 undefined（不限制）。
 *
 * 注意：scale === 0 一律视为「未配置精度」，不会真的用 0（否则浮点字段丢失小数位）。
 *
 * @param field 字段元数据
 * @returns 小数位数（0=整数），undefined 表示不限制
 */
export function resolveNumberPrecision(field: FieldMeta): number | undefined {
  // 后端显式返回了有效精度 → 直接用返回的精度
  if (field.scale > 0) return field.scale;

  // scale 为 0 或未下发 → 按 CLR 类型给默认精度（整数固定 0 位）
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

/**
 * 推导数值控件的步进值（el-input-number 的 :step）。
 * 与 resolveNumberPrecision 配合使用，避免步进超过精度范围。
 *
 * 规则同 resolveNumberPrecision：返回了精度（scale>0）按精度算步进，
 * 否则按类型给合理默认步进（浮点 0.1 / 十进制 0.01 / 整数 1）。
 *
 * @param field 字段元数据
 * @returns 步进值
 */
export function resolveNumberStep(field: FieldMeta): number {
  if (field.scale > 0) return Math.pow(10, -field.scale);
  if (field.typeName === 'Single' || field.typeName === 'Double') return 0.1;
  if (field.typeName === 'Decimal') return 0.01;
  return 1; // 整数默认步进 1
}
