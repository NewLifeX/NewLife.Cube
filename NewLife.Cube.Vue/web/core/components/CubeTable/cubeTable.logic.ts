/**
 * `CubeTable` 内置子组件的纯显示态计算（无 Vue / DOM / 网络依赖，可单测）
 *
 * 口径来源：搜索控件与列宽/对齐沿用 `core/views/components/ListSearchBar.vue`
 * 和 `ListTableContent.vue` 的既有实现（两者是 Section 遗留组件，`CubeTable` 落地后
 * 按 `docs/architecture/cube-engine.md` §7 P6 下线，故此处取同一份口径而不是新建第二套）。
 * 控件类型判定一律走 `core/utils/fieldControl.ts`（全项目唯一真理源），本文件不写类型映射表。
 */
import {
  enumOptionValue,
  isMoneyItemType,
  isPercentItemType,
  parseDecimalItemType,
  resolveListControl,
  resolveSearchControl,
} from '../../utils/fieldControl';
import { formatCellDateTime } from '../../engine/logic';
import type { FieldMeta, ListControlType } from '../../types/field';
import type { CubeColumnView, CubeSearchControl, CubeSearchKind } from './types';

/**
 * 搜索区是否需要「展开/收起」按钮。
 *
 * MES 的列表页是一体式搜索栏：收起态只有关键字一行，展开才出现条件面板。
 * 库场景的条件由 `meta` 元数据生成，所以「有条件」才给展开入口。
 */
export function showSearchToggle(fieldCount: number): boolean {
  return fieldCount > 0;
}

/** 布尔搜索选项：`''` 表示不参与筛选（提交时被 `dropEmptyParams` 丢弃） */
export const BOOLEAN_FILTER_OPTIONS = [
  { label: '全部', value: '' },
  { label: '是', value: 'true' },
  { label: '否', value: 'false' },
];

/** 附件存在性搜索选项 */
export const FILE_EXISTS_OPTIONS = [
  { label: '全部', value: '' },
  { label: '有', value: 'true' },
  { label: '无', value: 'false' },
];

/** 占整行的控件（范围类与值集多选） */
const FULL_ROW_KINDS: ReadonlySet<CubeSearchKind> = new Set<CubeSearchKind>([
  'lovMulti',
  'fileExists',
  'area',
]);

/**
 * 内联字典选项（`dataSource` 的键为字符串，纯数字键转 number 以便后端按枚举绑定）。
 * 值口径与表单侧 `FormContent.vue` 的消费方式一致。
 */
export function dictOptions(field: FieldMeta): Array<{ label: string; value: string | number }> {
  const entries = Object.entries(field.dataSource ?? {});
  return entries.map(([key, label]) => ({ label, value: enumOptionValue(key) }));
}

/** 单个搜索字段的控件显示态 */
export function buildSearchControl(field: FieldMeta): CubeSearchControl {
  // 后端声明的区间项直接使用显式起止参数（主时间字段固定为 dtStart/dtEnd）
  if (field.kind === 'range') {
    return {
      field,
      kind: 'range',
      key: field.name,
      maxKey: field.end ?? null,
      rangeType: (field.itemType ?? '').toLowerCase() === 'date' ? 'date' : 'datetime',
      options: [],
      span: 2,
    };
  }

  let kind: CubeSearchKind = resolveSearchControl(field);

  // 后端把 DateTime 字段声明为纯日期（ItemType='date'，如生日、班次日期）时，
  // `resolveSearchControl` 仍按 typeName 给 datetimeRange，搜索要用户填到时分秒。
  // 这里按同一份 itemType 降级为日期区间 —— 与 `widthForControl` 用 itemType 分宽度的口径一致。
  const itemType = (field.itemType ?? '').trim().toLowerCase();
  if (kind === 'datetimeRange' && itemType === 'date') kind = 'dateRange';

  // `resolveSearchControl` 对 `typeName === 'Enum'` 也返回 lov，但那种字段常常没有 lovCode
  // （后端只在显式配置值集时才下发）。没有码值就无从选择，降级成文本框，
  // 免得渲染出一个永远选不出东西的空 LOV。
  if ((kind === 'lov' || kind === 'lovMulti') && !field.lovCode) kind = 'text';

  // resolveSearchControl 不认 dataSource，字典枚举会落到 text —— 这里按同一份 dataSource 升级为下拉
  if (
    kind === 'text' &&
    !field.lovCode &&
    field.dataSource &&
    Object.keys(field.dataSource).length > 0
  ) {
    kind = 'dict';
  }

  const isRange = kind === 'numberRange' || kind === 'timeRange';
  // 范围类拆成两个后缀键（与 `ListSearchBar.vue:166/173/207/215` 一致），
  // 后端 `Index` 直接按 `名称_min` / `名称_max` 绑定，不需要前端再合并
  return {
    field,
    kind,
    key: isRange ? `${field.name}_min` : field.name,
    maxKey: isRange ? `${field.name}_max` : null,
    options:
      kind === 'dict'
        ? dictOptions(field)
        : kind === 'switch'
          ? BOOLEAN_FILTER_OPTIONS
          : kind === 'fileExists'
            ? FILE_EXISTS_OPTIONS
            : [],
    span: FULL_ROW_KINDS.has(kind) ? 2 : 1,
  };
}

export function buildSearchControls(fields: FieldMeta[]): CubeSearchControl[] {
  return fields.map(buildSearchControl);
}



/** 列宽（沿用 `ListTableContent.vue:60-66`，并补日期类宽度） */
export function widthForControl(control: ListControlType, field: FieldMeta): number | string {
  if (field.primaryKey) return 80;
  if (control === 'image' || control === 'file') return 100;
  if (control === 'color' || control === 'icon') return 90;
  if (control === 'boolean') return 90;
  if (control === 'date') {
    // `resolveListControl` 把 DateTime 也归为 date，用 itemType 区分只显示日期还是到时分秒
    const itemType = (field.itemType ?? '').trim().toLowerCase();
    return itemType === 'date' ? 120 : 165;
  }
  if (control === 'time') return 100;
  return 'auto';
}

/** 解析后端 TextAligns（1/2/3）或字符串形式的对齐方式，未声明返回 undefined */
export function normalizeTextAlign(value?: string | number): 'left' | 'center' | 'right' | undefined {
  if (value === undefined || value === null || value === '') return undefined;
  if (typeof value === 'number') {
    if (value === 1) return 'left';
    if (value === 2) return 'center';
    if (value === 3) return 'right';
    return undefined;
  }
  const text = String(value).trim().toLowerCase();
  if (text === 'left' || text === 'text-left') return 'left';
  if (text === 'center' || text === 'text-center') return 'center';
  if (text === 'right' || text === 'text-right') return 'right';
  return undefined;
}

/** 列头文字：后端 header 优先，其次 displayName，最后字段名 */
export function fieldHeader(field: FieldMeta): string {
  return field.header?.trim() || field.displayName || field.name;
}

/** 对齐（字段显式 textAlign 优先；否则数值右对齐、布尔/颜色/图标/图片/主键居中，其余左对齐） */
export function alignItemsForControl(control: ListControlType, field: FieldMeta): 'left' | 'center' | 'right' {
  const explicit = normalizeTextAlign(field.textAlign);
  if (explicit) return explicit;
  // 百分比与 MVC 同口径居中，金额与小数右对齐（与数字口径一致）
  if (isPercentItemType(field.itemType)) return 'center';
  if (isMoneyItemType(field.itemType) || parseDecimalItemType(field.itemType)) return 'right';
  if (control === 'number') return 'right';
  if (control === 'boolean' || control === 'color' || control === 'icon' || control === 'image') return 'center';
  if (field.primaryKey) return 'center';
  return 'left';
}

/**
 * 生成列显示态。
 *
 * - `visibility` 为 `null` 表示未启用「设置列」，全部显示（取消勾选的列被过滤掉）；
 * - `showKeyColumn` 决定主键列是否出现（缺省显示）；
 * - 列顺序保持元数据下发顺序，前端不擅自排序。
 */
export function buildColumnViews(
  fields: FieldMeta[],
  visibility: Record<string, boolean> | null = null,
  options: { showKeyColumn?: boolean } = {},
): CubeColumnView[] {
  const { showKeyColumn = true } = options;
  return fields
    .filter((field) => (field.primaryKey ? showKeyColumn : true))
    .filter((field) => field.hidden !== true)
    .filter((field) => {
      // 用户显式设置优先；没有配置时按字段可见性（visible=false 的列收起）
      if (visibility && field.name in visibility) return visibility[field.name] !== false;
      return field.visible !== false;
    })
    .map((field) => {
      const control = resolveListControl(field);
      return {
        key: field.name,
        label: fieldHeader(field),
        control,
        field,
        width: field.width ?? widthForControl(control, field),
        align: alignItemsForControl(control, field),
        fixed: field.fixed,
        isKey: Boolean(field.primaryKey),
      };
    });
}

/** 「设置列」候选项（key/label） */
export function columnItemsFromFields(
  fields: FieldMeta[],
): Array<{ key: string; label: string; visible?: boolean }> {
  return fields.map((f) => ({ key: f.name, label: f.displayName || f.name, visible: f.visible }));
}

/** 工具栏按钮开关入参（`undefined` 表示未显式指定，交由后端 `pageSetting` 决定） */
export interface ToolbarFlagInput {
  add?: boolean;
  batchDelete?: boolean;
  importFile?: boolean;
  exportFile?: boolean;
  downloadTemplate?: boolean;
  refresh?: boolean;
}

/** 操作行与指标行的按钮开关结论（同一份判定，两行各自取用） */
export interface ToolbarFlags {
  add: boolean;
  batchDelete: boolean;
  importFile: boolean;
  exportFile: boolean;
  downloadTemplate: boolean;
  refresh: boolean;
  /** 后端整页只读时一律隐藏写入口 */
  readOnly: boolean;
}

/**
 * 弹窗形态按字段数自适应（窄弹窗里塞 20 个字段等于没法用）。
 *
 * 与 `openListFormDialog.ts:198-204` 的分档同源，但统一用 `el-dialog` 承载：
 * 抽屉（`el-drawer`）需要为两种容器各写一遍表单体，收益不抵重复；
 * 需要换抽屉的业务页可以直接用 `#form` 整块替换（见 README「插槽」）。
 */
export function dialogShapeFor(fieldCount: number): { width: string; columns: number } {
  if (fieldCount > 15) return { width: '960px', columns: 3 };
  if (fieldCount > 10) return { width: '860px', columns: 2 };
  return { width: '700px', columns: 2 };
}

/**
 * 计算工具栏该显示哪些按钮。
 *
 * 优先级：显式 props > 后端 `pageSetting`（`enableAdd` / `enableSelect` / `isReadOnly`）> 默认。
 * `isReadOnly` 一票否决所有写入口（新增/批量删除），只留查询类动作。
 * 导入/导出/下载模板默认开启（对齐 `ListToolbar.vue`），后端未实现对应 Action 时由
 * 调用侧显式关掉，不做「请求失败再隐藏」的猜测。
 */
export function resolveToolbarFlags(
  input: ToolbarFlagInput,
  setting?: { enableAdd?: boolean; enableSelect?: boolean; isReadOnly?: boolean } | null,
  permissions?: { add?: boolean; delete?: boolean; export?: boolean },
): ToolbarFlags {
  const readOnly = setting?.isReadOnly === true;
  return {
    readOnly,
    add: input.add ?? (setting?.enableAdd !== false && permissions?.add !== false),
    batchDelete: input.batchDelete ?? (setting?.enableSelect !== false && permissions?.delete !== false),
    importFile: input.importFile ?? true,
    exportFile: input.exportFile ?? permissions?.export !== false,
    downloadTemplate: input.downloadTemplate ?? false,
    refresh: input.refresh ?? true,
  };
}

/** 列显隐的默认存储键（按实体前缀区分，避免多页共用一份配置） */
export function defaultColumnsStorageKey(prefix: string): string {
  return `cube-table:columns:${prefix}`;
}

/**
 * 多值归一：数组 / JSON 数组串 / 逗号串 / 单值 → `string[]`。
 * 与 `ListTableContent.vue:116-133` 同一口径（多选 LOV 列后端可能以任意外形下发）。
 */
export function normalizeMulti(raw: unknown): string[] {
  if (raw === null || raw === undefined || raw === '') return [];
  if (Array.isArray(raw)) return raw.map((v) => String(v));
  if (typeof raw === 'string') {
    const s = raw.trim();
    if (s.startsWith('[')) {
      try {
        const arr = JSON.parse(s) as unknown;
        if (Array.isArray(arr)) return arr.map((v) => String(v));
      } catch {
        /* 不是合法 JSON 就按逗号串处理 */
      }
    }
    return s.split(',').map((x) => x.trim()).filter(Boolean);
  }
  return [String(raw)];
}

/** 富文本 / Json 摘要截断（超长折叠到 popover 里看全文） */
export function truncate(text: string, len = 40): string {
  if (!text) return '';
  const clean = text.replace(/\s+/g, ' ').trim();
  return clean.length > len ? `${clean.slice(0, len)}…` : clean;
}

/**
 * 单元格的「片段数组」：值集列返回逐项标签（多值渲染成标签），其它列返回单元素数组。
 * 文本渲染与标签渲染共用它，避免两条分支对值集的处理漂移。
 */
export function cellParts(
  column: CubeColumnView,
  raw: unknown,
  options: { lovLabel: (lovCode: string, value: unknown) => string } = {
    lovLabel: (_code, value) => String(value ?? ''),
  },
): string[] {
  if (raw === null || raw === undefined || raw === '') return [];
  if (column.control === 'lov') {
    const values = normalizeMulti(raw);
    const code = column.field.lovCode;
    return code ? values.map((v) => options.lovLabel(code, v)) : values;
  }
  const one = singleCellText(column, raw);
  return one === '' ? [] : [one];
}

/** 百分比格式化。对齐 MVC `_List_Data_Item`：按描述中的单位先缩放，再乘 100 加 % */
export function formatPercentCell(field: FieldMeta, raw: unknown): string {
  let value = Number(raw);
  if (!Number.isFinite(value)) return String(raw ?? '');

  let scale = field.scale ?? 0;
  const description = field.description ?? '';
  if (description.includes('十分之一')) {
    value /= 10;
    scale += 1;
  }
  if (description.includes('百分之一')) {
    value /= 100;
    scale += 2;
  }
  if (description.includes('千分之一')) {
    value /= 1000;
    scale += 3;
  }
  if (description.includes('万分之一')) {
    value /= 10000;
    scale += 4;
  }

  const decimals = scale >= 2 ? scale - 2 : 0;
  return `${(value * 100).toFixed(decimals)}%`;
}

/** 金额格式化。默认两位小数，带千分位与币种符号 */
export function formatMoneyCell(field: FieldMeta, raw: unknown): string {
  const value = Number(raw);
  if (!Number.isFinite(value)) return String(raw ?? '');

  const scale = field.scale && field.scale > 0 ? field.scale : 2;
  return `¥${value.toLocaleString('zh-CN', {
    minimumFractionDigits: scale,
    maximumFractionDigits: scale,
  })}`;
}

/** decimal(p,s) 格式化。按声明的小数位补齐，带千分位 */
export function formatDecimalCell(field: FieldMeta, raw: unknown): string {
  const value = Number(raw);
  if (!Number.isFinite(value)) return String(raw ?? '');

  const decimalType = parseDecimalItemType(field.itemType);
  const scale = decimalType?.scale ?? field.scale ?? 0;
  return scale > 0
    ? value.toLocaleString('zh-CN', {
        minimumFractionDigits: scale,
        maximumFractionDigits: scale,
      })
    : String(value);
}

/** 非值集列的显示文本 */
function singleCellText(column: CubeColumnView, raw: unknown): string {
  const itemType = column.field.itemType;
  if (isPercentItemType(itemType)) return formatPercentCell(column.field, raw);
  if (isMoneyItemType(itemType)) return formatMoneyCell(column.field, raw);
  if (parseDecimalItemType(itemType)) return formatDecimalCell(column.field, raw);

  switch (column.control) {
    case 'date': {
      const itemType = (column.field.itemType ?? '').trim().toLowerCase();
      return formatCellDateTime(raw, itemType === 'date' ? 'date' : 'datetime');
    }
    case 'time':
      return formatCellDateTime(raw, 'time');
    case 'json':
    case 'html':
      return truncate(String(raw));
    case 'boolean':
      return cellBoolean(raw) ? '是' : '否';
    default:
      return String(raw);
  }
}

/**
 * 单元格文本（把「取显示态字符串」这件所有列都要做的事收进纯函数，视图只绑定）。
 *
 * - `lov`：多值翻译后用 `、` 连接（与 `ListTableContent.vue:169-178` 同口径）；
 * - `date` / `time`：走 `formatCellDateTime` 归一（旧 Section 组件对这两类没有分支，
 *   直接把后端 ISO 串透出来，`2026-10-19T08:30:00` 这种可读性很差）；
 * - `json` / `html`：截断摘要，全文交给 popover；
 * - 其余：`String(raw)`，空值返回空串由视图统一显示 `—`。
 */
export function cellText(
  column: CubeColumnView,
  raw: unknown,
  options?: {
    lovLabel?: (lovCode: string, value: unknown) => string;
    /** 当前行。用于 text 模板占位符替换 */
    row?: Record<string, unknown>;
    /** 当前查询参数。用于 {page:参数名} 占位符替换 */
    page?: Record<string, unknown>;
  },
): string {
  const text = column.field.text
    ? applyLinkTemplate(column.field.text, options?.row, options?.page)
    : cellParts(
        column,
        raw,
        options?.lovLabel ? { lovLabel: options.lovLabel } : undefined,
      ).join('、');
  const maxWidth = column.field.maxWidth ?? 0;
  return maxWidth > 0 ? truncate(text, maxWidth) : text;
}

/** 读取行值。元数据字段名与行对象可能大小写不一致（Id/id、ID） */
function readRowValue(row: Record<string, unknown>, key: string): unknown {
  if (key in row) return row[key];
  const hit = Object.keys(row).find((k) => k.toLowerCase() === key.toLowerCase());
  return hit ? row[hit] : undefined;
}

/** 读取多级路径（如 Parent.Name），逐级大小写不敏感取值 */
function readPathValue(row: Record<string, unknown>, path: string): unknown {
  let current: unknown = row;
  for (const part of path.split('.')) {
    if (current === null || current === undefined || typeof current !== 'object') return undefined;
    current = readRowValue(current as Record<string, unknown>, part);
  }
  return current;
}

/** 模板值转字符串：空值统一为空串，避免出现 "undefined" */
function formatTemplateValue(value: unknown): string {
  if (value === null || value === undefined) return '';
  return String(value);
}

/**
 * 替换列表字段模板中的占位符。
 *
 * - `{字段名}` / `{父.子}`：取当前行值；
 * - `{page:参数名}`：取当前页面查询参数，下钻时保留上下文；
 * - 不支持的 `{page:$BaseUrl(...)}` 等复杂表达式按空串处理（API 版不复刻）。
 */
export function applyLinkTemplate(
  template: string,
  row?: Record<string, unknown>,
  page?: Record<string, unknown>,
): string {
  if (!template) return '';
  return template
    .replace(/\{page:([^}]+)\}/g, (_match, name: string) =>
      formatTemplateValue(page ? readRowValue(page, name.trim()) : undefined),
    )
    .replace(/\{([^{}]+)\}/g, (_match, path: string) =>
      formatTemplateValue(row ? readPathValue(row, path.trim()) : undefined),
    );
}

/** 单元格取值：valueField 有值时优先，空值回退本字段 */
export function cellRawValue(row: Record<string, unknown>, column: CubeColumnView): unknown {
  const valueField = column.field.valueField;
  if (valueField) {
    const value = readPathValue(row, valueField);
    if (value !== null && value !== undefined && value !== '') return value;
  }
  return readRowValue(row, column.key);
}

/** 单元格链接地址：按行值 / 页面参数替换占位符 */
export function cellLinkUrl(
  column: CubeColumnView,
  row: Record<string, unknown>,
  page?: Record<string, unknown>,
): string {
  return applyLinkTemplate(column.field.url ?? '', row, page);
}

/** 单元格提示文字：title 优先，其次 headerTitle */
export function cellTitle(
  column: CubeColumnView,
  row: Record<string, unknown>,
  page?: Record<string, unknown>,
): string {
  const template = column.field.title || column.field.headerTitle;
  return template ? applyLinkTemplate(template, row, page) : '';
}

/** 该列是否需要「点击看全文」的 popover（Json / 富文本） */
export function isExpandableCell(control: ListControlType): boolean {
  return control === 'json' || control === 'html';
}

/**
 * 布尔单元格取值归一。
 *
 * 后端回包对布尔字段可能给出 `true/false`、`1/0`、`'true'/'false'` 三种形态
 * （XCode 序列化差异 + 前端 PascalCase 别名键），`Boolean('false') === true` 是经典坑，
 * 所以字符串必须显式比较，不能直接强转。
 */
export function cellBoolean(raw: unknown): boolean {
  if (typeof raw === 'string') {
    const s = raw.trim().toLowerCase();
    return s === 'true' || s === '1';
  }
  if (typeof raw === 'number') return raw !== 0;
  return raw === true;
}

/** 纯文本类单元格才需要溢出省略提示（图片/开关/色块加了会遮挡内容） */
const OVERFLOW_TOOLTIP_CONTROLS: ReadonlySet<ListControlType> = new Set<ListControlType>([
  'text',
  'number',
  'lov',
  'html',
  'json',
  'date',
  'time',
  'readonly',
]);

export function isOverflowTooltipCell(control: ListControlType): boolean {
  return OVERFLOW_TOOLTIP_CONTROLS.has(control);
}

/** 表格区开关入参 */
export interface GridFlagInput {
  selection?: boolean;
  indexColumn?: boolean;
  rowActions?: boolean;
  doubleClickEdit?: boolean;
}

/** 表格区开关结论 */
export interface GridFlags {
  selection: boolean;
  indexColumn: boolean;
  rowActions: boolean;
  doubleClickEdit: boolean;
  readOnly: boolean;
}

/**
 * 计算表格区行为。
 *
 * - 勾选列与工具栏「批量删除」同一口径（`enableSelect !== false`），
 *   否则会出现「有批量删除按钮却没有勾选框」或反之；
 * - 操作列与双击编辑在整页只读时一律关闭；
 * - 索引列默认关闭（属于额外信息密度，业务页显式要）。
 */
export function resolveGridFlags(
  input: GridFlagInput,
  setting?: {
    enableSelect?: boolean;
    isReadOnly?: boolean;
    enableTableDoubleClick?: boolean;
  } | null,
  permissions?: { update?: boolean; delete?: boolean },
): GridFlags {
  const readOnly = setting?.isReadOnly === true;
  const canBatch = permissions?.update !== false || permissions?.delete !== false;
  return {
    readOnly,
    selection: input.selection ?? (setting?.enableSelect !== false && canBatch),
    indexColumn: input.indexColumn ?? false,
    rowActions: input.rowActions ?? !readOnly,
    doubleClickEdit: input.doubleClickEdit ?? Boolean(setting?.enableTableDoubleClick),
  };
}

/** 操作列最多直接显示的动作数：超出即收起，第 4 个位置让给「更多」下拉 */
export const MAX_VISIBLE_ACTIONS = 4;

/** 单个行操作按钮的估算宽度（link 样式：图标 + 两字文本） */
const ACTION_BUTTON_WIDTH = 56;

/** 操作列单元格左右内边距的估算合计 */
const ACTION_CELL_PADDING = 24;

/** 按钮间留白（el-button 默认 margin-left 口径） */
const ACTION_BUTTON_GAP = 8;

/** 操作列布局结论 */
export interface RowActionLayout {
  /** 是否发生收起（动作数超过直接显示上限） */
  folded: boolean;
  /** 建议列宽（px）：按直接显示的动作数展开 */
  width: number;
}

/**
 * 按行动作数计算操作列布局：
 * ≤4 个全部直接显示、列宽随个数增长；>4 个只露前 3 个 + 「更多」下拉，
 * 列宽按 4 个单元封顶（后续新加的动作一律进「更多」，不再撑宽列）。
 */
export function layoutRowActions(count: number): RowActionLayout {
  const unitCount = Math.max(Math.min(count, MAX_VISIBLE_ACTIONS), 1);
  return {
    folded: count > MAX_VISIBLE_ACTIONS,
    width: ACTION_CELL_PADDING + unitCount * ACTION_BUTTON_WIDTH + (unitCount - 1) * ACTION_BUTTON_GAP,
  };
}

/**
 * 把某一行的动作列表切成「直接显示」与「收进更多」两段。
 *
 * 收起判定按行做：某行因 `visible(row)` 过滤后不足 4 个时，即使列宽按更多行
 * 撑过，也直接全显、不给这一行硬塞一个空的「更多」。
 */
export function splitRowActions<T>(actions: T[]): { direct: T[]; overflow: T[] } {
  if (actions.length <= MAX_VISIBLE_ACTIONS) return { direct: actions, overflow: [] };
  const cut = MAX_VISIBLE_ACTIONS - 1;
  return { direct: actions.slice(0, cut), overflow: actions.slice(cut) };
}
/** 工具栏按钮的主键字段名（来自元数据 `primaryKey`，缺省 `id`） */
export function toolbarKeyField(fields: FieldMeta[]): string {
  return fields.find((f) => f.primaryKey)?.name ?? 'id';
}

/**
 * 组装工具栏按钮的提交参数。
 *
 * 口径对齐 MVC 的 `data-fields`：名称 `keys` 表示选中行主键（逗号拼接，与批量删除的
 * `?ids=1,2,3` 一致），其余名称取当前查询值；空值不提交。
 */
export function buildToolbarPayload(options: {
  include?: string[] | null;
  selected: Record<string, unknown>[];
  keyField: string;
  query: Record<string, unknown>;
}): Record<string, unknown> {
  const payload: Record<string, unknown> = {};
  for (const name of options.include ?? []) {
    if (!name) continue;
    if (name.toLowerCase() === 'keys') {
      const keys = options.selected
        .map((row) => readRowValue(row, options.keyField))
        .filter((v) => v !== undefined && v !== null && v !== '')
        .map(String);
      if (keys.length > 0) payload.keys = keys.join(',');
      continue;
    }
    const value = options.query[name];
    if (value === undefined || value === null || value === '') continue;
    payload[name] = Array.isArray(value) ? value.join(',') : value;
  }
  return payload;
}

/** 拼接 link 按钮要携带的当前查询参数（对应 MVC 的 `page.GetBaseUrl`） */
export function toolbarQueryString(keyword: string, model: Record<string, unknown>): string {
  const params = new URLSearchParams();
  const kw = (keyword ?? '').trim();
  if (kw) params.set('q', kw);
  for (const [key, value] of Object.entries(model ?? {})) {
    if (value === undefined || value === null || value === '') continue;
    if (Array.isArray(value)) {
      if (value.length > 0) params.set(key, value.join(','));
      continue;
    }
    if (typeof value === 'object') continue;
    params.set(key, String(value));
  }
  return params.toString();
}
