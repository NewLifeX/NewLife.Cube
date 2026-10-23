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
import type { CubeRowAction } from '../components/CubeTable/types';
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
  /** 行/模型级显示条件 */
  visibleWhen?: (model: Record<string, unknown>) => boolean;
  /** 行/模型级必填条件 */
  requiredWhen?: (model: Record<string, unknown>) => boolean;
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

/** 页面呈现配置 */
export interface PagePreset {
  /** 页面设置覆盖 */
  setting?: Partial<PageSetting>;
  /** 列表区 */
  list?: ListPreset;
  /** 表单区（新增与编辑共用） */
  form?: FormPreset;
  /** 搜索区 */
  search?: SearchPreset;
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
  if (patch.visibleWhen !== undefined) next.visibleWhen = patch.visibleWhen;
  if (patch.requiredWhen !== undefined) next.requiredWhen = patch.requiredWhen;
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
