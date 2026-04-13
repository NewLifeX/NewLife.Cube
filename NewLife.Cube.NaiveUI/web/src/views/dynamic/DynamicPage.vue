<template>
  <div class="dynamic-page">
    <!-- ECharts 图表区域 -->
    <n-card v-if="chartList.length" size="small" style="margin-bottom: 16px">
      <div
        v-for="(_, idx) in chartList"
        :key="idx"
        :ref="(el: any) => setChartRef(el as HTMLElement, idx)"
        style="width: 100%; height: 400px"
      />
    </n-card>

    <!-- 搜索栏 -->
    <n-card v-if="searchFields.length" size="small" style="margin-bottom: 16px">
      <n-form ref="searchFormRef" :model="searchForm" label-placement="left" inline>
        <n-form-item v-for="f in searchFields" :key="f.field.name" :label="f.field.displayName ?? f.field.name">
          <field-input v-model:value="searchForm[f.field.name]" :mapping="f" style="width: 180px" />
        </n-form-item>
        <n-form-item>
          <n-space>
            <n-button type="primary" @click="loadData(1)">搜索</n-button>
            <n-button @click="resetSearch">重置</n-button>
          </n-space>
        </n-form-item>
      </n-form>
    </n-card>

    <!-- 工具栏 -->
    <div class="toolbar">
      <n-space>
        <n-button v-if="canAdd" type="primary" @click="showAdd">新增</n-button>
        <n-dropdown v-if="canExport" trigger="click" :options="exportOptions" @select="handleExport">
          <n-button>导出 <n-icon><arrow-down-icon /></n-icon></n-button>
        </n-dropdown>
        <n-upload v-if="canImport" :show-file-list="false" :custom-request="handleImport">
          <n-button>导入</n-button>
        </n-upload>
        <n-popconfirm v-if="canDelete" @positive-click="handleDeleteSelect">
          <template #trigger>
            <n-button :disabled="!checkedKeys.length" type="error">批量删除</n-button>
          </template>
          确认删除选中的 {{ checkedKeys.length }} 条记录？
        </n-popconfirm>
      </n-space>
    </div>

    <!-- 数据表格 -->
    <n-data-table
      :columns="tableColumns"
      :data="tableData"
      :loading="loading"
      :row-key="(row: Record<string, any>) => row[pkField] ?? row['id']"
      :checked-row-keys="checkedKeys"
      @update:checked-row-keys="(keys: any[]) => (checkedKeys = keys)"
      :pagination="pagination"
      :summary="statSummary"
      remote
      @update:page="loadData"
      @update:page-size="handlePageSizeChange"
    />

    <!-- 新增/编辑弹窗 -->
    <n-modal
      v-model:show="formVisible"
      :title="isEdit ? '编辑' : '新增'"
      preset="dialog"
      :positive-text="isEdit ? '保存' : '确定'"
      negative-text="取消"
      style="width: 680px"
      :loading="formLoading"
      @positive-click="handleSubmit"
    >
      <n-form ref="editFormRef" :model="editForm" label-placement="left" :label-width="120">
        <n-form-item
          v-for="f in formFields"
          :key="f.field.name"
          :label="f.field.displayName ?? f.field.name"
          :path="f.field.name"
          :rule="f.field.required ? { required: true, message: `${f.field.displayName} 不能为空` } : undefined"
        >
          <field-input v-model:value="editForm[f.field.name]" :mapping="f" />
        </n-form-item>
      </n-form>
    </n-modal>

    <!-- 详情弹窗 -->
    <n-modal v-model:show="detailVisible" title="详情" preset="card" style="width: 680px">
      <n-descriptions bordered :column="2">
        <n-descriptions-item
          v-for="f in detailFields"
          :key="f.field.name"
          :label="f.field.displayName ?? f.field.name"
        >
          {{ detailData[f.field.name] }}
        </n-descriptions-item>
      </n-descriptions>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, nextTick, markRaw, h, type Ref } from 'vue';
import { NButton, NSpace, NTag, NIcon, useMessage, type DataTableColumn, type DataTableCreateSummary, type FormInst, type PaginationProps, type UploadCustomRequestOptions } from 'naive-ui';
import { ArrowDown as ArrowDownIcon } from '@vicons/ionicons5';
import { FieldKind, Auth, type DataField } from '@cube/api-core';
import { resolveWidgets, type FieldMapping } from '@cube/field-mapping';
import * as echarts from 'echarts';
import api from '@/api';
import { useUserStore } from '@/stores/user';
import FieldInput from '@/components/FieldInput.vue';

const props = defineProps<{
  type: string;
  authId?: number;
}>();

const message = useMessage();
const userStore = useUserStore();
const loading = ref(false);
const formLoading = ref(false);

// --- 权限控制 ---
const menuPerms = computed(() => userStore.getMenuPermission(props.type));
const canAdd = computed(() => String(Auth.ADD) in menuPerms.value);
const canEdit = computed(() => String(Auth.EDIT) in menuPerms.value);
const canDelete = computed(() => String(Auth.DELETE) in menuPerms.value);
const canExport = computed(() => String(Auth.EXPORT) in menuPerms.value);
const canImport = computed(() => String(Auth.IMPORT) in menuPerms.value);

// --- 导出下拉选项 ---
const exportOptions = [
  { label: '导出 Excel', key: 'Excel' },
  { label: '导出 CSV', key: 'Csv' },
  { label: '导出 JSON', key: 'Json' },
  { label: '导出 XML', key: 'Xml' },
  { type: 'divider', key: 'd1' },
  { label: '导出模板', key: 'ExcelTemplate' },
];

// --- 字段元数据 ---
const listFields = ref<FieldMapping[]>([]);
const searchFields = ref<FieldMapping[]>([]);
const addFieldMappings = ref<FieldMapping[]>([]);
const editFieldMappings = ref<FieldMapping[]>([]);
const detailFields = ref<FieldMapping[]>([]);
const pkField = ref('id');

// --- 表格数据 ---
const tableData = ref<Record<string, any>[]>([]);
const checkedKeys = ref<(string | number)[]>([]);
const statData = ref<Record<string, unknown> | null>(null);

// --- 搜索表单 ---
const searchFormRef = ref<FormInst | null>(null);
const searchForm = ref<Record<string, any>>({});

// --- 分页 ---
const pagination = ref<PaginationProps>({
  page: 1,
  pageSize: 20,
  pageCount: 1,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50, 100],
});

// --- 编辑弹窗 ---
const formVisible = ref(false);
const isEdit = ref(false);
const editFormRef = ref<FormInst | null>(null);
const editForm = ref<Record<string, any>>({});
const formFields = computed(() => (isEdit.value ? editFieldMappings.value : addFieldMappings.value));

// --- 详情弹窗 ---
const detailVisible = ref(false);
const detailData = ref<Record<string, any>>({});

// --- ECharts ---
const chartList = ref<any[]>([]);
const chartInstances = ref<any[]>([]);

// --- 统计行 ---
const statSummary: DataTableCreateSummary | undefined = computed(() => {
  if (!statData.value) return undefined;
  return () => {
    const stat = statData.value!;
    return {
      [pkField.value]: { value: h('b', '合计') },
      ...Object.fromEntries(
        listFields.value.map((m) => [
          m.field.name,
          { value: stat[m.field.name] != null ? String(stat[m.field.name]) : '' },
        ])
      ),
    };
  };
}).value;

// --- 表格列构建 ---
const tableColumns = computed<DataTableColumn[]>(() => {
  const cols: DataTableColumn[] = [
    { type: 'selection' },
  ];

  for (const m of listFields.value) {
    const col: DataTableColumn = {
      key: m.field.name,
      title: m.field.displayName ?? m.field.name,
      width: m.field.maxWidth,
      ellipsis: { tooltip: true },
    };

    // Boolean 字段用 Tag 显示
    if (m.widget === 'switch') {
      col.render = (row: Record<string, any>) =>
        h(NTag, { type: row[m.field.name] ? 'success' : 'default', size: 'small' }, { default: () => (row[m.field.name] ? '是' : '否') });
    }

    // 有 dataSource 的字段显示映射标签
    if (m.field.dataSource && Object.keys(m.field.dataSource).length > 0) {
      col.render = (row: Record<string, any>) => {
        const val = String(row[m.field.name] ?? '');
        return m.field.dataSource![val] ?? val;
      };
    }

    // 有 URL 的字段显示链接
    if (m.field.url) {
      col.render = (row: Record<string, any>) => {
        const href = m.field.url!.replace(/\{(\w+)\}/g, (_, k) => row[k] ?? '');
        return h('a', { href, target: m.field.target ?? '_self' }, { default: () => row[m.field.name] });
      };
    }

    cols.push(col);
  }

  // 操作列（按权限控制按钮显隐）
  if (canEdit.value || canDelete.value) {
    cols.push({
      title: '操作',
      key: '__actions',
      width: 200,
      fixed: 'right',
      render: (row: Record<string, any>) =>
        h(NSpace, null, {
          default: () => [
            h(NButton, { text: true, type: 'info', onClick: () => showDetail(row) }, { default: () => '查看' }),
            canEdit.value ? h(NButton, { text: true, type: 'primary', onClick: () => showEdit(row) }, { default: () => '编辑' }) : null,
            canDelete.value ? h(NButton, { text: true, type: 'error', onClick: () => handleDelete(row) }, { default: () => '删除' }) : null,
          ].filter(Boolean),
        }),
    });
  } else {
    // 至少保留查看按钮
    cols.push({
      title: '操作',
      key: '__actions',
      width: 80,
      fixed: 'right',
      render: (row: Record<string, any>) =>
        h(NButton, { text: true, type: 'info', onClick: () => showDetail(row) }, { default: () => '查看' }),
    });
  }

  return cols;
});

// --- 数据加载 ---
async function loadFields() {
  const [listRes, searchRes, addRes, editRes, detailRes] = await Promise.all([
    api.page.getFields(props.type, FieldKind.List),
    api.page.getFields(props.type, FieldKind.Search),
    api.page.getFields(props.type, FieldKind.Add),
    api.page.getFields(props.type, FieldKind.Edit),
    api.page.getFields(props.type, FieldKind.Detail),
  ]);

  listFields.value = resolveWidgets(listRes.data ?? []);
  searchFields.value = resolveWidgets(searchRes.data ?? []);
  addFieldMappings.value = resolveWidgets(addRes.data ?? []);
  editFieldMappings.value = resolveWidgets(editRes.data ?? []);
  detailFields.value = resolveWidgets(detailRes.data ?? []);

  // 寻找主键字段
  const pk = (listRes.data ?? []).find((f) => f.primaryKey);
  if (pk) pkField.value = pk.name;
}

async function loadData(page?: number) {
  if (page) pagination.value.page = page;
  loading.value = true;
  try {
    const params = {
      ...searchForm.value,
      pageIndex: (pagination.value.page ?? 1) - 1,
      pageSize: pagination.value.pageSize,
    };
    const res = await api.page.getList(props.type, params);
    tableData.value = (res.data as any[]) ?? [];
    statData.value = (res.stat as Record<string, unknown>) ?? null;
    if (res.page) {
      pagination.value.itemCount = res.page.totalCount;
      pagination.value.pageCount = Math.ceil(res.page.totalCount / (pagination.value.pageSize ?? 20));
    }
  } finally {
    loading.value = false;
  }
}

function handlePageSizeChange(pageSize: number) {
  pagination.value.pageSize = pageSize;
  loadData(1);
}

function resetSearch() {
  searchForm.value = {};
  loadData(1);
}

// --- 新增/编辑 ---
function showAdd() {
  isEdit.value = false;
  editForm.value = {};
  formVisible.value = true;
}

async function showEdit(row: Record<string, any>) {
  isEdit.value = true;
  const id = row[pkField.value];
  const res = await api.page.getDetail(props.type, id);
  editForm.value = { ...(res.data as any) };
  formVisible.value = true;
}

async function handleSubmit() {
  try {
    await editFormRef.value?.validate();
  } catch {
    return false;
  }
  formLoading.value = true;
  try {
    if (isEdit.value) {
      await api.page.update(props.type, editForm.value);
      message.success('保存成功');
    } else {
      await api.page.add(props.type, editForm.value);
      message.success('新增成功');
    }
    formVisible.value = false;
    loadData();
  } catch (err: any) {
    message.error(err?.message ?? '操作失败');
    return false;
  } finally {
    formLoading.value = false;
  }
}

// --- 详情 ---
async function showDetail(row: Record<string, any>) {
  const id = row[pkField.value];
  const res = await api.page.getDetail(props.type, id);
  detailData.value = (res.data as any) ?? {};
  detailVisible.value = true;
}

// --- 删除 ---
async function handleDelete(row: Record<string, any>) {
  const id = row[pkField.value];
  try {
    await api.page.remove(props.type, id);
    message.success('删除成功');
    loadData();
  } catch (err: any) {
    message.error(err?.message ?? '删除失败');
  }
}

async function handleDeleteSelect() {
  try {
    await api.page.deleteSelect(props.type, checkedKeys.value);
    message.success('批量删除成功');
    checkedKeys.value = [];
    loadData();
  } catch (err: any) {
    message.error(err?.message ?? '批量删除失败');
  }
}

// --- 导出 ---
function handleExport(format: string) {
  const url = api.page.getExportUrl(props.type, format);
  window.open(url, '_blank');
}

// --- 导入 ---
async function handleImport({ file }: UploadCustomRequestOptions) {
  if (!file.file) return;
  try {
    await api.page.importFile(props.type, file.file);
    message.success('导入成功');
    loadData();
  } catch (err: any) {
    message.error(err?.message ?? '导入失败');
  }
}

// --- ECharts ---
async function loadChartData() {
  try {
    const res = await api.page.getChartData(props.type);
    if (Array.isArray(res.data) && res.data.length > 0) {
      chartList.value = res.data;
    } else {
      chartList.value = [];
    }
  } catch {
    chartList.value = [];
  }
}

function setChartRef(el: HTMLElement | null, idx: number) {
  if (!el) return;
  nextTick(() => {
    if (chartInstances.value[idx]) {
      chartInstances.value[idx].dispose();
    }
    const instance = markRaw(echarts.init(el));
    const option = chartList.value[idx];
    if (option) {
      instance.setOption(option);
    }
    chartInstances.value[idx] = instance;
  });
}

function onChartResize() {
  for (const inst of chartInstances.value) {
    inst?.resize();
  }
}

// --- 初始化 ---
onMounted(async () => {
  await loadFields();
  await Promise.all([loadData(1), loadChartData()]);
  window.addEventListener('resize', onChartResize);
});

onBeforeUnmount(() => {
  window.removeEventListener('resize', onChartResize);
  for (const inst of chartInstances.value) {
    inst?.dispose();
  }
});
</script>

<style scoped>
.dynamic-page {
  padding: 8px;
}
.toolbar {
  margin-bottom: 12px;
}
</style>
