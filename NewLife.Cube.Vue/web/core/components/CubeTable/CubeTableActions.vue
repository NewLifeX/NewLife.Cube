<script setup lang="ts">
/**
 * CubeTable 操作按钮行（列表页第一行，对应 MES ListPage 的 `.page-actions`）
 *
 * 排布与 MES 一致：业务按钮在最左（默认插槽），内置通用按钮（添加 / 下载模板 / 导入 / 导出 /
 * 批量删除）排在其后，右侧留给业务动作（`#extra`）。
 * 一行都没有内容时整行不渲染，不留一条空白带。
 */
import { computed, ref, useSlots } from 'vue';
import { Document, Download, Delete, Plus, Upload } from '@element-plus/icons-vue';
import { resolveToolbarFlags } from './cubeTable.logic';
import type { CubeEngineContext } from '../../engine/types';
import type { CubeTableActions, CubeToolbarFlags } from './types';

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    actions: CubeTableActions;
    /** 显式覆盖按钮开关；不传跟随后端 pageSetting */
    flags?: CubeToolbarFlags;
  }>(),
  { flags: () => ({}) },
);

const slots = useSlots();
/** 隐藏的导入文件框（模板里 ref 会自动解包成元素本体传给 openFilePicker） */
const fileInputRef = ref<HTMLInputElement | null>(null);
const flags = computed(() =>
  resolveToolbarFlags(
    {
      add: props.flags.add,
      batchDelete: props.flags.batchDelete,
      importFile: props.flags.importFile,
      exportFile: props.flags.exportFile,
      downloadTemplate: props.flags.downloadTemplate,
    },
    props.ctx.pageSetting.value,
  ),
);
const selectedCount = computed(() => props.ctx.selected.value.length);
const busy = computed(() => props.ctx.submitting.value);
/** 整行是否有任何内容 */
const visible = computed(
  () =>
    Boolean(slots.default || slots.extra) ||
    (!flags.value.readOnly && (flags.value.add || flags.value.batchDelete || flags.value.importFile || flags.value.downloadTemplate)) ||
    flags.value.exportFile,
);
</script>

<template>
  <div v-if="visible" class="cta">
    <div class="cta-left">
      <!-- 业务按钮先行，内置通用按钮排在其后（与 MES 一致） -->
      <slot :ctx="ctx" />

      <el-button v-if="flags.add && !flags.readOnly" type="primary" :icon="Plus" @click="ctx.openCreate()">
        添加
      </el-button>

      <template v-if="!flags.readOnly">
        <input
          ref="fileInputRef"
          class="cta-file"
          type="file"
          accept=".xlsx,.xls,.csv,.json,.zip"
          @change="actions.onImportFileChange"
        />
        <el-button
          v-if="flags.downloadTemplate"
          :icon="Document"
          @click="ctx.downloadTemplate()"
        >
          下载模板
        </el-button>
        <el-button v-if="flags.importFile" :icon="Upload" :loading="busy" @click="actions.openFilePicker(fileInputRef)">
          导入
        </el-button>
      </template>

      <el-button v-if="flags.exportFile" :icon="Download" @click="actions.exportNow()">导出</el-button>

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
    </div>

    <div class="cta-right">
      <slot name="extra" :ctx="ctx" />
    </div>
  </div>
</template>

<style scoped>
.cta {
  flex-shrink: 0;
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 40px;
  padding: 8px 14px;
  box-sizing: border-box;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
}
.cta-left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}
.cta-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}
/* 按钮间距统一由 flex gap 提供：取消 el-button 默认 margin-left，避免双倍间距 */
.cta :deep(.el-button + .el-button) {
  margin-left: 0;
}
.cta-file {
  display: none;
}
</style>
