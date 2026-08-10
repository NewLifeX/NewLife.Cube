<script setup lang="ts">
/**
 * 列表页（默认模板）
 *
 * 自动模式下从后端 GetPage 拉取 list / search / addForm / editForm 字段元数据，
 * 归一为 `FieldMeta[]` 交给子组件：
 *   - ListTableContent   按 resolveListControl 渲染单元格
 *   - ListSearchBar      按 resolveSearchControl 渲染搜索控件
 *   - ListFormDialog（命令式弹窗）按 resolveControl 渲染表单
 *
 * 新增/编辑弹窗使用命令式 openListFormDialog，无需在模板中声明 el-dialog。
 */
import {
  inject,
  provide,
  defineAsyncComponent,
  ref,
  reactive,
  computed,
  onMounted,
  watch,
} from 'vue';
import type { Component } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import request from '@newlifex/cube-vue/core/utils/request';
import {
  ListPageHeaderKey,
  ListSearchBarKey,
  ListToolbarKey,
  ListTableContentKey,
  ListPaginationKey,
  ListPageFooterKey,
  PageSectionRegistryKey,
  SectionKeyMap,
} from '@newlifex/cube-vue/core/composables/useSections';

import DefaultListPageHeader from '@newlifex/cube-vue/core/views/components/ListPageHeader.vue';
import DefaultListSearchBar from '@newlifex/cube-vue/core/views/components/ListSearchBar.vue';
import DefaultListToolbar from '@newlifex/cube-vue/core/views/components/ListToolbar.vue';
import DefaultListTableContent from '@newlifex/cube-vue/core/views/components/ListTableContent.vue';
import DefaultListPagination from '@newlifex/cube-vue/core/views/components/ListPagination.vue';
import DefaultListPageFooter from '@newlifex/cube-vue/core/views/components/ListPageFooter.vue';
// 图表弹窗懒加载（按需载入 echarts，仅在点击「图表」时加载）
const ChartDialog = defineAsyncComponent(
  () => import('@newlifex/cube-vue/core/views/components/ListChartDialog.vue'),
);
import { routeToApiPrefix, getValueByKey } from '@newlifex/cube-vue/core/utils/url';
import { openListFormDialog } from './modals/list-form-dialog/openListFormDialog';
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field';

/** 后端下发的原始字段（DataField 归一结构） */
interface BackendField {
  name: string;
  displayName: string;
  description?: string;
  typeName: string;
  itemType?: string;
  length?: number;
  nullable?: boolean;
  primaryKey?: boolean;
  readOnly?: boolean;
  mapField?: string;
  lovCode?: string;
  multiple?: boolean;
}

interface PageMeta {
  setting: {
    navView?: string;
    enableNavbar?: boolean;
    enableToolbar?: boolean;
    enableAdd?: boolean;
    enableKey?: boolean;
    enableSelect?: boolean;
    enableFooter?: boolean;
    isReadOnly?: boolean;
    enableTableDoubleClick?: boolean;
    orderByKey?: boolean;
    doubleDelete?: boolean;
  };
  list: BackendField[];
  addForm: BackendField[];
  editForm: BackendField[];
  detail: BackendField[];
  search: BackendField[];
}

interface Props {
  title?: string;
  subtitle?: string;
  data?: Record<string, unknown>[];
  loading?: boolean;
  total?: number;
  currentPage?: number;
  pageSize?: number;
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  total: 0,
  currentPage: 1,
  pageSize: 20,
});

const emit = defineEmits<{
  search: [params: Record<string, string>];
  reset: [];
  new: [];
  delete: [];
  export: [];
  import: [];
  chart: [];
  refresh: [];
  'update:currentPage': [page: number];
  'update:pageSize': [size: number];
}>();

const route = useRoute();

const registry = inject(
  PageSectionRegistryKey,
  {} as Record<string, Record<string, () => Promise<{ default: unknown }>>>,
);
/** 将路由路径转为小写以匹配 Section 注册表键（文件系统路径为小写） */
const normalizedRoutePath = route.path.toLowerCase();
const pageOverrides = registry[normalizedRoutePath] ?? {};

for (const [name, loader] of Object.entries(pageOverrides)) {
  const key = SectionKeyMap[name];
  if (key) {
    provide(key, defineAsyncComponent(loader as () => Promise<{ default: Component }>));
  }
}

const PageHeaderComp = inject(ListPageHeaderKey, DefaultListPageHeader);
const SearchBarComp = inject(ListSearchBarKey, DefaultListSearchBar);
const ToolbarComp = inject(ListToolbarKey, DefaultListToolbar);
const TableContentComp = inject(ListTableContentKey, DefaultListTableContent);
const PaginationComp = inject(ListPaginationKey, DefaultListPagination);
const PageFooterComp = inject(ListPageFooterKey, DefaultListPageFooter);

const apiPrefix = computed(() => routeToApiPrefix(route.path));

/** 导入：隐藏的文件选择框 */
const importInput = ref<HTMLInputElement | null>(null);
/** 图表弹窗状态与数据（GetChartData 返回的 ECharts 配置数组） */
const chartVisible = ref(false);
const chartList = ref<Record<string, unknown>[]>([]);

/** 后端字段 → 统一 FieldMeta */
function toFieldMeta(f: BackendField): FieldMeta {
  return {
    name: f.name,
    displayName: f.displayName,
    description: f.description,
    typeName: f.typeName,
    itemType: f.itemType,
    length: f.length,
    nullable: f.nullable,
    primaryKey: f.primaryKey,
    readOnly: f.readOnly,
    lovCode: f.lovCode,
    multiple: f.multiple,
  };
}

/** 后端字段 → 表单 FieldMeta 数组（过滤主键与只读字段） */
function backendFieldsToFormFields(fields: BackendField[]): FieldMeta[] {
  return fields.filter((f) => !f.primaryKey && !f.readOnly).map(toFieldMeta);
}

const auto = computed(() => !props.data);

const internalLoading = ref(false);
const internalList = ref<Record<string, unknown>[]>([]);
const internalTotal = ref(0);
const internalPage = ref(1);
const internalPageSize = ref(20);
const pageMeta = ref<PageMeta | null>(null);
const filterData = reactive<Record<string, unknown>>({});

/** 列表字段（含 Guid 作为只读文本展示，但不进搜索） */
const computedListFields = computed<FieldMeta[]>(() => {
  if (!pageMeta.value) return [];
  return pageMeta.value.list.map(toFieldMeta);
});

/** 搜索字段（过滤主键与 Guid，永不进搜索） */
const computedSearchFields = computed<FieldMeta[]>(() => {
  if (!pageMeta.value) return [];
  return pageMeta.value.search
    .filter((f) => !f.primaryKey && f.typeName !== 'Guid')
    .map(toFieldMeta);
});

function handleNew() {
  if (auto.value) {
    openListFormDialog({
      title: '新增',
      fields: backendFieldsToFormFields(pageMeta.value!.addForm ?? []),
      apiPrefix: apiPrefix.value,
      mode: 'add',
      routePath: route.path,
      onSuccess: () => fetchList(),
    });
  } else {
    emit('new');
  }
}

function handleEditRow(row: Record<string, unknown>) {
  if (auto.value) {
    openListFormDialog({
      title: '编辑',
      fields: backendFieldsToFormFields(pageMeta.value!.editForm ?? pageMeta.value!.addForm ?? []),
      modelValue: { ...row },
      apiPrefix: apiPrefix.value,
      mode: 'edit',
      routePath: route.path,
      onSuccess: () => fetchList(),
    });
  }
}

async function handleDeleteRow(row: Record<string, unknown>) {
  try {
    await ElMessageBox.confirm('确定要删除这条数据吗？', '确认删除', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning',
    });
    const id = getValueByKey(row, 'id') ?? getValueByKey(row, 'ID');
    await request({ url: `${apiPrefix.value}/${id}`, method: 'delete' });
    ElMessage.success('删除成功');
    await fetchList();
  } catch (err: any) {
    if (err !== 'cancel') {
      const msg = err?.response?.data?.message || err?.message || '删除失败';
      ElMessage.error(msg);
    }
  }
}

async function fetchPageMeta() {
  if (!auto.value) return;
  try {
    const url = apiPrefix.value + '/GetPage';
    console.log('[DefaultList] GetPage:', url);
    const res = await request({ url, method: 'get' });
    pageMeta.value = (res as any).data ?? (res as unknown as PageMeta);
  } catch (err) {
    console.error('[DefaultList] GetPage failed:', err);
  }
}

async function fetchList() {
  if (!auto.value) return;
  internalLoading.value = true;
  try {
    const params: Record<string, unknown> = {
      pageIndex: internalPage.value,
      pageSize: internalPageSize.value,
    };
    for (const [k, v] of Object.entries(filterData)) {
      if (v !== '' && v !== null && v !== undefined) {
        params[k] = v;
      }
    }
    const url = apiPrefix.value;
    console.log('[DefaultList] Index:', url, params);
    const res: any = await request({ url, method: 'get', params });
    if (res && res.data && res.page) {
      internalList.value = res.data;
      internalTotal.value = res.page.totalCount ?? res.page.total ?? 0;
    } else if (Array.isArray(res)) {
      internalList.value = res;
      internalTotal.value = res.length;
    } else {
      internalList.value = res && res.data ? res.data : (res ?? []);
      internalTotal.value = internalList.value.length;
    }
  } catch (err) {
    console.error('[DefaultList] Index failed:', err);
  } finally {
    internalLoading.value = false;
  }
}

function handleSearch(params: Record<string, string>) {
  if (auto.value) {
    Object.assign(filterData, params);
    internalPage.value = 1;
    fetchList();
  } else {
    emit('search', params);
  }
}

function handleReset() {
  if (auto.value) {
    for (const key in filterData) {
      delete filterData[key];
    }
    internalPage.value = 1;
    fetchList();
  } else {
    emit('reset');
  }
}

function handleDeleteRowTable(row: Record<string, unknown>) {
  handleDeleteRow(row);
}

function handleDelete() {
  if (auto.value) {
    ElMessage.info('请使用表格行操作进行删除');
  } else {
    emit('delete');
  }
}

async function handleRefresh() {
  if (auto.value) {
    await fetchList();
  } else {
    emit('refresh');
  }
}

function handleExport() {
  if (!auto.value) {
    emit('export');
    return;
  }
  exportData();
}

/** 触发浏览器下载（blob） */
function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

/** 根据响应 content-type 推断导出文件扩展名 */
function blobExt(contentType: string): string {
  if (contentType.includes('csv')) return '.csv';
  if (contentType.includes('json')) return '.json';
  if (contentType.includes('xml')) return '.xml';
  return '.bin';
}

/** 生成时间戳文件名片段（yyyyMMddHHmmss） */
function dateStamp(): string {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}${p(d.getHours())}${p(d.getMinutes())}${p(d.getSeconds())}`;
}

/** 导出：GET /{type}/ExportFile，返回文件流后触发下载 */
async function exportData() {
  try {
    const blob = (await request.get(apiPrefix.value + '/ExportFile', {
      responseType: 'blob',
    })) as unknown as Blob;
    const name = apiPrefix.value.split('/').filter(Boolean).pop() || 'export';
    downloadBlob(blob, `${name}_${dateStamp()}${blobExt(blob.type)}`);
    ElMessage.success('导出成功');
  } catch (err: any) {
    const msg = err?.message || '导出失败';
    ElMessage.error(msg);
  }
}

/** 导入：打开文件选择框（非自动模式交父组件处理） */
function handleImport() {
  if (!auto.value) {
    emit('import');
    return;
  }
  importInput.value?.click();
}

/** 导入：POST /{type}/ImportFile，上传选中的文件 */
async function onImportFileChange(e: Event) {
  const input = e.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = ''; // 重置，允许重复选择同一文件
  if (!file) return;
  const formData = new FormData();
  formData.append('file', file);
  try {
    await request.post(apiPrefix.value + '/ImportFile', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    ElMessage.success('导入成功');
    await fetchList();
  } catch (err: any) {
    const msg = err?.message || '导入失败';
    ElMessage.error(msg);
  }
}

/** 图表：GET /{type}/GetChartData，返回 ECharts 配置数组 */
async function handleChart() {
  if (!auto.value) {
    emit('chart');
    return;
  }
  try {
    const res: any = await request.get(apiPrefix.value + '/GetChartData');
    const data = res?.data ?? res;
    chartList.value = Array.isArray(data) ? data : [];
    chartVisible.value = true;
  } catch (err: any) {
    const msg = err?.message || '获取图表失败';
    ElMessage.error(msg);
  }
}

function handlePageChange(page: number) {
  if (auto.value) {
    internalPage.value = page;
    fetchList();
  } else {
    emit('update:currentPage', page);
  }
}

function handlePageSizeChange(size: number) {
  if (auto.value) {
    internalPageSize.value = size;
    internalPage.value = 1;
    fetchList();
  } else {
    emit('update:pageSize', size);
  }
}

const renderData = computed(() => (auto.value ? internalList.value : (props.data ?? [])));
const renderTotal = computed(() => (auto.value ? internalTotal.value : props.total));
const renderPage = computed(() => (auto.value ? internalPage.value : props.currentPage));
const renderPageSize = computed(() => (auto.value ? internalPageSize.value : props.pageSize));
const renderLoading = computed(() => (auto.value ? internalLoading.value : props.loading));

watch(
  () => route.path,
  () => {
    if (auto.value) {
      internalPage.value = 1;
      pageMeta.value = null;
      internalList.value = [];
      fetchPageMeta();
      fetchList();
    }
  },
);

onMounted(async () => {
  if (auto.value) {
    await fetchPageMeta();
    await fetchList();
  }
});
</script>

<template>
  <div class="list-page">
    <slot name="header">
      <component :is="PageHeaderComp" :title="title" :subtitle="subtitle" />
    </slot>
    <div class="lp-body">
      <div class="lp-card">
        <div class="lp-card-header">
          <slot name="search">
            <div class="lp-search-area">
              <component
                :is="SearchBarComp"
                :fields="computedSearchFields"
                @search="handleSearch"
                @reset="handleReset"
              />
            </div>
          </slot>
          <div class="lp-divider"></div>
          <slot name="toolbar">
            <div class="lp-toolbar-area">
              <component
                :is="ToolbarComp"
                @new="handleNew"
                @delete="handleDelete"
                @export="handleExport"
                @import="handleImport"
                @chart="handleChart"
                @refresh="handleRefresh"
              />
            </div>
          </slot>
        </div>
        <div class="lp-card-body">
          <slot name="table">
            <component
              :is="TableContentComp"
              :fields="computedListFields"
              :data="renderData"
              :loading="renderLoading"
              @edit="handleEditRow"
              @delete="handleDeleteRowTable"
            />
          </slot>
        </div>
        <div class="lp-card-footer">
          <slot name="pagination">
            <component
              :is="PaginationComp"
              :total="renderTotal"
              :current-page="renderPage"
              :page-size="renderPageSize"
              @update:current-page="handlePageChange"
              @update:page-size="handlePageSizeChange"
            />
          </slot>
        </div>
      </div>
      <slot name="footer">
        <component :is="PageFooterComp" />
      </slot>
    </div>
    <!-- 导入：隐藏的文件选择框 -->
    <input
      ref="importInput"
      type="file"
      accept=".xls,.xlsx,.csv,.json,.zip"
      style="display: none"
      @change="onImportFileChange"
    />

    <!-- 图表弹窗（懒加载 echarts） -->
    <ChartDialog v-model:visible="chartVisible" :charts="chartList" />
  </div>
</template>

<style lang="scss" scoped>
.list-page {
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.lp-body {
  flex: 1;
  overflow-y: auto;
  padding: var(--el-main-padding, 24px);
  background: var(--el-bg-color-page);
}

.lp-card {
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  box-shadow: var(--el-box-shadow-light);
  overflow: hidden;
  animation: fadeInUp 0.3s ease;
}

.lp-card-header {
  padding: 16px 20px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.lp-search-area {
  margin-bottom: 0;
}

.lp-divider {
  height: 1px;
  background: var(--el-border-color-light);
  margin: 12px 0;
}

.lp-toolbar-area {
  margin-top: 0;
}

.lp-card-body {
  min-height: 200px;
}

.lp-card-footer {
  padding: 12px 20px;
  border-top: 1px solid var(--el-border-color-light);
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

:deep(.el-dialog) {
  border-radius: var(--el-border-radius-base);
  background: var(--el-bg-color-overlay);
  box-shadow: var(--el-box-shadow-dark);
}

:deep(.el-dialog__header) {
  padding: 16px 20px;
  border-bottom: 1px solid var(--el-border-color-light);
}

:deep(.el-dialog__title) {
  font-weight: 600;
  color: var(--el-text-color-primary);
}

:deep(.el-dialog__body) {
  padding: 20px;
  color: var(--el-text-color-regular);
}

@media (max-width: 768px) {
  .lp-body {
    padding: 16px;
  }

  .lp-card {
    border-radius: var(--el-border-radius-small);
  }

  .lp-card-header {
    padding: 12px 16px;
  }

  .lp-card-footer {
    padding: 10px 16px;
  }
}

@media (max-width: 560px) {
  .lp-body {
    padding: 12px;
  }

  .lp-card-header {
    padding: 10px 12px;
  }

  .lp-card-footer {
    padding: 8px 12px;
  }
}
</style>
