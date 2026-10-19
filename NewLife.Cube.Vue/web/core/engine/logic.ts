/**
 * Cube 引擎纯函数集（不依赖 Vue / DOM / 网络，可被 Vitest 直接单测）
 *
 * 规范要求「任何数据转换、复杂计算、提交前拼装必须提取为 `*.logic.ts` 纯函数」
 * （见 `docs/standards/code-conventions.md` 关注点分离）。视图组件只调用本文件与引擎行为，
 * 不再自行拼参数、算文件名、判导入结果。
 */
import { getValueByKey, resolveKey } from '../utils/url';
import { isRequiredField } from '../utils/fieldControl';
import type { FieldMeta } from '../types/field';
import type { ImportResult } from './types';

/** 关键字查询参数名（后端 EntityController 的模糊查询入参） */
export const KEYWORD_PARAM = 'q';

/** 导出格式参数名（后端 `ExportFile` 取 `format`，大写亦兼容） */
export const EXPORT_FORMAT_PARAM = 'format';

/**
 * 丢弃空筛选条件：`undefined` / `null` / `''` 不参与请求。
 * 与 `core/views/index.vue:300` 的判定口径一致 —— `0` 与 `false` 必须保留，
 * 否则「数值=0」「布尔=否」这两类条件永远查不出来。
 */
export function dropEmptyParams(
  params: Record<string, unknown>,
): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue;
    out[key] = value;
  }
  return out;
}

/**
 * 列表/导出的查询条件（关键字 + 搜索模型，空值已剔除）。
 *
 * 不含分页参数：`pageIndex` / `pageSize` 由引擎发请求时现取当前页码补上，
 * 「查询条件」与「分页」因此只有一个拼装点。
 * 草案 §4.2 的写法把分页参数整个丢了，表现就是「点第 2 页请求的仍是第 1 页」。
 */
export function buildQueryParams(options: {
  keyword?: string;
  filters?: Record<string, unknown>;
  /** 是否把关键字并入 `q`（引擎 options.keyword） */
  keywordEnabled?: boolean;
}): Record<string, unknown> {
  const params: Record<string, unknown> = {};
  if (options.keywordEnabled !== false) {
    const kw = (options.keyword ?? '').trim();
    if (kw) params[KEYWORD_PARAM] = kw;
  }
  return { ...params, ...dropEmptyParams(options.filters ?? {}) };
}

/** 业务页绕开引擎 `refresh()` 自行发请求时，用它补分页参数（保持同一口径） */
export function withPagination(
  params: Record<string, unknown>,
  pageIndex: number,
  pageSize: number,
): Record<string, unknown> {
  return { pageIndex, pageSize, ...params };
}

/** 导出请求参数：格式 + 当前关键字与筛选（保证「导出当前筛选结果」语义） */
export function buildExportParams(options: {
  format: string;
  keyword?: string;
  filters?: Record<string, unknown>;
  keywordEnabled?: boolean;
}): Record<string, unknown> {
  const params: Record<string, unknown> = { [EXPORT_FORMAT_PARAM]: options.format };
  if (options.keywordEnabled !== false) {
    const kw = (options.keyword ?? '').trim();
    if (kw) params[KEYWORD_PARAM] = kw;
  }
  return { ...params, ...dropEmptyParams(options.filters ?? {}) };
}

/**
 * 依响应 `Content-Type` 推断导出文件扩展名。
 * 修正 `core/views/index.vue:387-392` 的缺口：该实现未识别 xlsx
 * （`spreadsheetml` / `officedocument`），导出 Excel 会落成 `.bin` 打不开。
 */
export function inferBlobExtension(contentType?: string): string {
  const type = (contentType ?? '').toLowerCase();
  if (type.includes('spreadsheetml') || type.includes('excel')) return '.xlsx';
  if (type.includes('csv')) return '.csv';
  if (type.includes('json')) return '.json';
  if (type.includes('xml')) return '.xml';
  if (type.includes('zip')) return '.zip';
  return '.xlsx';
}

/** 时间戳文件名片段（`yyyyMMddHHmmss`）；时钟可注入便于测试 */
export function dateStamp(now: Date = new Date()): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}${p(now.getMonth() + 1)}${p(now.getDate())}${p(
    now.getHours(),
  )}${p(now.getMinutes())}${p(now.getSeconds())}`;
}

/** 从实体前缀取业务名（`/api/Equipments/Equipment` → `Equipment`），用作文件名前缀 */
export function entityNameFromPrefix(prefix: string): string {
  const seg = prefix.split('/').filter(Boolean).pop();
  return seg || 'export';
}

/** 拼装下载文件名：`{前缀}_{时间戳}{扩展名}` */
export function buildDownloadName(prefix: string, stamp: string, ext: string): string {
  return `${prefix}_${stamp}${ext}`;
}

/**
 * 导入「0 行」判定。
 *
 * 后端 `ImportFile` 的 `message` 有两套文案口径：
 * - 外层汇总：`导入[xxx.xlsx] 共0行`
 * - 内层细分：`导入[xxx.csv]，共[10]行，成功[0]行，1行无效！`
 *
 * 0 行意味着数据没有落库（表头与字段名/显示名不匹配，或全部被合并策略跳过）。
 * 此时仍按绿色「成功」提示，就会出现「提示导入成功但列表没多一条」的误导，
 * 必须降级为警示。默认口径来自 Cube 后端 `ImportData` 的既有文案。
 */
export const ZERO_ROW_PATTERNS: ReadonlyArray<RegExp> = [
  /共\s*0\s*行/,
  /成功\s*\[\s*0\s*\]\s*行/,
];

/** 是否「0 行导入」；`patterns` 可注入以适配自定义导入 Action 的文案 */
export function isZeroRowImport(
  message?: string | null,
  patterns: ReadonlyArray<RegExp> = ZERO_ROW_PATTERNS,
): boolean {
  if (!message) return false;
  return patterns.some((p) => p.test(message));
}

/** 从 `ApiResponse` 形态的导入响应中解析结果（`code===0` 视为成功） */
export function parseImportResult(res: unknown): ImportResult {
  const body = (res ?? {}) as { code?: number; message?: string; data?: unknown };
  const message = typeof body.message === 'string' ? body.message : undefined;
  const rows = message ? extractImportRows(message) : null;
  return {
    ok: body.code === undefined || body.code === 0,
    message,
    rows,
    raw: res,
  };
}

/** 解析文案里的落库行数：优先「成功[N]行」，其次「共N行」；解析不出返回 null */
export function extractImportRows(message: string): number | null {
  const succeeded = /成功\s*\[\s*(\d+)\s*\]\s*行/.exec(message);
  if (succeeded) return Number(succeeded[1]);
  const total = /共\s*(\d+)\s*行/.exec(message);
  if (total) return Number(total[1]);
  return null;
}

/** 列显隐项 */
export interface ColumnVisibilityItem {
  /** 列标识（与字段名对应） */
  key: string;
  /** 列显示名 */
  label: string;
}

/** 默认全显示 */
export function defaultColumnVisibility(
  columns: ColumnVisibilityItem[],
): Record<string, boolean> {
  const out: Record<string, boolean> = {};
  for (const c of columns) out[c.key] = true;
  return out;
}

/**
 * 合并已存配置与当前列定义：
 * 只接受布尔值，未知 key 丢弃，新增列默认显示（避免升级后某列永久消失）。
 */
export function mergeColumnVisibility(
  columns: ColumnVisibilityItem[],
  saved?: Record<string, unknown> | null,
): Record<string, boolean> {
  const out = defaultColumnVisibility(columns);
  if (!saved || typeof saved !== 'object') return out;
  for (const c of columns) {
    const value = saved[c.key];
    if (typeof value === 'boolean') out[c.key] = value;
  }
  return out;
}

/** 解析已存列显隐 JSON（坏数据一律退回默认，不抛错） */
export function parseColumnVisibility(
  raw: string | null,
): Record<string, boolean> | null {
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return null;
    return parsed as Record<string, boolean>;
  } catch {
    return null;
  }
}

/**
 * 取行主键：显式 `idKey` 优先（来自元数据 `primaryKey`），
 * 依次容错 `id` / `ID` / `Id`（后端 JSON 为 camelCase，元数据为 PascalCase）。
 */
export function resolveRowId(
  row: Record<string, unknown>,
  idKey?: string,
): number | string | undefined {
  const candidates = [idKey, 'id', 'ID', 'Id'].filter(
    (k): k is string => typeof k === 'string' && k.length > 0,
  );
  for (const key of candidates) {
    const value = getValueByKey(row, key);
    if (value !== undefined && value !== null && value !== '') return value as number | string;
  }
  return undefined;
}

/** 主键字段名（取元数据里 `primaryKey` 的那个，缺省 `id`） */
export function primaryKeyFromFields(
  fields: Array<{ primaryKey?: boolean; name: string }>,
): string {
  return fields.find((f) => f.primaryKey)?.name ?? 'id';
}

/**
 * 找出第一个未填的必填字段。
 *
 * 必填口径唯一来源是 `core/utils/fieldControl.ts` 的 `isRequiredField()`
 * （由 nullable / primaryKey / 控件类型推导），这里不新增 `FieldMeta.required` 同义字段。
 * 取值用 `resolveKey` 做大小写容错：详情接口可能返回 `Id` 而模型键是 `id`。
 *
 * 说明：`openListFormDialog.ts:86-98` 有一份同语义的私有实现，属 P6 收敛项
 * （命令式弹窗届时由 `CubeTableFormDialog` 取代），当前保持两处并存以免改动既有页面。
 */
export function findMissingRequiredField(
  model: Record<string, unknown>,
  fields: FieldMeta[],
): FieldMeta | null {
  for (const field of fields) {
    if (!isRequiredField(field)) continue;
    const value = model[resolveKey(model, field.name)];
    if (value === undefined || value === null || value === '') return field;
    if (Array.isArray(value) && value.length === 0) return field;
  }
  return null;
}

/**
 * 清洗「列表行 → 可回传载荷」。
 *
 * Cube 列表回包里布尔字段会同时带 camelCase 与 PascalCase 两个键
 * （如 `enable:true` 与 `Enable:1`）。整行铺开后覆盖目标字段再 PUT 时，
 * 反序列化按出现顺序取到 PascalCase 旧值，实体与库里一致 → XCode 判定「无变化」→
 * **更新被静默忽略**（接口仍返回成功文案）。故 PUT 前必须剔除重复的 PascalCase 别名键。
 */
export function cleanRowForUpdate(row: Record<string, unknown>): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(row)) {
    const camel = key.charAt(0).toLowerCase() + key.slice(1);
    // 同名小写键存在时丢弃 PascalCase 别名键（它只是回包冗余副本，回传会反向覆盖目标值）
    if (camel !== key && camel in row) continue;
    out[key] = value;
  }
  return out;
}

/**
 * 单元格文本格式化（列表渲染用，纯函数）。
 *
 * `ListTableContent.vue` 对 `date` / `time` 两类没有分支，直接透出后端原始串
 * （`2026-10-19T08:30:00` 这类 ISO 值很难读）。引擎侧统一按
 * 日期 `YYYY-MM-DD`、日期时间 `YYYY-MM-DD HH:mm:ss`、时间 `HH:mm[:ss]` 归一。
 */
export function formatCellDateTime(value: unknown, mode: 'date' | 'datetime' | 'time'): string {
  const raw = typeof value === 'string' ? value.trim() : '';
  if (!raw) return '';
  // 已是 `YYYY-MM-DD[ T]HH:mm` 形态时直接切片，避免引入日期库依赖
  const match = /^(\d{4}-\d{2}-\d{2})[T ](\d{2}:\d{2})(:\d{2})?/.exec(raw);
  if (mode === 'date') {
    if (match) return match[1];
    return raw.slice(0, 10);
  }
  if (mode === 'time') {
    if (match) return match[3] ? match[2] : `${match[2]}:00`;
    const onlyTime = /^(\d{2}:\d{2})(:\d{2})?$/.exec(raw);
    if (onlyTime) return onlyTime[2] ? onlyTime[1] : `${onlyTime[1]}:00`;
    return raw;
  }
  if (match) return `${match[1]} ${match[2]}${match[3] ?? ''}`;
  return raw.replace('T', ' ');
}

/** 是否「空值」（列表单元格显示 `—` 的判定，0 与 false 不算空） */
export function isBlankCell(value: unknown): boolean {
  if (value === undefined || value === null) return true;
  if (typeof value === 'string') return value.trim() === '';
  return false;
}
