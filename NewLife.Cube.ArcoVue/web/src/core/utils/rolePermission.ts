/** 菜单权限目录项（Menu.Permission：flag#name） */
export type MenuPermFlag = { flag: number; name: string };

/** 叶子菜单行内权限勾选（不进入 tree children） */
export type PermFlagItem = {
  key: string;
  title: string;
  menuId: number;
  flag: number;
};

/** 授权树节点：仅真实菜单；权限位挂在叶子的 `perms` */
export type PermTreeNode = {
  key: string;
  title: string;
  children?: PermTreeNode[];
  menuId?: number;
  perms?: PermFlagItem[];
};

/** 菜单列表行（GetList camelCase / PascalCase） */
export type MenuRow = {
  id: number;
  parentId?: number;
  parentID?: number;
  displayName?: string;
  name?: string;
  permission?: string;
};

const DEFAULT_CATALOG: MenuPermFlag[] = [
  { flag: 1, name: '查看' },
  { flag: 2, name: '添加' },
  { flag: 4, name: '修改' },
  { flag: 8, name: '删除' },
];

export const MENU_PERMISSION_HINT = '权限子项由控制器 EntityAuthorize 生成，请勿手改。';

/** 去首尾斜杠、反斜杠改 /、小写 */
export function normalizeIamTypePath(typePath: unknown): string {
  return String(typePath ?? '')
    .trim()
    .replace(/\\/g, '/')
    .replace(/^\/+|\/+$/g, '')
    .toLowerCase();
}

function fieldName(field: { name?: string } | null | undefined): string {
  return String(field?.name ?? '').toLowerCase();
}

export function isRolePermissionField(
  typePath: unknown,
  field: { name?: string } | null | undefined,
): boolean {
  return normalizeIamTypePath(typePath) === 'admin/role' && fieldName(field) === 'permission';
}

export function isMenuPermissionField(
  typePath: unknown,
  field: { name?: string } | null | undefined,
): boolean {
  return normalizeIamTypePath(typePath) === 'admin/menu' && fieldName(field) === 'permission';
}

/** 角色 Permission 树或菜单目录说明：表单拉满 24 栅格 */
export function isIamPermissionFullWidth(
  typePath: unknown,
  field: { name?: string } | null | undefined,
): boolean {
  return isRolePermissionField(typePath, field) || isMenuPermissionField(typePath, field);
}

/** Menu.Permission → 目录；空则默认查看/添加/修改/删除 */
export function parseMenuPermissionCatalog(raw: unknown): MenuPermFlag[] {
  if (typeof raw !== 'string' || !raw.trim()) return DEFAULT_CATALOG.slice();
  const byFlag = new Map<number, string>();
  for (const part of raw.split(',')) {
    const seg = part.trim();
    const hash = seg.indexOf('#');
    if (hash < 0) continue;
    const flag = Number.parseInt(seg.slice(0, hash).trim(), 10);
    if (!Number.isFinite(flag) || flag <= 0) continue;
    const name = seg.slice(hash + 1).trim() || String(flag);
    byFlag.set(flag, name);
  }
  if (!byFlag.size) return DEFAULT_CATALOG.slice();
  return [...byFlag.entries()].map(([flag, name]) => ({ flag, name }));
}

/**
 * XCode `SavePermission` 把 `PermissionFlags.All`（UInt32 0xFFFFFFFF）写成 Int32 `-1`。
 * 解析时还原为无符号位掩码，勾选逻辑与 CubeNC `role.Has(menuId, flag)` 一致。
 */
export function parsePermFlags(raw: unknown): number | null {
  if (typeof raw === 'number' && Number.isFinite(raw)) return raw >>> 0;
  if (typeof raw === 'string' && raw.trim()) {
    const n = Number.parseInt(raw.trim(), 10);
    if (Number.isFinite(n)) return n >>> 0;
  }
  return null;
}

/** 与 XCode 一致：All 输出 -1，其余输出无符号十进制 */
export function formatPermFlags(flags: number): string {
  const u = flags >>> 0;
  if (u === 0xffffffff) return '-1';
  return String(u);
}

function putRoleFlags(map: Map<number, number>, menuId: number, flags: number | null) {
  if (!Number.isFinite(menuId) || menuId <= 0 || flags == null || flags === 0) return;
  map.set(menuId, flags);
}

function parseRolePermissionString(raw: string, map: Map<number, number>) {
  for (const part of raw.split(',')) {
    const seg = part.trim();
    const hash = seg.indexOf('#');
    if (hash < 0) continue;
    const menuId = Number.parseInt(seg.slice(0, hash).trim(), 10);
    putRoleFlags(map, menuId, parsePermFlags(seg.slice(hash + 1)));
  }
}

function parseRolePermissionDict(raw: Record<string, unknown>, map: Map<number, number>) {
  const nested = raw.permission ?? raw.Permission;
  if (typeof nested === 'string' && nested.trim()) {
    parseRolePermissionString(nested, map);
    return;
  }
  const dict = (raw.permissions ?? raw.Permissions ?? raw) as Record<string, unknown>;
  for (const [k, v] of Object.entries(dict)) {
    if (k === 'permission' || k === 'Permission' || k === 'permissions' || k === 'Permissions') continue;
    putRoleFlags(map, Number.parseInt(k, 10), parsePermFlags(v));
  }
}

/** Role.Permission → menuId → flags（兼容 `12#7`、All 的 `-1`、Permissions 字典） */
export function parseRolePermission(raw: unknown): Map<number, number> {
  const map = new Map<number, number>();
  if (raw == null || raw === '') return map;
  if (typeof raw === 'string') {
    parseRolePermissionString(raw, map);
    return map;
  }
  if (typeof raw === 'object' && !Array.isArray(raw)) {
    parseRolePermissionDict(raw as Record<string, unknown>, map);
  }
  return map;
}

/** flags>0 的项按 menuId 升序，无空格；All 写成 -1（对齐 XCode） */
export function serializeRolePermission(map: Map<number, number>): string {
  const parts: string[] = [];
  const ids = [...map.keys()].sort((a, b) => a - b);
  for (const id of ids) {
    const flags = (map.get(id) ?? 0) >>> 0;
    if (flags > 0) parts.push(`${id}#${formatPermFlags(flags)}`);
  }
  return parts.join(',');
}

function menuParentId(row: MenuRow): number {
  const v = row.parentId ?? row.parentID ?? 0;
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

/** 扁平菜单 → 树；仅叶子菜单挂权限叶 */
export function buildPermForest(menus: MenuRow[]): PermTreeNode[] {
  const nodeMap = new Map<number, PermTreeNode>();
  const rows: MenuRow[] = [];
  for (const raw of menus) {
    const id = Number(raw.id);
    if (!Number.isFinite(id) || id <= 0) continue;
    rows.push({ ...raw, id });
    nodeMap.set(id, {
      key: `m:${id}`,
      title: String(raw.displayName || raw.name || id),
      children: [],
      menuId: id,
    });
  }
  const roots: PermTreeNode[] = [];
  for (const row of rows) {
    const node = nodeMap.get(row.id)!;
    const pid = menuParentId(row);
    if (pid === 0 || !nodeMap.has(pid)) {
      roots.push(node);
    } else {
      nodeMap.get(pid)!.children!.push(node);
    }
  }

  const attachLeaves = (nodes: PermTreeNode[]) => {
    for (const node of nodes) {
      const realKids = node.children ?? [];
      if (realKids.length) {
        attachLeaves(realKids);
        continue;
      }
      const row = rows.find((r) => r.id === node.menuId);
      const catalog = parseMenuPermissionCatalog(row?.permission);
      node.children = undefined;
      node.perms = catalog.map((c) => ({
        key: `p:${node.menuId}:${c.flag}`,
        title: c.name,
        menuId: node.menuId!,
        flag: c.flag,
      }));
    }
  };
  attachLeaves(roots);
  return roots;
}

function flagOn(flags: number, flag: number): boolean {
  const f = flag >>> 0;
  if (f === 0) return false;
  return ((flags >>> 0) & f) === f;
}

/** 仅权限勾选 key 进入受控集合 */
export function checkedKeysFromRole(map: Map<number, number>, forest: PermTreeNode[]): string[] {
  const keys: string[] = [];
  const walk = (nodes: PermTreeNode[]) => {
    for (const n of nodes) {
      for (const p of n.perms ?? []) {
        if (flagOn(map.get(p.menuId) ?? 0, p.flag)) keys.push(p.key);
      }
      if (n.children?.length) walk(n.children);
    }
  };
  walk(forest);
  return keys;
}

export function roleMapFromCheckedKeys(keys: unknown, _forest?: PermTreeNode[]): Map<number, number> {
  const map = new Map<number, number>();
  const list = Array.isArray(keys) ? keys : [];
  for (const raw of list) {
    const s = String(raw);
    if (!s.startsWith('p:')) continue;
    const parts = s.slice(2).split(':');
    if (parts.length < 2) continue;
    const menuId = Number.parseInt(parts[0], 10);
    const flag = Number.parseInt(parts[1], 10);
    if (!Number.isFinite(menuId) || !Number.isFinite(flag) || flag <= 0) continue;
    map.set(menuId, (map.get(menuId) ?? 0) | flag);
  }
  return map;
}

export function collectPermLeafKeys(forest: PermTreeNode[]): string[] {
  const keys: string[] = [];
  const walk = (nodes: PermTreeNode[]) => {
    for (const n of nodes) {
      for (const p of n.perms ?? []) keys.push(p.key);
      if (n.children?.length) walk(n.children);
    }
  };
  walk(forest);
  return keys;
}

/** 某菜单或其子孙上的全部权限 key（分组全选 / 菜单项全选） */
export function collectPermKeysUnderNode(node: PermTreeNode | null | undefined): string[] {
  if (!node) return [];
  const keys: string[] = [];
  const walk = (n: PermTreeNode) => {
    for (const p of n.perms ?? []) keys.push(p.key);
    for (const c of n.children ?? []) walk(c);
  };
  walk(node);
  return keys;
}

export type NodePermCheckState = 'all' | 'some' | 'none';

/** 节点（含子孙）权限勾选态：全选 / 部分 / 未选 */
export function nodePermCheckState(
  keys: (string | number)[],
  node: PermTreeNode | null | undefined,
): NodePermCheckState {
  const all = collectPermKeysUnderNode(node);
  if (!all.length) return 'none';
  const set = new Set(keys.map(String));
  let n = 0;
  for (const k of all) if (set.has(k)) n += 1;
  if (n === 0) return 'none';
  if (n === all.length) return 'all';
  return 'some';
}

/** 勾选/取消当前节点及其子孙的全部权限位 */
export function toggleNodePerms(
  keys: (string | number)[],
  node: PermTreeNode | null | undefined,
  checked: boolean,
): string[] {
  const under = collectPermKeysUnderNode(node);
  if (!under.length) return keys.map(String);
  const drop = new Set(under);
  const rest = keys.map(String).filter((k) => !drop.has(k));
  return checked ? [...rest, ...under] : rest;
}

/** 标题槽节点 → 森林中的真实菜单节点 */
export function findPermNode(
  forest: PermTreeNode[],
  slot: { key?: unknown; node?: unknown; treeNodeData?: unknown } | string | null | undefined,
): PermTreeNode | undefined {
  const raw = typeof slot === 'object' && slot ? slot : null;
  const inner =
    raw && typeof raw.node === 'object'
      ? (raw.node as { key?: unknown })
      : raw && typeof raw.treeNodeData === 'object'
        ? (raw.treeNodeData as { key?: unknown })
        : raw;
  const k = typeof slot === 'string' ? slot : String(inner?.key ?? raw?.key ?? '');
  if (!k) return undefined;
  const walk = (nodes: PermTreeNode[]): PermTreeNode | undefined => {
    for (const n of nodes) {
      if (n.key === k) return n;
      if (n.children?.length) {
        const hit = walk(n.children);
        if (hit) return hit;
      }
    }
  };
  return walk(forest);
}

export function collectMenuKeys(forest: PermTreeNode[]): string[] {
  const keys: string[] = [];
  const walk = (nodes: PermTreeNode[]) => {
    for (const n of nodes) {
      keys.push(n.key);
      if (n.children?.length) walk(n.children);
    }
  };
  walk(forest);
  return keys;
}

/** 标题槽可能拿不到自定义字段：按 key 回查叶子 perms */
export function nodePerms(
  node: { key?: unknown; perms?: PermFlagItem[]; node?: unknown; treeNodeData?: unknown } | null | undefined,
  forest: PermTreeNode[],
): PermFlagItem[] {
  const raw = node as { key?: unknown; perms?: PermFlagItem[]; node?: unknown; treeNodeData?: unknown } | null;
  const inner =
    raw && typeof raw.node === 'object'
      ? (raw.node as { key?: unknown; perms?: PermFlagItem[] })
      : raw && typeof raw.treeNodeData === 'object'
        ? (raw.treeNodeData as { key?: unknown; perms?: PermFlagItem[] })
        : raw;
  if (inner?.perms?.length) return inner.perms;
  const k = String(inner?.key ?? raw?.key ?? '');
  if (!k) return [];
  const walk = (nodes: PermTreeNode[]): PermFlagItem[] | undefined => {
    for (const n of nodes) {
      if (n.key === k) return n.perms ?? [];
      if (n.children?.length) {
        const hit = walk(n.children);
        if (hit) return hit;
      }
    }
  };
  return walk(forest) ?? [];
}

/** 叶子菜单名列宽（em）：按字符最长；CJK 满宽，ASCII 约半宽 */
export function leafTitleColumnEm(forest: PermTreeNode[]): number {
  let max = 0;
  const walk = (nodes: PermTreeNode[]) => {
    for (const n of nodes) {
      if (n.perms?.length) {
        let w = 0;
        for (const ch of n.title) {
          w += /[\u1100-\uFFFF]/.test(ch) ? 1 : 0.55;
        }
        max = Math.max(max, w);
      }
      if (n.children?.length) walk(n.children);
    }
  };
  walk(forest);
  return max;
}

/** 行内勾选/取消某一权限位 */
export function togglePermKey(
  keys: (string | number)[],
  key: string,
  checked: boolean,
): string[] {
  const set = new Set(keys.map(String));
  if (checked) set.add(key);
  else set.delete(key);
  return [...set];
}

export function menuRowFromRecord(row: Record<string, unknown>): MenuRow | null {
  const id = Number(row.id ?? row.ID);
  if (!Number.isFinite(id) || id <= 0) return null;
  return {
    id,
    parentId: Number(row.parentId ?? row.parentID ?? 0) || 0,
    displayName: String(row.displayName ?? row.DisplayName ?? ''),
    name: String(row.name ?? row.Name ?? ''),
    permission: row.permission != null ? String(row.permission) : String(row.Permission ?? ''),
  };
}

type ListPage = { data?: unknown[]; page?: { totalCount?: number } };

/** 分页拉全量菜单；401/403 返回无权文案 */
export async function loadAllMenusForPermTree(
  getList: (type: string, params: { pageIndex: number; pageSize: number }) => Promise<ListPage>,
): Promise<{ menus: MenuRow[]; error?: string }> {
  const menus: MenuRow[] = [];
  try {
    for (let pageIndex = 1; pageIndex <= 10; pageIndex += 1) {
      const res = await getList('/Admin/Menu', { pageIndex, pageSize: 200 });
      const list = Array.isArray(res.data) ? res.data : [];
      for (const item of list) {
        if (item && typeof item === 'object') {
          const row = menuRowFromRecord(item as Record<string, unknown>);
          if (row) menus.push(row);
        }
      }
      const total = Number(res.page?.totalCount ?? menus.length);
      if (list.length === 0 || menus.length >= total) break;
    }
    return { menus };
  } catch (e: unknown) {
    const rec = e as {
      code?: number;
      status?: number;
      response?: { status?: number; data?: { code?: number } };
    };
    const code = Number(rec.code ?? rec.response?.data?.code ?? rec.status ?? rec.response?.status);
    if (code === 401 || code === 403) {
      return { menus: [], error: '无权加载菜单目录' };
    }
    const msg = (e as { message?: string })?.message;
    return { menus: [], error: msg || '加载菜单失败' };
  }
}
