<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import type { TableInstance } from 'element-plus';
import { usePageLogic } from '@newlifex/page-logic/pinia';
import { resolveColumnAlign, resolveColumnWidth } from '@newlifex/field-mapping';
import type { FieldMapping } from '@newlifex/field-mapping';
import { EXPORT_FORMATS, resolveUrl } from '@newlifex/page-utils';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import { useAuthStore } from '@/store/modules/auth';
import { resolveIconName } from '@/utils/icon';
import {
  buildSearchControl,
  buildTreeRows,
  canSort,
  formatCell,
  getCellLink,
  getRowValue,
  resolveImageSrc
} from '../shared';
import DynamicSearchBar from './search-bar.vue';
import EntityFormDialog from './entity-form-dialog.vue';

defineOptions({ name: 'EntityList' });

interface Props {
  /** 实体接口前缀，如 /Admin/User */
  type: string;
  /** 页面标题 */
  title?: string;
}

const props = defineProps<Props>();

const authStore = useAuthStore();

const page = usePageLogic(
  cubeApi,
  props.type,
  20,
  undefined,
  authStore.cubeInfo.permission,
  Boolean(authStore.cubeInfo.isSystemAdmin)
);

const { loading, tableData, statData, listFields, searchFields, pagination } = page;
const { addFields, editFields, canAdd, canEdit, canDelete, canExport, canImport, formLoading, pkField, entity } = page;

const errorMessage = ref('');
const searchParams = ref<Record<string, unknown>>({});
const sortParams = ref<Record<string, unknown>>({});

const controls = computed(() => searchFields.value.map(item => buildSearchControl(item.field)));

/** 后端声明为树形实体（/meta 的 setting.tree）：隐藏搜索与分页，一次取全量 */
const backendTree = computed(() => page.pageSetting.value?.tree === true);

/** 额外按树展示的层级实体：后端未声明 tree，但保留搜索与分页 */
const TREE_DISPLAY_TYPES = new Set(['/admin/department', '/cube/area']);

const treeDisplay = computed(() => backendTree.value || TREE_DISPLAY_TYPES.has(props.type.toLowerCase()));

/** 前端树形展示（部门/地区）默认展开全部节点；后端树形（菜单）保持折叠 */
const expandAll = computed(() => treeDisplay.value && !backendTree.value);

const tableRows = computed(() =>
  treeDisplay.value ? buildTreeRows(tableData.value, page.pkField.value) : tableData.value
);

/** 列设置项：树形模式下节点名虚字段不参与表格，也不出现在设置里 */
const columnSettingFields = computed(() =>
  treeDisplay.value
    ? listFields.value.filter(field => field.field.name.toLowerCase() !== 'treenodename')
    : listFields.value
);

/* ---------------- 列设置 ---------------- */

function storageKey() {
  return `${import.meta.env.VITE_STORAGE_PREFIX || ''}listColumns:${props.type}`;
}

const columnVisible = ref<Record<string, boolean>>(readColumnVisible());

function readColumnVisible(): Record<string, boolean> {
  try {
    const raw = localStorage.getItem(storageKey());
    return raw ? (JSON.parse(raw) as Record<string, boolean>) : {};
  } catch {
    return {};
  }
}

function isColumnVisible(field: FieldMapping) {
  const stored = columnVisible.value[field.field.name];
  if (stored !== undefined) return stored;
  return field.field.visible !== false;
}

const visibleFields = computed(() => {
  const fields = listFields.value.filter(isColumnVisible);
  if (!treeDisplay.value) return fields;

  // API 列表不返回节点名虚字段，改用名称列作为树列并置于首列承载展开箭头
  const rest = fields.filter(field => field.field.name.toLowerCase() !== 'treenodename');
  const nameIndex = rest.findIndex(field => ['displayname', 'name'].includes(field.field.name.toLowerCase()));
  if (nameIndex > 0) rest.unshift(...rest.splice(nameIndex, 1));
  return rest;
});

watch(
  columnVisible,
  value => {
    try {
      localStorage.setItem(storageKey(), JSON.stringify(value));
    } catch {
      // 忽略存储失败（隐私模式等）
    }
  },
  { deep: true }
);

function resetColumnSetting() {
  columnVisible.value = {};
}

/* ---------------- 表格 ---------------- */

function rowKey(row: Record<string, unknown>) {
  return String(getRowValue(row, page.pkField.value) ?? '');
}

function columnLabel(field: FieldMapping) {
  return field.field.header || field.field.displayName || field.field.name;
}

/** 树列承载展开箭头与缩进，需要更宽的固定下限 */
function columnMinWidth(field: FieldMapping) {
  if (treeDisplay.value && visibleFields.value[0] === field) return 240;
  return field.field.width === 'auto' ? 120 : undefined;
}

function cellLink(field: FieldMapping, row: Record<string, unknown>) {
  return getCellLink(field.field, row);
}

function cellText(field: FieldMapping, row: Record<string, unknown>) {
  if (field.field.text) return resolveUrl(field.field.text, row);
  return formatCell(field.field, row);
}

/** 同实体下钻链接（如地区父级 /Cube/Area?Id=110000、下级 ?parentId=110000） */
function drillParentId(href: string) {
  const [path, query = ''] = href.split('?');
  if (path.replace(/\/+$/, '').toLowerCase() !== props.type.toLowerCase()) return '';
  const params = new URLSearchParams(query);
  const id = params.get('parentId') || params.get('Id') || '';
  return /^\d+$/.test(id) && Number(id) > 0 ? id : '';
}

/** 点击下钻链接不跳转，直接在当前列表按父级编号过滤 */
function handleCellLinkClick(event: MouseEvent, field: FieldMapping, row: Record<string, unknown>) {
  const href = cellLink(field, row);
  const [path] = href.split('?');
  // 只拦截指向本实体的链接，其余（如外链）保持默认跳转
  if (path.replace(/\/+$/, '').toLowerCase() !== props.type.toLowerCase()) return;
  event.preventDefault();
  const id = drillParentId(href);
  if (!id) return;
  searchParams.value = { ...searchParams.value, parentId: id };
  page.setPagination(1);
  loadData();
}

/** 图片列的图片地址；非图片字段或值不像图片地址时返回空串，退回文本渲染 */
function cellImageSrc(field: FieldMapping, row: Record<string, unknown>) {
  if (field.widget !== 'image') return '';
  return resolveImageSrc(getRowValue(row, field.field.name));
}

function cellLinkTarget(field: FieldMapping) {
  return field.field.target === '_blank' ? '_blank' : undefined;
}

/** 固定居中的列：图标、类型、数据范围、数据部门、创建者、更新者、落库值、审计日志链接、地区层级/下级/父级 */
const CENTER_COLUMNS = new Set([
  'icon',
  'type',
  'datascope',
  'datadepartmentids',
  'createuser',
  'updateuser',
  'storeddisplayname',
  'link',
  'linkid',
  'level',
  'sub',
  'parentid'
]);

/** 表头统一居中；数值列右对齐便于比较大小，其余（名称/用户/种类/类别等）统一居中 */
function columnAlign(field: FieldMapping) {
  if (CENTER_COLUMNS.has(field.field.name.toLowerCase())) return 'center';
  const typeName = (field.field.typeName || '').toLowerCase();
  const timeLike = ['date', 'datetime', 'datetimeoffset', 'time', 'timespan'].includes(typeName);
  if (timeLike) return 'center';
  return resolveColumnAlign(field.field) === 'right' ? 'right' : 'center';
}

const statText = computed(() => {
  if (!statData.value) return '';
  const row = statData.value;
  return listFields.value
    .map(field => {
      const value = getRowValue(row, field.field.name);
      if (value === undefined || value === null || value === '') return '';
      return `${columnLabel(field)} ${formatCell(field.field, row)}`;
    })
    .filter(Boolean)
    .slice(0, 6)
    .join('　');
});

/* ---------------- 数据加载 ---------------- */

async function loadData() {
  errorMessage.value = '';
  try {
    await page.loadData({ ...searchParams.value, ...sortParams.value });
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : String(error);
  }
}

async function reload() {
  errorMessage.value = '';
  try {
    await page.loadFields();
    // 后端树形列表一次取全量，分页会裁掉父节点导致树不完整；前端树形展示保留分页
    if (backendTree.value) page.setPagination(1, 1000);
    syncColumnVisible();
    await loadData();
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : String(error);
  }
}

/** 首次加载时按字段元数据补齐列显示状态（visible=false 默认隐藏） */
function syncColumnVisible() {
  listFields.value.forEach(field => {
    if (columnVisible.value[field.field.name] === undefined) {
      columnVisible.value[field.field.name] = field.field.visible !== false;
    }
  });
}

function handleSearch(params: Record<string, unknown>) {
  searchParams.value = params;
  page.setPagination(1);
  loadData();
}

function handleReset() {
  searchParams.value = {};
  page.setPagination(1);
  loadData();
}

function handleSortChange({ prop, order }: { prop: string; order: 'ascending' | 'descending' | null }) {
  sortParams.value = prop && order ? { sort: prop, desc: order === 'descending' } : {};
  page.setPagination(1);
  loadData();
}

function handlePageChange(pageIndex: number) {
  page.setPagination(pageIndex);
  loadData();
}

function handlePageSizeChange(pageSize: number) {
  page.setPagination(1, pageSize);
  loadData();
}

/* ---------------- 布尔开关 / 图标 ---------------- */

const togglingKeys = ref<Set<string>>(new Set());

/** 列表布尔字段显示为开关：有修改权限且非虚拟/只读列 */
function isBooleanField(field: FieldMapping) {
  return (
    canEdit.value &&
    (field.field.typeName || '').toLowerCase() === 'boolean' &&
    field.field.virtual !== true &&
    field.field.readOnly !== true
  );
}

function toggleKey(field: FieldMapping, row: Record<string, unknown>) {
  return `${rowKey(row)}:${field.field.name}`;
}

function isToggling(field: FieldMapping, row: Record<string, unknown>) {
  return togglingKeys.value.has(toggleKey(field, row));
}

function isChecked(field: FieldMapping, row: Record<string, unknown>) {
  return Boolean(getRowValue(row, field.field.name));
}

/** 启用列走启停接口（带独立操作日志），其余布尔列按字段局部更新 */
async function handleBooleanChange(
  field: FieldMapping,
  row: Record<string, unknown>,
  value: string | number | boolean
) {
  const checked = value === true;
  const key = rowKey(row);
  const tk = toggleKey(field, row);
  togglingKeys.value = new Set(togglingKeys.value).add(tk);
  try {
    if ((field.field.name || '').toLowerCase() === 'enable') {
      await cubeApi.page.postAction(props.type, checked ? 'enableSelect' : 'disableSelect', { keys: key });
      ElMessage.success($t(checked ? 'page.list.enableSuccess' : 'page.list.disableSuccess'));
    } else {
      // 后端按整实体校验必填项，需先取详情再带全字段提交；沿用详情中的键名避免大小写重复
      const detail = (await cubeApi.page.getDetail<Record<string, unknown>>(props.type, key)).data ?? {};
      const name = field.field.name;
      const detailKey = Object.keys(detail).find(item => item.toLowerCase() === name.toLowerCase()) ?? name;
      await cubeApi.page.update(props.type, { ...detail, [detailKey]: checked }, key);
      ElMessage.success($t('common.modifySuccess'));
    }
    await loadData();
  } catch (error) {
    ElMessage.error(errorText(error));
  } finally {
    const next = new Set(togglingKeys.value);
    next.delete(tk);
    togglingKeys.value = next;
  }
}

/** 图标列的 Iconify 名称；非图标字段或空值时返回空串 */
function cellIconName(field: FieldMapping, row: Record<string, unknown>) {
  if ((field.field.name || '').toLowerCase() !== 'icon') return '';
  return resolveIconName(getRowValue(row, field.field.name));
}

/* ---------------- 新增 / 编辑 / 删除 ---------------- */

const tableRef = ref<TableInstance>();
const selectedRows = ref<Record<string, unknown>[]>([]);
const formVisible = ref(false);
const formMode = ref<'add' | 'edit'>('add');
const formRecord = ref<Record<string, unknown> | null>(null);

const formFields = computed(() => (formMode.value === 'add' ? addFields.value : editFields.value));
/** 表单映射下拉的额外选项来源：列表/搜索字段中带数据字典的字段（如部门搜索项） */
const optionFields = computed(() => [...listFields.value, ...searchFields.value]);
const selectedKeys = computed(() => selectedRows.value.map(row => rowKey(row)));

function errorText(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}

function handleSelectionChange(rows: Record<string, unknown>[]) {
  selectedRows.value = rows;
}

function openAdd() {
  formMode.value = 'add';
  formRecord.value = null;
  formVisible.value = true;
}

async function openEdit(row: Record<string, unknown>) {
  let record = row;
  try {
    record = await page.getDetail(rowKey(row));
  } catch {
    // 详情接口不可用时退回列表行数据
  }
  formMode.value = 'edit';
  formRecord.value = record;
  formVisible.value = true;
}

async function handleFormSubmit(data: Record<string, unknown>) {
  const isAdd = formMode.value === 'add';
  try {
    if (isAdd) {
      await page.add(data);
      ElMessage.success($t('common.addSuccess'));
      page.setPagination(1);
    } else {
      await page.update(data);
      ElMessage.success($t('common.modifySuccess'));
    }
    formVisible.value = false;
    await loadData();
  } catch (error) {
    ElMessage.error(errorText(error));
  }
}

async function handleDelete(row: Record<string, unknown>) {
  try {
    await ElMessageBox.confirm($t('common.confirmDelete'), $t('common.warning'), { type: 'warning' });
  } catch {
    return;
  }
  try {
    await page.remove(rowKey(row));
    ElMessage.success($t('common.deleteSuccess'));
    if (tableData.value.length === 1 && pagination.pageIndex > 1) page.setPagination(pagination.pageIndex - 1);
    await loadData();
  } catch (error) {
    ElMessage.error(errorText(error));
  }
}

async function handleBatchDelete() {
  const keys = selectedKeys.value;
  if (!keys.length) return;
  try {
    await ElMessageBox.confirm($t('page.list.confirmBatchDelete', { count: keys.length }), $t('common.warning'), {
      type: 'warning'
    });
  } catch {
    return;
  }
  try {
    await page.deleteSelect(keys);
    ElMessage.success($t('common.deleteSuccess'));
    tableRef.value?.clearSelection();
    if (tableData.value.length === keys.length && pagination.pageIndex > 1) page.setPagination(pagination.pageIndex - 1);
    await loadData();
  } catch (error) {
    ElMessage.error(errorText(error));
  }
}

function handleExport(format: string) {
  window.open(page.getExportUrl(format), '_blank');
}

/* ---------------- 高级：导入 ---------------- */

const importInput = ref<HTMLInputElement | null>(null);

function handleAdvancedCommand(command: string) {
  if (command === 'import') {
    importInput.value?.click();
    return;
  }
  handleExport(command);
}

async function handleImportChange(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  // 清空选择，允许再次导入同一文件
  input.value = '';
  if (!file) return;
  try {
    const res = await page.importFile(file);
    ElMessage.success(res.message || $t('page.list.importSuccess'));
    page.setPagination(1);
    await loadData();
  } catch (error) {
    ElMessage.error(errorText(error));
  }
}

onMounted(reload);
</script>

<template>
  <ElCard class="entity-list" body-class="entity-list__body">
    <template #header>
      <div class="flex-y-center justify-between gap-12px">
        <div class="min-w-0 flex-y-center gap-8px">
          <span class="truncate text-16px font-bold">{{ title || page.entity.value?.displayName || type }}</span>
          <ElTag v-if="page.pageSetting.value?.isReadOnly" size="small" type="info">
            {{ $t('page.list.readOnly') }}
          </ElTag>
        </div>
        <div class="flex-y-center gap-8px">
          <ElButton :loading="loading" @click="reload">
            <SvgIcon icon="mdi:refresh" class="mr-4px text-icon" />
            {{ $t('common.refresh') }}
          </ElButton>
          <ElPopover placement="bottom-end" trigger="click" :width="220">
            <template #reference>
              <ElButton>
                <SvgIcon icon="mdi:table-column" class="mr-4px text-icon" />
                {{ $t('common.columnSetting') }}
              </ElButton>
            </template>
            <div class="max-h-360px overflow-auto">
              <ElCheckbox
                v-for="field in columnSettingFields"
                :key="field.field.name"
                v-model="columnVisible[field.field.name]"
                class="w-full"
              >
                {{ columnLabel(field) }}
              </ElCheckbox>
            </div>
            <div class="mt-8px text-right">
              <ElButton size="small" text @click="resetColumnSetting">{{ $t('common.reset') }}</ElButton>
            </div>
          </ElPopover>
          <ElButton type="primary" :disabled="!canAdd" @click="openAdd">
            <SvgIcon icon="mdi:plus" class="mr-4px text-icon" />
            {{ $t('common.add') }}
          </ElButton>
          <ElButton type="danger" plain :disabled="!canDelete || !selectedKeys.length" @click="handleBatchDelete">
            {{ $t('common.batchDelete') }}
            <span v-if="selectedKeys.length">({{ selectedKeys.length }})</span>
          </ElButton>
          <ElDropdown trigger="click" :disabled="!canExport && !canImport" @command="handleAdvancedCommand">
            <ElButton>
              {{ $t('page.list.advanced') }}
              <SvgIcon icon="mdi:chevron-down" class="ml-4px text-icon" />
            </ElButton>
            <template #dropdown>
              <ElDropdownMenu>
                <ElDropdownItem
                  v-for="format in EXPORT_FORMATS"
                  :key="format.key"
                  :command="format.key"
                  :disabled="!canExport"
                >
                  {{ format.label }}
                </ElDropdownItem>
                <ElDropdownItem divided command="import" :disabled="!canImport">
                  {{ $t('page.list.import') }}
                </ElDropdownItem>
              </ElDropdownMenu>
            </template>
          </ElDropdown>
          <input
            ref="importInput"
            type="file"
            class="hidden"
            accept=".xls,.xlsx,.csv"
            @change="handleImportChange"
          />
        </div>
      </div>
    </template>

    <div class="entity-list__panel">
      <DynamicSearchBar
        v-if="controls.length && !backendTree"
        :controls="controls"
        :loading="loading"
        @search="handleSearch"
        @reset="handleReset"
      />

      <ElResult v-if="errorMessage" icon="error" :title="$t('page.list.loadFailed')" :sub-title="errorMessage">
        <template #extra>
          <ElButton type="primary" @click="reload">{{ $t('page.list.retry') }}</ElButton>
        </template>
      </ElResult>

      <ElTable
        v-else
        ref="tableRef"
        v-loading="loading"
        :data="tableRows"
        :row-key="rowKey"
        :tree-props="{ children: 'children' }"
        :default-expand-all="expandAll"
        height="100%"
        scrollbar-always-on
        stripe
        border
        class="entity-list__table"
        @sort-change="handleSortChange"
        @selection-change="handleSelectionChange"
      >
        <ElTableColumn v-if="canDelete" type="selection" width="46" />
        <ElTableColumn
          v-for="field in visibleFields"
          :key="field.field.name"
          :prop="field.field.name"
          :label="columnLabel(field)"
          :width="resolveColumnWidth(field.field)"
          :min-width="columnMinWidth(field)"
          :align="columnAlign(field)"
          header-align="center"
          :fixed="field.field.fixed"
          :sortable="canSort(field) ? 'custom' : false"
          show-overflow-tooltip
        >
          <template #default="{ row }">
            <ElSwitch
              v-if="isBooleanField(field)"
              :model-value="isChecked(field, row)"
              :loading="isToggling(field, row)"
              :disabled="isToggling(field, row)"
              @change="value => handleBooleanChange(field, row, value)"
            />
            <span
              v-else-if="cellIconName(field, row)"
              class="entity-list__icon"
              :title="String(getRowValue(row, field.field.name) ?? '')"
            >
              <SvgIcon :icon="cellIconName(field, row)" class="entity-list__icon-svg" />
            </span>
            <img
              v-else-if="cellImageSrc(field, row)"
              :src="cellImageSrc(field, row)"
              class="entity-list__image"
              alt=""
              draggable="false"
            />
            <ElLink
              v-else-if="cellLink(field, row)"
              :href="cellLink(field, row)"
              :target="cellLinkTarget(field)"
              type="primary"
              @click="handleCellLinkClick($event, field, row)"
            >
              {{ cellText(field, row) || (field.field.url ? columnLabel(field) : '-') }}
            </ElLink>
            <span v-else>{{ formatCell(field.field, row) }}</span>
          </template>
        </ElTableColumn>
        <ElTableColumn
          v-if="canEdit || canDelete"
          :label="$t('common.operate')"
          width="120"
          align="center"
          header-align="center"
          fixed="right"
        >
          <template #default="{ row }">
            <ElButton v-if="canEdit" link type="primary" @click="openEdit(row)">{{ $t('common.edit') }}</ElButton>
            <ElButton v-if="canDelete" link type="danger" @click="handleDelete(row)">{{ $t('common.delete') }}</ElButton>
          </template>
        </ElTableColumn>
        <template #empty>
          <ElEmpty :description="$t('common.noData')" :image-size="90" />
        </template>
      </ElTable>

      <div v-if="!backendTree" class="entity-list__footer">
        <span class="stat-text truncate">{{ statText }}</span>
        <ElPagination
          background
          :current-page="pagination.pageIndex"
          :page-size="pagination.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="pagination.totalCount"
          layout="total, sizes, prev, pager, next"
          @current-change="handlePageChange"
          @size-change="handlePageSizeChange"
        />
      </div>
    </div>

    <EntityFormDialog
      v-model="formVisible"
      :mode="formMode"
      :fields="formFields"
      :option-fields="optionFields"
      :record="formRecord"
      :loading="formLoading"
      :entity-name="entity?.displayName"
      :pk-field="pkField"
      :api-type="type"
      :tree-data="treeDisplay ? tableData : undefined"
      @submit="handleFormSubmit"
    />
  </ElCard>
</template>

<style scoped>
/* 页面撑满内容区：搜索区、表头固定顶部，分页固定底部，数据行区域内部滚动 */
.entity-list {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

/* 列表标题：在原 16px 左内间距基础上再增加 8px */
.entity-list :deep(.el-card__header) {
  padding-left: 24px;
}

.entity-list :deep(.entity-list__body) {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
}

/* 整页共用一个白色圆角面板，带轻微投影 */
.entity-list__panel {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
  min-height: 0;
  padding: 16px;
  border-radius: 8px;
  background: var(--el-bg-color-overlay);
  box-shadow: 0 2px 8px rgb(0 0 0 / 6%);
}

/* 搜索区不参与压缩，始终固定在面板顶部 */
.entity-list__panel > :deep(.dynamic-search) {
  flex-shrink: 0;
}

/* 搜索区与内容区共用面板左右内边距，保证左右对齐 */
.entity-list__table {
  flex: 1;
  min-height: 0;
  width: 100%;
}

.entity-list__panel > :deep(.el-result) {
  flex: 1;
}

.entity-list__footer {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

/* 居左单元格保持左右 5px 内间距 */
.entity-list__table :deep(td.el-table__cell.is-left .cell) {
  padding-left: 5px;
  padding-right: 5px;
}

.stat-text {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

/* 图标列：居中显示 20px 图标 */
.entity-list__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--el-text-color-regular);
  line-height: 1;
  vertical-align: middle;
}

.entity-list__icon :deep(svg) {
  width: 20px;
  height: 20px;
}

/* 图片列：32px 正方形圆角头像 */
.entity-list__image {
  display: inline-block;
  width: 32px;
  height: 32px;
  border-radius: 4px;
  object-fit: cover;
  vertical-align: middle;
  background: var(--el-fill-color-light);
}
</style>
