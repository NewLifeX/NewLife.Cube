<template>
  <div
    class="default-list"
    :class="{ 'default-list--fullscreen': fullscreen }"
    :style="listShellStyle"
  >
    <component :is="headerSection" v-if="headerSection" />

    <!-- 视图背景色覆盖：洞察（暂隐藏）+ 表格（含视图 Tab） -->
    <div class="list-surface" :class="{ 'list-surface--chrome': hasChromeBg }" :style="listSurfaceStyle">
      <!-- 洞察面板（原 QueryInsightPanel，更名 InsightPanel）：暂隐藏不渲染（v-if=false），等简易图表看板设计时启用；统计/图表状态由 DefaultList 持续维护 -->
      <InsightPanel
        v-if="false"
        :fields="searchFields"
        :model="searchForm"
        :show-stat="insight.showStat"
        :show-chart="insight.showChart"
        :stat-data="statData"
        :stat-labels="statLabels"
        :chart-data="chartData"
        :chart-loading="chartLoading"
        :chart-error="chartError"
        :master-time-name="masterTimeName"
        :master-time-display-name="masterTimeDisplayName"
        :enable-key="enableKey"
        :queries="savedQueries"
        :active-query-id="appliedQueryId"
        :params-dirty="queryParamsDirty"
        :can-save-query="queryHasParams"
        @search="handleSearch"
        @reset="handleReset"
        @apply="handleApplyQuery"
        @save-query="handleSaveQuery"
        @rename-query="handleRenameQuery"
        @delete-query="handleDeleteQuery"
      />

      <!-- 表格面板：视图 Tab + 工具栏 + 表格 + 分页 -->
      <div ref="tablePanelRef" class="list-panel list-panel--table">
        <div v-if="viewState" class="list-view-tabs">
          <ViewTabsToolbar
            :views="viewState.views"
            :active-id="viewState.activeViewId"
            :fields="listFields"
            :type-path="typePath"
            :is-admin="isAdmin"
            :fullscreen="fullscreen"
            @switch="onSwitchView"
            @create="onCreateView"
            @rename="onRenameView"
            @remove="onRemoveView"
            @duplicate="onDuplicateView"
            @reset="onResetViews"
            @save-as-default="onSaveAsDefault"
            @open-config="configDrawerVisible = true"
            @toggle-fullscreen="onToggleFullscreen"
          />
        </div>
        <div class="list-topbar">
          <a-space>
            <a-button v-if="flags.canAdd" type="primary" @click="openAdd">+ 添加记录</a-button>
          </a-space>
          <a-space>
            <!-- 甘特图缩放控制（仅甘特图视图显示，位于筛选前）：− / 当前等级 / + 按钮切换等级，默认月·日 -->
            <span v-if="activeViewKind === 'gantt'" class="tb-gantt-zoom">
              <button
                type="button"
                class="tb-zoom-btn"
                title="缩小"
                :disabled="ganttZoomLevel <= 0"
                @click="onGanttZoom(-1)"
              >
                −
              </button>
              <span class="tb-zoom-label">{{ ganttZoomLabel }}</span>
              <button
                type="button"
                class="tb-zoom-btn"
                title="放大"
                :disabled="ganttZoomLevel >= 4"
                @click="onGanttZoom(1)"
              >
                +
              </button>
            </span>
            <FilterBuilderPopover
              :visible="filterPopoverVisible"
              :fields="filterFields"
              :model-value="viewFilter"
              :can-save="!!activeViewId"
              @update:visible="onFilterPopoverVisible"
              @apply="onFilterApply"
              @save="onFilterSave"
            >
              <!-- 有筛选条件时按钮显示底纹，右上角主题主色圆形徽标（数字=条件数），点击徽标清除 -->
              <div
                class="tb-act"
                :class="{ 'is-active': viewFilter.conditions.length > 0 }"
              >
                <a-button v-if="chrome.showFilter" type="text">
                  <icon-park type="filter" />
                  筛选
                </a-button>
                <span
                  v-if="viewFilter.conditions.length"
                  class="tb-count"
                  title="清除筛选"
                  @click.stop="onClearFilter"
                >
                  {{ viewFilter.conditions.length }}
                </span>
              </div>
            </FilterBuilderPopover>

            <GroupPopover
              :visible="groupPopoverVisible"
              :fields="listFields"
              :model-value="viewGroup"
              :can-save="!!activeViewId"
              @update:visible="onGroupPopoverVisible"
              @apply="onGroupApply"
              @save="onGroupSave"
            >
              <!-- 仅表格视图支持分组（树状视图不允许，OSC-0015）；有分组时按钮底纹 + 右上角徽标（数字=分组字段数） -->
              <div
                class="tb-act"
                :class="{ 'is-active': viewGroup.length > 0 }"
              >
                <a-button
                  v-if="activeViewKind === 'table' && chrome.showGroup"
                  type="text"
                >
                  <icon-park type="connection-box" />
                  分组
                </a-button>
                <span
                  v-if="viewGroup.length"
                  class="tb-count"
                  title="清除分组"
                  @click.stop="onClearGroup"
                >
                  {{ viewGroup.length }}
                </span>
              </div>
            </GroupPopover>

            <a-button
              v-if="chrome.showSearch"
              type="text"
              @click="searchPanelOpen = !searchPanelOpen"
            >
              <icon-park type="search" />
              搜索
            </a-button>
            <a-dropdown v-if="advancedVisible" trigger="click" position="bottom">
              <a-button>
                高级 <icon-park type="down" />
              </a-button>
              <template #content>
                <a-doption v-if="flags.canImport" class="advanced-upload-option">
                  <a-upload
                    :custom-request="handleImport"
                    :show-file-list="false"
                    class="advanced-upload"
                  >
                    <template #upload-button>
                      <span class="advanced-menu-label">
                        <icon-park type="download" />
                        导入
                      </span>
                    </template>
                  </a-upload>
                </a-doption>
                <a-dsubmenu v-if="flags.canExport" value="export">
                  <template #default>
                    <icon-park type="export" />
                    导出
                  </template>
                  <template #content>
                    <a-doption
                      v-for="f in exportFormats"
                      :key="f.key"
                      @click="handleExport(f.key)"
                    >
                      {{ f.label }}
                    </a-doption>
                  </template>
                </a-dsubmenu>
                <a-doption
                  v-if="batchDeleteState.visible"
                  :disabled="batchDeleteState.disabled"
                  @click="confirmBatchDelete"
                >
                  <icon-park type="delete" />
                  批量删除
                </a-doption>
                <a-doption v-if="isAdmin" @click="formLayoutDrawerVisible = true">
                  <icon-park type="layout-one" />
                  表单布局
                </a-doption>
              </template>
            </a-dropdown>
          </a-space>
        </div>

        <a-spin :loading="loading" style="width: 100%">
          <template v-if="activeViewKind === 'table' || activeViewKind === 'tree'">
            <a-alert
              v-if="activeViewKind === 'tree' && tableData.length && !treeDataDetected"
              type="warning"
              style="margin-bottom: 8px"
            >
              当前数据无法组装为树。可改用表格视图，或确认后端返回含 ParentID/id 或 Path 字段的层级数据。
            </a-alert>
            <ListTable
              v-if="tableColumns.length"
              :records="displayRows"
              :columns="tableColumns"
              :row-key="pkField"
              :selected-keys="selectedKeys"
              :show-checkbox="activeViewKind === 'table'"
              :can-edit="flags.canEdit"
              :can-delete="flags.canDelete && chrome.allowDelete"
              :can-view-detail="chrome.allowViewDetail"
              :show-expand="chrome.expandRow"
              :enable-sort="chrome.showSort"
              :sort-state="activeSort"
              :hierarchy="activeViewKind === 'tree' && treeDataDetected"
              :grouped="isGrouped"
              :group-fields="isGrouped ? viewGroup : []"
              :group-label-of="groupLabelOf"
              :height="resolvedTableHeight"
              @row-dbl-click="openDetail"
              @selection-change="onSelectionChange"
              @columns-change="onColumnsChange"
              @sort-change="onSortChange"
              @action="onTableAction"
              @toggle-enable="onToggleEnable"
              @scroll-bottom="onTableScrollBottom"
            />
            <a-empty v-else description="暂无列表字段（GetPage.list 为空）" />
          </template>

          <CardList
            v-else-if="activeViewKind === 'card'"
            :key="cardListKey"
            :records="tableData"
            :columns="activeColumns"
            :fields="listFields"
            :mapping="activeCardMapping"
            :layout="activeCardMapping?.layout ?? 'standard'"
            :body-columns="activeCardMapping?.bodyColumns ?? 2"
            :field-orientation="activeCardMapping?.fieldOrientation ?? 'vertical'"
            :row-key="pkField"
            :height="resolvedTableHeight"
            :can-view-detail="chrome.allowViewDetail"
            :can-edit="flags.canEdit"
            :can-delete="flags.canDelete && chrome.allowDelete"
            :format-cell="renderCell"
            @detail="openDetail"
            @edit="openEdit"
            @delete="onCardDelete"
            @toggle-enable="onToggleEnable"
          />

          <KanbanBoard
            v-else-if="activeViewKind === 'kanban'"
            :records="tableData"
            :columns="activeColumns"
            :fields="listFields"
            :mapping="activeKanbanMapping"
            :row-key="pkField"
            :height="resolvedTableHeight"
            :can-view-detail="chrome.allowViewDetail"
            :can-edit="flags.canEdit"
            :can-delete="flags.canDelete && chrome.allowDelete"
            :format-cell="renderCell"
            @detail="openDetail"
            @edit="openEdit"
            @delete="onCardDelete"
            @toggle-enable="onToggleEnable"
          />

          <CalendarMonth
            v-else-if="activeViewKind === 'calendar'"
            :records="tableData"
            :fields="listFields"
            :mapping="activeCalendarMapping"
            :row-key="pkField"
            :height="resolvedTableHeight"
            @detail="openDetail"
          />

          <GanttView
            v-else-if="activeViewKind === 'gantt'"
            :records="tableData"
            :fields="listFields"
            :mapping="activeGanttMapping"
            :row-key="pkField"
            :height="resolvedTableHeight"
            :zoom-level="ganttZoomLevel"
            @detail="openDetail"
            @mapping-change="onGanttMappingChange"
          />

          <a-empty v-else description="未知视图类型" />
        </a-spin>

        <div v-if="showPagerBar" class="list-pager">
          <a-pagination
            :current="pagination.current"
            :page-size="effectivePageSize"
            :total="pagination.total"
            :page-size-options="
              activeViewKind === 'gantt' ? GANTT_PAGE_SIZE_OPTIONS : [...PAGE_SIZE_OPTIONS]
            "
            show-total
            show-page-size
            @change="onPageChange"
            @page-size-change="onPageSizeChange"
          />
        </div>
        <div v-else-if="isLargePageView" class="list-pager list-pager--hint">
          <a-typography-text type="secondary" class="list-pager-hint">
            当前视图一次最多加载 {{ effectivePageSize }} 条（共 {{ pagination.total }} 条）
          </a-typography-text>
        </div>
      </div>
    </div>

    <ViewConfigDrawer
      v-if="viewState"
      v-model:visible="configDrawerVisible"
      :type-path="typePath"
      :view-kind="activeViewKind"
      :view-name="getActiveView(viewState).name"
      :columns="activeColumns"
      :titles="columnTitles"
      :fields="listFields"
      :sort="activeSort"
      :chrome="getActiveView(viewState).chrome"
      :mapping="getActiveView(viewState).mapping"
      :insight="getActiveView(viewState).insight ?? null"
      @update:columns="onColumnsChange"
      @update:sort="onConfigSort"
      @update:chrome="onChromeChange"
      @update:mapping="onMappingChange"
      @update:insight="onInsightChange"
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
      :field-errors="fieldErrors"
      :layout="drawerFormLayout"
      @toggle-collapse="onToggleCollapse"
      @save="handleSave"
      @edit="drawerMode = 'edit'"
      @prev="navigateRecord(-1)"
      @next="navigateRecord(1)"
    />

    <ListChartModal v-model:visible="chartVisible" :charts="chartList" />

    <!-- 搜索抽屉（OSC-0016 面板重构）：右侧抽屉承载全部查询条件，每行一个；Q 第一、查询按钮右上角 -->
    <SearchDrawer
      v-model:visible="searchPanelOpen"
      :fields="searchFields"
      :model="searchForm"
      :master-time-name="masterTimeName"
      :master-time-display-name="masterTimeDisplayName"
      :enable-key="enableKey"
      :queries="savedQueries"
      :active-query-id="appliedQueryId"
      :params-dirty="queryParamsDirty"
      :can-save="queryHasParams"
      @search="handleSearch"
      @reset="handleReset"
      @apply="handleApplyQuery"
      @save-query="handleSaveQuery"
      @rename-query="handleRenameQuery"
      @delete-query="handleDeleteQuery"
    />

    <FormLayoutDrawer
      v-if="viewState"
      v-model:visible="formLayoutDrawerVisible"
      :type-path="typePath"
      :add-fields="addFields"
      :edit-fields="editFields"
      :detail-fields="detailFields"
      :can-configure="isAdmin"
    />
  </div>
</template>

<script setup lang="ts">
import {
  computed,
  defineAsyncComponent,
  nextTick,
  onBeforeUnmount,
  onMounted,
  reactive,
  ref,
  watch,
  type Component,
} from 'vue';
import { useRoute } from 'vue-router';
import { Message, Modal } from '@arco-design/web-vue';
import { ApiError, type PageSetting } from '@cube/api-core';
import { EXPORT_FORMATS } from '@cube/page-utils';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useViewProfileStore } from '@/stores/viewProfile';
import type { FieldMeta } from '@/core/types/field';
import { toFieldMetas } from '@/core/utils/fieldNormalize';
import { resolveListControl } from '@/core/utils/fieldControl';
import {
  defaultBadgeColumnWidth,
  isBadgeField,
  isBooleanToggleField,
  isEnableField,
  isTruthy,
  resolveCellBadge,
} from '@/core/utils/fieldBadge';
import { resolveCrudFlags } from '@/core/utils/permissions';
import { getValueByKey, normalizeKeysByFields, setValueByKey } from '@/core/utils/url';
import { formatFieldValue } from '@/core/utils/fieldFormat';
import {
  enrichFieldsWithEnumDataSource,
  enrichFieldsWithLookup,
  fetchBatchLabel,
} from '@/core/utils/lov-api';
import { getSectionLoader } from '@/core/composables/useSections';
import { selectListColumns } from '@/core/utils/listColumns';
import { prepareSubmitPayload } from '@/core/utils/submitPayload';
import { formatApiError } from '@/core/utils/apiError';
import { FieldKind } from '@cube/api-core';
import { resolveFieldsForKind } from '@/core/utils/fieldParts';
import {
  buildSortPayload,
  getActiveView,
  mergeColumns,
  resolveChrome,
  emptyViewFilter,
  serializeNamedView,
  type ColumnPref,
  type EntityViewState,
  type FormLayout,
  type ViewChrome,
  type ViewFilter,
  type ViewGroup,
  type ViewInsight,
  type ViewKind,
  type ViewMapping,
  type ViewSort,
} from '@/core/utils/viewProfile';
import {
  isLargePageViewKind,
  normalizePageSize,
  PAGE_SIZE_OPTIONS,
  parseViewKind,
  resolveBatchDeleteState,
  resolveViewPageSize,
  type CalendarMapping,
  type CardMapping,
  type GanttMapping,
  type KanbanMapping,
} from '@/core/utils/viewMapping';
import {
  cleanSearchParams,
  collectSearchKeys,
  matchesViewFilter,
  parseUrlSearch,
} from '@/core/utils/searchFilters';
import { isPersonField } from '@/core/utils/filterBuilder';
import { detectTreeData } from '@/core/utils/tree';
import { buildTree, canBuildTree } from '@/core/utils/treeBuilder';
import SearchDrawer from '@/features/search/SearchDrawer.vue';
import InsightPanel from '@/features/search/InsightPanel.vue';
/** VTable / 多视图异步加载，降低 DynamicPage 首包 */
const ListTable = defineAsyncComponent(() => import('@/features/vtable/ListTable.vue'));
const CardList = defineAsyncComponent(() => import('@/features/views/CardList.vue'));
const KanbanBoard = defineAsyncComponent(() => import('@/features/views/KanbanBoard.vue'));
const CalendarMonth = defineAsyncComponent(() => import('@/features/views/CalendarMonth.vue'));
const GanttView = defineAsyncComponent(() => import('@/features/views/GanttView.vue'));
import RecordDrawer from './RecordDrawer.vue';
import ListChartModal from './ListChartModal.vue';
import FormLayoutDrawer from './FormLayoutDrawer.vue';
import ViewTabsToolbar from './ViewTabsToolbar.vue';
import ViewConfigDrawer from './ViewConfigDrawer.vue';
import FilterBuilderPopover from './FilterBuilderPopover.vue';
import GroupPopover from './GroupPopover.vue';

const props = defineProps<{
  type: string;
  authId?: number;
}>();

const userStore = useUserStore();
const profileStore = useUserProfileStore();
const evpStore = useViewProfileStore();
const typePath = computed(() => props.type);

const listFields = ref<FieldMeta[]>([]);
const searchFields = ref<FieldMeta[]>([]);
const addFields = ref<FieldMeta[]>([]);
const editFields = ref<FieldMeta[]>([]);
const detailFields = ref<FieldMeta[]>([]);
const pageSetting = ref<PageSetting | null>(null);
const pkField = ref('id');

const tableData = ref<Record<string, unknown>[]>([]);
/** 后端原始数据（未应用视图级前端筛选 viewFilter）；视图切换/筛选变化时复用避免重复请求（重绘优化） */
const tableDataRaw = ref<Record<string, unknown>[]>([]);
const loading = ref(false);
/** Enable 徽标切换请求进行中：防止快速双击并发回跳 */
const enableBusy = ref(false);
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

const preferredDefaultView = computed<ViewKind>(() =>
  parseViewKind(profileStore.prefs.workspace.defaultView),
);

const preferredPageSize = computed(() =>
  Math.max(1, profileStore.prefs.workspace.pageSize || 20),
);

/** 页面级 PageSize（typePath 级，OSC-0012）；0 表示未配置，回退旧全局 workspace 种子 */
const pageProfileSize = computed(() => evpStore.getPageSize(typePath.value));

const effectivePageSizePref = computed(() => {
  const n = pageProfileSize.value;
  return n > 0 ? n : preferredPageSize.value;
});

const searchForm = reactive<Record<string, unknown>>({});
/** 主时间字段信息（OSC-0016）：GetPage setting 透传 */
const masterTimeName = ref<string | null>(null);
const masterTimeDisplayName = ref<string | null>(null);
/** 关键字 Q 启用（OSC-0016）；缺省 true */
const enableKey = ref(true);
/** 当前会话是否已显式执行过搜索/重置；未执行时有效条件取 URL→已保存基准（OSC-0012） */
const searchTouched = ref(false);
const route = useRoute();
const formModel = reactive<Record<string, unknown>>({});
const drawerVisible = ref(false);
const drawerMode = ref<'add' | 'edit' | 'detail'>('add');
/** 当前抽屉记录在可见列表（本页 tableData）中的下标；新建为 -1 */
const drawerRowIndex = ref(-1);
const saving = ref(false);
const chartVisible = ref(false);
const chartList = ref<unknown[]>([]);
const tableHeight = 520;
/** 后端字段级错误（FieldErrors），映射到表单对应字段（OSC-0009） */
const fieldErrors = ref<{ field: string; message: string }[]>([]);

const exportFormats = EXPORT_FORMATS;

const headerSection = computed<Component | null>(() => {
  const loader = getSectionLoader(typePath.value, 'ListPageHeader');
  if (!loader) return null;
  return defineAsyncComponent(loader as () => Promise<{ default: Component }>);
});

const flags = computed(() =>
  resolveCrudFlags(userStore.getMenuPermission(typePath.value), pageSetting.value),
);

/** 仅管理员角色可使用表单布局（与后端 Roles.Any(e => e.IsSystem) 对齐） */
const isAdmin = computed(() => userStore.userInfo?.isSystem === true);

/** 只读实体列表页不展示历史与评论（新建由表单 mode 自行隐藏） */
const showHistoryTabs = computed(() => pageSetting.value?.isReadOnly !== true);

/** 表单字段分区唯一入口：展示、回填、保存共用同一字段来源（OSC-0009） */
const fieldParts = computed(() => ({
  list: listFields.value,
  add: addFields.value,
  edit: editFields.value,
  detail: detailFields.value,
  search: searchFields.value,
}));

const drawerFields = computed(() =>
  resolveFieldsForKind(drawerMode.value, fieldParts.value),
);

/** 当前抽屉模式的受限表单布局（OSC-0013）；无配置返回 null */
const drawerFormLayout = computed<FormLayout | null>(() =>
  evpStore.getFormModeLayout(typePath.value, drawerMode.value),
);

/** 表单布局配置抽屉开关（OSC-0013） */
const formLayoutDrawerVisible = ref(false);

const metaKeys = computed(() => selectListColumns(listFields.value).map((f) => f.name));

const columnTitles = computed(() => {
  const m: Record<string, string> = {};
  for (const f of listFields.value) m[f.name] = f.displayName || f.name;
  return m;
});

/** 洞察统计标签显示名（按 listFields，与 GetList.stat 的 key 对齐，OSC-0012） */
const statLabels = computed(() => {
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

const activeViewKind = computed<ViewKind>(() =>
  viewState.value ? getActiveView(viewState.value).view : 'table',
);

/** 甘特图缩放等级（zoomScale.levels 下标 0~4：年/年月/月·日/周·日/日·时），默认月·日 */
const ganttZoomLevel = ref(2);
/** 甘特图分页器可选条数（大视图 pageSize 钳制 200~1000，甘特图底部现可翻页） */
const GANTT_PAGE_SIZE_OPTIONS = [200, 500, 1000];
/** 甘特图缩放等级名称（与 GanttView.buildZoomLevels 下标一一对应） */
const ganttZoomLabels = ['年', '年月', '月·日', '周·日', '日·时'];
/** 当前缩放等级名称（−/+ 按钮中间标签显示） */
const ganttZoomLabel = computed(
  () => ganttZoomLabels[ganttZoomLevel.value] ?? `${ganttZoomLevel.value}`,
);

/** 甘特图缩放等级切换（− 缩小 / + 放大，0~4 夹取） */
function onGanttZoom(delta: number) {
  ganttZoomLevel.value = Math.min(4, Math.max(0, ganttZoomLevel.value + delta));
}

const activeViewId = computed(() => viewState.value?.activeViewId ?? '');

/** 合法搜索 key（含范围字段 _min/_max 后缀，OSC-0012） */
const searchKeys = computed(() => collectSearchKeys(searchFields.value));

/** URL 搜索参数（当前会话；只影响本页，不自动写回存储） */
const urlSearch = computed(() =>
  parseUrlSearch(route.query as Record<string, unknown>, searchKeys.value),
);

/** 当前命名视图的已保存筛选（经合法 key 集清理） */
const savedSearch = computed(() => {
  if (!activeViewId.value) return {};
  return cleanSearchParams(
    evpStore.getViewFilters(typePath.value, activeViewId.value) ?? {},
    searchKeys.value,
  );
});

/** 未显式搜索时的基准条件：URL → 已保存 → 空 */
const baseSearch = computed(() => {
  if (Object.keys(urlSearch.value).length) return urlSearch.value;
  if (Object.keys(savedSearch.value).length) return savedSearch.value;
  return {};
});

/** 唯一有效搜索条件：会话内未搜索时取基准，搜索/重置后取表单正规化结果（OSC-0012）。
 *  筛选构建器为纯前端过滤，不并入后端请求（OSC-0015） */
const effectiveSearch = computed(() => {
  if (!searchTouched.value) return baseSearch.value;
  return cleanSearchParams({ ...searchForm }, searchKeys.value);
});

/** 当前命名视图的受限洞察配置（OSC-0012） */
const insight = computed<ViewInsight>(() => {
  if (!viewState.value) return { showStat: false, showChart: false };
  const v = getActiveView(viewState.value);
  return v?.insight ?? { showStat: false, showChart: false };
});

const chartData = ref<unknown[]>([]);
const chartLoading = ref(false);
const chartError = ref('');
/** 图表请求序号：切换筛选/视图/刷新时丢弃过期响应 */
let chartSeq = 0;

const activeMapping = computed(() =>
  viewState.value ? getActiveView(viewState.value).mapping : undefined,
);

const activeCardMapping = computed(
  () => (activeMapping.value?.kind === 'card' ? activeMapping.value : null) as CardMapping | null,
);

/** 列数/排版变更时强制重挂卡片列表，确保样式立即生效 */
const cardListKey = computed(() => {
  const m = activeCardMapping.value;
  return `card:${m?.layout ?? 'standard'}:${m?.bodyColumns ?? 2}:${m?.fieldOrientation ?? 'vertical'}`;
});
const activeKanbanMapping = computed(
  () =>
    (activeMapping.value?.kind === 'kanban' ? activeMapping.value : null) as KanbanMapping | null,
);
const activeCalendarMapping = computed(
  () =>
    (activeMapping.value?.kind === 'calendar'
      ? activeMapping.value
      : null) as CalendarMapping | null,
);
const activeGanttMapping = computed(
  () =>
    (activeMapping.value?.kind === 'gantt' ? activeMapping.value : null) as GanttMapping | null,
);

const isLargePageView = computed(() => isLargePageViewKind(activeViewKind.value));

const effectivePageSize = computed(() =>
  resolveViewPageSize(activeViewKind.value, pagination.pageSize, effectivePageSizePref.value),
);

const showPagerBar = computed(
  // 甘特图底部显示分页器可翻页（OSC-0019 后续）；看板/日历仍为大视图仅提示
  () => chrome.value.showPager && (!isLargePageView.value || activeViewKind.value === 'gantt'),
);

/** 树数据可用：后端已返回 children 树，或扁平行可组装为树 */
const treeDataDetected = computed(() => {
  const rows = tableData.value;
  return rows.length > 0 && (detectTreeData(rows) || canBuildTree(rows));
});

/** 树视图展示数据：后端已返回树或可组装时用树，否则回落原始行；字段元数据仍取自 GetPage */
const treeRows = computed(() => {
  if (activeViewKind.value !== 'tree') return tableData.value;
  // 后端已返回 children 树，直接使用，避免无谓重建
  if (detectTreeData(tableData.value)) return tableData.value;
  return canBuildTree(tableData.value) ? buildTree(tableData.value) : tableData.value;
});

const chrome = computed(() =>
  resolveChrome(viewState.value ? getActiveView(viewState.value) : null),
);

/** 批量删除门禁：表格视图 + 删除权限 + 允许删除 + 至少选中一行 */
const batchDeleteState = computed(() =>
  resolveBatchDeleteState({
    viewKind: activeViewKind.value,
    canDelete: flags.value.canDelete,
    allowDelete: chrome.value.allowDelete,
    selectedCount: selectedKeys.value.length,
  }),
);

/** 「高级」菜单仅在有可见操作（导入/导出/批量删除/管理员表单布局）时出现 */
const advancedVisible = computed(
  () =>
    flags.value.canImport ||
    flags.value.canExport ||
    batchDeleteState.value.visible ||
    isAdmin.value,
);

/** 搜索抽屉开关：默认收起，点击工具栏「搜索」打开 */
const searchPanelOpen = ref(false);

/** 筛选/分组弹层互斥：同一时刻只展示一个（OSC-0015） */
const activePopover = ref<'filter' | 'group' | null>(null);
const filterPopoverVisible = computed({
  get: () => activePopover.value === 'filter',
  set: (v: boolean) => {
    activePopover.value = v ? 'filter' : null;
  },
});
const groupPopoverVisible = computed({
  get: () => activePopover.value === 'group',
  set: (v: boolean) => {
    activePopover.value = v ? 'group' : null;
  },
});

/** 当前命名视图的筛选构建器方案（OSC-0015）：会话内存状态，应用仅改本地、保存才写 store 持久化 */
const localFilter = ref<ViewFilter>(emptyViewFilter());
const viewFilter = computed<ViewFilter>(() => localFilter.value);

/** 当前命名视图的多级分组字段（OSC-0015）：同筛选，应用仅改本地、保存才写 store */
const localGroup = ref<ViewGroup>([]);
const viewGroup = computed<ViewGroup>(() => localGroup.value);

/** 筛选构建器候选字段 = 当前视图可见列 ∪ 人员字段（创建者/更新者等即使列隐藏也可筛选，OSC-0015） */
const filterFields = computed<FieldMeta[]>(() => {
  const visible = new Set(
    activeColumns.value.filter((c) => c.visible).map((c) => c.key),
  );
  const visibleFields = listFields.value.filter((f) => visible.has(f.name));
  const hiddenPerson = listFields.value.filter(
    (f) => !visible.has(f.name) && isPersonField(f),
  );
  return [...visibleFields, ...hiddenPerson];
});

/** 分组展示：仅表格视图且配置了分组字段（树状视图不允许分组，OSC-0015） */
const isGrouped = computed(
  () => viewGroup.value.length > 0 && activeViewKind.value === 'table',
);

/** 表格数据：分组视图时对 treeRows 做多级分组（组头节点 + 数据行） */
/** 展示行：分组改由 ListTable 内 VTable 原生 groupBy 完成（OSC-0015 重构，参考官方 list-table-group-checkbox），
 *  此处始终传原始行（groupBy 在表格内部按分组字段重组并渲染组标题行） */
/** 列表/树增量渲染：非分组视图先渲染前 N 条，滚动接近底部（ListTable scrollBottom）再追加；
 *  分组视图需完整数据（VTable 内部 groupBy 分组统计），不做增量 */
const TABLE_INITIAL_VISIBLE = 100;
const TABLE_LOAD_STEP = 100;
const tableVisibleCount = ref(TABLE_INITIAL_VISIBLE);

const displayRows = computed(() => {
  const rows = treeRows.value;
  if (isGrouped.value) return rows;
  return rows.slice(0, tableVisibleCount.value);
});

/** 列表/树滚动接近底部：追加下一批行（懒加载，避免千条一次性传 VTable） */
function onTableScrollBottom() {
  const rows = treeRows.value;
  if (tableVisibleCount.value < rows.length) {
    tableVisibleCount.value = Math.min(rows.length, tableVisibleCount.value + TABLE_LOAD_STEP);
  }
}

/** 分组值显示标签：按分组字段 dataSource 枚举翻译（OSC-0015）；无映射回落显示原值 */
function groupLabelOf(field: string, value: unknown): string | undefined {
  const fm = listFields.value.find((f) => f.name === field);
  if (fm?.dataSource && value != null) return fm.dataSource[String(value)];
  return undefined;
}

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

/** 背景色：分布 + 搜索 + 表格（含视图 Tab） */
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

/** 预定义查询列表（OSC-0016）：实体级个人配置 */
const savedQueries = computed(() => evpStore.getQueries(typePath.value).queries);
/** 当前应用的预定义查询 id（会话内存，刷新后为 null） */
const appliedQueryId = computed(() => evpStore.getActiveQueryId(typePath.value));
/** 当前表单参数（cleanSearchParams 后）是否有任一非空键（含 Q/dtStart/dtEnd） */
const queryHasParams = computed(
  () => Object.keys(cleanSearchParams({ ...searchForm }, searchKeys.value)).length > 0,
);
/** 当前表单参数与 activeQuery 是否不一致（OSC-0016：不一致时条目不显示 ✓，应用标记保留） */
const queryParamsDirty = computed(() => {
  const id = appliedQueryId.value;
  if (!id) return false;
  const q = savedQueries.value.find((x) => x.id === id);
  if (!q) return false;
  const cur = cleanSearchParams({ ...searchForm }, searchKeys.value);
  return JSON.stringify(cur) !== JSON.stringify(q.params);
});

/** 列表面板引用：用于测量表格可用高度，保证分页器与外壳底部在首屏可见 */
const tablePanelRef = ref<HTMLElement | null>(null);
/** 当前视图是否全屏展示（固定铺满视口，覆盖顶部及左侧导航栏） */
const fullscreen = ref(false);
/** 动态测量的表格可用高度（default/fill 模式）；测量前回落固定 tableHeight */
const measuredTableHeight = ref(tableHeight);

/** 测量表格可用高度：scroll 可视区底 - 面板顶 - 面板内非表格固定部分（Tab/工具栏/分页器/padding）
 *  预留 16px 底部 gutter（= scroll padding-bottom），使底部间隙与左右上三边一致 */
function measureTableHeight() {
  if (typeof window === 'undefined') return;
  // fit 模式按内容自适应（分页器随内容在下方），无需测量
  if (chrome.value.heightMode === 'fit') return;
  const panel = tablePanelRef.value;
  if (!panel) return;
  const pr = panel.getBoundingClientRect();
  // 非表格固定部分 = 面板总高 - 当前表格高；表格高度更新后面板高随之变化，该差值保持稳定
  const nonTableH = pr.height - measuredTableHeight.value;
  let avail: number;
  if (fullscreen.value) {
    // 全屏：面板固定铺满视口（顶部贴近视口），可用高度 = 视口高 - 面板顶 - 非表格部分 - gutter
    avail = Math.floor(window.innerHeight - pr.top - nonTableH - 16);
  } else {
    const scroll = document.querySelector<HTMLElement>('.layout-content__scroll');
    if (!scroll) return;
    const sr = scroll.getBoundingClientRect();
    avail = Math.floor(sr.bottom - pr.top - nonTableH - 16);
  }
  measuredTableHeight.value = Math.max(240, avail);
}

/** 切换全屏/默认展示；布局变化后延迟多次重测表格高度，确保面板尺寸稳定后再填满 */
function onToggleFullscreen() {
  fullscreen.value = !fullscreen.value;
  nextTick(measureTableHeight);
  window.setTimeout(measureTableHeight, 200);
  window.setTimeout(measureTableHeight, 600);
}

/** Esc 退出全屏 */
function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && fullscreen.value) onToggleFullscreen();
}

/** scroll 容器尺寸变化（窗口/布局调整/侧栏折叠）时重测 */
let tableResizeObserver: ResizeObserver | null = null;
function observeTableHeight() {
  tableResizeObserver?.disconnect();
  tableResizeObserver = null;
  const scroll = document.querySelector<HTMLElement>('.layout-content__scroll');
  if (!scroll || typeof ResizeObserver === 'undefined') return;
  tableResizeObserver = new ResizeObserver(() => nextTick(measureTableHeight));
  tableResizeObserver.observe(scroll);
}

const resolvedTableHeight = computed(() => {
  const mode = chrome.value.heightMode;
  if (mode === 'fit') return Math.max(240, 48 + tableData.value.length * 40);
  // default/fill：动态填满可视空间，分页器与外壳底部保持在首屏内
  return measuredTableHeight.value;
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
      // Boolean 字段徽标（Enable 及任意 Boolean 字段）：有 Update 权限时可点击切换状态
      enableToggle: !!field && isBooleanToggleField(field) && flags.value.canEdit,
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

function renderCell(field: FieldMeta, record: Record<string, unknown>): string {
  return formatFieldValue(field, record, { labelCache });
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
  // 主时间字段与关键字开关（OSC-0016）：setting 透传搜索面板固定控件
  masterTimeName.value = pageSetting.value?.masterTimeName ?? null;
  masterTimeDisplayName.value = pageSetting.value?.masterTimeDisplayName ?? null;
  enableKey.value = pageSetting.value?.enableKey !== false;
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
  let search = toFieldMetas((meta.search || nested?.search) as never).filter(
    (f) => !!f.name && !f.primaryKey && f.typeName !== 'Guid',
  );
  let add = toFieldMetas((meta.addForm || nested?.form?.addForm) as never).filter(
    (f) => !!f.name,
  );
  let edit = toFieldMetas((meta.editForm || nested?.form?.editForm) as never).filter(
    (f) => !!f.name,
  );
  let detail = toFieldMetas((meta.detail || nested?.form?.detail) as never).filter(
    (f) => !!f.name,
  );
  // 各分区缺失时按 ViewKind 走 GetFields 兜底，保证表单/搜索有权威元数据（OSC-0009）
  if (!search.length) {
    try {
      const fb = await cubeApi.page.getFields(typePath.value, FieldKind.Search);
      search = toFieldMetas(fb.data).filter(
        (f) => !!f.name && !f.primaryKey && f.typeName !== 'Guid',
      );
    } catch {
      /* ignore */
    }
  }
  if (!add.length) {
    try {
      const fb = await cubeApi.page.getFields(typePath.value, FieldKind.Add);
      add = toFieldMetas(fb.data).filter((f) => !!f.name);
    } catch {
      /* ignore */
    }
  }
  if (!edit.length) {
    try {
      const fb = await cubeApi.page.getFields(typePath.value, FieldKind.Edit);
      edit = toFieldMetas(fb.data).filter((f) => !!f.name);
    } catch {
      /* ignore */
    }
  }
  if (!detail.length) {
    try {
      const fb = await cubeApi.page.getFields(typePath.value, FieldKind.Detail);
      detail = toFieldMetas(fb.data).filter((f) => !!f.name);
    } catch {
      /* ignore */
    }
  }
  // 一次 Meta 灌入 Enum dataSource；再按 Cube.Vue Lookup 补未知 typeName 枚举
  const allFields = [...list, ...search, ...add, ...edit, ...detail];
  await enrichFieldsWithEnumDataSource(allFields);
  await enrichFieldsWithLookup(allFields);
  listFields.value = list;
  searchFields.value = search;
  addFields.value = add;
  editFields.value = edit;
  detailFields.value = detail;
  const pk = listFields.value.find((f) => f.primaryKey);
  pkField.value = pk?.name || 'id';
}

async function loadProfile() {
  viewState.value = await evpStore.load(
    typePath.value,
    metaKeys.value,
    listFields.value,
    { defaultView: preferredDefaultView.value },
  );
  evpStore.setFields(typePath.value, listFields.value);
  // 再 rematch 一次，确保与最新 listFields 对齐
  if (metaKeys.value.length) {
    viewState.value = evpStore.rematch(typePath.value, metaKeys.value);
  }
}

function applyWorkspacePrefs() {
  // 页面级 PageSize 优先（OSC-0012），未配置才回落旧全局 workspace 种子
  const size = effectivePageSizePref.value;
  if (pagination.pageSize !== size) pagination.pageSize = size;
}

/** 将基准/已保存条件回填到搜索表单（视图切换、初始加载时调用） */
function applySearchToForm(params: Record<string, unknown>) {
  Object.keys(searchForm).forEach((k) => delete searchForm[k]);
  Object.assign(searchForm, params);
}

async function loadData(skipFetch = false) {
  // 翻页/重载后以当前页选择为准，避免旧主键残留导致批量删除误用
  selectedKeys.value = [];
  loading.value = true;
  // 数据重载 → 列表/树增量渲染从头开始（前 100 条），滚动再追加
  tableVisibleCount.value = TABLE_INITIAL_VISIBLE;
  try {
    const sort = buildSortPayload(activeSort.value);
    const pageSize = effectivePageSize.value;
    // 甘特图现可翻页（pageIndex 随分页器 current），看板/日历仍固定第一页大加载
    const pageIndex =
      isLargePageView.value && activeViewKind.value !== 'gantt' ? 0 : pagination.current - 1;
    let rows: Record<string, unknown>[];
    if (skipFetch && tableDataRaw.value.length) {
      // 复用已加载原始数据（视图切换/纯前端筛选变化：搜索、排序、加载量未变，避免重复请求后端）
      rows = tableDataRaw.value;
    } else {
      const res = await cubeApi.page.getList(typePath.value, {
        pageIndex,
        pageSize,
        ...sort,
        ...effectiveSearch.value,
      });
      rows = (res.data as Record<string, unknown>[]) || [];
      tableDataRaw.value = rows;
      statData.value = (res.stat as Record<string, unknown>) ?? null;
      if (res.page) pagination.total = res.page.totalCount || 0;
    }
    // 筛选构建器客户端复核（OSC-0015）：业务重写 Search 的控制器（如 Department.Search
    // 仅处理 id/parentId/enable/visible）与树控制器可能不应用通用等值过滤，对已加载数据
    // 兜底过滤保证筛选生效；普通控制器后端已过滤时此处幂等。同时覆盖 any 多条件 OR 降级。
    if (viewFilter.value.conditions.length) {
      const rawCount = rows.length;
      tableData.value = rows.filter((r) =>
        matchesViewFilter(r, viewFilter.value, filterFields.value),
      );
      // 本页已加载全部数据且后端未按筛选过滤（发生删减）时，纠正 total 反映过滤结果
      if (
        tableData.value.length !== rawCount &&
        pagination.total > 0 &&
        rawCount >= pagination.total
      ) {
        pagination.total = tableData.value.length;
      }
    } else {
      tableData.value = rows;
    }
    if (!skipFetch) await hydrateLovLabels(tableData.value);
  } finally {
    loading.value = false;
    // 洞察图表与列表同源（同一 effectiveSearch），随列表刷新；竞态由 chartSeq 保护
    void loadChart();
    // 数据/分页器渲染完成后重测表格高度（default/fill 模式填满可视区，分页器保持可见）
    nextTick(measureTableHeight);
  }
}

/** 加载固定图表（OSC-0012）：showChart 时带有效搜索请求 GetChartData；过期响应丢弃 */
async function loadChart() {
  if (!insight.value.showChart) {
    chartData.value = [];
    chartError.value = '';
    chartLoading.value = false;
    return;
  }
  const seq = ++chartSeq;
  chartLoading.value = true;
  chartError.value = '';
  try {
    const res = await cubeApi.page.getChartData(typePath.value, effectiveSearch.value);
    if (seq !== chartSeq) return;
    chartData.value = Array.isArray(res.data) ? res.data : [];
  } catch (err) {
    if (seq !== chartSeq) return;
    chartData.value = [];
    chartError.value = formatApiError(err, '图表加载失败');
  } finally {
    if (seq === chartSeq) chartLoading.value = false;
  }
}

function syncLocalState() {
  viewState.value = evpStore.getState(typePath.value);
  // 会话内筛选/分组与 store（含已保存配置）对齐（OSC-0015）
  localFilter.value = evpStore.getFilter(typePath.value);
  localGroup.value = evpStore.getGroup(typePath.value);
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

function onMappingChange(mapping: ViewMapping | undefined) {
  evpStore.updateMapping(typePath.value, mapping);
  syncLocalState();
}

/** 甘特拖拽表格宽度上报：tableWidth 随 mapping 持久化到 ViewsJson（OSC-0019） */
function onGanttMappingChange(mapping: GanttMapping) {
  evpStore.updateMapping(typePath.value, mapping);
  syncLocalState();
}

function onInsightChange(insight: ViewInsight) {
  evpStore.updateInsight(typePath.value, insight);
  syncLocalState();
}

/** Category 折叠切换：更新当前模式布局的 collapsedCategories（OSC-0013） */
function onToggleCollapse(category: string) {
  const mode = drawerMode.value;
  const cur = evpStore.getFormModeLayout(typePath.value, mode) ?? {
    order: [],
    hidden: [],
    collapsedCategories: [],
  };
  const set = new Set(cur.collapsedCategories);
  if (set.has(category)) set.delete(category);
  else set.add(category);
  evpStore.updateFormLayout(
    typePath.value,
    mode,
    { ...cur, collapsedCategories: [...set] },
    false,
  );
}

function onConfigRename(name: string) {
  if (!viewState.value) return;
  onRenameView(viewState.value.activeViewId, name);
}

function onSwitchView(id: string) {
  const wasTouched = searchTouched.value;
  const oldSort = JSON.stringify(buildSortPayload(activeSort.value));
  const oldPageSize = effectivePageSize.value;
  evpStore.switchView(typePath.value, id);
  syncLocalState();
  selectedKeys.value = [];
  pagination.current = 1;
  // 切换视图后重新计算条件来源并回填表单（OSC-0012）
  searchTouched.value = false;
  applySearchToForm(baseSearch.value);
  // 重绘优化：未显式搜索、且新视图加载量与排序与已加载数据一致时，复用数据避免重复请求后端
  const canReuse =
    !wasTouched &&
    effectivePageSize.value === oldPageSize &&
    JSON.stringify(buildSortPayload(activeSort.value)) === oldSort &&
    tableDataRaw.value.length > 0;
  loadData(canReuse);
}

function onCreateView(kind: ViewKind, name: string) {
  // 创建视图时按用户删除权限设置默认「允许删除记录」：有删除权限 → true，无 → false（需求 OSC）
  evpStore.addView(typePath.value, name, kind, {
    allowDelete: flags.value.canDelete,
  });
  syncLocalState();
  selectedKeys.value = [];
  pagination.current = 1;
  loadData();
}

function onRenameView(id: string, name: string) {
  evpStore.rename(typePath.value, id, name);
  syncLocalState();
}

function onRemoveView(id: string) {
  evpStore.remove(typePath.value, id);
  syncLocalState();
  selectedKeys.value = [];
  loadData();
}

function onDuplicateView(id: string) {
  evpStore.duplicate(typePath.value, id);
  syncLocalState();
  selectedKeys.value = [];
  loadData();
}

function onCardDelete(row: Record<string, unknown>) {
  if (!chrome.value.allowDelete) return;
  Modal.confirm({
    title: '确认删除？',
    content: '删除后不可恢复',
    onOk: () => handleDelete(row),
  });
}

function onResetViews() {
  // 「恢复默认」= 当前视图恢复到创建时的默认状态（保留视图本身，仅重置配置；不删除用户自定义视图）
  if (!activeViewId.value) return;
  evpStore.restoreView(typePath.value, activeViewId.value);
  syncLocalState();
  selectedKeys.value = [];
  pagination.current = 1;
  // 恢复配置不改变数据源，复用已加载数据（避免重复请求后端）
  loadData(true);
}

/** 系统管理员：将当前视图方案发布为全局模板（该实体默认视图；回落用户可见） */
async function onSaveAsDefault() {
  const st = evpStore.getState(typePath.value);
  if (!st?.views?.length) {
    Message.warning('当前无视图可保存');
    return;
  }
  if (!window.confirm('将当前视图方案保存为该实体默认视图？未个性化用户将默认看到此方案。')) return;
  try {
    await cubeApi.profile.putViewProfileTemplate({
      typePath: typePath.value,
      viewsJson: JSON.stringify(st.views.map(serializeNamedView)),
    });
    Message.success('已保存为默认视图');
    // 重新加载刷新模板来源域（管理员个人视图域不受影响）
    const entry = evpStore.byType[typePath.value];
    if (entry) await evpStore.load(typePath.value, entry.metaKeys);
  } catch (err) {
    Message.error(formatApiError(err, '保存默认视图失败'));
  }
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

/**
 * 点击 Boolean 字段徽标（Enable 及任意 Boolean 字段）：受 Update 权限控制（flags.canEdit）。
 * fieldName 由列表/树/卡片/看板点击携带；未携带时回退到 Enable 字段（兼容）。
 * 先乐观更新本地行——按切换后的实际值即时展示（开→success 徽标、关→danger 徽标，双向而非单一禁用态），
 * 再调后端确认；成功后 loadData 权威刷新，失败回滚并提示。
 * Enable 字段走既有 EnableSelect/DisableSelect；其余 Boolean 字段走单字段 Update（复用 Update 接口，不改后端）。
 */
async function onToggleEnable(row: Record<string, unknown>, fieldName?: string) {
  if (!flags.value.canEdit) return;
  const field = fieldName
    ? listFields.value.find(
        (f) => f.name === fieldName || f.name.toLowerCase() === (fieldName || '').toLowerCase(),
      )
    : listFields.value.find((f) => isEnableField(f));
  if (!field) return;
  const id = getValueByKey(row, pkField.value);
  if (id == null || id === '') return;
  // 防并发：切换请求进行中忽略再次点击，避免快速双击并发回跳
  if (enableBusy.value) return;
  enableBusy.value = true;
  const oldRaw = getValueByKey(row, field.name);
  const target = !isTruthy(oldRaw);
  const label = field.displayName || field.name;
  // 按字段类型写切换后的实际值（Boolean→true/false，数值→1/0），ListTable deep watch 即时重绘徽标
  const newRaw = field.typeName === 'Boolean' ? target : target ? 1 : 0;
  setValueByKey(row, field.name, newRaw);
  try {
    if (field.name.toLowerCase() === 'enable') {
      if (target) await cubeApi.page.enableSelect(typePath.value, [id as string | number]);
      else await cubeApi.page.disableSelect(typePath.value, [id as string | number]);
      Message.success(target ? '启用成功' : '禁用成功');
    } else {
      await updateSingleBooleanField(row, field, id as string | number, target);
      Message.success(target ? `${label}：已开启` : `${label}：已关闭`);
    }
    // 后端权威刷新，保证展示与后端一致（含筛选/排序/统计）
    await loadData();
  } catch (err) {
    // 失败回滚：恢复原状态展示
    setValueByKey(row, field.name, oldRaw);
    Message.error(formatApiError(err, '操作失败'));
  } finally {
    enableBusy.value = false;
  }
}

/**
 * 单字段 Update：拉完整详情 → 仅改目标字段 → 走既有 Update(PUT) 接口（与表单保存同模式），
 * 避免直接提交最小 payload 时覆盖其它字段。
 */
async function updateSingleBooleanField(
  row: Record<string, unknown>,
  field: FieldMeta,
  id: string | number,
  target: boolean,
) {
  // 与表单编辑同源的字段集（edit 分区回退）
  const targetFields = resolveFieldsForKind('edit', fieldParts.value);
  let detail: Record<string, unknown> = {};
  try {
    const res = await cubeApi.page.getDetail(typePath.value, id);
    detail = (res.data as Record<string, unknown>) || row;
  } catch {
    detail = row;
  }
  // 归一化到字段元数据名（PascalCase），仅保留可编辑字段
  const formModel = normalizeKeysByFields(detail, targetFields);
  // 主键 + 目标字段
  formModel[pkField.value] = getValueByKey(detail, pkField.value) ?? id;
  formModel[field.name] = target;
  const payload = prepareSubmitPayload(formModel, targetFields, {
    mode: 'edit',
    pkField: pkField.value,
  });
  await cubeApi.page.update(typePath.value, payload);
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
  // GetPage 字段名为 PascalCase，而 GetDetail 返回数据为 camelCase；
  // 按字段元数据归一化 key，否则编辑表单 model[field.name] 取不到值（内容为空）
  // 回填字段与 drawerFields 同源：detail 分区缺失时回退 edit → list，避免详情全空（OSC-0009）
  const targetFields = resolveFieldsForKind(mode, fieldParts.value);
  try {
    const res = await cubeApi.page.getDetail(typePath.value, id as string | number);
    Object.assign(
      formModel,
      normalizeKeysByFields((res.data as Record<string, unknown>) || row, targetFields),
    );
  } catch {
    Object.assign(formModel, normalizeKeysByFields(row, targetFields));
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

/** 筛选弹层可见性（互斥：打开筛选关闭分组） */
function onFilterPopoverVisible(v: boolean) {
  activePopover.value = v ? 'filter' : null;
}

/** 分组弹层可见性（互斥：打开分组关闭筛选） */
function onGroupPopoverVisible(v: boolean) {
  activePopover.value = v ? 'group' : null;
}

/** 应用筛选方案：写入 store 持久化（刷新/下次打开保留）；筛选为纯前端过滤，复用已加载数据重过滤 */
function onFilterApply(filter: ViewFilter) {
  evpStore.updateFilter(typePath.value, filter);
  localFilter.value = filter;
  pagination.current = 1;
  loadData(true);
}

/** 保存筛选方案到当前命名视图：写 store 持久化；不立即刷新（下次打开/刷新自动应用） */
function onFilterSave(filter: ViewFilter) {
  evpStore.updateFilter(typePath.value, filter);
  localFilter.value = filter;
  Message.success('筛选方案已保存到此视图');
}

/** 清除筛选方案（工具栏标签）：写入空方案持久化；筛选为纯前端过滤，复用已加载数据重过滤 */
function onClearFilter() {
  evpStore.updateFilter(typePath.value, emptyViewFilter());
  localFilter.value = emptyViewFilter();
  pagination.current = 1;
  loadData(true);
  Message.success('已清除筛选');
}

/** 应用分组方案：写入 store 持久化（刷新保留）并本地重分组 */
function onGroupApply(group: ViewGroup) {
  evpStore.updateGroup(typePath.value, group);
  localGroup.value = group;
}

/** 保存分组方案到当前命名视图：写 store 持久化 */
function onGroupSave(group: ViewGroup) {
  evpStore.updateGroup(typePath.value, group);
  localGroup.value = group;
  Message.success('分组方案已保存到此视图');
}

/** 清除分组方案：写入空方案持久化 */
function onClearGroup() {
  evpStore.updateGroup(typePath.value, []);
  localGroup.value = [];
  Message.success('已清除分组');
}

async function handleSave() {
  saving.value = true;
  try {
    const mode = drawerMode.value === 'add' ? 'add' : 'edit';
    // 保存字段集与表单回填同源（editForm → addForm），避免字段名不一致
    const fields = resolveFieldsForKind(mode, fieldParts.value);
    const payload = prepareSubmitPayload({ ...formModel }, fields, {
      mode,
      pkField: pkField.value,
    });
    if (mode === 'add') await cubeApi.page.add(typePath.value, payload);
    else await cubeApi.page.update(typePath.value, payload);
    Message.success('保存成功');
    fieldErrors.value = [];
    drawerVisible.value = false;
    await loadData();
  } catch (err) {
    // 后端字段级错误优先映射到表单字段；其余保留全局提示（OSC-0009）
    const errors =
      err instanceof ApiError
        ? (err.fieldErrors ?? [])
        : ((err as { response?: { data?: { fieldErrors?: { field: string; message: string }[] } } })
            .response?.data?.fieldErrors ?? []);
    fieldErrors.value = errors;
    if (!errors.length) {
      Message.error(formatApiError(err, '保存失败'));
    }
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

function confirmBatchDelete() {
  if (!batchDeleteState.value.visible || batchDeleteState.value.disabled) return;
  if (!selectedKeys.value.length) return;
  const count = selectedKeys.value.length;
  Modal.confirm({
    title: '确认批量删除？',
    content: `将删除已选中的 ${count} 条记录，删除后不可恢复`,
    onOk: () => handleBatchDelete(),
  });
}

async function handleBatchDelete() {
  if (!batchDeleteState.value.visible || batchDeleteState.value.disabled) return;
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
// 图表入口按钮已暂时移除（OSC-0007），图表区由后续独立 OSC 完善；保留 openChart 供其重新接线
void openChart;

function handleSearch() {
  // 显式搜索后有效条件取自表单（OSC-0012）
  searchTouched.value = true;
  pagination.current = 1;
  loadData();
}
function handleReset() {
  Object.keys(searchForm).forEach((k) => delete searchForm[k]);
  // 重置查询参数：一并清除当前应用的预定义查询标记（与删除的「清空查询参数」合并）
  evpStore.clearActiveQuery(typePath.value);
  searchTouched.value = true;
  pagination.current = 1;
  loadData();
}

/** 应用预定义查询（OSC-0016）：整体回填 searchForm（无残留键）→ 执行 → activeQueryId 由 store 设置 */
function handleApplyQuery(id: string) {
  const params = evpStore.applyQuery(typePath.value, id);
  if (!params) return;
  applySearchToForm(params);
  searchTouched.value = true;
  pagination.current = 1;
  loadData();
}

/** 保存当前查询为预定义（OSC-0016）：store 新增条目并指向，随后自动执行一次查询 */
function handleSaveQuery(name: string) {
  const params = cleanSearchParams({ ...searchForm }, searchKeys.value);
  evpStore.saveQueryAs(typePath.value, name, params);
  Message.success('已保存为预定义查询');
  searchTouched.value = true;
  pagination.current = 1;
  loadData();
}

/** 重命名当前查询（OSC-0016） */
function handleRenameQuery(id: string, name: string) {
  evpStore.renameQuery(typePath.value, id, name);
  Message.success('已重命名');
}

/** 删除预定义查询（OSC-0016）：删除后当前表单参数保留 */
function handleDeleteQuery(id: string) {
  evpStore.deleteQuery(typePath.value, id);
  Message.success('已删除');
}

function onPageChange(page: number) {
  pagination.current = page;
  loadData();
}
function onPageSizeChange(size: number) {
  pagination.pageSize = size;
  // 页面级 PageSize：普通视图与甘特图（现可翻页）保存到当前 typePath，不再写全局 workspace
  if (!isLargePageView.value || activeViewKind.value === 'gantt') {
    evpStore.setPageSize(typePath.value, normalizePageSize(size), true);
  }
  pagination.current = 1;
  loadData();
}
function onSelectionChange(keys: (string | number)[]) {
  selectedKeys.value = keys;
}

async function bootstrap() {
  await loadFields();
  await loadProfile();
  syncLocalState();
  applyWorkspacePrefs();
  // 初始回填 URL→已保存基准条件到搜索表单（OSC-0012）
  applySearchToForm(baseSearch.value);
  await loadData();
}

watch(typePath, () => {
  pagination.current = 1;
  selectedKeys.value = [];
  bootstrap();
});

// URL 参数变化（同页面路由 query 变更）时重新派生基准条件
watch(
  () => route.query,
  () => {
    searchTouched.value = false;
    applySearchToForm(baseSearch.value);
    pagination.current = 1;
    loadData();
  },
  { deep: true },
);

// 洞察图表开关变化时刷新图表区（不影响列表）
watch(
  () => insight.value.showChart,
  () => {
    void loadChart();
  },
);

// 视图/高度模式切换后重测表格可用高度（分页器与外壳底部保持在首屏内）。
// 视图组件（VTable/CardList 等）为异步加载，渲染完成后延迟多次重测直至高度收敛。
watch(
  () => [viewState.value?.view, chrome.value.heightMode] as const,
  () => {
    nextTick(measureTableHeight);
    window.setTimeout(measureTableHeight, 200);
    window.setTimeout(measureTableHeight, 600);
  },
);

onMounted(() => {
  bootstrap();
  // Esc 退出全屏
  window.addEventListener('keydown', onKeydown);
  // 初始渲染完成后测量表格可用高度，并监听 scroll 容器尺寸变化（窗口/侧栏/布局调整）
  nextTick(() => {
    measureTableHeight();
    observeTableHeight();
  });
});

onBeforeUnmount(() => {
  tableResizeObserver?.disconnect();
  window.removeEventListener('keydown', onKeydown);
});
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
/* 全屏：固定铺满视口，覆盖系统顶部及左侧导航栏；
   z-index 低于 Arco 弹层（1000+），全屏期间的抽屉/弹窗/气泡仍可正常显示 */
.default-list--fullscreen {
  position: fixed;
  inset: 0;
  z-index: 900;
  overflow: auto;
  background: var(--color-fill-2);
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
  border: none;
  border-radius: 8px;
  /* 允许内部横向滚动；勿用 overflow:hidden 把右侧 gutter 连带裁掉 */
  overflow-x: auto;
  overflow-y: visible;
}
.list-view-tabs {
  margin-bottom: 8px;
  padding-bottom: 4px;
  /* 横线由 Tab 组件自身（.arco-tabs-nav 下边框）提供，此处不额外加分隔线 */
}
.list-topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}
.advanced-upload {
  display: block;
  width: 100%;
  cursor: pointer;
}
.advanced-menu-label {
  font-size: var(--cube-font-size-body);
  color: var(--color-text-1);
}
.advanced-upload-option :deep(.arco-upload) {
  display: block;
  width: 100%;
}
/* 筛选/分组弹层锚点与激活底纹/徽标（OSC-0015） */
.tb-act {
  position: relative;
  display: inline-flex;
  align-items: center;
}
/* 甘特图缩放控制（位于筛选前，仅甘特图视图显示）：− / 当前等级 / + 按钮 */
.tb-gantt-zoom {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.tb-zoom-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  padding: 0;
  box-sizing: border-box;
  border: 1px solid var(--color-border-2);
  background: var(--color-bg-2);
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text-3);
  font-size: 16px;
  line-height: 1;
}
.tb-zoom-btn:hover:not(:disabled) {
  color: rgb(var(--primary-6));
  border-color: rgb(var(--primary-6));
  background: var(--color-fill-1);
}
.tb-zoom-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
.tb-zoom-label {
  min-width: 48px;
  text-align: center;
  font-size: 12px;
  color: var(--color-text-2);
  white-space: nowrap;
}
/* 有筛选/分组条件时按钮显示主题浅色底纹；文字用当前主题 Primary 色（--cube-primary，外观设置可换） */
.tb-act.is-active :deep(.arco-btn) {
  background: color-mix(in srgb, var(--cube-primary) 10%, #fff);
  color: var(--cube-primary);
  font-weight: 500;
}
/* 右上角圆形徽标（数字=条件数/分组字段数），点击清除；底色 = 当前主题 Primary 色
   注意：勿用 --color-primary-6（Arco 未定义该变量，会解析为透明）；--cube-primary 由 tokens 注入 html 并跟随主题 */
.tb-count {
  position: absolute;
  top: -5px;
  right: -6px;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 8px;
  background: var(--cube-primary);
  color: #fff;
  font-size: 11px;
  line-height: 16px;
  text-align: center;
  cursor: pointer;
  user-select: none;
  box-sizing: border-box;
}
.tb-count:hover {
  background: color-mix(in srgb, var(--cube-primary) 85%, #000);
}
.list-pager {
  margin-top: 12px;
  display: flex;
  justify-content: flex-end;
}
.list-pager-hint {
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
}
</style>
