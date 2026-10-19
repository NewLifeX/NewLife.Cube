<script setup lang="ts">
/**
 * CubeTable 工具栏（内置默认实现）
 *
 * 按钮显隐判定来自 `resolveToolbarFlags`（显式 props > 后端 pageSetting > 默认），
 * 需要用户点头或碰 DOM 的动作一律委托给 `actions`，本组件不写业务分支。
 * 插槽：`#metrics`（左侧统计区）、`#extra`（右侧追加按钮）。
 */
import { computed, ref } from 'vue';
import { Delete, Download, Plus, Refresh, Tickets, Upload } from '@element-plus/icons-vue';
import { resolveToolbarFlags } from './cubeTable.logic';
import type { CubeEngineContext } from '../../engine/types';
import type { CubeTableActions } from './types';

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    actions: CubeTableActions;
    /** 显式覆盖按钮开关；不传则跟随后端 pageSetting */
    showAdd?: boolean;
    showBatchDelete?: boolean;
    showImport?: boolean;
    showExport?: boolean;
    showDownloadTemplate?: boolean;
    showRefresh?: boolean;
  }>(),
  {
    showAdd: undefined,
    showBatchDelete: undefined,
    showImport: undefined,
    showExport: undefined,
    showDownloadTemplate: undefined,
    showRefresh: undefined,
  },
);

const fileInput = ref<HTMLInputElement | null>(null);
const flags = computed(() =>
  resolveToolbarFlags(
    {
      add: props.showAdd,
      batchDelete: props.showBatchDelete,
      importFile: props.showImport,
      exportFile: props.showExport,
      downloadTemplate: props.showDownloadTemplate,
      refresh: props.showRefresh,
    },
    props.ctx.pageSetting.value,
  ),
);
const selectedCount = computed(() => props.ctx.selected.value.length);
const busy = computed(() => props.ctx.submitting.value);
</script>

<template>
  <div class="ctt flex flex-wrap items-center gap-2 py-2">
    <div class="ctt-metrics flex items-center gap-3">
      <slot name="metrics" :ctx="ctx" />
    </div>

    <div class="ctt-buttons flex flex-wrap items-center gap-2">
      <el-button
        v-if="flags.add && !flags.readOnly"
        type="primary"
        :icon="Plus"
        @click="ctx.openCreate()"
      >
        新增
      </el-button>

      <el-button
        v-if="flags.batchDelete && !flags.readOnly"
        type="danger"
        plain
        :icon="Delete"
        :disabled="selectedCount === 0"
        @click="actions.removeSelectedRows()"
      >
        批量删除<template v-if="selectedCount > 0">（{{ selectedCount }}）</template>
      </el-button>

      <!-- 导入：隐藏文件框由按钮触发，change 直接交给引擎（0 行会降级为警示） -->
      <template v-if="flags.importFile && !flags.readOnly">
        <input
          ref="fileInput"
          class="ctt-file"
          type="file"
          accept=".xlsx,.xls,.csv,.json,.zip"
          @change="actions.onImportFileChange"
        />
        <el-button :icon="Upload" :loading="busy" @click="actions.openFilePicker(fileInput)">
          导入
        </el-button>
      </template>

      <el-button
        v-if="flags.downloadTemplate && !flags.readOnly"
        :icon="Tickets"
        @click="ctx.downloadTemplate()"
      >
        下载模板
      </el-button>

      <el-button v-if="flags.exportFile" :icon="Download" @click="actions.exportNow()">
        导出
      </el-button>

      <slot name="extra" :ctx="ctx" />
    </div>

    <el-button
      v-if="flags.refresh"
      class="ctt-refresh"
      :icon="Refresh"
      circle
      title="刷新"
      @click="ctx.refresh()"
    />
  </div>
</template>

<style scoped>
.ctt {
  row-gap: 8px;
}
.ctt-metrics:empty {
  display: none;
}
.ctt-buttons {
  margin-left: auto;
}
.ctt-file {
  display: none;
}
</style>
