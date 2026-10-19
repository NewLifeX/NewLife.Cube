<script setup lang="ts">
/**
 * CubeTable —— 默认列表页的单一集成组件
 *
 * 一屏之内的 搜索 / 工具栏 / 表格 / 分页 / 新增编辑弹窗 全部内置，
 * 定制一律走具名插槽（不再有 Section 覆盖文件）：
 *   `#header` `#search` `#toolbar` `#toolbar-extra` `#table` `#table-row-actions`
 *   `#col-<字段名>` `#empty` `#pagination` `#footer` `#form`
 *
 * 上下文三级解析（`docs/architecture/cube-engine.md` §5.2）：
 *   `:context` 外部指定 → `inject(CubeEngineKey)` 父级已建 → 按当前路由自动创建。
 */
import { computed, inject, useSlots } from 'vue';
import { Setting } from '@element-plus/icons-vue';
import { CubeEngineKey, useCubeEngine } from '../../engine/useCubeEngine';
import type { CubeEngineContext } from '../../engine/types';
import { useCubeTableActions } from './useCubeTableActions';
import { useCubeTableColumns } from './useCubeTableColumns';
import CubeTableSearch from './CubeTableSearch.vue';
import CubeTableToolbar from './CubeTableToolbar.vue';
import CubeTableGrid from './CubeTableGrid.vue';
import CubeTablePagination from './CubeTablePagination.vue';
import CubeTableFormDialog from './CubeTableFormDialog.vue';
import type { CubeGridFlags, CubeToolbarFlags } from './types';

const props = withDefaults(
  defineProps<{
    /** 外部指定引擎上下文（嵌入 / 自管引擎场景），传入则不再自动创建 */
    context?: CubeEngineContext | null;
    /** 无 context 且无注入时，是否按当前路由自动创建引擎 */
    autoEngine?: boolean;
    /** 自动创建时使用的前缀与路由（都不传则取当前路由） */
    apiPrefix?: string;
    routePath?: string;
    /** 自动创建时的初始每页条数 */
    pageSize?: number;
    /** 是否启用「设置列」（localStorage 记忆） */
    columnsSetting?: boolean;
    /** 列显隐存储键，默认按实体前缀分桶 */
    columnsStorageKey?: string;
    /** 工具栏 / 表格按钮与列行为开关（不传跟随后端 pageSetting） */
    toolbar?: CubeToolbarFlags;
    grid?: CubeGridFlags;
    /** 导出格式（默认 xlsx） */
    exportFormat?: string;
    /** 导入可选文件类型 */
    importAccept?: string;
    /** 删除是否二次确认（不传跟随后端 doubleDelete） */
    confirmDelete?: boolean;
    /** 分页可选项 */
    pageSizes?: number[];
  }>(),
  {
    context: null,
    autoEngine: true,
    pageSize: undefined,
    columnsSetting: true,
    exportFormat: 'xlsx',
    importAccept: undefined,
    confirmDelete: undefined,
    pageSizes: () => [10, 20, 50, 100],
  },
);

const injected = inject(CubeEngineKey, null);
const ctx: CubeEngineContext =
  props.context ?? injected ?? useCubeEngine({ routePath: props.routePath, apiPrefix: props.apiPrefix, pageSize: props.pageSize });

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
/** 业务页写过的 `#col-Xxx` 转发给表格区（内置渲染让位） */
const cellSlots = computed(() =>
  Object.keys(slots).filter((name) => name.startsWith('col-')),
);
</script>

<template>
  <div class="cube-table">
    <slot name="header" :ctx="ctx" />

    <el-alert
      v-if="ctx.error.value"
      class="ct-error"
      type="error"
      :title="ctx.error.value"
      show-icon
      :closable="false"
    >
      <template #default>
        <el-button link type="primary" @click="ctx.retry()">重试</el-button>
      </template>
    </el-alert>

    <slot name="search" :ctx="ctx">
      <CubeTableSearch :ctx="ctx" />
    </slot>

    <slot name="toolbar" :ctx="ctx">
      <CubeTableToolbar
        :ctx="ctx"
        :actions="actions"
        :show-add="toolbar?.add"
        :show-batch-delete="toolbar?.batchDelete"
        :show-import="toolbar?.importFile"
        :show-export="toolbar?.exportFile"
        :show-download-template="toolbar?.downloadTemplate"
        :show-refresh="toolbar?.refresh"
      >
        <template #extra>
          <!-- 内置「设置列」排在业务自定义按钮之前：它是视图工具，不该抢业务动作的位 -->
          <el-popover v-if="columnsSetting" placement="bottom-end" :width="220" trigger="click">
            <!-- 不再套一层 el-tooltip：它的触发指令落在 fragment 根上会告警并失效，
                 按钮自身的 title 已经能给悬停提示 -->
            <template #reference>
              <el-button class="ct-cols-trigger" :icon="Setting" circle title="设置列" />
            </template>
            <div class="ct-col-title">展示列</div>
            <div v-for="item in columns.items.value" :key="item.key" class="ct-col-item">
              <el-checkbox
                :model-value="columns.visibility.value[item.key] !== false"
                @change="(v: unknown) => columns.setVisible(item.key, v === true)"
              >
                {{ item.label }}
              </el-checkbox>
            </div>
            <div class="ct-col-footer">
              <el-button link type="primary" @click="columns.reset()">恢复默认</el-button>
            </div>
          </el-popover>
          <slot name="toolbar-extra" :ctx="ctx" />
        </template>
      </CubeTableToolbar>
    </slot>

    <slot name="table" :ctx="ctx">
      <CubeTableGrid
        :ctx="ctx"
        :actions="actions"
        :visibility="columnsSetting ? columns.visibility.value : null"
        v-bind="grid"
      >
        <template v-for="name in cellSlots" #[name]="scope">
          <slot :name="name" v-bind="scope" />
        </template>
        <template v-if="slots['table-row-actions']" #row-actions="scope">
          <slot name="table-row-actions" v-bind="scope" />
        </template>
        <template v-if="slots.empty" #empty>
          <slot name="empty" />
        </template>
      </CubeTableGrid>
    </slot>

    <slot name="pagination" :ctx="ctx">
      <CubeTablePagination :ctx="ctx" :page-sizes="pageSizes" />
    </slot>

    <slot name="footer" :ctx="ctx" />

    <slot name="form" :ctx="ctx">
      <CubeTableFormDialog :ctx="ctx" />
    </slot>
  </div>
</template>

<style scoped>
.cube-table {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 0;
}
.ct-error {
  margin-bottom: -4px;
}
.ct-col-title {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  padding-bottom: 6px;
}
.ct-col-item {
  padding: 1px 4px;
}
.ct-col-footer {
  border-top: 1px solid var(--el-border-color-lighter);
  margin-top: 6px;
  padding-top: 6px;
  text-align: right;
}
</style>
