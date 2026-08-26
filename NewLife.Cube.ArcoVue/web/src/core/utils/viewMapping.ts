/**
 * 多视图字段候选、mapping normalize、创建门禁、看板分桶、pageSize 策略（OSC-0006）
 */
import type { FieldMeta } from '@/core/types/field';
import { isBadgeField } from '@/core/utils/fieldBadge';
import { resolveListControl } from '@/core/utils/fieldControl';
import { preferTreeByType } from '@/core/utils/tree';
import { getValueByKey } from '@/core/utils/url';

export type ViewKind = 'table' | 'tree' | 'card' | 'kanban' | 'calendar' | 'gantt';

/** 卡片布局：标准 / 偏大 / 整行（OSC-0007） */
export type CardLayout = 'standard' | 'large' | 'row';
/** 卡片正文字段栅格列数 */
export type CardBodyColumns = 1 | 2 | 3;
/** 字段标签/内容排版：竖向上下，横向同行 */
export type CardFieldOrientation = 'vertical' | 'horizontal';

export type CardMapping = {
  kind: 'card';
  titleField: string;
  imageField?: string;
  layout: CardLayout;
  /** 正文排版列数；标准/偏大时 3 会回退为 2 */
  bodyColumns: CardBodyColumns;
  fieldOrientation: CardFieldOrientation;
};
export type KanbanMapping = {
  kind: 'kanban';
  groupField: string;
  titleField: string;
  imageField?: string;
};
export type GanttMapping = {
  kind: 'gantt';
  /** 标题字段 */
  titleField: string;
  /** 计划开始字段（基线，必填） */
  plannedStartField: string;
  /** 计划结束字段（基线，必填） */
  plannedEndField: string;
  /** 实际开始字段（主条，可选；与 actualEndField 成对生效） */
  actualStartField?: string;
  /** 实际结束字段（主条，可选；与 actualStartField 成对生效） */
  actualEndField?: string;
  /** 固定任务条颜色（hex），缺省主题主色 */
  barColor?: string;
  /** 左侧表格宽度（拖拽持久化），缺省 380 */
  tableWidth?: number;
};
export type CalendarMapping = {
  kind: 'calendar';
  startField: string;
  endField?: string;
  titleField: string;
  colorField?: string;
};
export type ViewMapping = CardMapping | KanbanMapping | GanttMapping | CalendarMapping;
export type DataSourceOption = { value: string; label: string };

/** 各视图分页器可选条数（与 a-pagination page-size-options 一致） */
export const PAGE_SIZE_OPTIONS = [20, 50, 100, 200, 500, 1000] as const;

export const LARGE_VIEW_PAGE_SIZE_DEFAULT = 200;
/** 与 PAGE_SIZE_OPTIONS 上限对齐，便于工作台 pageSize 在大视图间复用 */
export const LARGE_VIEW_PAGE_SIZE_MAX = 1000;

/** 归一化页面条数（OSC-0012）：仅接受 PAGE_SIZE_OPTIONS 合法值；非法/负数/非选项值归一为 0（未配置） */
export function normalizePageSize(raw: unknown): number {
  const n = typeof raw === 'number' ? raw : Number(raw);
  if (!Number.isInteger(n) || n <= 0) return 0;
  return (PAGE_SIZE_OPTIONS as readonly number[]).includes(n) ? n : 0;
}

export function isLargePageViewKind(kind: ViewKind): boolean {
  return kind === 'kanban' || kind === 'calendar' || kind === 'gantt';
}

/** 是否表格类视图：分组/排序/批量删除等工具仅在 table/tree 可用（OSC-0007） */
export function isTableLikeViewKind(kind: ViewKind): boolean {
  return kind === 'table' || kind === 'tree';
}

/** 仅接受合法卡片布局；缺失或非法一律回落标准 */
export function normalizeCardLayout(raw: unknown): CardLayout {
  return raw === 'large' || raw === 'row' ? raw : 'standard';
}

/** 正文列数；非法回落 2；标准/偏大不允许 3 */
export function normalizeCardBodyColumns(raw: unknown, layout: CardLayout): CardBodyColumns {
  const n = raw === 1 || raw === '1' ? 1 : raw === 3 || raw === '3' ? 3 : raw === 2 || raw === '2' ? 2 : 2;
  if (n === 3 && layout !== 'row') return 2;
  return n;
}

export function normalizeCardFieldOrientation(raw: unknown): CardFieldOrientation {
  return raw === 'horizontal' ? 'horizontal' : 'vertical';
}

export type BatchDeleteState = { visible: boolean; disabled: boolean };
export type BatchEnableState = { visible: boolean; disabled: boolean };

/**
 * 批量删除菜单门禁：仅表格视图 + 最终删除权限 + 视图允许删除 + 至少选中一行。
 * visible=false 时 disabled 恒为 true，防止调用方遗漏二次保护。
 */
export function resolveBatchDeleteState(args: {
  viewKind: ViewKind;
  canDelete: boolean;
  allowDelete: boolean;
  selectedCount: number;
}): BatchDeleteState {
  if (args.viewKind !== 'table' || !args.canDelete || !args.allowDelete) {
    return { visible: false, disabled: true };
  }
  return { visible: true, disabled: args.selectedCount <= 0 };
}

/** 一次最多启用/禁用条数（GET keys 查询串） */
export const BATCH_ENABLE_MAX = 200;

/**
 * 批量启停菜单门禁：仅 table + Update + EnableSelect≠false + 有 Enable 列。
 * 0 选中：可见但 disabled；>200：可见但 disabled（点击也拦截）。
 */
export function resolveBatchEnableState(args: {
  viewKind: ViewKind;
  canEdit: boolean;
  enableSelect: boolean | undefined;
  hasEnableField: boolean;
  selectedCount: number;
}): BatchEnableState {
  const enableOn = args.enableSelect !== false;
  if (args.viewKind !== 'table' || !args.canEdit || !enableOn || !args.hasEnableField) {
    return { visible: false, disabled: true };
  }
  if (args.selectedCount <= 0 || args.selectedCount > BATCH_ENABLE_MAX) {
    return { visible: true, disabled: true };
  }
  return { visible: true, disabled: false };
}

export function resolveViewPageSize(
  kind: ViewKind,
  pagerSize?: number,
  preferredLarge?: number,
): number {
  if (!isLargePageViewKind(kind)) {
    return Math.max(1, pagerSize && pagerSize > 0 ? pagerSize : 20);
  }
  const n = preferredLarge && preferredLarge > 0 ? preferredLarge : LARGE_VIEW_PAGE_SIZE_DEFAULT;
  return Math.min(LARGE_VIEW_PAGE_SIZE_MAX, Math.max(LARGE_VIEW_PAGE_SIZE_DEFAULT, n));
}

export function titleFieldCandidates(fields: FieldMeta[]): FieldMeta[] {
  return fields.filter((f) => {
    if (!f.name || f.primaryKey || f.typeName === 'Guid') return false;
    if (f.typeName === 'String') return true;
    return resolveListControl(f) === 'text';
  });
}

export function imageFieldCandidates(fields: FieldMeta[]): FieldMeta[] {
  return fields.filter((f) => (f.itemType || '').toLowerCase() === 'image');
}

export function groupFieldCandidates(fields: FieldMeta[]): FieldMeta[] {
  return fields.filter((f) => {
    if (!f.name || f.primaryKey) return false;
    if (f.typeName === 'Boolean') return true;
    if (f.typeName === 'Enum') return true;
    const c = resolveListControl(f);
    return c === 'boolean' || c === 'select' || c === 'lov' || isBadgeField(f);
  });
}

export function dateFieldCandidates(fields: FieldMeta[]): FieldMeta[] {
  return fields.filter((f) => f.typeName === 'DateTime' || resolveListControl(f) === 'date');
}

export function colorFieldCandidates(fields: FieldMeta[]): FieldMeta[] {
  return fields.filter((f) => isBadgeField(f));
}

function isNumericDataSourceKey(key: string): boolean {
  return /^-?\d+$/.test(key);
}

/** PrepareForApi 可能同时返回数字键与名称键；按 label 去重并优先数字键，同时保留别名映射供视图归并 */
export function normalizeDataSource(
  ds: Record<string, string>,
): { options: DataSourceOption[]; canonicalByKey: Map<string, string> } {
  const canonicalByLabel = new Map<string, string>();
  for (const [key, label] of Object.entries(ds)) {
    const prev = canonicalByLabel.get(label);
    if (prev == null || (isNumericDataSourceKey(key) && !isNumericDataSourceKey(prev))) {
      canonicalByLabel.set(label, key);
    }
  }

  const canonicalByKey = new Map<string, string>();
  for (const [key, label] of Object.entries(ds)) {
    canonicalByKey.set(key, canonicalByLabel.get(label) || key);
  }

  return {
    options: [...canonicalByLabel.entries()].map(([label, value]) => ({ value, label })),
    canonicalByKey,
  };
}

export function hasTreeMetadata(
  fields: FieldMeta[],
  typePath: string,
  rowsHadChildren?: boolean,
): boolean {
  if (preferTreeByType(typePath)) return true;
  if (rowsHadChildren) return true;
  return fields.some((f) => {
    const n = (f.name || '').toLowerCase();
    return (
      n === 'parent' ||
      n === 'parentid' ||
      n === 'parent_id' ||
      n === 'children' ||
      n.endsWith('parentid')
    );
  });
}

export function canCreateViewKind(
  kind: ViewKind,
  fields: FieldMeta[],
  typePath: string,
  opts?: { rowsHadChildren?: boolean },
): { ok: boolean; reason?: string } {
  switch (kind) {
    case 'table':
      return { ok: true };
    case 'tree':
      if (hasTreeMetadata(fields, typePath, opts?.rowsHadChildren)) return { ok: true };
      return { ok: false, reason: '当前实体无 Parent/children 等树元数据，无法创建树视图' };
    case 'card':
      if (titleFieldCandidates(fields).length) return { ok: true };
      return { ok: false, reason: '缺少可用的标题字段' };
    case 'kanban':
      if (!groupFieldCandidates(fields).length)
        return { ok: false, reason: '缺少可用的分组字段（枚举/布尔/选项）' };
      if (!titleFieldCandidates(fields).length) return { ok: false, reason: '缺少可用的标题字段' };
      return { ok: true };
    case 'calendar':
      if (dateFieldCandidates(fields).length) return { ok: true };
      return { ok: false, reason: '缺少 DateTime 字段作为开始日期' };
    case 'gantt':
      if (dateFieldCandidates(fields).length >= 2) return { ok: true };
      return { ok: false, reason: '甘特需要至少两个 DateTime 字段（开始/结束）' };
    default:
      return { ok: false, reason: '未知视图类型' };
  }
}

function pickFirst(names: string[], allowed: Set<string>): string | undefined {
  return names.find((n) => allowed.has(n));
}

export function seedMapping(kind: ViewKind, fields: FieldMeta[]): ViewMapping | undefined {
  const titles = titleFieldCandidates(fields).map((f) => f.name);
  const images = imageFieldCandidates(fields).map((f) => f.name);
  const groups = groupFieldCandidates(fields).map((f) => f.name);
  const dates = dateFieldCandidates(fields).map((f) => f.name);
  const colors = colorFieldCandidates(fields).map((f) => f.name);
  const title = titles[0] || fields.find((f) => f.name && !f.primaryKey)?.name || fields[0]?.name || '';

  if (kind === 'card') {
    if (!title) return undefined;
    return {
      kind: 'card',
      titleField: title,
      imageField: images[0],
      layout: 'standard',
      bodyColumns: 2,
      fieldOrientation: 'vertical',
    };
  }
  if (kind === 'kanban') {
    if (!groups[0] || !title) return undefined;
    return {
      kind: 'kanban',
      groupField: groups[0],
      titleField: title,
      imageField: images[0],
    };
  }
  if (kind === 'calendar') {
    if (!dates[0] || !title) return undefined;
    return {
      kind: 'calendar',
      startField: dates[0],
      endField: dates[1],
      titleField: title,
      colorField: colors[0],
    };
  }
  if (kind === 'gantt') {
    if (!dates[0] || !dates[1] || !title) return undefined;
    return {
      kind: 'gantt',
      titleField: title,
      plannedStartField: dates[0],
      plannedEndField: dates[1],
    };
  }
  return undefined;
}

/** 与 view 对齐；非法字段丢弃/回落候选首项 */
export function normalizeMapping(
  view: ViewKind,
  raw: unknown,
  fields: FieldMeta[],
): ViewMapping | undefined {
  if (view === 'table' || view === 'tree') return undefined;
  const names = new Set(fields.map((f) => f.name).filter(Boolean));
  const seeded = seedMapping(view, fields);
  if (!raw || typeof raw !== 'object') return seeded;

  const o = raw as Record<string, unknown>;
  const kind = (typeof o.kind === 'string' ? o.kind : view) as ViewKind;

  if (view === 'card' || kind === 'card') {
    const titleField =
      pickFirst([String(o.titleField || ''), seeded && 'titleField' in seeded ? seeded.titleField : ''], names) ||
      titleFieldCandidates(fields)[0]?.name;
    if (!titleField) return undefined;
    const imageRaw = typeof o.imageField === 'string' ? o.imageField : '';
    const layout = normalizeCardLayout(o.layout);
    return {
      kind: 'card',
      titleField,
      imageField: imageRaw && names.has(imageRaw) ? imageRaw : imageFieldCandidates(fields)[0]?.name,
      layout,
      bodyColumns: normalizeCardBodyColumns(o.bodyColumns, layout),
      fieldOrientation: normalizeCardFieldOrientation(o.fieldOrientation),
    };
  }

  if (view === 'kanban' || kind === 'kanban') {
    const groupField =
      pickFirst([String(o.groupField || '')], names) || groupFieldCandidates(fields)[0]?.name;
    const titleField =
      pickFirst([String(o.titleField || '')], names) || titleFieldCandidates(fields)[0]?.name;
    if (!groupField || !titleField) return seedMapping('kanban', fields);
    const imageRaw = typeof o.imageField === 'string' ? o.imageField : '';
    return {
      kind: 'kanban',
      groupField,
      titleField,
      imageField: imageRaw && names.has(imageRaw) ? imageRaw : imageFieldCandidates(fields)[0]?.name,
    };
  }

  if (view === 'calendar' || kind === 'calendar') {
    const startField =
      pickFirst([String(o.startField || '')], names) || dateFieldCandidates(fields)[0]?.name;
    const titleField =
      pickFirst([String(o.titleField || '')], names) || titleFieldCandidates(fields)[0]?.name;
    if (!startField || !titleField) return seedMapping('calendar', fields);
    const endRaw = typeof o.endField === 'string' ? o.endField : '';
    const colorRaw = typeof o.colorField === 'string' ? o.colorField : '';
    return {
      kind: 'calendar',
      startField,
      endField: endRaw && names.has(endRaw) ? endRaw : dateFieldCandidates(fields).find((f) => f.name !== startField)?.name,
      titleField,
      colorField: colorRaw && names.has(colorRaw) ? colorRaw : colorFieldCandidates(fields)[0]?.name,
    };
  }

  if (view === 'gantt' || kind === 'gantt') {
    const dates = dateFieldCandidates(fields);
    // 旧数据 startField/endField 迁移为计划字段（OSC-0019）
    const plannedStart =
      pickFirst([String(o.plannedStartField || ''), String(o.startField || '')], names) ||
      dates[0]?.name;
    const plannedEnd =
      pickFirst([String(o.plannedEndField || ''), String(o.endField || '')], names) ||
      dates.find((f) => f.name !== plannedStart)?.name ||
      dates[1]?.name;
    const titleField =
      pickFirst([String(o.titleField || '')], names) || titleFieldCandidates(fields)[0]?.name;
    if (!plannedStart || !plannedEnd || !titleField) return seedMapping('gantt', fields);

    // 实际字段成对校验：仅配置一个视为未配置实际（OSC-0019）
    const actualStartRaw = typeof o.actualStartField === 'string' ? o.actualStartField : '';
    const actualEndRaw = typeof o.actualEndField === 'string' ? o.actualEndField : '';
    const actualStart = actualStartRaw && names.has(actualStartRaw) ? actualStartRaw : '';
    const actualEnd = actualEndRaw && names.has(actualEndRaw) ? actualEndRaw : '';
    const hasActual = !!(actualStart && actualEnd);

    // barColor：合法 hex 才保留；旧 colorField（按字段着色）直接忽略（行为变更 OSC-0019）
    const barColorRaw = typeof o.barColor === 'string' ? o.barColor : '';
    const barColor = /^#[0-9a-fA-F]{6}$/.test(barColorRaw) ? barColorRaw : undefined;

    // tableWidth：合法正整数 280~640 夹取，否则缺省
    let tableWidth: number | undefined;
    const tw = Number(o.tableWidth);
    if (Number.isFinite(tw) && tw > 0) {
      tableWidth = Math.min(640, Math.max(280, Math.round(tw)));
    }

    return {
      kind: 'gantt',
      titleField,
      plannedStartField: plannedStart,
      plannedEndField: plannedEnd,
      actualStartField: hasActual ? actualStart : undefined,
      actualEndField: hasActual ? actualEnd : undefined,
      barColor,
      tableWidth,
    };
  }

  return seeded;
}

export type KanbanBucket = {
  key: string;
  label: string;
  rows: Record<string, unknown>[];
};

/** dataSource 序优先，未命中进「未分组」 */
export function bucketKanban(
  rows: Record<string, unknown>[],
  groupField: string,
  dataSource?: Record<string, string> | null,
): KanbanBucket[] {
  const order: string[] = [];
  const labelOf = new Map<string, string>();
  let canonicalByKey = new Map<string, string>();
  if (dataSource) {
    const normalized = normalizeDataSource(dataSource);
    canonicalByKey = normalized.canonicalByKey;
    for (const opt of normalized.options) {
      order.push(opt.value);
      labelOf.set(opt.value, opt.label);
    }
  }
  const buckets = new Map<string, Record<string, unknown>[]>();
  const ungrouped: Record<string, unknown>[] = [];

  for (const row of rows) {
    // 字段名大小写容错取值（FieldMeta.name 为 PascalCase，数据行 key 为 camelCase）
    const raw = getValueByKey(row, groupField);
    if (raw == null || raw === '') {
      ungrouped.push(row);
      continue;
    }
    const key = canonicalByKey.get(String(raw)) || String(raw);
    if (!buckets.has(key)) {
      buckets.set(key, []);
      if (!order.includes(key)) order.push(key);
      if (!labelOf.has(key)) labelOf.set(key, key);
    }
    buckets.get(key)!.push(row);
  }

  const result: KanbanBucket[] = [];
  for (const key of order) {
    const list = buckets.get(key);
    if (!list?.length) {
      // 仍展示空列（有 dataSource 定义时）
      if (dataSource && labelOf.has(key)) {
        result.push({ key, label: labelOf.get(key) || key, rows: [] });
      }
      continue;
    }
    result.push({ key, label: labelOf.get(key) || key, rows: list });
  }
  // 行中出现但不在 order 的（上面已 push 进 order）
  for (const [key, list] of buckets) {
    if (!result.some((b) => b.key === key)) {
      result.push({ key, label: labelOf.get(key) || key, rows: list });
    }
  }
  if (ungrouped.length) {
    result.push({ key: '__ungrouped__', label: '未分组', rows: ungrouped });
  }
  return result;
}

/**
 * 多级分组节点（OSC-0015）。组头节点 `__group=true` 且带 `__groupHeader`（ListTable 识别）/label/count/path/children；
 * 数据行节点 `__group` 缺失且为原记录对象。
 */
export interface GroupNode {
  /** 是否为组头节点 */
  __group: boolean;
  /** 组头标记（ListTable 识别组头行；含 label 与多级折叠 path） */
  __groupHeader?: { label: string; path: string };
  /** 分组字段名（组头） */
  groupField?: string;
  /** 组值显示标签（组头，已按 dataSource 翻译；空值→未分组） */
  label?: string;
  /** 组内行数（组头，含子孙分组） */
  count?: number;
  /** 组原始值（组头） */
  value?: unknown;
  /** 分组路径（组头，多级折叠 key：`v1::v2`） */
  path?: string;
  /** 子节点：下级组头或数据行（组头） */
  children?: GroupNode[];
  [key: string]: unknown;
}

/** 按字段逐级分组的递归实现（OSC-0015）；空 groupFields 直接返回原行 */
function groupLevel(
  rows: Record<string, unknown>[],
  groupFields: string[],
  level: number,
  meta: Map<string, FieldMeta>,
  parentPath: string,
): GroupNode[] {
  if (level >= groupFields.length) return rows as GroupNode[];

  const fieldName = groupFields[level];
  const fm = meta.get(fieldName);
  const groups = new Map<string, GroupNode>();

  for (const row of rows) {
    const raw = getValueByKey(row, fieldName);
    const value = raw == null ? '' : String(raw);
    const label = value === '' ? '未分组' : (fm?.dataSource?.[value] ?? value);
    let node = groups.get(value);
    if (!node) {
      node = {
        __group: true,
        __groupHeader: { label, path: parentPath ? `${parentPath}::${value}` : value },
        groupField: fieldName,
        label,
        value,
        path: parentPath ? `${parentPath}::${value}` : value,
        count: 0,
        children: [],
      };
      groups.set(value, node);
    }
    node.children!.push(row as GroupNode);
    node.count! += 1;
  }

  const out: GroupNode[] = [];
  for (const node of groups.values()) {
    // 多级：对组内行再按下一字段分组；count 重算为子节点总和
    if (level + 1 < groupFields.length) {
      node.children = groupLevel(node.children!, groupFields, level + 1, meta, node.path!);
      node.count = (node.children as GroupNode[]).reduce(
        (acc, c) => acc + (c.__group ? (c.count ?? 0) : 1),
        0,
      );
    }
    out.push(node);
  }
  return out;
}

/**
 * 多字段多级分组（OSC-0015）。对已加载数据按 groupFields 逐级分组为树结构，
 * 组头含 label（dataSource 翻译）、count（含子孙）与 path（折叠 key）。
 * 空 groupFields / 空数据 / 未知分组字段：安全回退（未知字段按空值进「未分组」）。
 */
export function groupRows(
  records: Record<string, unknown>[],
  groupFields: string[],
  fields: FieldMeta[],
): GroupNode[] {
  if (!records.length || !groupFields.length) return records as GroupNode[];
  const meta = new Map(fields.map((f) => [f.name, f]));
  return groupLevel(records, groupFields, 0, meta, '');
}

/** 是否为多级分组组头节点（OSC-0015；ListTable 识别组头行） */
export function isGroupHeaderRow(row: Record<string, unknown>): boolean {
  return (row as GroupNode).__group === true;
}

/** 组头行首列显示文案：`label (count)`；非组头行返回 null（OSC-0015） */
export function groupHeaderCell(row: Record<string, unknown>): string | null {
  const node = row as GroupNode;
  if (node.__group !== true) return null;
  return `${node.__groupHeader?.label ?? node.label ?? ''} (${node.count ?? 0})`;
}

/** 分组字段上限（OSC-0015：≤3 字段） */
export const GROUP_FIELDS_LIMIT = 3;

/** 可继续添加的分组字段候选：未选、且未达上限（OSC-0015） */
export function nextGroupFieldNames(
  allFields: string[],
  selected: readonly string[],
  limit = GROUP_FIELDS_LIMIT,
): string[] {
  if (selected.length >= limit) return [];
  const chosen = new Set(selected);
  return allFields.filter((f) => !chosen.has(f));
}

/** 添加分组字段：去重 + 上限保护；非法返回原数组（OSC-0015） */
export function pushGroupField(
  group: readonly string[],
  field: string,
  limit = GROUP_FIELDS_LIMIT,
): string[] {
  if (!field || group.includes(field) || group.length >= limit) return [...group];
  return [...group, field];
}

/** 上移（dir=-1）/下移（dir=1）；越界或非法返回原数组（OSC-0015） */
export function moveGroupField(
  group: readonly string[],
  index: number,
  dir: -1 | 1,
): string[] {
  const target = index + dir;
  if (index < 0 || index >= group.length || target < 0 || target >= group.length) {
    return [...group];
  }
  const next = [...group];
  [next[index], next[target]] = [next[target], next[index]];
  return next;
}

/** 删除指定下标分组字段（OSC-0015） */
export function removeGroupField(group: readonly string[], index: number): string[] {
  return group.filter((_, i) => i !== index);
}

export const VIEW_KIND_LABEL: Record<ViewKind, string> = {
  table: '表格',
  tree: '树状',
  card: '卡片',
  kanban: '看板',
  calendar: '日历',
  gantt: '甘特图',
};

/** 视图类型 → 默认视图名称（「保存视图为默认XX视图」文案；需求：列表/看板/卡片/树状/日历/甘特） */
export const DEFAULT_VIEW_KIND_NAME: Record<ViewKind, string> = {
  table: '列表',
  tree: '树状',
  card: '卡片',
  kanban: '看板',
  calendar: '日历',
  gantt: '甘特',
};

/** 「+」新建视图菜单文案（类型名 +「视图」） */
export function viewKindCreateLabel(kind: ViewKind): string {
  return `${VIEW_KIND_LABEL[kind]}视图`;
}

/** 「保存视图为默认XX视图」文案；未知类型回落「列表」 */
export function defaultViewKindName(kind: ViewKind): string {
  return DEFAULT_VIEW_KIND_NAME[kind] || '列表';
}

export function parseViewKind(raw: unknown): ViewKind {
  if (
    raw === 'tree' ||
    raw === 'card' ||
    raw === 'kanban' ||
    raw === 'calendar' ||
    raw === 'gantt'
  ) {
    return raw;
  }
  return 'table';
}
