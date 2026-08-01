/**
 * 树形组装工具：将后端返回的扁平列表组装为树。
 *
 * 支持的两种元数据（字段名大小写不敏感）：
 * - ParentID + id：每行含父级字段与主键字段，按 parentID → id 挂载
 * - Path：每行含层级路径字段（如 /根/子/孙），按路径段挂载；
 *   部门/地区等实体同时返回 path 与 parentPath 时，优先用 parentPath 精确关联父节点
 *
 * 子节点统一挂到父节点的 `children` 数组；数据行保持原样（默认浅拷贝），
 * 与 `detectTreeData` / VTable hierarchy 直接兼容。
 */

/** 树节点：原始行数据 + children 子节点数组 */
export type TreeRow<T> = T & { children: TreeRow<T>[] };

/** 树构建选项 */
export interface BuildTreeOptions<T> {
  /** 主键字段名；默认自动探测（id/ID/Id） */
  idKey?: string;
  /** 父级字段名；默认自动探测（ParentID/parentId/parent_id/pid 等） */
  parentKey?: string;
  /** 路径字段名；默认自动探测（path/Path/fullPath 等） */
  pathKey?: string;
  /** 父级路径字段名；默认自动探测（parentPath/ParentPath 等） */
  parentPathKey?: string;
  /** 是否浅拷贝行后再挂 children，默认 true，避免污染后端原始数据 */
  clone?: boolean;
  /** 自定义主键提取器（优先于 idKey） */
  getId?: (row: T) => unknown;
  /** 自定义父级提取器（优先于 parentKey） */
  getParentId?: (row: T) => unknown;
  /** 自定义路径提取器（优先于 pathKey） */
  getPath?: (row: T) => string | null | undefined;
  /** 自定义父级路径提取器（优先于 parentPathKey） */
  getParentPath?: (row: T) => string | null | undefined;
}

/** 行对象视图，用于字段探测 */
type RowLike = Record<string, unknown>;

const ID_KEYS = ['id'];
const PARENT_KEYS = ['parentid', 'parent_id', 'pid'];
const PATH_KEYS = ['path', 'fullpath', 'treepath'];
const PARENT_PATH_KEYS = ['parentpath'];

function rowOf(row: unknown): RowLike | null {
  return row != null && typeof row === 'object' && !Array.isArray(row)
    ? (row as RowLike)
    : null;
}

/** 在行中查找匹配候选的键（大小写不敏感）；endsWith 用于 xxxParentID 类后缀匹配 */
function findKey(row: RowLike, candidates: string[], endsWith?: string): string | undefined {
  const keys = Object.keys(row);
  for (const k of keys) {
    if (candidates.includes(k.toLowerCase())) return k;
  }
  if (endsWith) {
    for (const k of keys) {
      if (k.toLowerCase().endsWith(endsWith)) return k;
    }
  }
  return undefined;
}

function hasField(rows: unknown[], candidates: string[], endsWith?: string): boolean {
  return rows.some((r) => {
    const o = rowOf(r);
    return !!o && !!findKey(o, candidates, endsWith);
  });
}

/** 探测：列表含 ParentID（父级）字段 */
function hasParentField(rows: unknown[]): boolean {
  return hasField(rows, PARENT_KEYS, 'parentid');
}

/** 探测：列表含 id（主键）字段 */
function hasIdField(rows: unknown[]): boolean {
  return hasField(rows, ID_KEYS);
}

/** 探测：列表含 Path（路径）字段 */
function hasPathField(rows: unknown[]): boolean {
  return hasField(rows, PATH_KEYS);
}

/** 探测：列表含 ParentPath（父级路径）字段 */
function hasParentPathField(rows: unknown[]): boolean {
  return hasField(rows, PARENT_PATH_KEYS);
}

/** 探测：列表含 ParentID 与 id 字段，可按父级组装树 */
export function hasParentIdData<T>(rows: T[], options?: BuildTreeOptions<T>): boolean {
  if (options?.getParentId || options?.parentKey) return true;
  return hasParentField(rows) && hasIdField(rows);
}

/** 探测：列表含 Path / ParentPath 字段，可按路径组装树 */
export function hasPathData<T>(rows: T[], options?: BuildTreeOptions<T>): boolean {
  if (options?.getPath || options?.pathKey) return true;
  // 部门/地区等实体同时返回 path 与 parentPath，任一项存在即视为可组装
  return hasPathField(rows) || hasParentPathField(rows);
}

/** 探测：列表能否组装为树（含 ParentID+id 或 Path） */
export function canBuildTree<T>(rows: T[], options?: BuildTreeOptions<T>): boolean {
  return hasParentIdData(rows, options) || hasPathData(rows, options);
}

/** 空值/0/-1 视为根节点 */
function isRootValue(v: unknown): boolean {
  if (v == null || v === '') return true;
  if (typeof v === 'number') return v === 0 || v === -1;
  if (typeof v === 'string') return v === '0' || v === '-1';
  return false;
}

/** 主键/父级值归一化为字符串（Int64 后端可能序列化为字符串） */
function normalizeKey(v: unknown): string | null {
  if (v == null || v === '') return null;
  return String(v);
}

/** 规范化路径：/根/子/ → 根/子；'' 或 '/' → '' */
function normalizePath(p: string): string {
  return p
    .replace(/[\\/]+/g, '/')
    .replace(/^\/+|\/+$/g, '')
    .trim();
}

/** 解析路径为段数组：/根/子 → [根, 子]；空 → [] */
function splitPath(p: string): string[] {
  const n = normalizePath(p);
  return n ? n.split('/').filter(Boolean) : [];
}

/** 内部挂载容器：node 与 children 同一引用，便于挂载 */
interface Holder<T> {
  node: TreeRow<T>;
  children: TreeRow<T>[];
}

function makeNode<T>(row: T, clone: boolean): Holder<T> {
  const children: TreeRow<T>[] = [];
  const base = (clone ? { ...(row as RowLike) } : row) as TreeRow<T>;
  base.children = children;
  return { node: base, children };
}

function resolveGetter<T>(
  custom: ((row: T) => unknown) | undefined,
  key: string | undefined,
  candidates: string[],
  endsWith?: string,
): (row: T) => unknown {
  if (custom) return custom;
  return (row: T) => {
    const o = rowOf(row);
    if (!o) return undefined;
    const k = key || findKey(o, candidates, endsWith);
    return k ? o[k] : undefined;
  };
}

/** 根据 ParentID → id 组装树；父级缺失/自引用/空值的行提升为根，避免丢数据 */
export function buildTreeByParentId<T>(
  rows: T[],
  options: BuildTreeOptions<T> = {},
): TreeRow<T>[] {
  if (!rows.length) return [];
  const clone = options.clone !== false;
  const getId = resolveGetter(options.getId, options.idKey, ID_KEYS);
  const getParentId = resolveGetter(options.getParentId, options.parentKey, PARENT_KEYS, 'parentid');

  const holders = rows.map((r) => makeNode(r, clone));
  const byId = new Map<string, Holder<T>>();
  for (const h of holders) {
    const id = normalizeKey(getId(h.node));
    if (id) byId.set(id, h);
  }

  const roots: Holder<T>[] = [];
  for (const h of holders) {
    const rawPid = getParentId(h.node);
    if (isRootValue(rawPid)) {
      roots.push(h);
      continue;
    }
    const pid = normalizeKey(rawPid);
    const parent = pid ? byId.get(pid) : undefined;
    if (parent && parent !== h) {
      parent.children.push(h.node);
    } else {
      roots.push(h);
    }
  }
  return roots.map((h) => h.node);
}

/** 根据 Path 组装树；父路径缺失/自引用的行提升为根，避免丢数据 */
export function buildTreeByPath<T>(
  rows: T[],
  options: BuildTreeOptions<T> = {},
): TreeRow<T>[] {
  if (!rows.length) return [];
  const clone = options.clone !== false;
  const getPath = resolveGetter(options.getPath, options.pathKey, PATH_KEYS) as (
    row: T,
  ) => string | null | undefined;
  const getParentPath = resolveGetter(
    options.getParentPath,
    options.parentPathKey,
    PARENT_PATH_KEYS,
  ) as (row: T) => string | null | undefined;

  const holders = rows.map((r) => makeNode(r, clone));
  const byPath = new Map<string, Holder<T>>();
  for (const h of holders) {
    byPath.set(normalizePath(getPath(h.node) ?? ''), h);
  }

  const roots: Holder<T>[] = [];
  for (const h of holders) {
    // 部门/地区等后端同时返回 path 与 parentPath，优先用 parentPath 精确关联父节点，
    // 避免从 path 末段推导产生的歧义（如节点名称含分隔符）
    const rawParentPath = getParentPath(h.node);
    if (rawParentPath != null && rawParentPath !== '') {
      const parent = byPath.get(normalizePath(String(rawParentPath)));
      if (parent && parent !== h) {
        parent.children.push(h.node);
        continue;
      }
    }
    // 回退：从完整 path 推导父路径
    const segments = splitPath(getPath(h.node) ?? '');
    if (segments.length <= 1) {
      roots.push(h);
      continue;
    }
    const parentPath = segments.slice(0, -1).join('/');
    const parent = byPath.get(parentPath);
    if (parent && parent !== h) {
      parent.children.push(h.node);
    } else {
      roots.push(h);
    }
  }
  return roots.map((h) => h.node);
}

/** 组装树：优先 ParentID+id，其次 Path；无元数据时原样返回（不挂 children） */
export function buildTree<T>(rows: T[], options: BuildTreeOptions<T> = {}): TreeRow<T>[] {
  if (!rows.length) return [];
  if (hasParentIdData(rows, options)) return buildTreeByParentId(rows, options);
  if (hasPathData(rows, options)) return buildTreeByPath(rows, options);
  return rows as unknown as TreeRow<T>[];
}
