/**
 * 页面呈现配置（preset）
 *
 * preset 是**纯前端配置**：只做元数据没覆盖到的呈现打磨，合并到 `/meta` 结果上；
 * 不写组件、不发请求、不改后端。合并顺序：`/meta` → preset → 用户列偏好。
 *
 * 业务页可在模块加载时注册：
 * ```ts
 * registerPagePreset('/Admin/User', {
 *   list: { patch: { Remark: { hidden: true } } },
 *   form: { columns: 1 },
 * });
 * ```
 */
import type { FieldMeta } from '../types/field';
import type { CubeGridFlags, CubeRowAction } from '../components/CubeTable/types';
import type { NormalizedPageFields, PageSetting } from './types';

/** 单字段呈现补丁 */
export interface FieldPresetPatch {
  /** 表头文字 */
  label?: string;
  /** 列宽。数字为像素，'auto' 表示自适应 */
  width?: number | 'auto';
  /** 对齐方式 */
  align?: 'left' | 'center' | 'right';
  /** 固定列 */
  fixed?: 'left' | 'right';
  /** 渲染器名（等价于后端的 view） */
  renderer?: string;
  /** 是否隐藏 */
  hidden?: boolean;
  /**
   * 默认是否显示。
   *
   * `false` 只影响初始态：字段仍保留在列表元数据里，会出现在「设置列 → 可见列」中，
   * 用户可自行勾选显示。需要彻底移除（连候选都不出现）请用 `ListPreset.remove`。
   */
  visible?: boolean;
  /** 溢出提示开关：false 关闭引擎列的 show-overflow-tooltip */
  overflowTooltip?: boolean;
  /** 行/模型级显示条件 */
  visibleWhen?: (model: Record<string, unknown>) => boolean;
  /** 行/模型级必填条件 */
  requiredWhen?: (model: Record<string, unknown>) => boolean;
  /** 默认值：新增表单初值，且留空提交时回填 */
  defaultValue?: unknown;
}

/** 列表区补丁 */
export interface ListPreset {
  /** 移除字段名 */
  remove?: string[];
  /** 字段补丁，键为字段名（大小写不敏感） */
  patch?: Record<string, FieldPresetPatch>;
  /** 追加字段 */
  append?: FieldMeta[];
  /** 列顺序；未列出的字段保持原顺序排在后面 */
  order?: string[];
  /** 追加行操作，与页面 `extraRowActions` 一起参与列宽自适应 */
  actions?: CubeRowAction[];
}

/** 表单区补丁 */
export interface FormPreset {
  /** 移除字段名 */
  remove?: string[];
  /** 字段补丁，键为字段名（大小写不敏感） */
  patch?: Record<string, FieldPresetPatch>;
  /** 追加字段 */
  append?: FieldMeta[];
  /** 弹窗表单列数 */
  columns?: 1 | 2 | 3;
  /** 分组方式。函数返回分组标题 */
  groupBy?: 'category' | ((field: FieldMeta) => string);
}

/** 搜索区补丁 */
export interface SearchPreset {
  /** 移除查询项名 */
  remove?: string[];
  /** 追加查询项 */
  append?: FieldMeta[];
}

/** 工具栏区补丁 */
export interface ToolbarPreset {
  /**
   * 移除后端下发的工具栏按钮。
   *
   * 匹配按钮的 `name` 或 `action`（大小写不敏感）；后端按钮名通常是方法名，
   * 不受界面语言影响，例如 `RefreshName`。
   */
  remove?: string[];
}

/** 页面呈现配置 */
export interface PagePreset {
  /** 页面设置覆盖 */
  setting?: Partial<PageSetting>;
  /**
   * 表格区开关覆盖（勾选列 / 操作列宽等）。
   *
   * 与 `setting` 不同：这些是纯前端呈现项，后端 `/meta` 不下发；
   * 由默认列表页透传给 `CubeTable` 的 `grid` 入参。
   */
  grid?: CubeGridFlags;
  /** 列表区 */
  list?: ListPreset;
  /** 表单区（新增与编辑共用） */
  form?: FormPreset;
  /** 搜索区 */
  search?: SearchPreset;
  /** 工具栏区 */
  toolbar?: ToolbarPreset;
}

/** 字段补丁规格（列表与表单共用） */
interface FieldPatchSpec {
  remove?: string[];
  patch?: Record<string, FieldPresetPatch>;
  append?: FieldMeta[];
  order?: string[];
}

/** 把字段名归一为小写，便于大小写不敏感匹配 */
function normalizeNames(names?: string[]): Set<string> {
  return new Set((names ?? []).map((name) => name.trim().toLowerCase()).filter(Boolean));
}

/** 应用单字段补丁 */
function applyFieldPatch(field: FieldMeta, patch: FieldPresetPatch): FieldMeta {
  const next: FieldMeta = { ...field };
  if (patch.label !== undefined) next.header = patch.label;
  if (patch.width !== undefined) next.width = patch.width;
  if (patch.align !== undefined) next.textAlign = patch.align;
  if (patch.fixed !== undefined) next.fixed = patch.fixed;
  if (patch.renderer !== undefined) next.view = patch.renderer;
  if (patch.hidden !== undefined) next.hidden = patch.hidden;
  if (patch.visible !== undefined) next.visible = patch.visible;
  if (patch.overflowTooltip !== undefined) next.overflowTooltip = patch.overflowTooltip;
  if (patch.visibleWhen !== undefined) next.visibleWhen = patch.visibleWhen;
  if (patch.requiredWhen !== undefined) next.requiredWhen = patch.requiredWhen;
  if (patch.defaultValue !== undefined) next.defaultValue = patch.defaultValue;
  return next;
}

/**
 * 应用字段数组补丁：移除 → 补丁 → 排序 → 追加。
 * 名称为空或重复的字段按原顺序保留，追加字段直接排在末尾。
 */
export function applyFieldPreset(fields: FieldMeta[], spec?: FieldPatchSpec | null): FieldMeta[] {
  if (!spec) return fields;

  const removed = normalizeNames(spec.remove);
  const patches = new Map<string, FieldPresetPatch>();
  for (const [name, patch] of Object.entries(spec.patch ?? {})) {
    patches.set(name.trim().toLowerCase(), patch);
  }

  let result = fields
    .filter((field) => !removed.has(field.name.toLowerCase()))
    .map((field) => {
      const patch = patches.get(field.name.toLowerCase());
      return patch ? applyFieldPatch(field, patch) : field;
    });

  if (spec.order?.length) {
    const map = new Map(result.map((field) => [field.name.toLowerCase(), field]));
    const ordered: FieldMeta[] = [];
    for (const name of spec.order) {
      const field = map.get(name.trim().toLowerCase());
      if (!field) continue;
      ordered.push(field);
      map.delete(name.trim().toLowerCase());
    }
    for (const field of result) {
      if (map.has(field.name.toLowerCase())) ordered.push(field);
    }
    result = ordered;
  }

  if (spec.append?.length) {
    const exists = new Set(result.map((field) => field.name.toLowerCase()));
    for (const field of spec.append) {
      if (!field?.name || exists.has(field.name.toLowerCase())) continue;
      result.push({ ...field });
      exists.add(field.name.toLowerCase());
    }
  }

  return result;
}

/**
 * 过滤后端下发的工具栏按钮。
 *
 * 名单按 `name` 或 `action` 精确匹配（大小写不敏感、忽略首尾空格）。
 * 不传或名单为空时原样返回，避免无谓复制。
 */
export function filterToolbarItems<T extends { name?: string; action?: string }>(
  items: T[],
  spec?: ToolbarPreset | null,
): T[] {
  const names = (spec?.remove ?? []).map((name) => name.trim().toLowerCase()).filter(Boolean);
  if (!names.length) return items;

  const removed = new Set(names);
  return items.filter((item) => {
    const name = String(item.name ?? '').trim().toLowerCase();
    const action = String(item.action ?? '').trim().toLowerCase();
    return !removed.has(name) && !removed.has(action);
  });
}

/** 搜索区补丁。keyword 查询项单独处理，不进入普通字段数组 */
function applySearchPreset(meta: NormalizedPageFields, spec?: SearchPreset | null): NormalizedPageFields {
  if (!spec) return meta;

  const removed = normalizeNames(spec.remove);
  const keywordRemoved = removed.has('q') || removed.has('keyword');
  const appendedKeyword = spec.append?.find((field) => field.kind === 'keyword');

  return {
    ...meta,
    searchFields: applyFieldPreset(meta.searchFields, { remove: spec.remove, append: spec.append }),
    keyword: appendedKeyword ? { ...appendedKeyword } : keywordRemoved ? undefined : meta.keyword,
  };
}

/**
 * 把 preset 合并到归一后的元数据上。
 *
 * 只做「呈现层」合并：字段顺序、标题、列宽、对齐、固定、渲染器、显隐、分组与行操作；
 * 不改变字段的实体类型与提交语义。
 */
export function applyPagePreset(
  meta: NormalizedPageFields,
  preset?: PagePreset | null,
): NormalizedPageFields {
  if (!preset) return meta;

  const setting: PageSetting = { ...meta.setting, ...preset.setting };
  return {
    ...applySearchPreset(meta, preset.search),
    setting,
    listFields: applyFieldPreset(meta.listFields, preset.list),
    addFields: applyFieldPreset(meta.addFields, preset.form),
    editFields: applyFieldPreset(meta.editFields, preset.form),
    detailFields: applyFieldPreset(meta.detailFields, preset.form),
  };
}

/** preset 注册表。键为路由路径（归一为小写、按 / 开头、去尾斜杠） */
const presets = new Map<string, PagePreset>();

/** 归一页面路径，供注册与查找共用 */
export function normalizePresetPath(path?: string | null): string {
  const text = (path ?? '').trim().toLowerCase();
  if (!text) return '';
  const withSlash = text.startsWith('/') ? text : `/${text}`;
  return withSlash.replace(/\/+$/, '') || '/';
}

/** 注册页面呈现配置。同一路径后注册覆盖先注册 */
export function registerPagePreset(path: string, preset: PagePreset): void {
  const key = normalizePresetPath(path);
  if (!key) return;
  presets.set(key, preset);
}

/** 取页面呈现配置。未注册返回 null */
export function getPagePreset(path?: string | null): PagePreset | null {
  const key = normalizePresetPath(path);
  return key ? presets.get(key) ?? null : null;
}

/** 清空注册表（测试用） */
export function clearPagePresets(): void {
  presets.clear();
}
