<template>
  <div>
    <!-- Toolbar -->
    <t-space style="margin-bottom: 16px" breakLine>
      <t-button theme="primary" @click="handleAdd">新增</t-button>
      <t-button theme="danger" :disabled="selectedIds.length === 0" @click="handleBatchDelete">批量删除({{ selectedIds.length }})</t-button>
      <t-button variant="outline" @click="handleExport">导出</t-button>
      <t-upload :action="`${typePath}/ImportFile`" :auto-upload="true" theme="custom" accept=".csv,.xls,.xlsx" @success="loadData">
        <t-button variant="outline">导入</t-button>
      </t-upload>
      <div style="flex: 1" />
      <t-input v-model="keyword" placeholder="搜索..." style="width: 240px" clearable @enter="handleSearch">
        <template #suffix-icon><search-icon @click="handleSearch" style="cursor: pointer" /></template>
      </t-input>
    </t-space>

    <!-- Table -->
    <t-table
      :data="data"
      :columns="tableColumns"
      :loading="loading"
      row-key="id"
      :selected-row-keys="selectedIds"
      @select-change="(keys: number[]) => selectedIds = keys"
      :pagination="pagination"
      @page-change="onPageChange"
      bordered
      stripe
    />

    <!-- Form Dialog -->
    <t-dialog v-model:visible="showForm" :header="isEdit ? '编辑' : '新增'" :footer="false" width="600px">
      <t-form label-align="right" :label-width="100">
        <t-form-item v-for="f in editFields" :key="f.name" :label="f.displayName || f.name">
          <FieldInput :field="f" v-model="formData[toCamelCase(f.name)]" />
        </t-form-item>
      </t-form>
      <div style="display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px">
        <t-button variant="outline" @click="showForm = false">取消</t-button>
        <t-button theme="primary" @click="handleSave">保存</t-button>
      </div>
    </t-dialog>

    <!-- Detail Dialog -->
    <t-dialog v-model:visible="showDetail" header="详情" :footer="false" width="600px">
      <t-descriptions :column="1" bordered>
        <t-descriptions-item v-for="f in detailFields" :key="f.name" :label="f.displayName || f.name">
          {{ detailData[toCamelCase(f.name)] ?? '-' }}
        </t-descriptions-item>
      </t-descriptions>
    </t-dialog>

    <!-- Delete Confirm -->
    <t-dialog v-model:visible="showDeleteConfirm" header="确认删除" @confirm="handleDelete" confirmBtn="删除" :cancelBtn="'取消'">
      <p>此操作不可撤销，确定要删除吗？</p>
    </t-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, h } from 'vue';
import { useRoute } from 'vue-router';
import { SearchIcon } from 'tdesign-icons-vue-next';
import { api } from '@/api';
import FieldInput from '@/components/FieldInput.vue';
import { FieldKind, type DataField, type PageResult } from '@cube/api-core';
import { toCamelCase } from '@cube/field-mapping';
import { Button as TButton } from 'tdesign-vue-next';

const route = useRoute();
const typePath = computed(() => '/' + (route.params.type as string || ''));

// Fields
const listFields = ref<DataField[]>([]);
const editFields = ref<DataField[]>([]);
const detailFields = ref<DataField[]>([]);

// Data
const data = ref<any[]>([]);
const total = ref(0);
const pageIndex = ref(1);
const pageSize = ref(20);
const keyword = ref('');
const loading = ref(false);
const selectedIds = ref<number[]>([]);

// Dialog
const showForm = ref(false);
const showDetail = ref(false);
const showDeleteConfirm = ref(false);
const deleteTargetId = ref<number | null>(null);
const formData = ref<Record<string, any>>({});
const detailData = ref<Record<string, any>>({});
const isEdit = ref(false);

// Table columns
const tableColumns = computed(() => {
  const cols: any[] = [{ colKey: 'row-select', type: 'multiple', width: 50 }];
  for (const f of listFields.value) {
    cols.push({ colKey: toCamelCase(f.name), title: f.displayName || f.name, ellipsis: true });
  }
  cols.push({
    colKey: 'op', title: '操作', width: 180, fixed: 'right',
    cell: (_h: any, { row }: any) => {
      return h('div', { style: 'display: flex; gap: 8px' }, [
        h(TButton, { theme: 'primary', variant: 'text', size: 'small', onClick: () => handleDetail(row.id) }, () => '查看'),
        h(TButton, { theme: 'primary', variant: 'text', size: 'small', onClick: () => handleEdit(row.id) }, () => '编辑'),
        h(TButton, { theme: 'danger', variant: 'text', size: 'small', onClick: () => confirmDelete(row.id) }, () => '删除'),
      ]);
    },
  });
  return cols;
});

// Pagination
const pagination = computed(() => ({
  current: pageIndex.value,
  pageSize: pageSize.value,
  total: total.value,
  showJumper: true,
  showSizer: true,
}));

function onPageChange(info: { current: number; pageSize: number }) {
  pageIndex.value = info.current;
  pageSize.value = info.pageSize;
  loadData();
}

// Load fields
async function loadFields() {
  try {
    const [listRes, editRes, detailRes] = await Promise.all([
      api.page.getFields(typePath.value, FieldKind.List),
      api.page.getFields(typePath.value, FieldKind.Edit),
      api.page.getFields(typePath.value, FieldKind.Detail),
    ]);
    listFields.value = listRes?.data ?? [];
    editFields.value = editRes?.data ?? [];
    detailFields.value = detailRes?.data ?? [];
  } catch { /* ignore */ }
}

// Load data
async function loadData() {
  loading.value = true;
  try {
    const params: Record<string, any> = { pageIndex: pageIndex.value, pageSize: pageSize.value };
    if (keyword.value) params.key = keyword.value;
    const res = await api.page.getList(typePath.value, params) as { data: PageResult<any> };
    data.value = res?.data?.data ?? [];
    total.value = res?.data?.totalCount ?? 0;
  } catch { /* ignore */ }
  loading.value = false;
}

function handleSearch() {
  pageIndex.value = 1;
  loadData();
}

// CRUD
function handleAdd() {
  formData.value = {};
  isEdit.value = false;
  showForm.value = true;
}

async function handleEdit(id: number) {
  try {
    const res = await api.page.getDetail(typePath.value, id);
    formData.value = res?.data ? { ...res.data } : {};
    isEdit.value = true;
    showForm.value = true;
  } catch { /* ignore */ }
}

async function handleDetail(id: number) {
  try {
    const res = await api.page.getDetail(typePath.value, id);
    detailData.value = res?.data ?? {};
    showDetail.value = true;
  } catch { /* ignore */ }
}

async function handleSave() {
  try {
    if (isEdit.value) {
      await api.page.update(typePath.value, formData.value);
    } else {
      await api.page.create(typePath.value, formData.value);
    }
    showForm.value = false;
    await loadData();
  } catch { /* ignore */ }
}

function confirmDelete(id: number) {
  deleteTargetId.value = id;
  showDeleteConfirm.value = true;
}

async function handleDelete() {
  if (deleteTargetId.value == null) return;
  try {
    await api.page.delete(typePath.value, deleteTargetId.value);
    showDeleteConfirm.value = false;
    deleteTargetId.value = null;
    await loadData();
  } catch { /* ignore */ }
}

async function handleBatchDelete() {
  if (selectedIds.value.length === 0) return;
  try {
    await api.page.deleteSelect(typePath.value, selectedIds.value);
    selectedIds.value = [];
    await loadData();
  } catch { /* ignore */ }
}

function handleExport() {
  const params = new URLSearchParams({ pageIndex: String(pageIndex.value), pageSize: String(pageSize.value) });
  if (keyword.value) params.set('key', keyword.value);
  window.open(`${typePath.value}/ExportCsv?${params}`, '_blank');
}

// Watch route
watch(typePath, () => {
  pageIndex.value = 1;
  keyword.value = '';
  selectedIds.value = [];
  loadFields();
  loadData();
}, { immediate: true });
</script>
