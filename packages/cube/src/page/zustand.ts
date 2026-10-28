/**
 * 列表页 Zustand 适配器
 *
 * 将 PageLogic 桥接为 React hooks，供 React 系皮肤使用。
 *
 * @example
 * ```ts
 * import { createPageStore } from '@newlifex/cube/zustand';
 * import api from '@/api';
 * const usePageStore = createPageStore(api, '/Admin/User');
 * ```
 */

import { create, type StoreApi, type UseBoundStore } from 'zustand';
import type { CubeApi, ApiResponse } from '../api';
import { PageLogic, type PageState, type Pagination } from './index';
import type { FieldMapping } from '../field';

export interface ZustandPageState extends PageState {
  loadFields: () => Promise<void>;
  loadData: (searchParams?: Record<string, unknown>) => Promise<void>;
  setPagination: (page: number, pageSize?: number) => void;
  add: (data: Record<string, unknown>) => Promise<ApiResponse<unknown>>;
  update: (data: Record<string, unknown>) => Promise<ApiResponse<unknown>>;
  remove: (id: number | string) => Promise<ApiResponse<unknown>>;
  restore: (id: number | string) => Promise<ApiResponse<unknown>>;
  deleteSelect: (keys: (number | string)[]) => Promise<ApiResponse<unknown>>;
  /** 按条件删除全部，params 为搜索条件（不传则后端拒绝空条件删除） */
  deleteAll: (params?: Record<string, unknown>) => Promise<ApiResponse<unknown>>;
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
 * @param menuPermissions - 菜单权限（对象或返回对象的函数），用于推断 canAdd/canEdit 等
 * @param userPermission - 当前用户权限串（`menuId#flag` 逗号分隔）；提供时优先按 menuId + requiredPermissions 判定
 * @param isSystemAdmin - 是否系统管理员（动作权限直通）
 */
export function createPageStore(
  api: CubeApi,
  type: string,
  defaultPageSize = 20,
  menuPermissions?: Record<string, string> | (() => Record<string, string>),
  userPermission?: string | (() => string | undefined),
  isSystemAdmin?: boolean | (() => boolean),
): UseBoundStore<StoreApi<ZustandPageState>> {
  let logic: PageLogic;

  return create<ZustandPageState>((set) => {
    logic = new PageLogic({
      api,
      update: (partial) => set(partial),
      defaultPageSize,
      menuPermissions,
      userPermission,
      isSystemAdmin,
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

      // 操作方法
      loadFields: () => logic.loadFields(type),
      loadData: (searchParams) => logic.loadData(type, searchParams),
      setPagination: (page, pageSize) => logic.setPagination(page, pageSize),
      add: (data) => logic.add(type, data),
      update: (data) => logic.update_(type, data),
      remove: (id) => logic.remove(type, id),
      restore: (id) => logic.restore(type, id),
      deleteSelect: (keys) => logic.deleteSelect(type, keys),
      deleteAll: (params) => logic.deleteAll(type, params),
      getDetail: (id) => logic.getDetail(type, id),
      getExportUrl: (format) => logic.getExportUrl(type, format),
      importFile: (file) => logic.importFile(type, file),
      uploadFile: (file, category) => logic.uploadFile(type, file, category ? { title: category } : undefined),
      loadChart: () => logic.loadChart(type),
    };
  });
}
