/**
 * 值集管理页纯函数（OSC-2610019c9d）。
 *
 * 权限按钮、列表过滤、ENUM/LIST 配置保存体组装、类型展示文案。
 */
import { Auth, checkAuth } from '@newlifex/page-utils';

/** 值集列表行（手工定义） */
export interface LovRow {
  id: string;
  lovCode: string;
  name: string;
  type: string;
  valueField: string;
  labelField: string;
  enabled: boolean;
  remark: string;
}

/** 权限按钮显隐 */
export interface LovPermFlags {
  canView: boolean;
  canAdd: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

/** ENUM 配置行 */
export interface LovEnumItemDraft {
  value: string;
  label: string;
  sort: number;
  enabled?: boolean;
}

/** LIST 列表配置（对齐 Cube.Vue lov/config.vue） */
export interface LovListConfigDraft {
  requestUrl: string;
  method: string;
  pageable: boolean;
  pageNumField: string;
  pageSizeField: string;
  dataPath: string;
  totalPath: string;
  fixedParams: string;
  proxyRequest: boolean;
}

/** LIST 搜索字段草稿 */
export interface LovSearchFieldDraft {
  field: string;
  title: string;
  componentType: string;
  paramType: string;
  required: boolean;
  defaultValue: string;
  sort: number;
  refLovCode: string;
}

/** LIST 表格列草稿 */
export interface LovTableColumnDraft {
  field: string;
  title: string;
  width: number;
  align: string;
  sortable: boolean;
  refLovCode: string;
  formatType: string;
  sort: number;
}

/** 类型码 → 用户友好文案 */
export function lovTypeLabel(type: string | null | undefined): string {
  const t = (type ?? '').toUpperCase();
  if (t === 'ENUM') return '枚举';
  if (t === 'LIST') return '自定义列表';
  return type || '-';
}

/** 空列表配置默认值（对齐 Cube.Vue emptyListConfig） */
export function emptyListConfig(): LovListConfigDraft {
  return {
    requestUrl: '',
    method: 'GET',
    pageable: false,
    pageNumField: 'pageNo',
    pageSizeField: 'pageSize',
    dataPath: 'data.list',
    totalPath: 'data.total',
    fixedParams: '',
    proxyRequest: false,
  };
}

/** 请求地址以 / 开头 → 同源同应用，禁止代理 */
export function isSameAppListUrl(url: string | null | undefined): boolean {
  return !!url && url.startsWith('/');
}

/** 行归一化（兼容 PascalCase / camelCase） */
export function lovRowOf(row: Record<string, unknown>): LovRow | null {
  const lovCode = String(row.lovCode ?? row.LovCode ?? row.id ?? row.Id ?? '');
  if (!lovCode) return null;
  return {
    id: String(row.id ?? row.Id ?? lovCode),
    lovCode,
    name: String(row.name ?? row.Name ?? ''),
    type: String(row.type ?? row.Type ?? ''),
    valueField: String(row.valueField ?? row.ValueField ?? ''),
    labelField: String(row.labelField ?? row.LabelField ?? ''),
    enabled: Boolean(row.enabled ?? row.Enabled ?? true),
    remark: String(row.remark ?? row.Remark ?? ''),
  };
}

/**
 * 权限：无键视为可看可写；有键时按 Detail/Insert/Update/Delete。
 * 有键且无 Detail → canView=false（整页告警，不请求列表）。
 */
export function lovPermFlags(perms: Record<string, string> | null | undefined): LovPermFlags {
  const p = perms ?? {};
  const keys = Object.keys(p);
  if (keys.length === 0) {
    return { canView: true, canAdd: true, canEdit: true, canDelete: true };
  }
  return {
    canView: checkAuth(p, Auth.VIEW),
    canAdd: checkAuth(p, Auth.ADD),
    canEdit: checkAuth(p, Auth.EDIT),
    canDelete: checkAuth(p, Auth.DELETE),
  };
}

/** 前端关键字过滤编码与名称（请求仍带 Q） */
export function filterLovRows(rows: LovRow[], keyword: string): LovRow[] {
  const q = keyword.trim().toLowerCase();
  if (!q) return rows;
  return rows.filter(
    (r) => r.lovCode.toLowerCase().includes(q) || r.name.toLowerCase().includes(q),
  );
}

/** 仅 ENUM / LIST 可在此配置保存 */
export function canSaveConfig(type: string | null | undefined): boolean {
  const t = (type ?? '').toUpperCase();
  return t === 'ENUM' || t === 'LIST';
}

/** ENUM 保存体：剔除空 value 行 */
export function buildEnumSaveBody(
  id: string,
  items: LovEnumItemDraft[],
): { id: string; enumItems: LovEnumItemDraft[] } {
  const enumItems = items
    .filter((it) => String(it.value ?? '').trim() !== '')
    .map((it, i) => ({
      value: String(it.value),
      label: String(it.label ?? ''),
      sort: Number(it.sort) || i,
      enabled: it.enabled !== false,
    }));
  return { id, enumItems };
}

/** LIST 保存体（listConfig 全字段对齐 Cube.Vue SaveConfig） */
export function buildListSaveBody(
  id: string,
  opts: {
    listConfig: LovListConfigDraft;
    searchFields?: LovSearchFieldDraft[];
    tableColumns?: LovTableColumnDraft[];
  },
): {
  id: string;
  listConfig: LovListConfigDraft;
  searchFields: LovSearchFieldDraft[];
  tableColumns: LovTableColumnDraft[];
} {
  const lc = opts.listConfig;
  const sameApp = isSameAppListUrl(lc.requestUrl);
  return {
    id,
    listConfig: {
      requestUrl: lc.requestUrl ?? '',
      method: lc.method || 'GET',
      pageable: !!lc.pageable,
      pageNumField: lc.pageNumField || 'pageNo',
      pageSizeField: lc.pageSizeField || 'pageSize',
      dataPath: lc.dataPath || '',
      totalPath: lc.totalPath || '',
      fixedParams: lc.fixedParams || '',
      proxyRequest: sameApp ? false : !!lc.proxyRequest,
    },
    searchFields: (opts.searchFields ?? []).filter((f) => String(f.field ?? '').trim()),
    tableColumns: (opts.tableColumns ?? []).filter((c) => String(c.field ?? '').trim()),
  };
}

/** 值集编码校验：非空、字母数字点下划线、最长 64 */
export function validateLovCode(code: string): string | null {
  const c = code.trim();
  if (!c) return '值集编码不能为空';
  if (c.length > 64) return '值集编码最长 64 字符';
  if (!/^[A-Za-z0-9._]+$/.test(c)) return '值集编码仅允许字母、数字、点与下划线';
  return null;
}

/** 按类型补齐 Enum. / List. 前缀（与后端 CheckCode 一致） */
export function ensureLovCodePrefix(code: string, type: string): string {
  const c = code.trim();
  const t = type.toUpperCase();
  if (t === 'ENUM') {
    if (/^Enum\./i.test(c)) return `Enum.${c.slice(5)}`;
    return `Enum.${c}`;
  }
  if (t === 'LIST') {
    if (/^List\./i.test(c)) return `List.${c.slice(5)}`;
    return `List.${c}`;
  }
  return c;
}
