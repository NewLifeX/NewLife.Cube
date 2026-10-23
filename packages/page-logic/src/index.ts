/**
 * @newlifex/page-logic — 魔方前端列表页业务编排逻辑（框架无关核心）
 *
 * 将字段加载、列表查询、CRUD、导入导出、图表数据等列表页通用逻辑
 * 封装为框架无关的编排类，各框架通过适配器桥接到具体状态管理。
 */

import type { CubeApi, DataField, ApiResponse, PageParams, PageSetting, EntityMeta, ToolbarItem } from '@newlifex/api-core';
import { resolveWidgets, type FieldMapping } from '@newlifex/field-mapping';
import { buildExportUrl } from '@newlifex/page-utils';

// ======================== 类型 ========================

/** 分页信息 */
export interface Pagination {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
}

/** 页面状态快照 */
export interface PageState {
  /** 列表字段映射 */
  listFields: FieldMapping[];
  /** 搜索字段映射 */
  searchFields: FieldMapping[];
  /** 新增字段映射 */
  addFields: FieldMapping[];
  /** 编辑字段映射 */
  editFields: FieldMapping[];
  /** 详情字段映射 */
  detailFields: FieldMapping[];
  /** 主键字段名 */
  pkField: string;
  /** 表格数据 */
  tableData: Record<string, unknown>[];
  /** 统计行数据 */
  statData: Record<string, unknown> | null;
  /** 分页 */
  pagination: Pagination;
  /** 图表数据 */
  chartList: unknown[];
  /** 数据加载中 */
  loading: boolean;
  /** 表单提交中 */
  formLoading: boolean;
  /** 页面设置（由 meta 返回的 setting 字段） */
  pageSetting: PageSetting | null;
  /** 实体标识（meta.entity：名称/显示名/主键） */
  entity: EntityMeta | null;
  /** 工具栏自定义按钮（meta.toolbar，服务端已按权限过滤） */
  toolbar: ToolbarItem[];
  /** 页面 UI 扩展（meta.ui，核心不解释，透传给 preset 与渲染器） */
  ui: Record<string, unknown>;
  /** 当前页面生效的呈现配置（未注册为 null） */
  preset: PagePreset | null;
  /** 是否允许新增（来自 pageSetting 与菜单权限） */
  canAdd: boolean;
  /** 是否允许编辑（来自菜单权限） */
  canEdit: boolean;
  /** 是否允许删除（来自 pageSetting 与菜单权限） */
  canDelete: boolean;
  /** 是否允许导出（来自菜单权限） */
  canExport: boolean;
  /** 是否允许导入（来自菜单权限） */
  canImport: boolean;
  /** 是否可用开发功能（高级菜单 备份/还原/备份导出/清空数据表）。开发模式 && 系统管理员 */
  canDevelop: boolean;
}

/** 状态变更回调 */
export type PageStateUpdater = (partial: Partial<PageState>) => void;

/** 单个字段的呈现补丁（preset，纯前端；不改字段类型与提交语义） */
export interface FieldPresetPatch {
  /** 表头文案（写回 header） */
  label?: string;
  /** 列宽（像素或 auto；由皮肤读取） */
  width?: number | 'auto';
  /** 对齐（left/center/right） */
  align?: 'left' | 'center' | 'right';
  /** 固定列（由皮肤读取） */
  fixed?: 'left' | 'right';
  /** 渲染器名（写回 view） */
  renderer?: string;
  /** 是否隐藏（写回 visible=false） */
  hidden?: boolean;
  /** 行/模型级显示条件（由皮肤读取） */
  visibleWhen?: (model: Record<string, unknown>) => boolean;
  /** 模型级必填条件（由皮肤读取） */
  requiredWhen?: (model: Record<string, unknown>) => boolean;
}

/** 字段集合补丁：移除 → 补丁 → 排序 → 追加 */
export interface FieldPresetSpec {
  /** 移除字段名 */
  remove?: string[];
  /** 字段补丁，键为字段名（大小写不敏感） */
  patch?: Record<string, FieldPresetPatch>;
  /** 追加字段（名称重复时跳过） */
  append?: DataField[];
  /** 列序；未列出的字段保持原序排在后面 */
  order?: string[];
}

/** 页面呈现配置（对应呈现架构 §6.6 / F1） */
export interface PagePreset {
  /** 页面设置覆盖 */
  setting?: Partial<PageSetting>;
  /** 列表区 */
  list?: FieldPresetSpec;
  /** 表单区（新增/编辑共用） */
  form?: FieldPresetSpec & {
    /** 弹窗/表单列数 */
    columns?: 1 | 2 | 3;
    /** 分组方式 */
    groupBy?: 'auto' | 'category' | 'tabs' | 'flat';
  };
  /** 搜索区 */
  search?: { remove?: string[]; append?: DataField[] };
}

/**
 * 应用字段集合补丁：移除 → 补丁 → 排序 → 追加。
 *
 * 只改呈现字段（header/textAlign/view/visible），不改 typeName、dataSource 等提交语义。
 *
 * @param fields 原始字段数组
 * @param spec 补丁；为空时原样返回
 * @returns 应用补丁后的新数组
 */
export function applyFieldPreset(fields: DataField[], spec?: FieldPresetSpec | null): DataField[] {
  if (!spec) return fields;

  const lower = (s: string) => s.toLowerCase();
  const removed = new Set((spec.remove ?? []).map(lower));
  const patches = new Map(Object.entries(spec.patch ?? {}).map(([k, v]) => [lower(k), v]));

  let result = fields
    .filter((f) => !removed.has(lower(f.name)))
    .map((f) => {
      const patch = patches.get(lower(f.name));
      if (!patch) return f;
      const next: DataField = { ...f };
      if (patch.label !== undefined) next.header = patch.label;
      if (patch.align !== undefined) next.textAlign = patch.align;
      if (patch.renderer !== undefined) next.view = patch.renderer;
      if (patch.hidden !== undefined) next.visible = !patch.hidden;
      return next;
    });

  if (spec.order?.length) {
    const map = new Map(result.map((f) => [lower(f.name), f]));
    const ordered: DataField[] = [];
    for (const name of spec.order) {
      const field = map.get(lower(name));
      if (!field) continue;
      ordered.push(field);
      map.delete(lower(name));
    }
    for (const field of result) {
      if (map.has(lower(field.name))) ordered.push(field);
    }
    result = ordered;
  }

  if (spec.append?.length) {
    const exists = new Set(result.map((f) => lower(f.name)));
    for (const field of spec.append) {
      if (!field?.name || exists.has(lower(field.name))) continue;
      result.push({ ...field });
      exists.add(lower(field.name));
    }
  }

  return result;
}

/** preset 注册表。键为实体路径前缀（大小写不敏感） */
const pagePresets = new Map<string, PagePreset>();

/**
 * 注册页面呈现配置（同一路径后注册覆盖先注册）
 *
 * @param type 实体路径前缀（如 `/Admin/User`）
 * @param preset 页面呈现配置
 */
export function registerPagePreset(type: string, preset: PagePreset): void {
  if (!type) return;
  pagePresets.set(type.toLowerCase(), preset);
}

/**
 * 取页面呈现配置
 *
 * @param type 实体路径前缀
 * @returns 页面呈现配置；未注册返回 null
 */
export function getPagePreset(type?: string | null): PagePreset | null {
  if (!type) return null;
  return pagePresets.get(type.toLowerCase()) ?? null;
}

/** 清空 preset 注册表（测试用） */
export function clearPagePresets(): void {
  pagePresets.clear();
}

/** 魔方动作权限位（对齐 XCode.Membership.PermissionFlags） */
export const PermissionFlags = {
  /** 查看 */
  Detail: 1,
  /** 新增/导入 */
  Insert: 2,
  /** 修改 */
  Update: 4,
  /** 删除 */
  Delete: 8,
} as const;

/**
 * 解析用户权限串为 menuId → 权限位。
 *
 * 服务端 `UserInfo.permission` 格式为 `id#flag,id#flag`；`-1` 等价于 All（0xFFFFFFFF）。
 * 非法项（缺 #、非数字）跳过，不抛异常。
 *
 * @param value 权限串，可空
 * @returns menuId → 位集合；无有效项时返回空 Map
 */
export function parsePermissionString(value?: string | null): Map<number, number> {
  const map = new Map<number, number>();
  if (!value) return map;

  for (const item of value.split(',')) {
    const text = item.trim();
    if (!text) continue;
    const index = text.indexOf('#');
    if (index <= 0) continue;
    const idText = text.slice(0, index).trim();
    const flagText = text.slice(index + 1).trim();
    if (!idText || !flagText) continue;
    const id = Number(idText);
    const flag = Number(flagText);
    if (!Number.isInteger(id) || !Number.isInteger(flag)) continue;
    map.set(id, flag);
  }

  return map;
}

/**
 * 权限位测试：判断某菜单的位集合是否包含动作位。
 *
 * @param map 权限串解析结果
 * @param menuId 菜单编号；空表示无法判定
 * @param flag 动作位；空表示无法判定
 * @returns true 有权限 / false 无权限 / null 无法判定（交给调用方回退口径）
 */
export function hasPermissionAction(
  map: Map<number, number>,
  menuId?: number | null,
  flag?: number | null,
): boolean | null {
  if (menuId == null || flag == null) return null;
  const bits = map.get(menuId);
  if (bits == null) return false;

  return (bits & flag) === flag;
}

/** PageLogic 构造选项 */
export interface PageLogicOptions {
  /** CubeApi 实例 */
  api: CubeApi;
  /** 状态变更回调 */
  update: PageStateUpdater;
  /** 默认每页大小 */
  defaultPageSize?: number;
  /** 菜单权限（来自 auth-logic getMenuPermission），用于推断 canAdd/canEdit/canDelete 等；
   *  可传静态对象或返回对象的函数（菜单异步加载后动态读取） */
  menuPermissions?: Record<string, string> | (() => Record<string, string>);
  /** 当前用户权限串（UserInfo.permission，如 `1#3,2#8`）。提供时优先用 setting.menuId + requiredPermissions 做位测试 */
  userPermission?: string | (() => string | undefined);
  /** 是否系统管理员。true 时编辑/删除等动作直通（对齐服务端系统角色） */
  isSystemAdmin?: boolean | (() => boolean);
  /** 页面呈现配置；缺省按实体路径从注册表查找 */
  preset?: PagePreset | null;
}

// ======================== 核心逻辑 ========================

/**
 * 列表页业务编排逻辑核心类
 */
export class PageLogic {
  private api: CubeApi;
  private update: PageStateUpdater;
  private state: PageState;
  private getMenuPermissions: () => Record<string, string>;
  private getUserPermission: () => string | undefined;
  private getIsSystemAdmin: () => boolean;
  private explicitPreset?: PagePreset | null;

  constructor(options: PageLogicOptions) {
    this.api = options.api;
    this.update = options.update;
    const mp = options.menuPermissions;
    this.getMenuPermissions =
      typeof mp === 'function'
        ? (mp as () => Record<string, string>)
        : () => (mp as Record<string, string> | undefined) ?? {};
    const up = options.userPermission;
    this.getUserPermission =
      typeof up === 'function' ? (up as () => string | undefined) : () => up;
    const sa = options.isSystemAdmin;
    this.getIsSystemAdmin =
      typeof sa === 'function' ? (sa as () => boolean) : () => sa === true;
    this.explicitPreset = options.preset;
    this.state = {
      listFields: [],
      searchFields: [],
      addFields: [],
      editFields: [],
      detailFields: [],
      pkField: 'id',
      tableData: [],
      statData: null,
      pagination: { pageIndex: 1, pageSize: options.defaultPageSize ?? 20, totalCount: 0 },
      chartList: [],
      loading: false,
      formLoading: false,
      pageSetting: null,
      entity: null,
      toolbar: [],
      ui: {},
      preset: null,
      canAdd: true,
      canEdit: true,
      canDelete: true,
      canExport: true,
      canImport: true,
      canDevelop: false,
    };
  }

  /** 当前状态快照 */
  getState(): Readonly<PageState> {
    return this.state;
  }

  /** 并行加载 5 类字段元数据，同时提取页面设置与权限标志 */
  async loadFields(type: string): Promise<void> {
    const pageRes = await this.api.page.getPage(type);
    const pageMeta = pageRes.data ?? {};

    // 页面呈现配置：显式传入优先，否则按实体路径查注册表
    const preset = this.explicitPreset !== undefined ? this.explicitPreset : getPagePreset(type);
    const listData = applyFieldPreset(pageMeta.list ?? pageMeta.fields?.list ?? [], preset?.list);
    const addData = applyFieldPreset(pageMeta.addForm ?? pageMeta.fields?.form?.addForm ?? [], preset?.form);
    const editData = applyFieldPreset(pageMeta.editForm ?? pageMeta.fields?.form?.editForm ?? [], preset?.form);
    const detailData = applyFieldPreset(pageMeta.detail ?? pageMeta.fields?.form?.detail ?? [], preset?.form);
    const searchData = applyFieldPreset(
      pageMeta.search ?? pageMeta.fields?.search ?? [],
      preset?.search ? { remove: preset.search.remove, append: preset.search.append } : null,
    );

    const listFields = resolveWidgets(listData);
    const searchFields = resolveWidgets(searchData);
    const addFields = resolveWidgets(addData);
    const editFields = resolveWidgets(editData);
    const detailFields = resolveWidgets(detailData);

    // 实体标识、页面设置与扩展块（/meta 视图自描述）
    const baseSetting = pageMeta.setting ?? pageMeta.pageSetting ?? null;
    const pageSetting = preset?.setting ? { ...(baseSetting ?? {}), ...preset.setting } : baseSetting;
    const entity = pageMeta.entity ?? null;
    const toolbar = pageMeta.toolbar ?? [];
    const ui = pageMeta.ui ?? {};

    // 主键：优先实体标识下发的主键，其次列表字段的 primaryKey（列表可隐藏主键列）
    const pk = listData.find((f) => f.primaryKey);
    const pkField = entity?.primaryKey ?? pk?.name ?? 'id';

    // 动作权限：优先 menuId + requiredPermissions + 用户权限串位测试（/meta 因此可缓存）；
    // meta 未下发时回退菜单树权限映射，兼容旧后端与配置页
    const perms = this.getMenuPermissions();
    const hasLegacy = (codes: number[], labels: string[], keys: string[]) =>
      keys.some((k) => perms[k] !== undefined) ||
      codes.some((c) => perms[String(c)] !== undefined) ||
      Object.values(perms).some((v) => labels.includes(v));
    const legacyAdd = hasLegacy([2], ['新增', '添加', 'Add'], ['Add']);
    const legacyEdit = hasLegacy([4], ['编辑', '修改', 'Edit'], ['Edit']);
    const legacyDelete = hasLegacy([8], ['删除', 'Delete'], ['Delete']);
    // 导出/导入的后端授权：export=[EntityAuthorize(Detail=1)]、import=[EntityAuthorize(Insert=2)]，
    // 部分后端可能定义 16/32，故旧口径两种授权码都兼容
    const legacyExport = hasLegacy([1, 16], ['查看', '导出', 'Export', 'Detail'], ['Detail', 'Export']);
    const legacyImport = hasLegacy([2, 32], ['添加', '新增', '导入', 'Import', 'Insert'], ['Insert', 'Import']);
    // 无任何权限配置时（菜单未配 permissions 且 meta 无 menuId）默认允许
    const noPermConfig = Object.keys(perms).length === 0 && !pageSetting?.menuId;

    const permMap = parsePermissionString(this.getUserPermission());
    const isSystem = this.getIsSystemAdmin();
    const menuId = pageSetting?.menuId;
    const required = pageSetting?.requiredPermissions;
    /** 权限位测试；无法判定（无 menuId 或未声明 flag）时返回 null，交给旧口径 */
    const allowed = (flag?: number): boolean | null => {
      if (isSystem) return true;
      return hasPermissionAction(permMap, menuId, flag);
    };

    const canAdd =
      (allowed(required?.add) ?? (noPermConfig || legacyAdd)) &&
      pageSetting?.enableAdd !== false &&
      pageSetting?.isReadOnly !== true;
    const canEdit = (allowed(required?.update) ?? (noPermConfig || legacyEdit)) && pageSetting?.isReadOnly !== true;
    const canDelete = (allowed(required?.delete) ?? (noPermConfig || legacyDelete)) && pageSetting?.isReadOnly !== true;
    const canExport = allowed(PermissionFlags.Detail) ?? (noPermConfig || legacyExport);
    const canImport = allowed(PermissionFlags.Insert) ?? (noPermConfig || legacyImport);
    // 开发功能（备份/还原/备份导出/清空数据表）：开发模式 && 系统管理员（对齐 MVC Develop 条件）
    const canDevelop = !!(pageSetting?.develop && pageSetting?.isSystem);

    this.state.listFields = listFields;
    this.state.searchFields = searchFields;
    this.state.addFields = addFields;
    this.state.editFields = editFields;
    this.state.detailFields = detailFields;
    this.state.pkField = pkField;
    this.state.pageSetting = pageSetting;
    this.state.entity = entity;
    this.state.toolbar = toolbar;
    this.state.ui = ui;
    this.state.preset = preset;
    this.state.canAdd = canAdd;
    this.state.canEdit = canEdit;
    this.state.canDelete = canDelete;
    this.state.canExport = canExport;
    this.state.canImport = canImport;
    this.state.canDevelop = canDevelop;

    this.update({
      listFields,
      searchFields,
      addFields,
      editFields,
      detailFields,
      pkField,
      pageSetting,
      entity,
      toolbar,
      ui,
      preset,
      canAdd,
      canEdit,
      canDelete,
      canExport,
      canImport,
      canDevelop,
    });
  }

  /** 加载列表数据（分页 + 搜索） */
  async loadData(type: string, searchParams?: Record<string, unknown>): Promise<void> {
    this.state.loading = true;
    this.update({ loading: true });

    try {
      // 后端 Pager.PageIndex 从 1 开始、默认 1；PageSize 默认 20。
      // 第一页不传 pageIndex，pageSize 恰为 20 不传，交由后端默认值，避免冗余参数
      const pageIndex = this.state.pagination.pageIndex;
      const pageSize = this.state.pagination.pageSize;
      const params: PageParams = {
        ...(pageIndex > 1 ? { pageIndex } : {}),
        ...(pageSize !== 20 ? { pageSize } : {}),
        ...searchParams,
      };

      const res = await this.api.page.getList(type, params);

      const tableData = (res.data ?? []) as Record<string, unknown>[];
      const pagination = { ...this.state.pagination };

      if (res.page) {
        pagination.totalCount = res.page.totalCount;
        pagination.pageIndex = res.page.pageIndex; // 后端从 1 开始，与前端一致
        pagination.pageSize = res.page.pageSize;
      }

      const statData = (res.stat as Record<string, unknown>) ?? null;

      this.state.tableData = tableData;
      this.state.pagination = pagination;
      this.state.statData = statData;

      this.update({ tableData, pagination, statData });
    } finally {
      this.state.loading = false;
      this.update({ loading: false });
    }
  }

  /** 设置分页参数 */
  setPagination(page: number, pageSize?: number): void {
    this.state.pagination.pageIndex = page;
    if (pageSize !== undefined) this.state.pagination.pageSize = pageSize;
    this.update({ pagination: { ...this.state.pagination } });
  }

  /** 新增记录 */
  async add(type: string, data: Record<string, unknown>): Promise<ApiResponse<unknown>> {
    this.state.formLoading = true;
    this.update({ formLoading: true });
    try {
      return await this.api.page.add(type, data);
    } finally {
      this.state.formLoading = false;
      this.update({ formLoading: false });
    }
  }

  /** 编辑记录 */
  async update_(type: string, data: Record<string, unknown>): Promise<ApiResponse<unknown>> {
    this.state.formLoading = true;
    this.update({ formLoading: true });
    try {
      return await this.api.page.update(type, data);
    } finally {
      this.state.formLoading = false;
      this.update({ formLoading: false });
    }
  }

  /** 删除单条记录 */
  async remove(type: string, id: number | string): Promise<ApiResponse<unknown>> {
    return this.api.page.remove(type, id);
  }

  /** 恢复软删除单条记录 */
  async restore(type: string, id: number | string): Promise<ApiResponse<unknown>> {
    return this.api.page.restore(type, id);
  }

  /** 批量删除 */
  async deleteSelect(type: string, keys: (number | string)[]): Promise<ApiResponse<unknown>> {
    return this.api.page.deleteSelect(type, keys);
  }

  /** 按条件删除，params 为搜索条件（至少需携带一个参数，否则后端拒绝） */
  async deleteAll(type: string, params?: Record<string, unknown>): Promise<ApiResponse<unknown>> {
    return this.api.page.deleteAll(type, params);
  }

  /** 获取详情 */
  async getDetail<T = Record<string, unknown>>(type: string, id: number | string): Promise<T> {
    const res = await this.api.page.getDetail<T>(type, id);
    return res.data;
  }

  /** 获取导出 URL */
  getExportUrl(type: string, format: string): string {
    return buildExportUrl(type, format, this.api.client.defaults.baseURL);
  }

  /** 导入文件 */
  async importFile(type: string, file: File): Promise<ApiResponse<unknown>> {
    return this.api.page.importFile(type, file);
  }

  /** 上传文件，type 为实体路径前缀，options.id 为主记录主键（0=新增） */
  async uploadFile(type: string, file: File, options?: { id?: number; title?: string }): Promise<ApiResponse<Record<string, unknown>>> {
    return this.api.page.uploadFile(type, file, options);
  }

  /** 加载图表数据 */
  async loadChart(type: string): Promise<unknown[]> {
    try {
      const res = await this.api.page.getChartData(type);
      const chartList = res.data ?? [];
      this.state.chartList = chartList;
      this.update({ chartList });
      return chartList;
    } catch {
      return [];
    }
  }

  /** 字典查询 */
  async lookup(codes: string) {
    return this.api.page.lookup(codes);
  }
}

// 重新导出供适配器使用
export { FieldKind } from '@newlifex/api-core';
export type { DataField } from '@newlifex/api-core';
export type { FieldMapping } from '@newlifex/field-mapping';
export { resolveWidgets, resolveWidget } from '@newlifex/field-mapping';
export { buildExportUrl, EXPORT_FORMATS, resolveUrl, checkAuth, Auth } from '@newlifex/page-utils';
