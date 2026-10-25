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
import i18n from '../../i18n';
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
  {
    get label() {
      return i18n.global.t('grid.filterAll');
    },
    value: '',
  },
  {
    get label() {
      return i18n.global.t('common.yes');
    },
    value: 'true',
  },
  {
    get label() {
      return i18n.global.t('common.no');
    },
    value: 'false',
  },
];

/** 附件存在性搜索选项 */
export const FILE_EXISTS_OPTIONS = [
  {
    get label() {
      return i18n.global.t('grid.filterAll');
    },
    value: '',
  },
  {
    get label() {
      return i18n.global.t('grid.filterHas');
    },
    value: 'true',
  },
  {
    get label() {
      return i18n.global.t('grid.filterNone');
    },
    value: 'false',
  },
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
 * 按用户列序排序。未出现在 order 中的新列按原顺序追加到末尾。
 * 大小写不敏感，返回的仍是原对象引用。
 */
export function sortByOrder<T>(
  items: T[],
  order: string[] | null | undefined,
  keyOf: (item: T) => string,
): T[] {
  if (!order?.length || items.length === 0) return items;

  const map = new Map<string, T>();
  for (const item of items) map.set(keyOf(item).toLowerCase(), item);

  const result: T[] = [];
  for (const key of order) {
    const hit = map.get(String(key).toLowerCase());
    if (!hit) continue;
    result.push(hit);
    map.delete(String(key).toLowerCase());
  }
  for (const item of items) {
    if (map.has(keyOf(item).toLowerCase())) result.push(item);
  }
  return result;
}

/** 解析持久化的列序。坏数据返回 null，由调用方回退元数据顺序 */
export function parseColumnOrder(raw: string | null | undefined): string[] | null {
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw);
    if (!Array.isArray(parsed)) return null;
    const keys = parsed.filter((item): item is string => typeof item === 'string' && item.length > 0);
    return keys.length > 0 ? keys : null;
  } catch {
    return null;
  }
}

/**
 * 合并列序：只保留当前存在的列，新列追加到末尾。
 * 缺省配置返回元数据顺序，保证升级后新字段不会凭空消失。
 */
export function mergeColumnOrder(keys: string[], saved: string[] | null | undefined): string[] {
  const known = new Map(keys.map((key) => [key.toLowerCase(), key]));
  const result: string[] = [];
  for (const key of saved ?? []) {
    const hit = known.get(String(key).toLowerCase());
    if (!hit || result.includes(hit)) continue;
    result.push(hit);
    known.delete(String(key).toLowerCase());
  }
  for (const key of keys) {
    if (!result.includes(key)) result.push(key);
  }
  return result;
}

/**
 * 生成列显示态。
 *
 * - `visibility` 为 `null` 表示未启用「设置列」，全部显示（取消勾选的列被过滤掉）；
 * - `showKeyColumn` 决定主键列是否出现（缺省显示）；
 * - `order` 为用户列序；缺省保持元数据下发顺序。
 */
export function buildColumnViews(
  fields: FieldMeta[],
  visibility: Record<string, boolean> | null = null,
  options: { showKeyColumn?: boolean; order?: string[] | null } = {},
): CubeColumnView[] {
  const { showKeyColumn = true } = options;
  return sortByOrder(fields, options.order, (field) => field.name)
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
  order?: string[] | null,
): Array<{ key: string; label: string; visible?: boolean }> {
  const items = fields.map((f) => ({ key: f.name, label: f.displayName || f.name, visible: f.visible }));
  return sortByOrder(items, order, (item) => item.key);
}

/** 工具栏响应。沿用 MVC onActionComplete 约定：message / data.refresh / data.url / data.time */
export interface ToolbarResponse {
  message?: string;
  data?: Record<string, unknown>;
}

/** 工具栏响应解析结果。url 与 refresh 互斥，delay 为毫秒 */
export interface ToolbarResult {
  message?: string;
  refresh: boolean;
  url?: string;
  delay: number;
}

/** 解析工具栏响应。`url='[refresh]'` 视为刷新，`time` 为跳转/刷新前的延迟秒数 */
export function parseToolbarResult(res?: ToolbarResponse): ToolbarResult {
  const body = res?.data ?? {};
  const message = res?.message ?? (body.message as string | undefined);
  const url = body.url as string | undefined;
  const time = Number(body.time ?? 0);

  return {
    message: message ? String(message) : undefined,
    refresh: url === '[refresh]' ? true : url ? false : body.refresh === true,
    url: url && url !== '[refresh]' ? String(url) : undefined,
    delay: time > 0 ? time * 1000 : 0,
  };
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
 * 是否提供批量启用/禁用。
 *
 * 对齐 MVC `_List_Toolbar.cshtml` 的自动注入规则：实体含 Boolean 类型的 `Enable` 字段时，
 * 批量区多出「批量启用 / 批量禁用」两个动作（后端 `enableSelect` / `disableSelect`）。
 * @param fields 列表字段元数据
 */
export function canBatchEnable(fields: FieldMeta[]): boolean {
  return (fields ?? []).some(
    (field) => field.name?.toLowerCase() === 'enable' && /bool/i.test(field.typeName ?? ''),
  );
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

/**
 * 统计行单元格文案（对齐 MVC `_List_Data_Stat` 口径）。
 *
 * - 主键列显示「合计」标签；
 * - 布尔、日期时间、外键（`*ID`/`*Id`）列留空 —— 汇总这些值没有意义；
 * - 其余列用同一套 `cellText` 格式化（金额/百分号/时间跨度/字典都跟着走）。
 *
 * @param column 列视图
 * @param stat 后端 `stat` 汇总实体
 * @param totalLabel 主键列显示的合计标签（已本地化）
 */
export function statCellText(
  column: CubeColumnView,
  stat: Record<string, unknown> | null | undefined,
  totalLabel: string,
): string {
  if (!stat) return '';
  if (column.field.primaryKey) return totalLabel;

  const name = column.field.name ?? '';
  if (/id$/i.test(name)) return '';

  const type = (column.field.typeName ?? '').trim().toLowerCase();
  if (type === 'boolean' || type === 'datetime' || type === 'guid') return '';

  const raw = readRowValue(stat, name);
  if (raw === null || raw === undefined || raw === '') return '';

  return cellText(column, raw);
}

/** 富文本 / Json 摘要截断（超长折叠到 popover 里看全文） */
export function truncate(text: string, len = 40): string {
  if (!text) return '';
  const clean = text.replace(/\s+/g, ' ').trim();
  return clean.length > len ? `${clean.slice(0, len)}…` : clean;
}

/**
 * 枚举 / 字典列的显示文案（后端 `dataSource` 下发的本地化字典）。
 *
 * 后端对枚举型字段会下发 `{ "0": "普通", "3": "布尔型" }` 这类字典（键是字符串形式的值，文案已按界面语言本地化）。
 * 列表单元格按它翻译，前端不再维护字段的枚举映射表——与 MVC 直接显示后端枚举描述同一口径。
 *
 * @param field 列表字段元数据
 * @param raw 行数据里的原始值
 * @returns 字典文案；未命中返回空串，由调用方回落到其它格式化
 */
export function dictLabel(field: FieldMeta, raw: unknown): string {
  const dict = field.dataSource;
  if (!dict || raw === null || raw === undefined || raw === '') return '';

  const label = dict[String(raw)];
  return typeof label === 'string' ? label : '';
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

/** 数值类型名集合（用于判断 ItemType=time/timespan 的载体是秒数还是日期） */
function isNumericTypeName(typeName?: string): boolean {
  const text = (typeName ?? '').trim().toLowerCase();
  return (
    text === 'int32' ||
    text === 'int64' ||
    text === 'int16' ||
    text === 'decimal' ||
    text === 'double' ||
    text === 'single' ||
    text === 'float' ||
    text === 'byte' ||
    text === 'sbyte'
  );
}

/**
 * .NET 日期格式串转文本（支持 yyyy/yy/MM/M/dd/d/HH/H/hh/h/mm/m/ss/s/fff/f/tt 与引号字面量）。
 *
 * 后端 ItemType 形如 `time-yyyy年MM月dd日`，只覆盖常用 token，未知字符原样输出。
 */
export function formatDotNetDate(date: Date, format: string): string {
  if (!format) return '';
  if (Number.isNaN(date.getTime())) return '';

  const pad = (value: number, width = 2) => String(value).padStart(width, '0');
  const hours = date.getHours();
  const tokens: Array<[string, string]> = [
    ['yyyy', String(date.getFullYear())],
    ['yy', String(date.getFullYear()).slice(-2)],
    ['MM', pad(date.getMonth() + 1)],
    ['M', String(date.getMonth() + 1)],
    ['dd', pad(date.getDate())],
    ['d', String(date.getDate())],
    ['HH', pad(hours)],
    ['H', String(hours)],
    ['hh', pad(hours % 12 || 12)],
    ['h', String(hours % 12 || 12)],
    ['mm', pad(date.getMinutes())],
    ['m', String(date.getMinutes())],
    ['ss', pad(date.getSeconds())],
    ['s', String(date.getSeconds())],
    ['fff', pad(date.getMilliseconds(), 3)],
    ['f', String(Math.floor(date.getMilliseconds() / 100))],
    ['tt', hours < 12 ? 'AM' : 'PM'],
  ];

  let result = '';
  let index = 0;
  while (index < format.length) {
    const ch = format[index];
    // 引号内为字面量，原样输出
    if (ch === "'" || ch === '"') {
      const end = format.indexOf(ch, index + 1);
      if (end < 0) {
        result += format.slice(index + 1);
        break;
      }
      result += format.slice(index + 1, end);
      index = end + 1;
      continue;
    }
    const hit = tokens.find(([token]) => format.startsWith(token, index));
    if (hit) {
      result += hit[1];
      index += hit[0].length;
      continue;
    }
    result += ch;
    index += 1;
  }
  return result;
}

/** .NET TimeSpan 默认格式：`[-][d.]hh:mm:ss[.fffffff]` */
function formatTimeSpanDefault(totalSeconds: number): string {
  const negative = totalSeconds < 0;
  const abs = Math.abs(totalSeconds);
  const days = Math.floor(abs / 86400);
  const hours = Math.floor((abs % 86400) / 3600);
  const minutes = Math.floor((abs % 3600) / 60);
  const seconds = Math.floor(abs % 60);
  const fraction = Math.round((abs - Math.floor(abs)) * 1000);
  const pad = (value: number) => String(value).padStart(2, '0');

  let text = days > 0 ? `${days}.${pad(hours)}:${pad(minutes)}:${pad(seconds)}` : `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
  if (fraction > 0) text += `.${String(fraction).padStart(3, '0').replace(/0+$/, '')}`;
  return negative ? `-${text}` : text;
}

/** .NET TimeSpan 自定义格式（支持 d/hh/h/mm/m/ss/s/f…） */
function formatTimeSpanCustom(totalSeconds: number, format: string): string {
  const negative = totalSeconds < 0;
  const abs = Math.abs(totalSeconds);
  const days = Math.floor(abs / 86400);
  const hours = Math.floor((abs % 86400) / 3600);
  const minutes = Math.floor((abs % 3600) / 60);
  const seconds = Math.floor(abs % 60);
  const milliseconds = Math.round((abs - Math.floor(abs)) * 1000);
  const pad = (value: number, width = 2) => String(value).padStart(width, '0');

  const tokens: Array<[string, string]> = [
    ['dd', pad(days)],
    ['d', String(days)],
    ['hh', pad(hours)],
    ['h', String(hours)],
    ['mm', pad(minutes)],
    ['m', String(minutes)],
    ['ss', pad(seconds)],
    ['s', String(seconds)],
    ['fff', pad(milliseconds, 3)],
    ['ff', pad(Math.floor(milliseconds / 10), 2)],
    ['f', String(Math.floor(milliseconds / 100))],
  ];

  let result = '';
  let index = 0;
  while (index < format.length) {
    const ch = format[index];
    if (ch === "'" || ch === '"') {
      const end = format.indexOf(ch, index + 1);
      if (end < 0) {
        result += format.slice(index + 1);
        break;
      }
      result += format.slice(index + 1, end);
      index = end + 1;
      continue;
    }
    if (ch === '\\' && index + 1 < format.length) {
      result += format[index + 1];
      index += 2;
      continue;
    }
    const hit = tokens.find(([token]) => format.startsWith(token, index));
    if (hit) {
      result += hit[1];
      index += hit[0].length;
      continue;
    }
    result += ch;
    index += 1;
  }
  return negative ? `-${result}` : result;
}

/**
 * 时长格式化（ItemType=`TimeSpan` / `TimeSpan:<格式>`）。对齐后端 `ViewHelper.FormatValue`：
 * 按字段描述里的单位（毫秒/秒/分/小时）换算，再套默认或自定义格式。
 */
export function formatTimeSpanCell(field: FieldMeta, raw: unknown): string {
  const value = Number(raw);
  if (!Number.isFinite(value)) return String(raw ?? '');

  const description = (field.description ?? '').toLowerCase();
  let totalSeconds = value;
  if (/毫秒|ms|millisecond/.test(description)) totalSeconds = value / 1000;
  else if (/小时|hour/.test(description)) totalSeconds = value * 3600;
  else if (/分|minute/.test(description)) totalSeconds = value * 60;

  const itemType = (field.itemType ?? '').trim();
  const colon = itemType.indexOf(':');
  if (colon > 0) {
    const format = itemType.slice(colon + 1).trim();
    if (format) return formatTimeSpanCustom(totalSeconds, format);
  }
  return formatTimeSpanDefault(totalSeconds);
}

/** 时间戳（Unix 秒）格式化（ItemType=`Time` / `Time:<格式>`），对齐后端 `ToDateTime().ToLocalTime()` */
export function formatTimeStampCell(field: FieldMeta, raw: unknown): string {
  const seconds = Number(raw);
  if (!Number.isFinite(seconds)) return String(raw ?? '');

  const date = new Date(seconds * 1000);
  if (Number.isNaN(date.getTime()) || date.getFullYear() <= 1) return '';

  const itemType = (field.itemType ?? '').trim();
  const colon = itemType.indexOf(':');
  if (colon > 0) {
    const format = itemType.slice(colon + 1).trim();
    if (format) return formatDotNetDate(date, format);
  }
  return formatDotNetDate(date, 'yyyy-MM-dd HH:mm:ss');
}

/** 日期时间格式（ItemType=`time` 或 `time-<格式>`），对齐 MVC `_List_Data_Item` */
export function formatTimeItemCell(field: FieldMeta, raw: unknown): string {
  const itemType = (field.itemType ?? '').trim();
  const text = String(raw ?? '');
  if (!text) return '';

  const date = new Date(text);
  if (Number.isNaN(date.getTime())) return text;

  const custom = /^time-(.+)$/i.exec(itemType);
  if (custom) return formatDotNetDate(date, custom[1]);
  return formatDotNetDate(date, 'yyyy-MM-dd HH:mm:ss');
}

/**
 * 容量格式化（ItemType=`GMK`）。对齐后端 `ToGMK`：1024 进制，不足 1K 取整数，其余保留两位小数。
 */
export function formatGmkCell(raw: unknown): string {
  const value = Math.trunc(Number(raw));
  if (!Number.isFinite(value)) return String(raw ?? '');
  if (value < 0) return `-${formatGmkCell(-value)}`;
  if (value < 1024) return value.toLocaleString('zh-CN');

  const units = ['K', 'M', 'G', 'T', 'P', 'E', 'Z', 'Y'];
  let size = value / 1024;
  for (const unit of units) {
    if (size < 1024 || unit === 'Y') {
      return `${size.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}${unit}`;
    }
    size /= 1024;
  }
  return String(value);
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
  // 枚举/字典列：后端在 dataSource 里下发了本地化后的「值 → 文案」，列表直接显示文案（对齐 MVC 枚举列口径）
  const label = dictLabel(column.field, raw);
  if (label !== '') return label;

  const itemType = column.field.itemType;
  if (isPercentItemType(itemType)) return formatPercentCell(column.field, raw);
  if (isMoneyItemType(itemType)) return formatMoneyCell(column.field, raw);
  if (parseDecimalItemType(itemType)) return formatDecimalCell(column.field, raw);

  // ItemType 精确格式化（MVC `_List_Data_Item` / `ViewHelper.FormatValue` 同口径）
  const normalized = (itemType ?? '').trim().toLowerCase();
  if (normalized === 'gmk') return formatGmkCell(raw);
  if (normalized.startsWith('timespan')) return formatTimeSpanCell(column.field, raw);
  // TimeSpan 列上的 time 表示时钟时间，继续走下方 control='time' 的归一
  const isSpanColumn = (column.field.typeName ?? '').trim().toLowerCase() === 'timespan';
  if (normalized.startsWith('time') && !isSpanColumn) {
    // 数值列上的 Time 是 Unix 秒；日期/字符串列才是时间格式
    return isNumericTypeName(column.field.typeName)
      ? formatTimeStampCell(column.field, raw)
      : formatTimeItemCell(column.field, raw);
  }

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
      return cellBoolean(raw) ? i18n.global.t('common.yes') : i18n.global.t('common.no');
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

/**
 * 链接模板是否至少有一个有效行值。
 *
 * 模板不含行占位符（纯跳转按钮，如「查看」）时始终可用；
 * 含 `{TraceId}` 等占位符但该行值全为空时返回 false，单元格回退为普通显示/空值占位。
 */
export function linkTemplateHasValue(
  template: string,
  row: Record<string, unknown>,
): boolean {
  if (!template) return false;
  const placeholders = [...template.matchAll(/\{(?!page:)([^{}:]+)\}/g)].map((match) => match[1]!.trim());
  if (!placeholders.length) return true;
  return placeholders.some((path) => {
    const value = readPathValue(row, path);
    return value !== null && value !== undefined && value !== '';
  });
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

/** 按钮图标 + 图标与文字间距 + 按钮左右内边距的估算合计 */
const ACTION_ICON_EXTRA = 34;

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
 * 估算单个行操作按钮宽度：中日韩字符按全角、其余按半角累加，再加图标与内边距。
 *
 * 文案随界面语言变化（查看 60px / Delete 79px / Löschen 88px），固定值会把按钮挤成「删除…」。
 * @param label 按钮文案
 * @returns 估算宽度（px），不低于固定单元宽
 */
export function estimateActionWidth(label: string): number {
  const text = label ?? '';
  const wide = (text.match(/[\u2e80-\u9fff\uff00-\uffef\u3000-\u303f]/g) ?? []).length;
  const narrow = text.length - wide;
  return Math.max(ACTION_BUTTON_WIDTH, Math.round(wide * 13 + narrow * 7.5 + ACTION_ICON_EXTRA));
}

/**
 * 按行动作数计算操作列布局：
 * ≤4 个全部直接显示、列宽随个数增长；>4 个只露前 3 个 + 「更多」下拉，
 * 列宽按 4 个单元封顶（后续新加的动作一律进「更多」，不再撑宽列）。
 *
 * @param count 单行动作数
 * @param labels 直接显示的按钮文案（按显示顺序，可含「更多」）；缺省按固定单元宽估算
 */
export function layoutRowActions(count: number, labels?: string[]): RowActionLayout {
  const unitCount = Math.max(Math.min(count, MAX_VISIBLE_ACTIONS), 1);
  const width =
    labels && labels.length >= unitCount
      ? ACTION_CELL_PADDING +
        labels.slice(0, unitCount).reduce((sum, label) => sum + estimateActionWidth(label), 0) +
        (unitCount - 1) * ACTION_BUTTON_GAP
      : ACTION_CELL_PADDING + unitCount * ACTION_BUTTON_WIDTH + (unitCount - 1) * ACTION_BUTTON_GAP;

  return { folded: count > MAX_VISIBLE_ACTIONS, width };
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
 * 把扁平列表按父字段组装成树（`setting.tree` 为真的列表用）。
 *
 * - 父字段按 `parentID` / `parentId` / `ParentID` 依次探测，取行上实际存在的那个；
 * - 找不到父节点的行按根节点处理（分页裁剪、父级被权限过滤时不会丢行）；
 * - 保持同层原顺序，子节点挂在 `children` 上；入参不被修改。
 * @param rows 接口返回的扁平行
 * @returns 树形行
 */
export function buildTreeRows(rows: Record<string, unknown>[]): Record<string, unknown>[] {
  if (!rows?.length) return [];

  const parentKeys = ['parentID', 'parentId', 'ParentID', 'ParentId'];
  const first = rows[0];
  const parentKey = parentKeys.find((key) => key in first);
  if (!parentKey) return rows;

  const idOf = (row: Record<string, unknown>) => {
    const id = row['id'] ?? row['ID'] ?? row['Id'];
    return id == null ? '' : String(id);
  };

  const nodes = new Map<string, Record<string, unknown>>();
  for (const row of rows) nodes.set(idOf(row), { ...row });

  const roots: Record<string, unknown>[] = [];
  for (const row of rows) {
    const node = nodes.get(idOf(row))!;
    const parentId = row[parentKey];
    const parent = parentId == null ? undefined : nodes.get(String(parentId));

    if (!parent || parent === node) {
      roots.push(node);
      continue;
    }

    const children = (parent['children'] as Record<string, unknown>[] | undefined) ?? [];
    children.push(node);
    parent['children'] = children;
  }

  return roots;
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
