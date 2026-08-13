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
import { defineAsyncComponent } from 'vue';
import { useDefaultList } from './useDefaultList';
/** VTable / 多视图异步加载，降低 DynamicPage 首包 */
const ListTable = defineAsyncComponent(() => import('@/features/vtable/ListTable.vue'));
const CardList = defineAsyncComponent(() => import('@/features/views/CardList.vue'));
const KanbanBoard = defineAsyncComponent(() => import('@/features/views/KanbanBoard.vue'));
const CalendarMonth = defineAsyncComponent(() => import('@/features/views/CalendarMonth.vue'));
const GanttView = defineAsyncComponent(() => import('@/features/views/GanttView.vue'));
import SearchDrawer from '@/features/search/SearchDrawer.vue';
import InsightPanel from '@/features/search/InsightPanel.vue';
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

const {
  headerSection,
  fullscreen,
  listShellStyle,
  hasChromeBg,
  listSurfaceStyle,
  searchFields,
  searchForm,
  insight,
  statData,
  statLabels,
  chartData,
  chartLoading,
  chartError,
  masterTimeName,
  masterTimeDisplayName,
  enableKey,
  savedQueries,
  appliedQueryId,
  queryParamsDirty,
  queryHasParams,
  handleSearch,
  handleReset,
  handleApplyQuery,
  handleSaveQuery,
  handleRenameQuery,
  handleDeleteQuery,
  tablePanelRef,
  viewState,
  listFields,
  typePath,
  isAdmin,
  onSwitchView,
  onCreateView,
  onRenameView,
  onRemoveView,
  onDuplicateView,
  onResetViews,
  onSaveAsDefault,
  configDrawerVisible,
  onToggleFullscreen,
  flags,
  openAdd,
  activeViewKind,
  ganttZoomLevel,
  ganttZoomLabel,
  onGanttZoom,
  filterPopoverVisible,
  filterFields,
  viewFilter,
  activeViewId,
  onFilterPopoverVisible,
  onFilterApply,
  onFilterSave,
  onClearFilter,
  chrome,
  groupPopoverVisible,
  viewGroup,
  onGroupPopoverVisible,
  onGroupApply,
  onGroupSave,
  onClearGroup,
  searchPanelOpen,
  advancedVisible,
  handleImport,
  exportFormats,
  handleExport,
  batchDeleteState,
  confirmBatchDelete,
  formLayoutDrawerVisible,
  loading,
  tableData,
  treeDataDetected,
  tableColumns,
  displayRows,
  pkField,
  selectedKeys,
  activeSort,
  isGrouped,
  groupLabelOf,
  resolvedTableHeight,
  openDetail,
  onSelectionChange,
  onColumnsChange,
  onSortChange,
  onTableAction,
  onToggleEnable,
  onTableScrollBottom,
  cardListKey,
  activeCardMapping,
  activeColumns,
  renderCell,
  openEdit,
  onCardDelete,
  activeKanbanMapping,
  activeCalendarMapping,
  activeGanttMapping,
  onGanttMappingChange,
  showPagerBar,
  pagination,
  effectivePageSize,
  GANTT_PAGE_SIZE_OPTIONS,
  PAGE_SIZE_OPTIONS,
  onPageChange,
  onPageSizeChange,
  isLargePageView,
  getActiveView,
  columnTitles,
  onConfigSort,
  onChromeChange,
  onMappingChange,
  onInsightChange,
  onConfigRename,
  drawerVisible,
  drawerFields,
  formModel,
  drawerMode,
  saving,
  showHistoryTabs,
  drawerCanPrev,
  drawerCanNext,
  fieldErrors,
  drawerFormLayout,
  onToggleCollapse,
  handleSave,
  navigateRecord,
  chartVisible,
  chartList,
  addFields,
  editFields,
  detailFields,
} = useDefaultList(props);
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
