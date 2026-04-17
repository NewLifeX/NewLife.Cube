/**
 * @cube/page-logic/pinia — Pinia 适配器
 *
 * 将 PageLogic 桥接为 Pinia composable，供 Vue 系皮肤使用。
 *
 * @example
 * ```ts
 * import { usePageLogic } from '@cube/page-logic/pinia';
 * const page = usePageLogic(api, '/Admin/User');
 * await page.loadFields();
 * await page.loadData();
 * ```
 */

import { ref, reactive, type Ref } from 'vue';
import type { CubeApi, ApiResponse, PageSetting } from '@cube/api-core';
import { PageLogic, type FieldMapping, type Pagination } from './index';

export interface PiniaPageLogic {
  listFields: Ref<FieldMapping[]>;
  searchFields: Ref<FieldMapping[]>;
  addFields: Ref<FieldMapping[]>;
  editFields: Ref<FieldMapping[]>;
  detailFields: Ref<FieldMapping[]>;
  pkField: Ref<string>;
  tableData: Ref<Record<string, unknown>[]>;
  statData: Ref<Record<string, unknown> | null>;
  pagination: Pagination;
  chartList: Ref<unknown[]>;
  loading: Ref<boolean>;
  formLoading: Ref<boolean>;
  pageSetting: Ref<PageSetting | null>;
  canAdd: Ref<boolean>;
  canEdit: Ref<boolean>;
  canDelete: Ref<boolean>;
  canExport: Ref<boolean>;
  canImport: Ref<boolean>;

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
 * 创建 Pinia/Vue composable 风格的 PageLogic
 *
 * @param api - CubeApi 实例
 * @param type - 路径前缀，如 `/Admin/User`
 * @param defaultPageSize - 默认每页大小
 * @param menuPermissions - 来自 auth-logic 的菜单权限映射，用于推断 canAdd/canEdit 等
 */
export function usePageLogic(api: CubeApi, type: string, defaultPageSize = 20, menuPermissions?: Record<string, string>): PiniaPageLogic {
  const listFields = ref<FieldMapping[]>([]);
  const searchFields = ref<FieldMapping[]>([]);
  const addFields = ref<FieldMapping[]>([]);
  const editFields = ref<FieldMapping[]>([]);
  const detailFields = ref<FieldMapping[]>([]);
  const pkField = ref('id');
  const tableData = ref<Record<string, unknown>[]>([]);
  const statData = ref<Record<string, unknown> | null>(null);
  const pagination = reactive<Pagination>({ pageIndex: 1, pageSize: defaultPageSize, totalCount: 0 });
  const chartList = ref<unknown[]>([]);
  const loading = ref(false);
  const formLoading = ref(false);
  const pageSetting = ref<PageSetting | null>(null);
  const canAdd = ref(true);
  const canEdit = ref(true);
  const canDelete = ref(true);
  const canExport = ref(true);
  const canImport = ref(true);

  const logic = new PageLogic({
    api,
    update: (partial) => {
      if (partial.listFields !== undefined) listFields.value = partial.listFields;
      if (partial.searchFields !== undefined) searchFields.value = partial.searchFields;
      if (partial.addFields !== undefined) addFields.value = partial.addFields;
      if (partial.editFields !== undefined) editFields.value = partial.editFields;
      if (partial.detailFields !== undefined) detailFields.value = partial.detailFields;
      if (partial.pkField !== undefined) pkField.value = partial.pkField;
      if (partial.tableData !== undefined) tableData.value = partial.tableData;
      if (partial.statData !== undefined) statData.value = partial.statData;
      if (partial.pagination !== undefined) Object.assign(pagination, partial.pagination);
      if (partial.chartList !== undefined) chartList.value = partial.chartList;
      if (partial.loading !== undefined) loading.value = partial.loading;
      if (partial.formLoading !== undefined) formLoading.value = partial.formLoading;
      if (partial.pageSetting !== undefined) pageSetting.value = partial.pageSetting;
      if (partial.canAdd !== undefined) canAdd.value = partial.canAdd;
      if (partial.canEdit !== undefined) canEdit.value = partial.canEdit;
      if (partial.canDelete !== undefined) canDelete.value = partial.canDelete;
      if (partial.canExport !== undefined) canExport.value = partial.canExport;
      if (partial.canImport !== undefined) canImport.value = partial.canImport;
    },
    defaultPageSize,
    menuPermissions,
  });

  return {
    listFields,
    searchFields,
    addFields,
    editFields,
    detailFields,
    pkField,
    tableData,
    statData,
    pagination,
    chartList,
    loading,
    formLoading,
    pageSetting,
    canAdd,
    canEdit,
    canDelete,
    canExport,
    canImport,

    loadFields: () => logic.loadFields(type),
    loadData: (searchParams) => logic.loadData(type, searchParams),
    setPagination: (page, pageSize) => {
      logic.setPagination(page, pageSize);
      pagination.pageIndex = page;
      if (pageSize !== undefined) pagination.pageSize = pageSize;
    },
    add: (data) => logic.add(type, data),
    update: (data) => logic.update_(type, data),
    remove: (id) => logic.remove(type, id),
    deleteSelect: (keys) => logic.deleteSelect(type, keys),
    deleteAll: () => logic.deleteAll(type),
    getDetail: (id) => logic.getDetail(type, id),
    getExportUrl: (format) => logic.getExportUrl(type, format),
    importFile: (file) => logic.importFile(type, file),
    uploadFile: (file, category) => logic.uploadFile(type, file, category ? { title: category } : undefined),
    loadChart: () => logic.loadChart(type),
  };
}
