<script setup lang="ts">
/**
 * CubeTable —— 一个组件就是一个列表页（结构与视觉对齐 MES ListPage）
 *
 * 从上到下固定五段：操作栏 → 搜索条件 → 表格统计 → 表格 → 分页。
 * 页面不滚动，只有表格数据区滚动（`.ct-panel` + `el-table height=100%`）。
 *
 * 上下文三级解析：`:context` 外部指定 → `inject(CubeEngineKey)` 父级已建 → 按当前路由自建。
 * props / 插槽命名沿用 MES ListPage，业务页两边平移时不用改称呼。
 */
import { computed, inject, useSlots } from 'vue';
import { useI18n } from 'vue-i18n';
import { CubeEngineKey, useCubeEngine } from '../../engine/useCubeEngine';
import { useCubeTableActions } from './useCubeTableActions';
import { useCubeTableColumns } from './useCubeTableColumns';
import type { CubeEngineContext } from '../../engine/types';
import type { CubeTableProps } from './types';
import CubeTableActions from './CubeTableActions.vue';
import CubeTableSearch from './CubeTableSearch.vue';
import CubeTableMetrics from './CubeTableMetrics.vue';
import CubeTableColumnSetting from './CubeTableColumnSetting.vue';
import CubeTableGrid from './CubeTableGrid.vue';
import CubeTablePagination from './CubeTablePagination.vue';
import CubeTableFormDialog from './CubeTableFormDialog.vue';

/** 本组件自己消费的插槽；其余（default / append 等）原样转给表格区 */
const RESERVED_SLOTS = [
  'header',
  'actions',
  'actions-extra',
  'search',
  'search-bar',
  'search-advanced',
  'metrics',
  'toolbar-extra',
  'table',
  'row-actions',
  'footer',
  'footer-extra',
  'form',
];

const props = withDefaults(defineProps<CubeTableProps>(), {
  context: null,
  autoEngine: true,
  pageSize: undefined,
  searchPlaceholder: undefined,
  showSearch: true,
  showRefresh: true,
  metrics: () => [],
  pageSizes: () => [10, 20, 50],
  totalUnit: undefined,
  sizeUnit: undefined,
  columnsSetting: true,
  showAdd: undefined,
  showBatchDelete: undefined,
  showImport: undefined,
  showExport: undefined,
  showDownloadTemplate: undefined,
  exportFormat: 'xlsx',
  confirmDelete: undefined,
  extraRowActions: () => [],
});

const injected = inject(CubeEngineKey, null);
const { t } = useI18n();
const ctx: CubeEngineContext =
  props.context ??
  injected ??
  useCubeEngine({
    routePath: props.routePath,
    apiPrefix: props.apiPrefix,
    pageSize: props.pageSize,
    extraQuery: props.queryParams,
  });

const slots = useSlots();
const actions = useCubeTableActions(ctx, {
  confirmDelete: props.confirmDelete === undefined ? undefined : () => props.confirmDelete === true,
  exportFormat: () => props.exportFormat,
  importAccept: () => props.importAccept,
});
actions.bindBlobDownload();

const columns = useCubeTableColumns(() => ctx.listFields.value, {
  prefix: () => ctx.apiPrefix.value,
  enabled: props.columnsSetting,
  storageKey: props.columnsStorageKey,
});

/** 转发给表格区的插槽名（业务自写列即 `#default`，与 MES 同一机制） */
const gridSlotNames = computed(() => Object.keys(slots).filter((name) => !RESERVED_SLOTS.includes(name)));

/** 列显隐结果同时给自写列用（业务列 `v-if="visibleColumns.Xxx"`） */
const visibleColumns = computed(() => (props.columnsSetting ? columns.visibility.value : {}));

/** preset 追加的行操作排在页面显式 extraRowActions 之前 */
const mergedRowActions = computed(() => [
  ...(ctx.preset?.value?.list?.actions ?? []),
  ...(props.extraRowActions ?? []),
]);
</script>

<template>
  <div class="cube-table">
    <!-- ⓪ 页头：框架默认页传标题，业务自绘页面一般不用 -->
    <slot name="header" :ctx="ctx" />

    <!-- ① 操作栏：业务按钮在前，内置通用按钮在后（与 MES 一致，#actions 是「加内容」不是「换整行」） -->
    <CubeTableActions
      :ctx="ctx"
      :actions="actions"
      :flags="{
        add: showAdd,
        batchDelete: showBatchDelete,
        importFile: showImport,
        exportFile: showExport,
        downloadTemplate: showDownloadTemplate,
      }"
    >
      <template v-if="slots.actions" #default="scope">
        <slot name="actions" v-bind="scope" />
      </template>
      <template v-if="slots['actions-extra']" #extra="scope">
        <slot name="actions-extra" v-bind="scope" />
      </template>
    </CubeTableActions>

    <!-- ② 搜索条件：关键字行 + 可展开条件面板。
         #search-bar / #search-advanced 是 MES 口径的「换内容」，#search 是 Cube 扩展的「换整块」 -->
    <template v-if="showSearch || slots.search || slots['search-bar']">
      <slot name="search" :ctx="ctx">
        <CubeTableSearch :ctx="ctx" :placeholder="searchPlaceholder ?? t('userSearch.keyword')">
          <template v-if="slots['search-bar']" #bar="scope">
            <slot name="search-bar" v-bind="scope" />
          </template>
          <template v-if="slots['search-advanced']" #advanced="scope">
            <slot name="search-advanced" v-bind="scope" />
          </template>
        </CubeTableSearch>
      </slot>
    </template>

    <!-- ③④⑤ 主面板：统计行 + 表格 + 分页 -->
    <div class="ct-panel">
      <CubeTableMetrics
        :ctx="ctx"
        :metrics="metrics"
        :show-refresh="showRefresh"
        :show-column-setting="columnsSetting"
      >
        <template v-if="slots.metrics" #metrics="scope">
          <slot name="metrics" v-bind="scope" />
        </template>
        <template v-if="slots['toolbar-extra']" #extra="scope">
          <slot name="toolbar-extra" v-bind="scope" />
        </template>
        <template v-if="columnsSetting" #column-setting>
          <CubeTableColumnSetting
            :items="columns.items.value"
            :visibility="columns.visibility.value"
            @set="(key: string, visible: boolean) => columns.setVisible(key, visible)"
            @order="columns.setOrder"
            @reset="columns.reset()"
          />
        </template>
      </CubeTableMetrics>

      <div class="ct-table-wrap">
        <slot name="table" :ctx="ctx" :data="ctx.list.value" :loading="ctx.loading.value">
          <CubeTableGrid
            :ctx="ctx"
            :actions="actions"
            :visibility="visibleColumns"
            :column-order="columns.order.value"
            :reset-visibility="columns.reset"
            :extra-row-actions="mergedRowActions"
            v-bind="grid"
          >
            <template v-for="name in gridSlotNames" #[name]="scope">
              <slot
                :name="name"
                v-bind="scope ?? {}"
                :visible-columns="visibleColumns"
                :reset-column-visibility="columns.reset"
              />
            </template>
            <template v-if="slots['row-actions']" #row-actions="scope">
              <slot name="row-actions" v-bind="scope" :ctx="ctx" />
            </template>
          </CubeTableGrid>
        </slot>
      </div>

      <slot name="footer" :ctx="ctx" :total="ctx.total.value" :page="ctx.page.value" :page-size="ctx.pageSize.value">
        <CubeTablePagination
          :ctx="ctx"
          :page-sizes="pageSizes"
          :unit="totalUnit ?? t('grid.unit')"
          :size-unit="sizeUnit"
        >
          <template v-if="slots['footer-extra']" #extra>
            <slot name="footer-extra" :ctx="ctx" />
          </template>
        </CubeTablePagination>
      </slot>
    </div>

    <slot name="form" :ctx="ctx">
      <CubeTableFormDialog :ctx="ctx" />
    </slot>
  </div>
</template>

<style scoped>
/* 列表页骨架：与 MES ListPage 同一套尺寸（页高 100%、内边距 12px、三段卡片）。
   滚动模型契约：`.cube-table` 靠 `height:100%` 撑满，要求宿主内容区给确定高度
   （如 `height:100vh; overflow:hidden`，框架 TopMenu 即如此）；宿主未给定时百分比高度
   不生效，整页会随内容撑高。overflow:hidden 在定高下兜住组件自身不外溢滚动条。 */
.cube-table {
  height: 100%;
  box-sizing: border-box;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  padding: 12px;
}

.ct-panel {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  box-shadow: var(--el-box-shadow-light);
  overflow: hidden;
}

/* 表格容器：横向溢出滚动，纵向交给 el-table 内部（表头固定、数据区滚动） */
.ct-table-wrap {
  flex: 1;
  min-height: 0;
  overflow-x: auto;
  overflow-y: hidden;
}
.ct-table-wrap :deep(.el-table) {
  --el-table-border-color: var(--el-border-color-extra-light);
  --el-table-header-bg-color: var(--el-fill-color-light);
  --el-table-row-hover-bg-color: var(--el-color-primary-light-9);
}
.ct-table-wrap :deep(th.el-table__cell) {
  height: 48px;
  color: var(--el-text-color-primary);
  font-weight: 500;
}
.ct-table-wrap :deep(td.el-table__cell) {
  height: 58px;
}
</style>
