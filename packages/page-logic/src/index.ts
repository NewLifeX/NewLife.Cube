/**
 * @cube/page-logic — 魔方前端列表页业务编排逻辑（框架无关核心）
 *
 * 将字段加载、列表查询、CRUD、导入导出、图表数据等列表页通用逻辑
 * 封装为框架无关的编排类，各框架通过适配器桥接到具体状态管理。
 */

import type { CubeApi, DataField, ApiResponse, PageParams } from '@cube/api-core';
import { resolveWidgets, type FieldMapping } from '@cube/field-mapping';
import { buildExportUrl } from '@cube/page-utils';

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
}

// ======================== 核心逻辑 ========================

/**
 * 列表页业务编排逻辑核心类
 */
export class PageLogic {
  private api: CubeApi;
  private update: PageStateUpdater;
  private state: PageState;

  constructor(options: PageLogicOptions) {
    this.api = options.api;
    this.update = options.update;
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
    };
  }

  /** 当前状态快照 */
  getState(): Readonly<PageState> {
    return this.state;
  }

  /** 并行加载 5 类字段元数据 */
  async loadFields(type: string): Promise<void> {
    const pageRes = await this.api.page.getPage(type);
    const pageMeta = pageRes.data ?? {};

    const listData = pageMeta.list ?? pageMeta.fields?.list ?? [];
    const addData = pageMeta.addForm ?? pageMeta.fields?.form?.addForm ?? [];
    const editData = pageMeta.editForm ?? pageMeta.fields?.form?.editForm ?? [];
    const detailData = pageMeta.detail ?? pageMeta.fields?.form?.detail ?? [];
    const searchData = pageMeta.search ?? pageMeta.fields?.search ?? [];

    const listFields = resolveWidgets(listData);
    const searchFields = resolveWidgets(searchData);
    const addFields = resolveWidgets(addData);
    const editFields = resolveWidgets(editData);
    const detailFields = resolveWidgets(detailData);

    // 推断主键字段
    const pk = listData.find((f) => f.primaryKey);
    const pkField = pk?.name ?? 'id';

    this.state.listFields = listFields;
    this.state.searchFields = searchFields;
    this.state.addFields = addFields;
    this.state.editFields = editFields;
    this.state.detailFields = detailFields;
    this.state.pkField = pkField;

    this.update({ listFields, searchFields, addFields, editFields, detailFields, pkField });
  }

  /** 加载列表数据（分页 + 搜索） */
  async loadData(type: string, searchParams?: Record<string, unknown>): Promise<void> {
    this.state.loading = true;
    this.update({ loading: true });

    try {
      const params: PageParams = {
        pageIndex: this.state.pagination.pageIndex - 1, // 后端从 0 开始
        pageSize: this.state.pagination.pageSize,
        ...searchParams,
      };

      const res = await this.api.page.getList(type, params);

      const tableData = (res.data ?? []) as Record<string, unknown>[];
      const pagination = { ...this.state.pagination };

      if (res.page) {
        pagination.totalCount = res.page.totalCount;
        pagination.pageIndex = res.page.pageIndex + 1; // 转为从 1 开始
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
export { FieldKind } from '@cube/api-core';
export type { DataField } from '@cube/api-core';
export type { FieldMapping } from '@cube/field-mapping';
export { resolveWidgets, resolveWidget } from '@cube/field-mapping';
export { buildExportUrl, EXPORT_FORMATS, resolveUrl, checkAuth, Auth } from '@cube/page-utils';
