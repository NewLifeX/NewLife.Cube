<script setup lang="ts">
import { getValueByKey } from '@newlifex/cube-vue/core/utils/url';
interface Column {
  key: string;
  label: string;
  width?: string;
  align?: 'left' | 'center' | 'right';
  mono?: boolean;
  render?: (row: Record<string, unknown>) => string;
}

interface Props {
  columns?: Column[];
  data?: Record<string, unknown>[];
  loading?: boolean;
  selectable?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  columns: () =>
    [
      { key: 'id', label: 'ID', width: '80px', mono: true },
      { key: 'name', label: '名称' },
      { key: 'status', label: '状态' },
    ] satisfies Column[],
  data: () => [
    { id: '10001', name: '示例数据 A', status: 'active' },
    { id: '10002', name: '示例数据 B', status: 'disabled' },
    { id: '10003', name: '示例数据 C', status: 'pending' },
  ],
  loading: false,
  selectable: false,
});

const emit = defineEmits<{
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
}>();

const STATUS_LABELS: Record<string, { text: string; cls: string }> = {
  active: { text: '正常', cls: 'ok' },
  disabled: { text: '禁用', cls: 'er' },
  pending: { text: '待审核', cls: 'wn' },
  draft: { text: '草稿', cls: 'in' },
};

function getCellValue(row: Record<string, unknown>, col: Column): string {
  if (col.render) return col.render(row);
  return String(getValueByKey(row, col.key) ?? '');
}

function getStatusInfo(val: string) {
  return STATUS_LABELS[val] ?? null;
}

function getSelectionColumnWidth() {
  return 46;
}
</script>

<template>
  <div class="list-table-content">
    <el-table
      :data="data"
      class="ltc-table"
      v-loading="loading"
      empty-text=""
      header-row-class-name="ltc-header-row"
      stripe
      border
    >
      <el-table-column
        v-if="selectable"
        type="selection"
        :width="getSelectionColumnWidth()"
        align="center"
      />

      <el-table-column
        v-for="col in columns"
        :key="col.key"
        :prop="col.key"
        :label="col.label"
        :width="col.width"
        :align="col.align ?? 'left'"
      >
        <template #header>
          <el-tooltip :content="col.label" placement="top" :show-after="500">
            <span class="ltc-header-text">{{ col.label }}</span>
          </el-tooltip>
        </template>
        <template #default="scope">
          <template
            v-if="
              col.key === 'status' && getStatusInfo(String(getValueByKey(scope.row, col.key) ?? ''))
            "
          >
            <el-tag
              effect="plain"
              round
              disable-transitions
              :class="`ltc-tag ltc-tag--${getStatusInfo(String(getValueByKey(scope.row, col.key) ?? ''))!.cls}`"
            >
              {{ getStatusInfo(String(getValueByKey(scope.row, col.key) ?? ''))!.text }}
            </el-tag>
          </template>
          <span v-else class="ltc-cell-text" :class="{ 'ltc-cell-text--mono': col.mono }">
            {{ getCellValue(scope.row, col) }}
          </span>
        </template>
      </el-table-column>

      <el-table-column label="操作" width="120" fixed="right" align="center">
        <template #header>
          <el-tooltip content="操作" placement="top" :show-after="500">
            <span class="ltc-header-text">操作</span>
          </el-tooltip>
        </template>
        <template #default="scope">
          <div class="ltc-actions">
            <button class="ltc-action-btn ltc-action-btn--edit" @click="emit('edit', scope.row)">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="14" height="14">
                <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z"></path>
                <path d="m15 5 4 4"></path>
              </svg>
              <span>编辑</span>
            </button>
            <button class="ltc-action-btn ltc-action-btn--delete" @click="emit('delete', scope.row)">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="14" height="14">
                <path d="M3 6h18"></path>
                <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"></path>
                <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"></path>
              </svg>
              <span>删除</span>
            </button>
          </div>
        </template>
      </el-table-column>


    </el-table>
  </div>
</template>

<style lang="scss" scoped>
.list-table-content {
  width: 100%;
}

.ltc-table {
  :deep(.el-table__inner-wrapper::before) {
    display: none;
  }

  :deep(.el-table__header-wrapper th.el-table__cell) {
    background: var(--el-fill-color-lighter);
    border-bottom: 2px solid var(--el-border-color-light);
    padding: 0;
  }

  :deep(.el-table__header-wrapper .cell) {
    padding: 14px 16px;
    font-family: var(--el-font-family);
    font-size: 12px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--el-text-color-secondary);
  }

  :deep(.el-table__body-wrapper td.el-table__cell) {
    padding: 0;
    border-bottom: 1px solid var(--el-border-color-lighter);
    transition: background 0.15s ease;
  }

  :deep(.el-table__body tr:hover > td.el-table__cell) {
    background: var(--el-color-primary-light-9);
  }

  :deep(.el-table__body tr.el-table__row--striped > td.el-table__cell) {
    background: var(--el-fill-color-lighter);
  }

  :deep(.el-table__body tr.el-table__row--striped:hover > td.el-table__cell) {
    background: var(--el-color-primary-light-9);
  }

  :deep(.el-table__body .cell) {
    padding: 14px 16px;
    line-height: 1.5;
  }

  :deep(.el-checkbox__input.is-checked .el-checkbox__inner),
  :deep(.el-checkbox__input.is-indeterminate .el-checkbox__inner) {
    background-color: var(--el-color-primary);
    border-color: var(--el-color-primary);
  }

  :deep(.el-checkbox__inner:hover) {
    border-color: var(--el-color-primary);
  }

  :deep(.el-table__fixed-right) {
    border-left: 1px solid var(--el-border-color-light);
  }

  :deep(.el-table__fixed-right::before) {
    background-color: transparent;
  }
}

.ltc-header-text {
  display: inline-block;
  max-width: 100%;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: bottom;
}

.ltc-cell-text {
  font-family: var(--el-font-family);
  font-size: 13px;
  color: var(--el-text-color-primary);

  &--mono {
    font-family: var(--el-font-family-mono);
    font-size: 12px;
    font-weight: 500;
  }
}

.ltc-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.ltc-action-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: transparent;
  border: none;
  border-radius: 4px;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.15s ease;

  &--edit {
    color: var(--el-text-color-secondary);

    &:hover {
      background: var(--el-color-primary-light-9);
      color: var(--el-color-primary);
    }
  }

  &--delete {
    color: var(--el-text-color-secondary);

    &:hover {
      background: var(--el-color-danger-light-9);
      color: var(--el-color-danger);
    }
  }
}

.ltc-tag {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-family: var(--el-font-family);
  font-size: 12px;
  font-weight: 500;
  padding: 4px 12px;
  border-radius: var(--el-border-radius-round);
  border: none;

  &::before {
    content: '';
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: currentColor;
    flex-shrink: 0;
  }

  &--ok {
    background: var(--el-color-success-light-9);
    color: var(--el-color-success);
  }

  &--wn {
    background: var(--el-color-warning-light-9);
    color: var(--el-color-warning);
  }

  &--er {
    background: var(--el-color-danger-light-9);
    color: var(--el-color-danger);
  }

  &--in {
    background: var(--el-fill-color);
    color: var(--el-text-color-secondary);
  }
}
</style>
