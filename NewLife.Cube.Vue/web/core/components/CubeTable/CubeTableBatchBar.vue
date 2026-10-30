<script setup lang="ts">
/**
 * 选中态批量操作条
 *
 * 只在勾选数据后出现：已选数量 + 批量启用/禁用 + 批量删除 + 取消选择。
 * 不勾选时整条不渲染，避免列表页常驻一排置灰按钮。
 */
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { CircleCheck, CircleClose, Delete } from '@element-plus/icons-vue';
import request from '../../utils/request';
import { canBatchEnable, toolbarKeyField } from './cubeTable.logic';
import { usePagePermissions } from '../../composables/usePagePermissions';
import type { CubeEngineContext } from '../../engine/types';
import type { CubeTableActions } from './types';
import type { ToolbarFlags } from './cubeTable.logic';

const props = defineProps<{
  ctx: CubeEngineContext;
  actions: CubeTableActions;
  flags: ToolbarFlags;
}>();

const { t } = useI18n();
const permissions = usePagePermissions(props.ctx);
const count = computed(() => props.ctx.selected.value.length);
const busy = computed(() => props.ctx.submitting.value);
/** 正在执行的批量动作名，防止重复提交 */
const pending = ref<string | null>(null);

/** 实体含 Boolean 类型 Enable 字段时提供批量启用/禁用（与 MVC 工具栏自动注入同口径） */
const batchEnableAvailable = computed(
  () =>
    canBatchEnable(props.ctx.listFields.value) &&
    !props.flags.readOnly &&
    permissions.canUpdate.value,
);
const batchDeleteAvailable = computed(
  () => props.flags.batchDelete && !props.flags.readOnly && permissions.canDelete.value,
);

const keyField = computed(() => toolbarKeyField(props.ctx.listFields.value));

/** 勾选行主键（与工具栏动作同一取键口径） */
function selectedKeys(): unknown[] {
  const key = keyField.value;
  return props.ctx.selected.value
    .map((row) => row[key] ?? row[key.toLowerCase()] ?? row[key.toUpperCase()])
    .filter((value) => value !== null && value !== undefined && value !== '');
}

/** 批量启用 / 禁用：POST 到后端 enableSelect / disableSelect，成功后刷新列表 */
async function batchSetEnable(enable: boolean): Promise<void> {
  const keys = selectedKeys();
  if (keys.length === 0 || pending.value) return;

  const act = enable ? 'enableSelect' : 'disableSelect';
  const label = enable ? t('grid.batchEnable') : t('grid.batchDisable');
  try {
    await ElMessageBox.confirm(t('grid.batchEnableConfirm', { count: keys.length, action: label }), {
      type: 'warning',
    });
  } catch {
    return;
  }

  pending.value = label;
  try {
    const res = (await request({
      url: `${props.ctx.apiPrefix.value}/${act}`,
      method: 'post',
      data: { keys: keys.join(',') },
    })) as { message?: string } | undefined;
    ElMessage.success(res?.message || t('common.saveSuccess'));
    props.ctx.setSelected([]);
    await props.ctx.refresh();
  } catch (err) {
    // 请求层已统一提示，这里只避免未捕获 rejection
    console.error('[CubeTable] batch enable/disable failed:', err);
  } finally {
    pending.value = null;
  }
}

async function removeSelected(): Promise<void> {
  if (pending.value) return;
  pending.value = t('grid.batchDelete');
  try {
    await props.actions.removeSelectedRows();
  } finally {
    pending.value = null;
  }
}

function clearSelection(): void {
  props.ctx.setSelected([]);
}
</script>

<template>
  <div v-if="count > 0" class="ctb" data-role="batch-bar">
    <span class="ctb-count">{{ t('grid.selectedCount', { count }) }}</span>

    <el-button
      v-if="batchEnableAvailable"
      type="success"
      plain
      size="small"
      :icon="CircleCheck"
      :loading="pending === t('grid.batchEnable')"
      :disabled="busy && pending !== t('grid.batchEnable')"
      data-role="batch-enable"
      @click="batchSetEnable(true)"
    >
      {{ t('grid.batchEnable') }}
    </el-button>
    <el-button
      v-if="batchEnableAvailable"
      type="warning"
      plain
      size="small"
      :icon="CircleClose"
      :loading="pending === t('grid.batchDisable')"
      :disabled="busy && pending !== t('grid.batchDisable')"
      data-role="batch-disable"
      @click="batchSetEnable(false)"
    >
      {{ t('grid.batchDisable') }}
    </el-button>
    <el-button
      v-if="batchDeleteAvailable"
      type="danger"
      plain
      size="small"
      :icon="Delete"
      :loading="pending === t('grid.batchDelete')"
      :disabled="busy && pending !== t('grid.batchDelete')"
      data-role="batch-delete"
      @click="removeSelected"
    >
      {{ t('grid.batchDelete') }}
    </el-button>
    <el-button link size="small" data-role="clear-selection" @click="clearSelection">
      {{ t('grid.clearSelection') }}
    </el-button>
  </div>
</template>

<style scoped>
.ctb {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  padding: 4px 10px;
  border: 1px solid var(--el-color-primary-light-7);
  border-radius: 6px;
  background: var(--el-color-primary-light-9);
}
.ctb-count {
  font-size: 12px;
  font-weight: 600;
  color: var(--el-color-primary);
  white-space: nowrap;
}
</style>
