<template>
  <div style="padding: 0 16px;">
    <!-- 搜索区域 -->
    <a-card v-if="searchFields.length" style="margin-bottom: 16px;">
      <a-form :model="searchForm" layout="inline" @submit="handleSearch">
        <a-form-item v-for="field in searchFields" :key="field.name" :label="field.displayName || field.name">
          <FieldInput :field="field" :value="searchForm[field.name]" @update:value="(v) => (searchForm[field.name] = v)" />
        </a-form-item>
        <a-form-item>
          <a-space>
            <a-button type="primary" html-type="submit">
              <template #icon><icon-search /></template>
              搜索
            </a-button>
            <a-button @click="handleReset">重置</a-button>
          </a-space>
        </a-form-item>
      </a-form>
    </a-card>

    <!-- 操作栏 -->
    <div style="display: flex; justify-content: space-between; margin-bottom: 12px;">
      <a-space>
        <a-button type="primary" @click="handleAdd">
          <template #icon><icon-plus /></template>
          新增
        </a-button>
        <a-button status="danger" :disabled="!selectedKeys.length" @click="handleBatchDelete">
          <template #icon><icon-delete /></template>
          批量删除
        </a-button>
      </a-space>
      <a-space>
        <a-button @click="handleExportCsv">导出CSV</a-button>
        <a-upload :action="importUrl" :show-file-list="false" @success="handleImportSuccess">
          <template #upload-button>
            <a-button>导入</a-button>
          </template>
        </a-upload>
      </a-space>
    </div>

    <!-- 数据表格 -->
    <a-table
      :columns="columns"
      :data="tableData"
      :loading="loading"
      :pagination="pagination"
      :row-selection="{ type: 'checkbox', showCheckedAll: true, selectedRowKeys: selectedKeys, onSelectionChange: onSelectionChange }"
      row-key="id"
      :bordered="{ cell: true }"
      @page-change="onPageChange"
      @page-size-change="onPageSizeChange"
    >
      <template #operations="{ record }">
        <a-space>
          <a-button type="text" size="mini" @click="handleDetail(record)">详情</a-button>
          <a-button type="text" size="mini" @click="handleEdit(record)">编辑</a-button>
          <a-popconfirm content="确认删除？" @ok="handleDelete(record)">
            <a-button type="text" size="mini" status="danger">删除</a-button>
          </a-popconfirm>
        </a-space>
      </template>
    </a-table>

    <!-- 新增/编辑弹窗 -->
    <a-modal v-model:visible="formVisible" :title="isEdit ? '编辑' : '新增'" :width="640" @ok="handleFormSubmit" :ok-loading="submitLoading">
      <a-form :model="formData" layout="vertical">
        <a-form-item v-for="field in formFields" :key="field.name" :label="field.displayName || field.name" :required="!!field.required">
          <FieldInput :field="field" :value="formData[field.name]" @update:value="(v) => (formData[field.name] = v)" />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 详情弹窗 -->
    <a-modal v-model:visible="detailVisible" title="详情" :width="640" :footer="false">
      <a-descriptions :column="1" bordered>
        <a-descriptions-item v-for="field in detailFields" :key="field.name" :label="field.displayName || field.name">
          {{ detailData[field.name] ?? '-' }}
        </a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import { IconSearch, IconPlus, IconDelete } from '@arco-design/web-vue/es/icon';
import { FieldKind, type DataField } from '@cube/api-core';
import cubeApi from '@/api';
import FieldInput from '@/components/FieldInput.vue';

const route = useRoute();
const typePath = computed(() => '/' + (route.params.type as string[] || []).join('/'));

// 字段
const listFields = ref<DataField[]>([]);
const searchFields = ref<DataField[]>([]);
const addFields = ref<DataField[]>([]);
const editFields = ref<DataField[]>([]);
const detailFields = ref<DataField[]>([]);

// 表格
const tableData = ref<Record<string, any>[]>([]);
const loading = ref(false);
const selectedKeys = ref<(string | number)[]>([]);
const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showTotal: true,
  showPageSize: true,
});

// 搜索
const searchForm = reactive<Record<string, any>>({});

// 表单弹窗
const formVisible = ref(false);
const isEdit = ref(false);
const formData = reactive<Record<string, any>>({});
const submitLoading = ref(false);

// 详情弹窗
const detailVisible = ref(false);
const detailData = reactive<Record<string, any>>({});

// 表格列
const columns = computed(() => {
  const cols = listFields.value
    .filter((f) => f.visible !== false)
    .map((f) => ({
      title: f.displayName || f.name,
      dataIndex: f.name,
      ellipsis: true,
      tooltip: true,
      width: f.maxWidth || undefined,
    }));
  cols.push({ title: '操作', dataIndex: 'operations', ellipsis: false, tooltip: false, width: 180 } as any);
  return cols;
});

const formFields = computed(() => (isEdit.value ? editFields.value : addFields.value));
const importUrl = computed(() => `${typePath.value}/ImportFile`);

// 加载字段
async function loadFields() {
  const path = typePath.value;
  const [list, search, add, edit, detail] = await Promise.all([
    cubeApi.page.getFields(path, FieldKind.List),
    cubeApi.page.getFields(path, FieldKind.Search),
    cubeApi.page.getFields(path, FieldKind.Add),
    cubeApi.page.getFields(path, FieldKind.Edit),
    cubeApi.page.getFields(path, FieldKind.Detail),
  ]);
  listFields.value = list.data || [];
  searchFields.value = search.data || [];
  addFields.value = add.data || [];
  editFields.value = edit.data || [];
  detailFields.value = detail.data || [];
}

// 加载列表
async function loadData() {
  loading.value = true;
  try {
    const res = await cubeApi.page.getList(typePath.value, {
      pageIndex: pagination.current,
      pageSize: pagination.pageSize,
      ...searchForm,
    });
    tableData.value = res.data || [];
    if (res.pager) pagination.total = res.pager.totalCount || 0;
  } finally {
    loading.value = false;
  }
}

function onPageChange(page: number) {
  pagination.current = page;
  loadData();
}

function onPageSizeChange(size: number) {
  pagination.pageSize = size;
  pagination.current = 1;
  loadData();
}

function onSelectionChange(keys: (string | number)[]) {
  selectedKeys.value = keys;
}

function handleSearch() {
  pagination.current = 1;
  loadData();
}

function handleReset() {
  Object.keys(searchForm).forEach((k) => delete searchForm[k]);
  pagination.current = 1;
  loadData();
}

function handleAdd() {
  isEdit.value = false;
  Object.keys(formData).forEach((k) => delete formData[k]);
  formVisible.value = true;
}

function handleEdit(row: Record<string, any>) {
  isEdit.value = true;
  Object.keys(formData).forEach((k) => delete formData[k]);
  Object.assign(formData, row);
  formVisible.value = true;
}

async function handleFormSubmit() {
  submitLoading.value = true;
  try {
    if (isEdit.value) {
      await cubeApi.page.update(typePath.value, formData);
    } else {
      await cubeApi.page.add(typePath.value, formData);
    }
    Message.success(isEdit.value ? '编辑成功' : '新增成功');
    formVisible.value = false;
    loadData();
  } catch {
    Message.error('操作失败');
  } finally {
    submitLoading.value = false;
  }
}

function handleDetail(row: Record<string, any>) {
  Object.keys(detailData).forEach((k) => delete detailData[k]);
  Object.assign(detailData, row);
  detailVisible.value = true;
}

async function handleDelete(row: Record<string, any>) {
  await cubeApi.page.remove(typePath.value, row.id);
  Message.success('删除成功');
  loadData();
}

async function handleBatchDelete() {
  if (!selectedKeys.value.length) return;
  await cubeApi.page.deleteSelect(typePath.value, selectedKeys.value as number[]);
  Message.success('批量删除成功');
  selectedKeys.value = [];
  loadData();
}

function handleExportCsv() {
  window.open(`${typePath.value}/ExportCsv`, '_blank');
}

function handleImportSuccess() {
  Message.success('导入成功');
  loadData();
}

// 路由变化时重新加载
watch(typePath, () => {
  pagination.current = 1;
  loadFields().then(loadData);
});

onMounted(() => {
  loadFields().then(loadData);
});
</script>
