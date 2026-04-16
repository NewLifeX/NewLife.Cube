<template>
  <div>
    <!-- ECharts 图表区域 -->
    <div v-if="chartList.length" style="margin-bottom: 16px;">
      <div
        v-for="(_, idx) in chartList"
        :key="idx"
        :ref="(el: any) => setChartRef(el as HTMLElement, idx)"
        style="width: 100%; height: 400px; background: var(--td-bg-color-container); border-radius: 6px; margin-bottom: 8px;"
      />
    </div>

    <!-- Toolbar -->
    <t-space style="margin-bottom: 16px" breakLine>
      <t-button v-if="canAdd" theme="primary" @click="handleAdd">新增</t-button>
      <t-button v-if="canDelete" theme="danger" :disabled="selectedIds.length === 0" @click="handleBatchDelete">批量删除({{ selectedIds.length }})</t-button>
      <t-dropdown v-if="canExport" :options="exportOptions" @click="handleExport">
        <t-button variant="outline">导出 <chevron-down-icon /></t-button>
      </t-dropdown>
      <t-upload v-if="canImport" :action="`${typePath}/ImportFile`" :auto-upload="true" theme="custom" accept=".csv,.xls,.xlsx" @success="loadData">
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
      :foot-data="footData"
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
    <t-dialog v-model:visible="showDeleteConfirm" header="确认删除" @confirm="handleDeleteConfirm" confirmBtn="删除" :cancelBtn="'取消'">
      <p>此操作不可撤销，确定要删除吗？</p>
    </t-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick, markRaw, h } from 'vue';
import { useRoute } from 'vue-router';
import { SearchIcon, ChevronDownIcon } from 'tdesign-icons-vue-next';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import FieldInput from '@/components/FieldInput.vue';
import { Auth, type DataField } from '@cube/api-core';
import { toCamelCase } from '@cube/field-mapping';
import { Button as TButton } from 'tdesign-vue-next';
import * as echarts from 'echarts';

const route = useRoute();
const userStore = useUserStore();
const typePath = computed(() => '/' + (route.params.type as string || ''));

// --- 权限控制 ---
const menuPerms = computed(() => userStore.getMenuPermission(typePath.value));
const canAdd = computed(() => String(Auth.ADD) in menuPerms.value);
const canEdit = computed(() => String(Auth.EDIT) in menuPerms.value);
const canDelete = computed(() => String(Auth.DELETE) in menuPerms.value);
const canExport = computed(() => String(Auth.EXPORT) in menuPerms.value);
const canImport = computed(() => String(Auth.IMPORT) in menuPerms.value);

const exportOptions = [
  { content: '导出 Excel', value: 'Excel' },
  { content: '导出 CSV', value: 'Csv' },
  { content: '导出 JSON', value: 'Json' },
  { content: '导出 XML', value: 'Xml' },
  { content: '导出模板', value: 'ExcelTemplate' },
];

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
const statData = ref<Record<string, unknown> | null>(null);

// Dialog
const showForm = ref(false);
const showDetail = ref(false);
const showDeleteConfirm = ref(false);
const deleteTargetId = ref<number | null>(null);
const formData = ref<Record<string, any>>({});
const detailData = ref<Record<string, any>>({});
const isEdit = ref(false);

// ECharts
const chartList = ref<any[]>([]);
const chartInstances = ref<any[]>([]);

// 统计行（footData）
const footData = computed(() => {
  if (!statData.value) return [];
  const row: Record<string, any> = {};
  for (const f of listFields.value) {
    const key = toCamelCase(f.name);
    row[key] = statData.value[f.name] ?? statData.value[key] ?? '';
  }
  // 第一列显示"合计"
  const firstField = listFields.value[0];
  if (firstField) row[toCamelCase(firstField.name)] = '合计';
  return [row];
});

// Table columns
const tableColumns = computed(() => {
  const cols: any[] = [{ colKey: 'row-select', type: 'multiple', width: 50 }];
  for (const f of listFields.value) {
    cols.push({ colKey: toCamelCase(f.name), title: f.displayName || f.name, ellipsis: true });
  }
  cols.push({
    colKey: 'op', title: '操作', width: 180, fixed: 'right',
    cell: (_h: any, { row }: any) => {
      const btns = [
        h(TButton, { theme: 'primary', variant: 'text', size: 'small', onClick: () => handleDetail(row.id) }, () => '查看'),
      ];
      if (canEdit.value) btns.push(h(TButton, { theme: 'primary', variant: 'text', size: 'small', onClick: () => handleEditById(row.id) }, () => '编辑'));
      if (canDelete.value) btns.push(h(TButton, { theme: 'danger', variant: 'text', size: 'small', onClick: () => confirmDelete(row.id) }, () => '删除'));
      return h('div', { style: 'display: flex; gap: 8px' }, btns);
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

async function loadFields() {
  try {
    const pageRes = await api.page.getPage(typePath.value);
    const pageMeta = pageRes?.data ?? {};
    listFields.value = pageMeta.list ?? pageMeta.fields?.list ?? [];
    editFields.value = pageMeta.editForm ?? pageMeta.fields?.form?.editForm ?? [];
    detailFields.value = pageMeta.detail ?? pageMeta.fields?.form?.detail ?? [];
  } catch { /* ignore */ }
}

async function loadData() {
  loading.value = true;
  try {
    const params: Record<string, any> = { pageIndex: pageIndex.value, pageSize: pageSize.value };
    if (keyword.value) params.key = keyword.value;
    const res = await api.page.getList(typePath.value, params) as any;
    data.value = res?.data?.data ?? res?.data ?? [];
    total.value = res?.data?.totalCount ?? res?.page?.totalCount ?? 0;
    statData.value = res?.stat ?? null;
  } catch { /* ignore */ }
  loading.value = false;
}

// ECharts
async function loadChartData() {
  try {
    const res = await api.page.getChartData(typePath.value);
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

function handleSearch() { pageIndex.value = 1; loadData(); }

// CRUD
function handleAdd() { formData.value = {}; isEdit.value = false; showForm.value = true; }

async function handleEditById(id: number) {
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
    if (isEdit.value) await api.page.update(typePath.value, formData.value);
    else await api.page.add(typePath.value, formData.value);
    showForm.value = false;
    await loadData();
  } catch { /* ignore */ }
}

function confirmDelete(id: number) { deleteTargetId.value = id; showDeleteConfirm.value = true; }

async function handleDeleteConfirm() {
  if (deleteTargetId.value == null) return;
  try {
    await api.page.remove(typePath.value, deleteTargetId.value);
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

function handleExport(item: any) {
  const format = item?.value || 'Excel';
  window.open(api.page.getExportUrl(typePath.value, format), '_blank');
}

// Watch route
watch(typePath, () => {
  pageIndex.value = 1;
  keyword.value = '';
  selectedIds.value = [];
  loadFields();
  loadData();
  loadChartData();
}, { immediate: true });

onMounted(() => {
  loadChartData();
  window.addEventListener('resize', onChartResize);
});

onBeforeUnmount(() => {
  window.removeEventListener('resize', onChartResize);
  for (const inst of chartInstances.value) { inst?.dispose(); }
});
</script>
