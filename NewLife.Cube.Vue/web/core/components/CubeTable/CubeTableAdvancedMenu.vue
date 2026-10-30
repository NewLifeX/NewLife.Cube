<script setup lang="ts">
/**
 * 列表页「高级」菜单
 *
 * 收纳低频/危险操作：下载导入模板、导入、多格式导出、删除全部、开发工具。
 * 业务自定义工具栏按钮不进入本菜单，仍平铺在操作区。
 */
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessageBox } from 'element-plus';
import {
  ArrowDown,
  Delete,
  Document,
  Download,
  FolderOpened,
  RefreshLeft,
  Upload,
} from '@element-plus/icons-vue';
import { hasActiveSearch } from '../../engine/logic';
import { usePagePermissions } from '../../composables/usePagePermissions';
import type { CubeEngineContext, DevelopAction } from '../../engine/types';
import type { CubeTableActions } from './types';
import type { ToolbarFlags } from './cubeTable.logic';

const props = defineProps<{
  ctx: CubeEngineContext;
  actions: CubeTableActions;
  flags: ToolbarFlags;
}>();

const { t } = useI18n();
const permissions = usePagePermissions(props.ctx);
const fileInputRef = ref<HTMLInputElement | null>(null);

const hasQuery = computed(() => hasActiveSearch(props.ctx.keyword.value, props.ctx.searchModel));
const canImport = computed(
  () => props.flags.importFile && !props.flags.readOnly && permissions.canAdd.value,
);
const canDeleteAll = computed(
  () =>
    props.flags.batchDelete &&
    !props.flags.readOnly &&
    permissions.canDelete.value &&
    hasQuery.value &&
    props.ctx.total.value > 0,
);
const showDevelop = computed(
  () => props.ctx.pageSetting.value.develop === true && props.ctx.pageSetting.value.isSystem === true,
);
const canClearTable = computed(
  () => showDevelop.value && !hasQuery.value && props.ctx.total.value > 0,
);

const visible = computed(
  () =>
    props.flags.advanced &&
    Boolean(
      props.flags.downloadTemplate ||
        canImport.value ||
        props.flags.exportFile ||
        canDeleteAll.value ||
        showDevelop.value,
    ),
);

/** 危险操作确认：统一「危险色 + 明确动词」，避免默认 OK/Cancel 英文按钮 */
async function confirmDanger(message: string, title: string): Promise<boolean> {
  try {
    await ElMessageBox.confirm(message, title, {
      type: 'warning',
      confirmButtonText: t('button.confirm'),
      cancelButtonText: t('button.cancel'),
      confirmButtonClass: 'el-button--danger',
    });
    return true;
  } catch {
    return false;
  }
}

function openFilePicker(): void {
  fileInputRef.value?.click();
}

async function onFileChange(event: Event): Promise<void> {
  await props.actions.onImportFileChange(event);
}

async function deleteAll(): Promise<void> {
  if (!(await confirmDanger(t('grid.deleteAllConfirm', { count: props.ctx.total.value }), t('grid.deleteAll'))))
    return;
  await props.ctx.removeAll();
}

async function develop(act: DevelopAction): Promise<void> {
  if (act === 'Restore') {
    if (!(await confirmDanger(t('grid.restoreConfirm'), t('grid.restore')))) return;
  } else if (act === 'Clear') {
    if (
      !(await confirmDanger(
        t('grid.clearTableConfirm', { count: props.ctx.total.value }),
        t('grid.clearTable'),
      ))
    )
      return;
  }
  await props.ctx.developAction(act);
}

async function onCommand(command: string): Promise<void> {
  if (props.ctx.submitting.value) return;

  if (command === 'template') {
    await props.ctx.downloadTemplate();
    return;
  }
  if (command === 'import') {
    openFilePicker();
    return;
  }
  if (command.startsWith('export:')) {
    await props.ctx.exportFile(command.slice('export:'.length));
    return;
  }
  if (command === 'delete-all') {
    await deleteAll();
    return;
  }
  if (command === 'backup') {
    await develop('Backup');
    return;
  }
  if (command === 'backup-export') {
    await develop('BackupAndExport');
    return;
  }
  if (command === 'restore') {
    await develop('Restore');
    return;
  }
  if (command === 'clear') {
    await develop('Clear');
  }
}
</script>

<template>
  <el-dropdown v-if="visible" trigger="click" :disabled="ctx.submitting.value" @command="onCommand">
    <el-button size="small" data-role="advanced">
      {{ t('grid.advanced') }}
      <el-icon class="el-icon--right"><ArrowDown /></el-icon>
    </el-button>

    <template #dropdown>
      <el-dropdown-menu class="ctam-menu">
        <el-dropdown-item
          v-if="flags.downloadTemplate"
          command="template"
          :icon="Document"
          :disabled="ctx.submitting.value"
        >
          {{ t('grid.downloadTemplate') }}
        </el-dropdown-item>
        <el-dropdown-item
          v-if="canImport"
          command="import"
          :icon="Upload"
          :disabled="ctx.submitting.value"
        >
          {{ t('grid.import') }}
        </el-dropdown-item>

        <el-dropdown-item
          v-if="flags.exportFile"
          command="export:xlsx"
          :icon="Download"
          divided
          :disabled="ctx.submitting.value"
        >
          {{ t('grid.exportExcel') }}
        </el-dropdown-item>
        <el-dropdown-item v-if="flags.exportFile" command="export:csv" :icon="Download" :disabled="ctx.submitting.value">
          {{ t('grid.exportCsv') }}
        </el-dropdown-item>
        <el-dropdown-item v-if="flags.exportFile" command="export:json" :icon="Download" :disabled="ctx.submitting.value">
          {{ t('grid.exportJson') }}
        </el-dropdown-item>
        <el-dropdown-item v-if="flags.exportFile" command="export:xml" :icon="Download" :disabled="ctx.submitting.value">
          {{ t('grid.exportXml') }}
        </el-dropdown-item>
        <el-dropdown-item v-if="flags.exportFile" command="export:zip" :icon="Download" :disabled="ctx.submitting.value">
          {{ t('grid.exportZip') }}
        </el-dropdown-item>

        <el-dropdown-item
          v-if="canDeleteAll"
          command="delete-all"
          :icon="Delete"
          divided
          class="ctam-danger"
          :disabled="ctx.submitting.value"
        >
          {{ t('grid.deleteAll') }}
        </el-dropdown-item>

        <el-dropdown-item
          v-if="showDevelop"
          command="backup"
          :icon="FolderOpened"
          divided
          :disabled="ctx.submitting.value"
        >
          {{ t('grid.backup') }}
        </el-dropdown-item>
        <el-dropdown-item v-if="showDevelop" command="backup-export" :icon="Download" :disabled="ctx.submitting.value">
          {{ t('grid.backupAndExport') }}
        </el-dropdown-item>
        <el-dropdown-item v-if="showDevelop" command="restore" :icon="RefreshLeft" :disabled="ctx.submitting.value">
          {{ t('grid.restore') }}
        </el-dropdown-item>
        <el-dropdown-item
          v-if="canClearTable"
          command="clear"
          :icon="Delete"
          class="ctam-danger"
          :disabled="ctx.submitting.value"
        >
          {{ t('grid.clearTable') }}
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>

  <input
    ref="fileInputRef"
    class="ctam-file"
    type="file"
    accept=".xlsx,.xls,.csv,.json,.zip"
    data-role="advanced-import-file"
    @change="onFileChange"
  />
</template>

<style scoped>
.ctam-file {
  display: none;
}
.ctam-menu :deep(.ctam-danger) {
  color: var(--el-color-danger);
}
.ctam-menu :deep(.ctam-danger .el-icon) {
  color: inherit;
}
</style>
