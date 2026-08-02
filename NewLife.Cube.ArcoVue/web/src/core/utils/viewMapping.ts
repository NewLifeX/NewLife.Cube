/**
 * 多视图字段候选、mapping normalize、创建门禁、看板分桶、pageSize 策略（OSC-0006）
 */
import type { FieldMeta } from '@/core/types/field';
import { isBadgeField } from '@/core/utils/fieldBadge';
import { resolveListControl } from '@/core/utils/fieldControl';
import { preferTreeByType } from '@/core/utils/tree';
import { getValueByKey } from '@/core/utils/url';

export type ViewKind = 'table' | 'tree' | 'card' | 'kanban' | 'calendar' | 'gantt';

export type CardMapping = { kind: 'card'; titleField: string; imageField?: string };
export type KanbanMapping = {
  kind: 'kanban';
  groupField: string;
  titleField: string;
  imageField?: string;
};
export type GanttMapping = {
  kind: 'gantt';
  startField: string;
  endField: string;
  titleField: string;
  colorField?: string;
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

export const LARGE_VIEW_PAGE_SIZE_DEFAULT = 200;
export const LARGE_VIEW_PAGE_SIZE_MAX = 500;

export function isLargePageViewKind(kind: ViewKind): boolean {
  return kind === 'kanban' || kind === 'calendar' || kind === 'gantt';
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
    return { kind: 'card', titleField: title, imageField: images[0] };
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
      startField: dates[0],
      endField: dates[1],
      titleField: title,
      colorField: colors[0],
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
    return {
      kind: 'card',
      titleField,
      imageField: imageRaw && names.has(imageRaw) ? imageRaw : imageFieldCandidates(fields)[0]?.name,
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
    const startField = pickFirst([String(o.startField || '')], names) || dates[0]?.name;
    const endField =
      pickFirst([String(o.endField || '')], names) ||
      dates.find((f) => f.name !== startField)?.name ||
      dates[1]?.name;
    const titleField =
      pickFirst([String(o.titleField || '')], names) || titleFieldCandidates(fields)[0]?.name;
    if (!startField || !endField || !titleField) return seedMapping('gantt', fields);
    const colorRaw = typeof o.colorField === 'string' ? o.colorField : '';
    return {
      kind: 'gantt',
      startField,
      endField,
      titleField,
      colorField: colorRaw && names.has(colorRaw) ? colorRaw : colorFieldCandidates(fields)[0]?.name,
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

export const VIEW_KIND_LABEL: Record<ViewKind, string> = {
  table: '表格',
  tree: '树状',
  card: '卡片',
  kanban: '看板',
  calendar: '日历',
  gantt: '甘特图',
};

/** 「+」新建视图菜单文案（类型名 +「视图」） */
export function viewKindCreateLabel(kind: ViewKind): string {
  return `${VIEW_KIND_LABEL[kind]}视图`;
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
