<template>
  <div class="default-list" :style="listShellStyle">
    <component :is="headerSection" v-if="headerSection" />

    <!-- 视图背景色覆盖：视图工具栏 + 分布 + 搜索 + 表格 -->
    <div class="list-surface" :class="{ 'list-surface--chrome': hasChromeBg }" :style="listSurfaceStyle">
      <div class="page-tools">
        <a-space>
          <NamedViewsToolbar
            v-if="viewState"
            :views="viewState.views"
            :active-id="viewState.activeViewId"
            @switch="onSwitchView"
            @create="onCreateView"
            @rename="onRenameView"
            @remove="onRemoveView"
            @reset="onResetViews"
            @open-config="configDrawerVisible = true"
          />
          <a-button
            v-if="flags.canDelete && chrome.allowDelete"
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

      <div v-if="statData" class="list-panel list-panel--dist">
        <div class="list-dist">
          <div
            v-for="f in listFields.filter((x) => statData![x.name] != null)"
            :key="f.name"
            class="list-dist-item"
          >
            <div class="list-dist-label">{{ f.displayName || f.name }}</div>
            <div class="list-dist-value">{{ statData[f.name] }}</div>
          </div>
        </div>
      </div>

      <!-- 搜索面板：与表格面板视觉独立 -->
      <div
        v-if="showSearchPanel && searchFields.length"
        class="list-panel list-panel--search"
      >
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
      </div>

      <!-- 表格面板：工具栏 + 表格 + 分页 -->
      <div class="list-panel list-panel--table">
        <div class="list-topbar">
          <a-space>
            <a-button
              v-if="flags.canAdd && chrome.allowAdd"
              type="primary"
              @click="openAdd"
            >
              + {{ chrome.addButtonText || '添加记录' }}
            </a-button>
            <a-button v-if="chrome.customButton" @click="onCustomButton">自定义</a-button>
          </a-space>
          <a-space>
            <a-button
              v-if="chrome.showFilter"
              type="text"
              @click="filterPanelOpen = !filterPanelOpen"
            >
              筛选
            </a-button>
            <a-button v-if="chrome.showGroup" type="text" @click="onToolbarGroup">分组</a-button>
            <a-button v-if="chrome.showSort" type="text" @click="onToolbarSort">排序</a-button>
            <a-button
              v-if="chrome.showSearch"
              type="text"
              @click="searchPanelOpen = !searchPanelOpen"
            >
              搜索
            </a-button>
          </a-space>
        </div>

        <a-spin :loading="loading" style="width: 100%">
          <ListTable
            v-if="tableColumns.length"
            :records="tableData"
            :columns="tableColumns"
            :row-key="pkField"
            :selected-keys="selectedKeys"
            :show-checkbox="flags.canDelete && chrome.allowDelete"
            :can-edit="flags.canEdit"
            :can-delete="flags.canDelete && chrome.allowDelete"
            :can-view-detail="chrome.allowViewDetail"
            :show-expand="chrome.expandRow"
            :enable-sort="chrome.showSort"
            :height="resolvedTableHeight"
            @row-dbl-click="openDetail"
            @selection-change="onSelectionChange"
            @columns-change="onColumnsChange"
            @sort-change="onSortChange"
            @action="onTableAction"
          />
          <a-empty v-else description="暂无列表字段（GetPage.list 为空）" />
        </a-spin>

        <div v-if="chrome.showPager" class="list-pager">
          <a-pagination
            :current="pagination.current"
            :page-size="pagination.pageSize"
            :total="pagination.total"
            show-total
            show-page-size
            @change="onPageChange"
            @page-size-change="onPageSizeChange"
          />
        </div>
      </div>
    </div>

    <ViewConfigDrawer
      v-if="viewState"
      v-model:visible="configDrawerVisible"
      :type-path="typePath"
      :view-name="getActiveView(viewState).name"
      :columns="activeColumns"
      :titles="columnTitles"
      :sort="activeSort"
      :chrome="getActiveView(viewState).chrome"
      @update:columns="onColumnsChange"
      @update:sort="onConfigSort"
      @update:chrome="onChromeChange"
      @update:name="onConfigRename"
    />

    <RecordDrawer
      v-model:visible="drawerVisible"
      :type-path="typePath"
      :fields="drawerFields"
      :model="formModel"
      :mode="drawerMode"
      :pk-field="pkField"
      :can-edit="flags.canEdit"
      :saving="saving"
      :show-history-tabs="showHistoryTabs"
      :can-prev="drawerCanPrev"
      :can-next="drawerCanNext"
      @save="handleSave"
      @edit="drawerMode = 'edit'"
      @prev="navigateRecord(-1)"
      @next="navigateRecord(1)"
    />

    <ListChartModal v-model:visible="chartVisible" :charts="chartList" />
  </div>
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent, onMounted, reactive, ref, watch, type Component } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import { IconDown } from '@arco-design/web-vue/es/icon';
import type { PageSetting } from '@cube/api-core';
import { EXPORT_FORMATS } from '@cube/page-utils';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { useEntityViewProfileStore } from '@/stores/entityViewProfile';
import type { ControlType, FieldMeta } from '@/core/types/field';
import { toFieldMetas } from '@/core/utils/fieldNormalize';
import { resolveListControl, resolveSearchControl } from '@/core/utils/fieldControl';
import {
  defaultBadgeColumnWidth,
  isBadgeField,
  resolveCellBadge,
  resolveCellLabel,
} from '@/core/utils/fieldBadge';
import { resolveCrudFlags } from '@/core/utils/permissions';
import { getValueByKey } from '@/core/utils/url';
import { enrichFieldsWithEnumDataSource, fetchBatchLabel } from '@/core/utils/lov-api';
import { getSectionLoader } from '@/core/composables/useSections';
import { selectListColumns } from '@/core/utils/listColumns';
import { prepareSubmitPayload } from '@/core/utils/submitPayload';
import { formatApiError } from '@/core/utils/apiError';
import { FieldKind } from '@cube/api-core';
import {
  buildSortPayload,
  getActiveView,
  mergeColumns,
  resolveChrome,
  type ColumnPref,
  type EntityViewState,
  type ViewChrome,
  type ViewSort,
} from '@/core/utils/entityViewProfile';
import FieldInput from '@/components/FieldInput.vue';
import ListTable from '@/features/vtable/ListTable.vue';
import RecordDrawer from './RecordDrawer.vue';
import ListChartModal from './ListChartModal.vue';
import NamedViewsToolbar from './NamedViewsToolbar.vue';
import ViewConfigDrawer from './ViewConfigDrawer.vue';

const props = defineProps<{
  type: string;
  authId?: number;
}>();

const userStore = useUserStore();
const evpStore = useEntityViewProfileStore();
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
const labelCache = reactive<Record<string, Record<string, string>>>({});
const configDrawerVisible = ref(false);
const viewState = ref<EntityViewState | null>(null);

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
});

const searchForm = reactive<Record<string, unknown>>({});
const formModel = reactive<Record<string, unknown>>({});
const drawerVisible = ref(false);
const drawerMode = ref<'add' | 'edit' | 'detail'>('add');
/** 当前抽屉记录在可见列表（本页 tableData）中的下标；新建为 -1 */
const drawerRowIndex = ref(-1);
const saving = ref(false);
const chartVisible = ref(false);
const chartList = ref<unknown[]>([]);
const tableHeight = 520;

const exportFormats = EXPORT_FORMATS;

const headerSection = computed<Component | null>(() => {
  const loader = getSectionLoader(typePath.value, 'ListPageHeader');
  if (!loader) return null;
  return defineAsyncComponent(loader as () => Promise<{ default: Component }>);
});

const flags = computed(() =>
  resolveCrudFlags(userStore.getMenuPermission(typePath.value), pageSetting.value),
);

/** 只读实体列表页不展示历史与评论（新建由表单 mode 自行隐藏） */
const showHistoryTabs = computed(() => pageSetting.value?.isReadOnly !== true);

const drawerFields = computed(() => {
  if (drawerMode.value === 'add') return addFields.value;
  if (drawerMode.value === 'edit') return editFields.value;
  return detailFields.value.length ? detailFields.value : listFields.value;
});

const metaKeys = computed(() => selectListColumns(listFields.value).map((f) => f.name));

const columnTitles = computed(() => {
  const m: Record<string, string> = {};
  for (const f of listFields.value) m[f.name] = f.displayName || f.name;
  return m;
});

/** 展示用列：偏好 ∩ 元数据；偏好空时回落元数据全列 */
const activeColumns = computed<ColumnPref[]>(() => {
  const keys = metaKeys.value;
  const prefs = viewState.value ? getActiveView(viewState.value).columns : [];
  return mergeColumns(keys, prefs);
});

const activeSort = computed<ViewSort | null>(() =>
  viewState.value ? getActiveView(viewState.value).sort || null : null,
);

const chrome = computed(() =>
  resolveChrome(viewState.value ? getActiveView(viewState.value) : null),
);

const filterPanelOpen = ref(false);
const searchPanelOpen = ref(true);

const listShellStyle = computed(() => {
  const c = chrome.value;
  const style: Record<string, string> = { padding: '0 4px' };
  if (c.widthMode === 'fill') style.width = '100%';
  if (c.heightMode === 'fill') style.minHeight = 'calc(100vh - 180px)';
  return style;
});

function hexToRgba(hex: string, opacityPct: number): string {
  const h = hex.replace('#', '');
  if (h.length !== 6) return hex;
  const r = parseInt(h.slice(0, 2), 16);
  const g = parseInt(h.slice(2, 4), 16);
  const b = parseInt(h.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${Math.max(0, Math.min(1, opacityPct / 100))})`;
}

/** 是否配置了视图自定义背景（作用于搜索+表格整块） */
const hasChromeBg = computed(() => {
  const c = chrome.value;
  return c.bgPreset === 'custom' && !!c.bgColor;
});

/** 背景色：视图工具栏（命名视图/配置/图表）+ 分布 + 搜索 + 表格 */
const listSurfaceStyle = computed(() => {
  const c = chrome.value;
  const style: Record<string, string> = {};
  if (c.bgPreset === 'custom' && c.bgColor && c.bgColor !== 'transparent') {
    style.backgroundColor = c.bgColor.startsWith('#')
      ? hexToRgba(c.bgColor, c.bgOpacity)
      : c.bgColor;
  } else if (c.bgPreset === 'custom' && c.bgColor === 'transparent') {
    style.backgroundColor = 'transparent';
  }
  if (c.bgBlur > 0) style.backdropFilter = `blur(${Math.round(c.bgBlur / 5)}px)`;
  return style;
});

const showSearchPanel = computed(
  () =>
    searchFields.value.length > 0 &&
    ((chrome.value.showSearch && searchPanelOpen.value) ||
      (chrome.value.showFilter && filterPanelOpen.value)),
);

const resolvedTableHeight = computed(() => {
  const mode = chrome.value.heightMode;
  if (mode === 'fit') return Math.max(240, 48 + tableData.value.length * 40);
  if (mode === 'fill' && typeof window !== 'undefined') {
    return Math.max(400, window.innerHeight - 360);
  }
  return tableHeight;
});

const tableColumns = computed(() =>
  activeColumns.value.map((pref) => {
    const field = listFields.value.find((f) => f.name === pref.key);
    const badge = !!field && isBadgeField(field);
    const width =
      pref.width ||
      (field && badge ? defaultBadgeColumnWidth(field) : undefined);
    return {
      pref: width && !pref.width ? { ...pref, width } : pref,
      title: pref.title?.trim() || field?.displayName || pref.key,
      badge,
      badgeOf: field
        ? (row: Record<string, unknown>) => {
            const raw = getValueByKey(row, field.name);
            return resolveCellBadge(field, raw);
          }
        : undefined,
      format: (row: Record<string, unknown>) =>
        field ? renderCell(field, row) : String(row[pref.key] ?? '-'),
    };
  }),
);

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
  // 优先元数据 dataSource（GetPage 已物化 / Enum Meta 灌入）
  if (field.dataSource && Object.keys(field.dataSource).length) {
    return resolveCellLabel(field, raw);
  }
  const kind = resolveListControl(field);
  if (kind === 'boolean') return resolveCellLabel(field, raw);
  if (kind === 'lov' && field.lovCode) {
    const map = labelCache[field.lovCode] || {};
    return map[String(raw)] ?? String(raw);
  }
  return String(raw);
}

async function hydrateLovLabels(rows: Record<string, unknown>[]) {
  // 仅对仍无 dataSource 的 LIST/其它 LOV 走 BatchLabel；Enum 已在 enrich 阶段灌入
  const lovFields = listFields.value.filter(
    (f) =>
      f.lovCode &&
      resolveListControl(f) === 'lov' &&
      !(f.dataSource && Object.keys(f.dataSource).length),
  );
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
      // 回写到字段，后续行/徽章不再重复请求
      f.dataSource = { ...(f.dataSource || {}), ...map };
    } catch {
      /* ignore */
    }
  }
}

async function loadFields() {
  const page = await cubeApi.page.getPage(typePath.value);
  // 开发代理未覆盖业务 Area 时，Vite 会返回 index.html 字符串，导致 list 为空
  if (!page || typeof page !== 'object' || Array.isArray(page)) {
    Message.error('GetPage 响应无效：请确认开发代理已转发业务 Area（如 /School）到后端');
    listFields.value = [];
    return;
  }
  const meta = (page.data || {}) as Record<string, unknown>;
  if (typeof meta === 'string' || !meta || Array.isArray(meta)) {
    Message.error('GetPage 未返回 JSON（常为代理未命中）。请重启 pnpm dev 后再试');
    listFields.value = [];
    return;
  }
  pageSetting.value =
    (meta.setting as PageSetting | undefined) ??
    (meta.pageSetting as PageSetting | undefined) ??
    null;
  let list = toFieldMetas(
    (meta.list as never) || ((meta.fields as { list?: never })?.list),
  ).filter((f) => !!f.name);
  // GetPage.list 异常为空时回落 GetFields(List)
  if (!list.length) {
    try {
      const fb = await cubeApi.page.getFields(typePath.value, FieldKind.List);
      list = toFieldMetas(fb.data).filter((f) => !!f.name);
    } catch {
      /* ignore */
    }
  }
  const nested = meta.fields as
    | { list?: unknown; search?: unknown; form?: { addForm?: unknown; editForm?: unknown; detail?: unknown } }
    | undefined;
  const search = toFieldMetas((meta.search || nested?.search) as never).filter(
    (f) => !!f.name && !f.primaryKey && f.typeName !== 'Guid',
  );
  const add = toFieldMetas((meta.addForm || nested?.form?.addForm) as never).filter(
    (f) => !!f.name,
  );
  const edit = toFieldMetas((meta.editForm || nested?.form?.editForm) as never).filter(
    (f) => !!f.name,
  );
  const detail = toFieldMetas((meta.detail || nested?.form?.detail) as never).filter(
    (f) => !!f.name,
  );
  // 一次 Meta 灌入 Enum dataSource，列表徽章与表单下拉共用
  await enrichFieldsWithEnumDataSource([...list, ...search, ...add, ...edit, ...detail]);
  listFields.value = list;
  searchFields.value = search;
  addFields.value = add;
  editFields.value = edit;
  detailFields.value = detail;
  const pk = listFields.value.find((f) => f.primaryKey);
  pkField.value = pk?.name || 'id';
}

async function loadProfile() {
  viewState.value = await evpStore.load(typePath.value, metaKeys.value);
  // 再 rematch 一次，确保与最新 listFields 对齐
  if (metaKeys.value.length) {
    viewState.value = evpStore.rematch(typePath.value, metaKeys.value);
  }
}

async function loadData() {
  loading.value = true;
  try {
    const sort = buildSortPayload(activeSort.value);
    const res = await cubeApi.page.getList(typePath.value, {
      pageIndex: pagination.current - 1,
      pageSize: pagination.pageSize,
      ...sort,
      ...searchForm,
    });
    const rows = (res.data as Record<string, unknown>[]) || [];
    tableData.value = rows;
    statData.value = (res.stat as Record<string, unknown>) ?? null;
    if (res.page) pagination.total = res.page.totalCount || 0;
    await hydrateLovLabels(rows);
  } finally {
    loading.value = false;
  }
}

function syncLocalState() {
  viewState.value = evpStore.getState(typePath.value);
}

function onColumnsChange(cols: ColumnPref[]) {
  // 列宽/顺序写入当前命名视图（ViewsJson + columnsJson）
  evpStore.updateColumns(typePath.value, cols);
  syncLocalState();
}

function onSortChange(payload: { field: string; desc: boolean } | null) {
  evpStore.updateSort(typePath.value, payload, true);
  syncLocalState();
  pagination.current = 1;
  loadData();
}

function onConfigSort(sort: ViewSort | null) {
  onSortChange(sort);
}

function onChromeChange(next: ViewChrome) {
  evpStore.updateChrome(typePath.value, next);
  syncLocalState();
}

function onConfigRename(name: string) {
  if (!viewState.value) return;
  onRenameView(viewState.value.activeViewId, name);
}

function onSwitchView(id: string) {
  evpStore.switchView(typePath.value, id);
  syncLocalState();
  pagination.current = 1;
  loadData();
}

function onCreateView(name: string) {
  evpStore.addView(typePath.value, name);
  syncLocalState();
  loadData();
}

function onRenameView(id: string, name: string) {
  evpStore.rename(typePath.value, id, name);
  syncLocalState();
}

function onRemoveView(id: string) {
  evpStore.remove(typePath.value, id);
  syncLocalState();
  loadData();
}

async function onResetViews() {
  await evpStore.reset(typePath.value, metaKeys.value);
  syncLocalState();
  pagination.current = 1;
  loadData();
}

function onTableAction(payload: { action: 'detail' | 'edit' | 'delete'; row: Record<string, unknown> }) {
  if (payload.action === 'edit') openEdit(payload.row);
  else if (payload.action === 'delete') {
    if (!chrome.value.allowDelete) return;
    Modal.confirm({
      title: '确认删除？',
      content: '删除后不可恢复',
      onOk: () => handleDelete(payload.row),
    });
  } else openDetail(payload.row);
}

function clearModel() {
  Object.keys(formModel).forEach((k) => delete formModel[k]);
}

function findVisibleRowIndex(row: Record<string, unknown>): number {
  const id = getValueByKey(row, pkField.value);
  if (id == null || id === '') return -1;
  return tableData.value.findIndex((r) => getValueByKey(r, pkField.value) === id);
}

const drawerCanPrev = computed(
  () => drawerMode.value !== 'add' && drawerRowIndex.value > 0,
);
const drawerCanNext = computed(
  () =>
    drawerMode.value !== 'add' &&
    drawerRowIndex.value >= 0 &&
    drawerRowIndex.value < tableData.value.length - 1,
);

async function loadRecordIntoDrawer(
  row: Record<string, unknown>,
  mode: 'edit' | 'detail',
) {
  drawerMode.value = mode;
  drawerRowIndex.value = findVisibleRowIndex(row);
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

function openAdd() {
  drawerMode.value = 'add';
  drawerRowIndex.value = -1;
  clearModel();
  drawerVisible.value = true;
}

async function openEdit(row: Record<string, unknown>) {
  await loadRecordIntoDrawer(row, 'edit');
}

async function openDetail(row: Record<string, unknown>) {
  if (!chrome.value.allowViewDetail) return;
  await loadRecordIntoDrawer(row, 'detail');
}

async function navigateRecord(delta: -1 | 1) {
  const next = drawerRowIndex.value + delta;
  if (next < 0 || next >= tableData.value.length) return;
  const row = tableData.value[next];
  if (!row) return;
  const mode = drawerMode.value === 'edit' ? 'edit' : 'detail';
  await loadRecordIntoDrawer(row, mode);
}

function onToolbarGroup() {
  Message.info('分组能力将在后续版本提供');
}

function onToolbarSort() {
  Message.info('请点击表头进行排序');
}

function onCustomButton() {
  Message.info('自定义按钮：可按业务扩展');
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

function handleExport(format: string | number | Record<string, unknown> | undefined) {
  const key = String(format);
  window.open(`${typePath.value}/ExportFile?format=${encodeURIComponent(key)}`, '_blank');
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
  await loadProfile();
  await loadData();
}

watch(typePath, () => {
  pagination.current = 1;
  selectedKeys.value = [];
  bootstrap();
});

onMounted(bootstrap);
</script>

<style scoped>
.default-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
}
.page-tools {
  display: flex;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
}
.list-surface {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
}
.list-surface--chrome {
  padding: 12px;
  border-radius: 8px;
}
.list-panel {
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
  padding: 16px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  overflow: hidden;
}
.list-panel--search {
  padding-bottom: 4px;
}
.list-dist {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}
.list-dist-item {
  min-width: 120px;
  padding: 10px 14px;
  border-radius: 6px;
  background: var(--color-fill-2);
  border: 1px solid var(--color-border-2);
}
.list-dist-label {
  font-size: 12px;
  color: var(--color-text-3);
  margin-bottom: 4px;
}
.list-dist-value {
  font-size: 18px;
  font-weight: 600;
  color: var(--color-text-1);
}
.list-topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}
.list-pager {
  margin-top: 12px;
  display: flex;
  justify-content: flex-end;
}
</style>
