/**
 * @newlifex/page-logic — 魔方前端列表页业务编排逻辑（框架无关核心）
 *
 * 将字段加载、列表查询、CRUD、导入导出、图表数据等列表页通用逻辑
 * 封装为框架无关的编排类，各框架通过适配器桥接到具体状态管理。
 */

import type { CubeApi, DataField, ApiResponse, PageParams, PageSetting } from '@newlifex/api-core';
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
  /** 全部可用列表字段（应用用户列配置前，供列设置面板使用） */
  allListFields: FieldMapping[];
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
  /** 页面设置（由 GetPage 返回的 setting 字段） */
  pageSetting: PageSetting | null;
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
}

/** 状态变更回调 */
export type PageStateUpdater = (partial: Partial<PageState>) => void;

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

  constructor(options: PageLogicOptions) {
    this.api = options.api;
    this.update = options.update;
    const mp = options.menuPermissions;
    this.getMenuPermissions =
      typeof mp === 'function'
        ? (mp as () => Record<string, string>)
        : () => (mp as Record<string, string> | undefined) ?? {};
    this.state = {
      listFields: [],
      allListFields: [],
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
      canAdd: true,
      canEdit: true,
      canDelete: true,
      canExport: true,
      canImport: true,
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

    const listData = pageMeta.list ?? pageMeta.fields?.list ?? [];
    const allListData = pageMeta.allList ?? listData;
    const addData = pageMeta.addForm ?? pageMeta.fields?.form?.addForm ?? [];
    const editData = pageMeta.editForm ?? pageMeta.fields?.form?.editForm ?? [];
    const detailData = pageMeta.detail ?? pageMeta.fields?.form?.detail ?? [];
    const searchData = pageMeta.search ?? pageMeta.fields?.search ?? [];

    const listFields = resolveWidgets(listData);
    const allListFields = resolveWidgets(allListData);
    const searchFields = resolveWidgets(searchData);
    const addFields = resolveWidgets(addData);
    const editFields = resolveWidgets(editData);
    const detailFields = resolveWidgets(detailData);

    // 推断主键字段
    const pk = listData.find((f) => f.primaryKey);
    const pkField = pk?.name ?? 'id';

    // 提取页面设置
    const pageSetting = pageMeta.setting ?? pageMeta.pageSetting ?? null;

    // 计算权限标志（菜单权限 + 页面设置双重限制）
    // 权限码（2 的幂，对应 Auth 常量）优先按 key 匹配，兼容不同后端的权限标签文案（新增/添加、编辑/修改）
    const perms = this.getMenuPermissions();
    const hasPerm = (codes: number[], labels: string[], keys: string[]) =>
      keys.some((k) => perms[k] !== undefined) ||
      codes.some((c) => perms[String(c)] !== undefined) ||
      Object.values(perms).some((v) => labels.includes(v));
    const hasAdd = hasPerm([2], ['新增', '添加', 'Add'], ['Add']);
    const hasEdit = hasPerm([4], ['编辑', '修改', 'Edit'], ['Edit']);
    const hasDel = hasPerm([8], ['删除', 'Delete'], ['Delete']);
    // 导出/导入的后端授权：NewLife.Cube ExportFile=[EntityAuthorize(Detail=1)]、ImportFile=[EntityAuthorize(Insert=2)]，
    // 部分后端可能定义 16/32，故两种授权码都兼容
    const hasExport = hasPerm([1, 16], ['查看', '导出', 'Export', 'Detail'], ['Detail', 'Export']);
    const hasImport = hasPerm([2, 32], ['添加', '新增', '导入', 'Import', 'Insert'], ['Insert', 'Import']);
    // 无权限配置时（菜单未配 permissions）默认允许
    const noPermConfig = Object.keys(perms).length === 0;

    const canAdd = (noPermConfig || hasAdd) && (pageSetting?.enableAdd !== false) && (pageSetting?.isReadOnly !== true);
    const canEdit = (noPermConfig || hasEdit) && (pageSetting?.isReadOnly !== true);
    const canDelete = (noPermConfig || hasDel) && (pageSetting?.isReadOnly !== true);
    const canExport = noPermConfig || hasExport;
    const canImport = noPermConfig || hasImport;

    this.state.listFields = listFields;
    this.state.allListFields = allListFields;
    this.state.searchFields = searchFields;
    this.state.addFields = addFields;
    this.state.editFields = editFields;
    this.state.detailFields = detailFields;
    this.state.pkField = pkField;
    this.state.pageSetting = pageSetting;
    this.state.canAdd = canAdd;
    this.state.canEdit = canEdit;
    this.state.canDelete = canDelete;
    this.state.canExport = canExport;
    this.state.canImport = canImport;

    this.update({ listFields, allListFields, searchFields, addFields, editFields, detailFields, pkField, pageSetting, canAdd, canEdit, canDelete, canExport, canImport });
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
