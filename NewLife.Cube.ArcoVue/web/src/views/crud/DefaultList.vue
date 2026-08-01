<template>
  <div class="default-list" style="padding: 0 4px">
    <component :is="headerSection" v-if="headerSection" />
    <!-- 搜索 -->
    <a-card v-if="searchFields.length" style="margin-bottom: 12px" :bordered="false">
      <a-form :model="searchForm" layout="inline" @submit.prevent="handleSearch">
        <a-form-item
          v-for="field in searchFields"
          :key="field.name"
          :label="field.displayName || field.name"
        >
          <FieldInput
            :field="field"
            :model-value="searchForm[field.name]"
            :control-override="searchControlOf(field)"
            style="min-width: 160px"
            @update:model-value="(v) => (searchForm[field.name] = v)"
          />
        </a-form-item>
        <a-form-item>
          <a-space>
            <a-button type="primary" html-type="submit">搜索</a-button>
            <a-button @click="handleReset">重置</a-button>
          </a-space>
        </a-form-item>
      </a-form>
    </a-card>

    <!-- 工具条 -->
    <div style="display: flex; justify-content: space-between; margin-bottom: 12px">
      <a-space>
        <a-button v-if="flags.canAdd" type="primary" @click="openAdd">新增</a-button>
        <a-button
          v-if="flags.canDelete"
          status="danger"
          :disabled="!selectedKeys.length"
          @click="handleBatchDelete"
        >
          批量删除
        </a-button>
      </a-space>
      <a-space>
        <a-button @click="openChart">图表</a-button>
        <a-dropdown v-if="flags.canExport" @select="handleExport">
          <a-button>导出 <icon-down /></a-button>
          <template #content>
            <a-doption v-for="f in exportFormats" :key="f.key" :value="f.key">{{ f.label }}</a-doption>
          </template>
        </a-dropdown>
        <a-upload
          v-if="flags.canImport"
          :custom-request="handleImport"
          :show-file-list="false"
        >
          <template #upload-button>
            <a-button>导入</a-button>
          </template>
        </a-upload>
      </a-space>
    </div>

    <!-- 表格 -->
    <a-table
      :columns="columns"
      :data="tableData"
      :loading="loading"
      :pagination="isTree ? false : pagination"
      :row-selection="
        flags.canDelete
          ? {
              type: 'checkbox',
              showCheckedAll: true,
              selectedRowKeys: selectedKeys,
              onChange: onSelectionChange,
            }
          : undefined
      "
      :row-key="(record: Record<string, unknown>) => String(getValueByKey(record, pkField) ?? '')"
      :bordered="{ cell: true }"
      v-bind="treeProps"
      @page-change="onPageChange"
      @page-size-change="onPageSizeChange"
      @row-click="onRowClick"
    >
      <template #operations="{ record }">
        <a-space @click.stop>
          <a-button type="text" size="mini" @click="openDetail(record)">详情</a-button>
          <a-button v-if="flags.canEdit" type="text" size="mini" @click="openEdit(record)">编辑</a-button>
          <a-popconfirm v-if="flags.canDelete" content="确认删除？" @ok="handleDelete(record)">
            <a-button type="text" size="mini" status="danger">删除</a-button>
          </a-popconfirm>
        </a-space>
      </template>
    </a-table>

    <div
      v-if="statData"
      style="padding: 8px 16px; background: var(--color-fill-2); border-radius: 4px; margin-top: 8px"
    >
      <a-space>
        <span style="font-weight: bold">合计</span>
        <span
          v-for="f in listFields.filter((x) => statData![x.name] != null)"
          :key="f.name"
          style="margin-left: 16px"
        >
          {{ f.displayName || f.name }}：{{ statData[f.name] }}
        </span>
      </a-space>
    </div>

    <RecordDrawer
      v-model:visible="drawerVisible"
      :type-path="typePath"
      :fields="drawerFields"
      :model="formModel"
      :mode="drawerMode"
      :pk-field="pkField"
      :can-edit="flags.canEdit"
      :saving="saving"
      @save="handleSave"
      @edit="drawerMode = 'edit'"
    />

    <ListChartModal v-model:visible="chartVisible" :charts="chartList" />
  </div>
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent, onMounted, reactive, ref, watch, type Component } from 'vue';
import { Message } from '@arco-design/web-vue';
import { IconDown } from '@arco-design/web-vue/es/icon';
import type { PageSetting } from '@cube/api-core';
import { EXPORT_FORMATS } from '@cube/page-utils';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import type { ControlType, FieldMeta } from '@/core/types/field';
import { toFieldMetas } from '@/core/utils/fieldNormalize';
import { resolveListControl, resolveSearchControl } from '@/core/utils/fieldControl';
import { resolveCrudFlags } from '@/core/utils/permissions';
import { getValueByKey } from '@/core/utils/url';
import { detectTreeData, preferTreeByType } from '@/core/utils/tree';
import { fetchBatchLabel } from '@/core/utils/lov-api';
import { getSectionLoader } from '@/core/composables/useSections';
import { selectListColumns } from '@/core/utils/listColumns';
import { prepareSubmitPayload } from '@/core/utils/submitPayload';
import { formatApiError } from '@/core/utils/apiError';
import FieldInput from '@/components/FieldInput.vue';
import RecordDrawer from './RecordDrawer.vue';
import ListChartModal from './ListChartModal.vue';

const props = defineProps<{
  type: string;
  authId?: number;
}>();

const userStore = useUserStore();
const typePath = computed(() => props.type);

const listFields = ref<FieldMeta[]>([]);
const searchFields = ref<FieldMeta[]>([]);
const addFields = ref<FieldMeta[]>([]);
const editFields = ref<FieldMeta[]>([]);
const detailFields = ref<FieldMeta[]>([]);
const pageSetting = ref<PageSetting | null>(null);
const pkField = ref('id');

const tableData = ref<Record<string, unknown>[]>([]);
const loading = ref(false);
const selectedKeys = ref<(string | number)[]>([]);
const statData = ref<Record<string, unknown> | null>(null);
const isTree = ref(false);
const labelCache = reactive<Record<string, Record<string, string>>>({});

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showTotal: true,
  showPageSize: true,
});

const searchForm = reactive<Record<string, unknown>>({});
const formModel = reactive<Record<string, unknown>>({});
const drawerVisible = ref(false);
const drawerMode = ref<'add' | 'edit' | 'detail'>('add');
const saving = ref(false);
const chartVisible = ref(false);
const chartList = ref<unknown[]>([]);

const exportFormats = EXPORT_FORMATS;

const headerSection = computed<Component | null>(() => {
  const loader = getSectionLoader(typePath.value, 'ListPageHeader');
  if (!loader) return null;
  return defineAsyncComponent(loader as () => Promise<{ default: Component }>);
});

const flags = computed(() =>
  resolveCrudFlags(userStore.getMenuPermission(typePath.value), pageSetting.value),
);

const drawerFields = computed(() => {
  if (drawerMode.value === 'add') return addFields.value;
  if (drawerMode.value === 'edit') return editFields.value;
  return detailFields.value.length ? detailFields.value : listFields.value;
});

const treeProps = computed(() =>
  isTree.value ? { defaultExpandAllRows: true } : {},
);

/**
 * GetPage 返回的 list 数组即为列表可见字段。
 * 后端 DataField.Visible 默认 false 且 Fill 不置 true，不可再用 visible 过滤列。
 */
const columns = computed(() => {
  const cols = selectListColumns(listFields.value).map((f) => ({
    title: f.displayName || f.name,
    dataIndex: f.name,
    ellipsis: true,
    tooltip: true,
    width: f.maxWidth || undefined,
    render: ({ record }: { record: Record<string, unknown> }) => renderCell(f, record),
  }));
  cols.push({
    title: '操作',
    dataIndex: 'operations',
    slotName: 'operations',
    ellipsis: false,
    tooltip: false,
    width: 180,
  } as any);
  return cols;
});

function searchControlOf(field: FieldMeta): ControlType {
  const s = resolveSearchControl(field);
  const map: Record<string, ControlType> = {
    text: 'input',
    numberRange: 'inputNumber',
    dateRange: 'datePicker',
    datetimeRange: 'datePicker',
    timeRange: 'timePicker',
    lov: 'lov',
    lovMulti: 'lovMulti',
    switch: 'switch',
    fileExists: 'switch',
    select: 'select',
  };
  return map[s] ?? 'input';
}

function renderCell(field: FieldMeta, record: Record<string, unknown>): string {
  const raw = getValueByKey(record, field.name);
  if (raw == null || raw === '') return '-';
  const kind = resolveListControl(field);

  if (kind === 'boolean') {
    return raw === true || raw === 1 || raw === '1' || raw === 'true' ? '是' : '否';
  }
  if (kind === 'select' && field.dataSource) {
    return field.dataSource[String(raw)] ?? String(raw);
  }
  if (kind === 'lov' && field.lovCode) {
    const map = labelCache[field.lovCode] || {};
    return map[String(raw)] ?? String(raw);
  }
  if (kind === 'url' || field.url) {
    // 链接仍用纯文本展示目标值；完整 a 标签留给后续增强
    return String(raw);
  }
  if (kind === 'color') {
    return String(raw);
  }
  return String(raw);
}

async function hydrateLovLabels(rows: Record<string, unknown>[]) {
  const lovFields = listFields.value.filter((f) => f.lovCode && resolveListControl(f) === 'lov');
  for (const f of lovFields) {
    const code = f.lovCode!;
    const values = [
      ...new Set(
        rows
          .map((r) => getValueByKey(r, f.name))
          .filter((v) => v != null && v !== '')
          .map(String),
      ),
    ];
    if (!values.length) continue;
    try {
      const map = await fetchBatchLabel({ lovCode: code, values });
      labelCache[code] = { ...(labelCache[code] || {}), ...map };
    } catch {
      /* ignore */
    }
  }
}

async function loadFields() {
  const page = await cubeApi.page.getPage(typePath.value);
  const meta = page.data || {};
  pageSetting.value = meta.setting ?? meta.pageSetting ?? null;
  listFields.value = toFieldMetas(meta.list || meta.fields?.list);
  searchFields.value = toFieldMetas(meta.search || meta.fields?.search).filter(
    (f) => !f.primaryKey && f.typeName !== 'Guid',
  );
  addFields.value = toFieldMetas(meta.addForm || meta.fields?.form?.addForm);
  editFields.value = toFieldMetas(meta.editForm || meta.fields?.form?.editForm);
  detailFields.value = toFieldMetas(meta.detail || meta.fields?.form?.detail);
  const pk = listFields.value.find((f) => f.primaryKey);
  pkField.value = pk?.name || 'id';
}

async function loadData() {
  loading.value = true;
  try {
    const res = await cubeApi.page.getList(typePath.value, {
      pageIndex: pagination.current - 1,
      pageSize: isTree.value || preferTreeByType(typePath.value) ? 10000 : pagination.pageSize,
      ...searchForm,
    });
    const rows = (res.data as Record<string, unknown>[]) || [];
    tableData.value = rows;
    isTree.value = detectTreeData(rows) || (preferTreeByType(typePath.value) && detectTreeData(rows));
    // 若启发式为树但本页无 children，仍允许扁平
    if (preferTreeByType(typePath.value) && detectTreeData(rows)) isTree.value = true;
    else isTree.value = detectTreeData(rows);

    statData.value = (res.stat as Record<string, unknown>) ?? null;
    if (res.page) pagination.total = res.page.totalCount || 0;
    await hydrateLovLabels(rows);
  } finally {
    loading.value = false;
  }
}

function clearModel() {
  Object.keys(formModel).forEach((k) => delete formModel[k]);
}

function openAdd() {
  drawerMode.value = 'add';
  clearModel();
  drawerVisible.value = true;
}

async function openEdit(row: Record<string, unknown>) {
  drawerMode.value = 'edit';
  clearModel();
  const id = getValueByKey(row, pkField.value);
  try {
    const res = await cubeApi.page.getDetail(typePath.value, id as string | number);
    Object.assign(formModel, (res.data as object) || row);
  } catch {
    Object.assign(formModel, row);
  }
  drawerVisible.value = true;
}

async function openDetail(row: Record<string, unknown>) {
  drawerMode.value = 'detail';
  clearModel();
  const id = getValueByKey(row, pkField.value);
  try {
    const res = await cubeApi.page.getDetail(typePath.value, id as string | number);
    Object.assign(formModel, (res.data as object) || row);
  } catch {
    Object.assign(formModel, row);
  }
  drawerVisible.value = true;
}

function onRowClick(row: Record<string, unknown>) {
  openDetail(row);
}

async function handleSave() {
  saving.value = true;
  try {
    const mode = drawerMode.value === 'add' ? 'add' : 'edit';
    const fields = mode === 'add' ? addFields.value : editFields.value;
    const payload = prepareSubmitPayload({ ...formModel }, fields, {
      mode,
      pkField: pkField.value,
    });
    if (mode === 'add') await cubeApi.page.add(typePath.value, payload);
    else await cubeApi.page.update(typePath.value, payload);
    Message.success('保存成功');
    drawerVisible.value = false;
    await loadData();
  } catch (err) {
    Message.error(formatApiError(err, '保存失败'));
  } finally {
    saving.value = false;
  }
}

async function handleDelete(row: Record<string, unknown>) {
  const id = getValueByKey(row, pkField.value);
  await cubeApi.page.remove(typePath.value, id as string | number);
  Message.success('删除成功');
  loadData();
}

async function handleBatchDelete() {
  if (!selectedKeys.value.length) return;
  await cubeApi.page.deleteSelect(typePath.value, selectedKeys.value);
  Message.success('批量删除成功');
  selectedKeys.value = [];
  loadData();
}

function handleExport(format: string | number | Record<string, any> | undefined) {
  const key = String(format);
  const url = `${typePath.value}/ExportFile?format=${encodeURIComponent(key)}`;
  window.open(url, '_blank');
}

async function handleImport(option: {
  fileItem: { file?: File };
  onSuccess: () => void;
  onError: () => void;
}) {
  const file = option.fileItem.file;
  if (!file) {
    option.onError();
    return;
  }
  try {
    await cubeApi.page.importFile(typePath.value, file);
    Message.success('导入成功');
    option.onSuccess();
    loadData();
  } catch {
    Message.error('导入失败');
    option.onError();
  }
}

async function openChart() {
  try {
    const res = await cubeApi.page.getChartData(typePath.value);
    chartList.value = Array.isArray(res.data) ? res.data : [];
  } catch {
    chartList.value = [];
  }
  chartVisible.value = true;
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

async function bootstrap() {
  await loadFields();
  await loadData();
}

watch(typePath, () => {
  pagination.current = 1;
  bootstrap();
});

onMounted(bootstrap);
</script>
