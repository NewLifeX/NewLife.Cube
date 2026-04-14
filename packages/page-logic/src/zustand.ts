/**
 * @cube/page-logic/zustand — Zustand 适配器
 *
 * 将 PageLogic 桥接为 React hooks，供 React 系皮肤使用。
 *
 * @example
 * ```ts
 * import { createPageStore } from '@cube/page-logic/zustand';
 * import api from '@/api';
 * const usePageStore = createPageStore(api, '/Admin/User');
 * ```
 */

import { create, type StoreApi } from 'zustand';
import type { CubeApi, ApiResponse } from '@cube/api-core';
import { PageLogic, type PageState, type FieldMapping, type Pagination } from './index';

export interface ZustandPageState extends PageState {
  loadFields: () => Promise<void>;
  loadData: (searchParams?: Record<string, unknown>) => Promise<void>;
  setPagination: (page: number, pageSize?: number) => void;
  add: (data: Record<string, unknown>) => Promise<ApiResponse<unknown>>;
  update: (data: Record<string, unknown>) => Promise<ApiResponse<unknown>>;
  remove: (id: number | string) => Promise<ApiResponse<unknown>>;
  deleteSelect: (keys: (number | string)[]) => Promise<ApiResponse<unknown>>;
  deleteAll: () => Promise<ApiResponse<unknown>>;
  getDetail: <T = Record<string, unknown>>(id: number | string) => Promise<T>;
  getExportUrl: (format: string) => string;
  importFile: (file: File) => Promise<ApiResponse<unknown>>;
  uploadFile: (file: File, category?: string) => Promise<ApiResponse<Record<string, unknown>>>;
  loadChart: () => Promise<unknown[]>;
}

/**
 * 创建 Zustand 页面逻辑 Store
 *
 * @param api - CubeApi 实例
 * @param type - 路径前缀，如 `/Admin/User`
 * @param defaultPageSize - 默认每页大小
 */
export function createPageStore(
  api: CubeApi,
  type: string,
  defaultPageSize = 20,
): StoreApi<ZustandPageState> {
  let logic: PageLogic;

  return create<ZustandPageState>((set) => {
    logic = new PageLogic({
      api,
      update: (partial) => set(partial),
      defaultPageSize,
    });

    return {
      // 初始状态
      listFields: [],
      searchFields: [],
      addFields: [],
      editFields: [],
      detailFields: [],
      pkField: 'id',
      tableData: [],
      statData: null,
      pagination: { pageIndex: 1, pageSize: defaultPageSize, totalCount: 0 },
      chartList: [],
      loading: false,
      formLoading: false,

      // 操作方法
      loadFields: () => logic.loadFields(type),
      loadData: (searchParams) => logic.loadData(type, searchParams),
      setPagination: (page, pageSize) => logic.setPagination(page, pageSize),
      add: (data) => logic.add(type, data),
      update: (data) => logic.update_(type, data),
      remove: (id) => logic.remove(type, id),
      deleteSelect: (keys) => logic.deleteSelect(type, keys),
      deleteAll: () => logic.deleteAll(type),
      getDetail: (id) => logic.getDetail(type, id),
      getExportUrl: (format) => logic.getExportUrl(type, format),
      importFile: (file) => logic.importFile(type, file),
      uploadFile: (file, category) => logic.uploadFile(file, category),
      loadChart: () => logic.loadChart(type),
    };
  });
}
