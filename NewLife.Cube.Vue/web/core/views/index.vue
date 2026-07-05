<script setup lang="ts">
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
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import request from 'cube-front/core/utils/request';
import {
  ListPageHeaderKey,
  ListSearchBarKey,
  ListToolbarKey,
  ListTableContentKey,
  ListPaginationKey,
  ListPageFooterKey,
  PageSectionRegistryKey,
  SectionKeyMap,
} from 'cube-front/core/composables/useSections';

import DefaultListPageHeader from 'cube-front/core/views/components/ListPageHeader.vue';
import DefaultListSearchBar from 'cube-front/core/views/components/ListSearchBar.vue';
import DefaultListToolbar from 'cube-front/core/views/components/ListToolbar.vue';
import DefaultListTableContent from 'cube-front/core/views/components/ListTableContent.vue';
import DefaultListPagination from 'cube-front/core/views/components/ListPagination.vue';
import DefaultListPageFooter from 'cube-front/core/views/components/ListPageFooter.vue';
import FormPage from 'cube-front/core/views/form.vue';
import { routeToApiPrefix, getValueByKey } from 'cube-front/core/utils/url';

interface FormField {
  key: string;
  label: string;
  type: 'text' | 'email' | 'tel' | 'select' | 'textarea' | 'radio';
  required?: boolean;
  fullWidth?: boolean;
  placeholder?: string;
  options?: Array<{ value: string; label: string }>;
}

const TYPE_TO_SEARCH_TYPE: Record<string, 'text' | 'select'> = {
  String: 'text',
  Int32: 'text',
  Int64: 'text',
  Decimal: 'text',
  Double: 'text',
  Boolean: 'select',
};

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
}

interface BackendSearchField extends BackendField {
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
  search: BackendSearchField[];
}

interface Column {
  key: string;
  label: string;
  width?: string;
  align?: 'left' | 'center' | 'right';
  mono?: boolean;
}

interface SearchFieldItem {
  key: string;
  label: string;
  type: 'text' | 'select';
  options?: Array<{ value: string | number; label: string }>;
}

interface Props {
  title?: string;
  subtitle?: string;
  columns?: Column[];
  data?: Record<string, unknown>[];
  loading?: boolean;
  total?: number;
  currentPage?: number;
  pageSize?: number;
  searchFields?: SearchFieldItem[];
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
  refresh: [];
  'update:currentPage': [page: number];
  'update:pageSize': [size: number];
}>();

const route = useRoute();
const router = useRouter();

const registry = inject(
  PageSectionRegistryKey,
  {} as Record<string, Record<string, () => Promise<{ default: unknown }>>>,
);
const pageOverrides = registry[route.path] ?? {};

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

// 弹窗表单相关
const dialogVisible = ref(false);
const dialogTitle = ref('新增');
const dialogMode = ref<'add' | 'edit'>('add');
const dialogFormData = ref<Record<string, unknown>>({});
const dialogFields = ref<FormField[]>([]);
const isSubmitting = ref(false);

const TYPE_TO_FORM_TYPE: Record<string, FormField['type']> = {
  String: 'text',
  Int32: 'text',
  Int64: 'text',
  Decimal: 'text',
  Double: 'text',
  Boolean: 'select',
  DateTime: 'text',
};

function backendFieldsToFormFields(fields: BackendField[]): FormField[] {
  return fields
    .filter((f) => !f.primaryKey && !f.readOnly)
    .map((f) => {
      const type = TYPE_TO_FORM_TYPE[f.typeName] ?? 'text';
      const item: FormField = { key: f.name, label: f.displayName || f.name, type };
      if (!f.nullable && !f.primaryKey) {
        item.required = true;
      }
      if (f.length && f.length > 50 && type === 'text') {
        item.type = 'textarea';
      }
      if (f.description) {
        item.placeholder = f.description;
      }
      if (f.typeName === 'Boolean') {
        item.type = 'select';
        item.options = [
          { value: 'true', label: '是' },
          { value: 'false', label: '否' },
        ];
      }
      return item;
    });
}

function openDialog(mode: 'add' | 'edit', row?: Record<string, unknown>) {
  dialogMode.value = mode;
  dialogTitle.value = mode === 'add' ? '新增' : '编辑';
  dialogFormData.value = mode === 'edit' && row ? { ...row } : {};
  if (pageMeta.value) {
    const sourceFields = mode === 'add' ? pageMeta.value.addForm : pageMeta.value.editForm;
    dialogFields.value = backendFieldsToFormFields(sourceFields || pageMeta.value.addForm || []);
  }
  dialogVisible.value = true;
}

function closeDialog() {
  dialogVisible.value = false;
}

async function submitDialog() {
  if (isSubmitting.value) return;
  isSubmitting.value = true;
  try {
    const url = apiPrefix.value;
    const data = { ...dialogFormData.value };
    if (dialogMode.value === 'edit') {
      await request({ url, method: 'put', data });
      ElMessage.success('更新成功');
    } else {
      await request({ url, method: 'post', data });
      ElMessage.success('新增成功');
    }
    closeDialog();
    await fetchList();
  } catch (err: any) {
    const msg = err?.response?.data?.message || err?.message || '操作失败';
    ElMessage.error(msg);
  } finally {
    isSubmitting.value = false;
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

function handleDialogUpdate(val: Record<string, unknown>) {
  dialogFormData.value = val;
}
const auto = computed(() => !props.columns);

const internalLoading = ref(false);
const internalList = ref<Record<string, unknown>[]>([]);
const internalTotal = ref(0);
const internalPage = ref(1);
const internalPageSize = ref(20);
const pageMeta = ref<PageMeta | null>(null);
const filterData = reactive<Record<string, unknown>>({});

const computedSearchFields = computed<SearchFieldItem[]>(() => {
  if (props.searchFields) return props.searchFields;
  if (!pageMeta.value) return [];
  return pageMeta.value.search
    .filter((f) => !f.primaryKey)
    .map((f) => {
      const type = TYPE_TO_SEARCH_TYPE[f.typeName] ?? 'text';
      const item: SearchFieldItem = { key: f.name, label: f.displayName, type };
      if (f.typeName === 'Boolean') {
        item.options = [
          { value: 'true', label: '是' },
          { value: 'false', label: '否' },
        ];
      }
      return item;
    });
});

const computedColumns = computed<Column[]>(() => {
  if (props.columns) return props.columns;
  if (!pageMeta.value) return [];
  return pageMeta.value.list
    .filter((f) => !f.name.endsWith('ID') || f.primaryKey)
    .map((f) => {
      const col: Column = { key: f.name, label: f.displayName || f.name };
      if (f.typeName === 'Int32' || f.typeName === 'Int64') {
        col.align = 'right';
      }
      if (f.primaryKey) {
        col.mono = true;
        col.width = '80px';
      }
      if (f.typeName === 'String' && (f.itemType === 'image' || (f.itemType && f.itemType.startsWith('file')))) {
        col.width = '100px';
      }
      return col;
    });
});

async function fetchPageMeta() {
  if (!auto.value) return;
  try {
    const url =  apiPrefix.value + '/GetPage';
    console.log('[DefaultList] GetPage:', url);
    const res = await request({ url, method: 'get' });
    pageMeta.value = (res as any).data ?? res as unknown as PageMeta;
  } catch (err) {
    console.error('[DefaultList] GetPage failed:', err);
  }
}

async function fetchList() {
  if (!auto.value) return;
  internalLoading.value = true;
  try {
    const params: Record<string, unknown> = { pageIndex: internalPage.value, pageSize: internalPageSize.value };
    for (const [k, v] of Object.entries(filterData)) {
      if (v !== '' && v !== null && v !== undefined) {
        params[k] = v;
      }
    }
    const url =  apiPrefix.value;
    console.log('[DefaultList] Index:', url, params);
    const res: any = await request({ url, method: 'get', params });
    if (res && res.data && res.page) {
      internalList.value = res.data;
      internalTotal.value = res.page.totalCount ?? res.page.total ?? 0;
    } else if (Array.isArray(res)) {
      internalList.value = res;
      internalTotal.value = res.length;
    } else {
      internalList.value = (res && res.data) ? res.data : (res ?? []);
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

function handleNew() {
  if (auto.value) {
    openDialog('add');
  } else {
    emit('new');
  }
}

function handleEditRow(row: Record<string, unknown>) {
  if (auto.value) {
    openDialog('edit', row);
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
  emit('export');
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

watch(() => route.path, () => {
  if (auto.value) {
    internalPage.value = 1;
    pageMeta.value = null;
    internalList.value = [];
    fetchPageMeta();
    fetchList();
  }
});

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
      <slot name="search">
        <component :is="SearchBarComp" :fields="searchFields ?? computedSearchFields" @search="handleSearch" @reset="handleReset" />
      </slot>
      <slot name="toolbar">
        <component :is="ToolbarComp" @new="handleNew" @delete="handleDelete" @export="handleExport" @refresh="handleRefresh" />
      </slot>
      <slot name="table">
        <component :is="TableContentComp" :columns="columns ?? computedColumns" :data="renderData" :loading="renderLoading" @edit="handleEditRow" @delete="handleDeleteRowTable" />
      </slot>
      <slot name="pagination">
        <component :is="PaginationComp" :total="renderTotal" :current-page="renderPage" :page-size="renderPageSize" @update:current-page="handlePageChange" @update:page-size="handlePageSizeChange" />
      </slot>
      <slot name="footer">
        <component :is="PageFooterComp" />
      </slot>
    </div>

    <!-- 新增/编辑弹窗 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="700px" destroy-on-close>
      <FormPage
        :fields="dialogFields"
        :model-value="dialogFormData"
        :show-continue="false"
        @update:model-value="handleDialogUpdate"
        @submit="submitDialog"
        @cancel="closeDialog"
      />
    </el-dialog>
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
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  background: var(--bg);

  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-thumb {
    background: #c8d4c8;
    border-radius: 3px;
  }
}
</style>