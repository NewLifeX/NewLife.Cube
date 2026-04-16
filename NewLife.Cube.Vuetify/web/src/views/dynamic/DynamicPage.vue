<template>
  <div>
    <!-- ECharts 图表区域 -->
    <v-card v-if="chartList.length" class="mb-4 pa-4">
      <div
        v-for="(_, idx) in chartList"
        :key="idx"
        :ref="(el: any) => setChartRef(el as HTMLElement, idx)"
        style="width: 100%; height: 400px"
      />
    </v-card>

    <!-- 搜索 -->
    <v-card v-if="searchFields.length" class="mb-4 pa-4">
      <v-form @submit.prevent="handleSearch">
        <v-row dense>
          <v-col v-for="field in searchFields" :key="field.name" cols="12" sm="6" md="3">
            <FieldInput :field="field" :value="searchForm[field.name]" @update:value="(v: any) => (searchForm[field.name] = v)" />
          </v-col>
          <v-col cols="12" sm="6" md="3" class="d-flex align-center ga-2">
            <v-btn type="submit" color="primary" prepend-icon="mdi-magnify">搜索</v-btn>
            <v-btn variant="outlined" @click="handleReset">重置</v-btn>
          </v-col>
        </v-row>
      </v-form>
    </v-card>

    <!-- 操作栏 -->
    <div class="d-flex justify-space-between mb-3">
      <div class="d-flex ga-2">
        <v-btn v-if="canAdd" color="primary" prepend-icon="mdi-plus" @click="handleAdd">新增</v-btn>
        <v-btn v-if="canDelete" color="error" :disabled="!selected.length" prepend-icon="mdi-delete" @click="handleBatchDelete">批量删除</v-btn>
      </div>
      <div class="d-flex ga-2">
        <v-menu v-if="canExport">
          <template #activator="{ props: menuProps }">
            <v-btn variant="outlined" v-bind="menuProps" append-icon="mdi-chevron-down">导出</v-btn>
          </template>
          <v-list density="compact">
            <v-list-item v-for="opt in exportOptions" :key="opt.value" @click="handleExport(opt.value)">
              <v-list-item-title>{{ opt.label }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
        <v-btn v-if="canImport" variant="outlined" @click="triggerImport">导入</v-btn>
        <input ref="fileInput" type="file" style="display: none;" @change="handleImport" />
      </div>
    </div>

    <!-- 数据表格 -->
    <v-data-table
      :headers="headers"
      :items="tableData"
      :loading="loading"
      show-select
      v-model="selected"
      item-value="id"
      :items-per-page="pageSize"
      hide-default-footer
    >
      <template #item.actions="{ item }">
        <v-btn variant="text" size="small" @click="handleDetail(item)">详情</v-btn>
        <v-btn v-if="canEdit" variant="text" size="small" @click="handleEdit(item)">编辑</v-btn>
        <v-btn v-if="canDelete" variant="text" size="small" color="error" @click="confirmDelete(item)">删除</v-btn>
      </template>
      <template #bottom>
        <!-- 统计行 -->
        <div v-if="statData" class="d-flex ga-4 pa-2" style="background: #f5f5f5;">
          <span class="font-weight-bold">合计</span>
          <span v-for="f in listFields.filter(f => statData![f.name] != null)" :key="f.name">
            {{ f.displayName || f.name }}：{{ statData[f.name] }}
          </span>
        </div>
        <div class="d-flex justify-end align-center pa-2 ga-4">
          <span class="text-body-2">共 {{ total }} 条</span>
          <v-pagination v-model="page" :length="totalPages" density="compact" :total-visible="5" @update:model-value="loadData" />
        </div>
      </template>
    </v-data-table>

    <!-- 新增/编辑弹窗 -->
    <v-dialog v-model="formDialogVisible" max-width="640" persistent>
      <v-card>
        <v-card-title>{{ isEdit ? '编辑' : '新增' }}</v-card-title>
        <v-card-text>
          <v-form>
            <v-row dense>
              <v-col v-for="field in formFields" :key="field.name" cols="12">
                <FieldInput :field="field" :value="formData[field.name]" @update:value="(v: any) => (formData[field.name] = v)" />
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="formDialogVisible = false">取消</v-btn>
          <v-btn color="primary" :loading="submitLoading" @click="handleFormSubmit">确定</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- 详情弹窗 -->
    <v-dialog v-model="detailDialogVisible" max-width="640">
      <v-card>
        <v-card-title>详情</v-card-title>
        <v-card-text>
          <v-table density="compact">
            <tbody>
              <tr v-for="field in detailFields" :key="field.name">
                <td class="font-weight-bold" style="width: 160px;">{{ field.displayName || field.name }}</td>
                <td>{{ detailData[field.name] ?? '-' }}</td>
              </tr>
            </tbody>
          </v-table>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="detailDialogVisible = false">关闭</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- 删除确认 -->
    <v-dialog v-model="deleteDialogVisible" max-width="360">
      <v-card>
        <v-card-title>确认删除</v-card-title>
        <v-card-text>确定要删除此记录吗？</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="deleteDialogVisible = false">取消</v-btn>
          <v-btn color="error" @click="doDelete">删除</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted, onBeforeUnmount, nextTick, markRaw } from 'vue';
import { useRoute } from 'vue-router';
import { Auth, type DataField } from '@cube/api-core';
import * as echarts from 'echarts';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import FieldInput from '@/components/FieldInput.vue';

const route = useRoute();
const userStore = useUserStore();
const typePath = computed(() => '/' + (route.params.type as string[] || []).join('/'));

// --- 权限控制 ---
const menuPerms = computed(() => userStore.getMenuPermission(typePath.value));
const canAdd = computed(() => String(Auth.ADD) in menuPerms.value);
const canEdit = computed(() => String(Auth.EDIT) in menuPerms.value);
const canDelete = computed(() => String(Auth.DELETE) in menuPerms.value);
const canExport = computed(() => String(Auth.EXPORT) in menuPerms.value);
const canImport = computed(() => String(Auth.IMPORT) in menuPerms.value);

const exportOptions = [
  { label: '导出 Excel', value: 'Excel' },
  { label: '导出 CSV', value: 'Csv' },
  { label: '导出 JSON', value: 'Json' },
  { label: '导出 XML', value: 'Xml' },
  { label: '导出模板', value: 'ExcelTemplate' },
];

// 字段
const listFields = ref<DataField[]>([]);
const searchFields = ref<DataField[]>([]);
const addFields = ref<DataField[]>([]);
const editFields = ref<DataField[]>([]);
const detailFields = ref<DataField[]>([]);

// 表格
const tableData = ref<Record<string, any>[]>([]);
const loading = ref(false);
const selected = ref<any[]>([]);
const page = ref(1);
const pageSize = ref(20);
const total = ref(0);
const totalPages = computed(() => Math.ceil(total.value / pageSize.value) || 1);
const statData = ref<Record<string, unknown> | null>(null);

// 搜索
const searchForm = reactive<Record<string, any>>({});

// 表单弹窗
const formDialogVisible = ref(false);
const isEdit = ref(false);
const formData = reactive<Record<string, any>>({});
const submitLoading = ref(false);

// 详情弹窗
const detailDialogVisible = ref(false);
const detailData = reactive<Record<string, any>>({});

// 删除确认
const deleteDialogVisible = ref(false);
const deleteTarget = ref<Record<string, any> | null>(null);

// 文件导入
const fileInput = ref<HTMLInputElement | null>(null);

// ECharts
const chartList = ref<any[]>([]);
const chartInstances = ref<any[]>([]);

// 表头
const headers = computed(() => {
  const cols = listFields.value
    .filter((f) => f.visible !== false)
    .map((f) => ({
      title: f.displayName || f.name,
      key: f.name,
      sortable: false,
    }));
  cols.push({ title: '操作', key: 'actions', sortable: false });
  return cols;
});

const formFields = computed(() => (isEdit.value ? editFields.value : addFields.value));

async function loadFields() {
  const path = typePath.value;
  const page = await cubeApi.page.getPage(path);
  const meta = page.data || {};
  listFields.value = meta.list || meta.fields?.list || [];
  searchFields.value = meta.search || meta.fields?.search || [];
  addFields.value = meta.addForm || meta.fields?.form?.addForm || [];
  editFields.value = meta.editForm || meta.fields?.form?.editForm || [];
  detailFields.value = meta.detail || meta.fields?.form?.detail || [];
}

async function loadData() {
  loading.value = true;
  try {
    const res = await cubeApi.page.getList(typePath.value, {
      pageIndex: page.value - 1,
      pageSize: pageSize.value,
      ...searchForm,
    });
    tableData.value = res.data || [];
    statData.value = (res.stat as Record<string, unknown>) ?? null;
    if (res.page) total.value = res.page.totalCount || 0;
  } finally {
    loading.value = false;
  }
}

async function loadChartData() {
  try {
    const res = await cubeApi.page.getChartData(typePath.value);
    chartList.value = Array.isArray(res.data) && res.data.length > 0 ? res.data : [];
  } catch {
    chartList.value = [];
  }
}

function setChartRef(el: HTMLElement | null, idx: number) {
  if (!el) return;
  nextTick(() => {
    if (chartInstances.value[idx]) chartInstances.value[idx].dispose();
    const instance = markRaw(echarts.init(el));
    if (chartList.value[idx]) instance.setOption(chartList.value[idx]);
    chartInstances.value[idx] = instance;
  });
}

function onChartResize() {
  for (const inst of chartInstances.value) { inst?.resize(); }
}

function handleSearch() { page.value = 1; loadData(); }
function handleReset() { Object.keys(searchForm).forEach((k) => delete searchForm[k]); page.value = 1; loadData(); }

function handleAdd() { isEdit.value = false; Object.keys(formData).forEach((k) => delete formData[k]); formDialogVisible.value = true; }
function handleEdit(row: Record<string, any>) { isEdit.value = true; Object.keys(formData).forEach((k) => delete formData[k]); Object.assign(formData, row); formDialogVisible.value = true; }

async function handleFormSubmit() {
  submitLoading.value = true;
  try {
    if (isEdit.value) await cubeApi.page.update(typePath.value, formData);
    else await cubeApi.page.add(typePath.value, formData);
    formDialogVisible.value = false;
    loadData();
  } finally { submitLoading.value = false; }
}

function handleDetail(row: Record<string, any>) { Object.keys(detailData).forEach((k) => delete detailData[k]); Object.assign(detailData, row); detailDialogVisible.value = true; }
function confirmDelete(row: Record<string, any>) { deleteTarget.value = row; deleteDialogVisible.value = true; }
async function doDelete() { if (deleteTarget.value) { await cubeApi.page.remove(typePath.value, deleteTarget.value.id); deleteDialogVisible.value = false; deleteTarget.value = null; loadData(); } }
async function handleBatchDelete() { if (!selected.value.length) return; await cubeApi.page.deleteSelect(typePath.value, selected.value as number[]); selected.value = []; loadData(); }
function handleExport(format: string) { window.open(cubeApi.page.getExportUrl(typePath.value, format), '_blank'); }
function triggerImport() { fileInput.value?.click(); }

async function handleImport(e: Event) {
  const input = e.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  await cubeApi.page.importFile(typePath.value, file);
  input.value = '';
  loadData();
}

watch(typePath, () => { page.value = 1; loadFields().then(loadData); loadChartData(); });
onMounted(() => { loadFields().then(loadData); loadChartData(); window.addEventListener('resize', onChartResize); });
onBeforeUnmount(() => { window.removeEventListener('resize', onChartResize); for (const inst of chartInstances.value) { inst?.dispose(); } });
</script>
