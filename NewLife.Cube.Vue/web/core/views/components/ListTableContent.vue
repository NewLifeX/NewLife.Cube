<script setup lang="ts">
import { getValueByKey } from 'cube-front/core/utils/url';
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
        show-overflow-tooltip
      >
        <template #default="scope">
          <template v-if="col.key === 'status' && getStatusInfo(String(getValueByKey(scope.row, col.key) ?? ''))">
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
        <template #default="scope">
          <el-button type="primary" link @click="emit('edit', scope.row)">编辑</el-button>
          <el-button type="danger" link @click="emit('delete', scope.row)">删除</el-button>
        </template>
      </el-table-column>

      <template #empty>
        <div class="ltc-empty">暂无数据</div>
      </template>
    </el-table>
  </div>
</template>

<style lang="scss" scoped>
.list-table-content {
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  box-shadow: var(--el-box-shadow-light);
  overflow: hidden;
}

.ltc-table {
  :deep(.el-table__inner-wrapper::before) {
    display: none;
  }

  :deep(.el-table__header-wrapper th.el-table__cell) {
    background: var(--el-bg-color);
    border-bottom: 1px solid var(--el-border-color-light);
    padding: 0;
  }

  :deep(.el-table__header-wrapper .cell) {
    padding: 10px 14px;
    font-family: 'Fira Sans', system-ui, sans-serif;
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--el-text-color-secondary);
  }

  :deep(.el-table__body-wrapper td.el-table__cell) {
    padding: 0;
    border-bottom: 1px solid var(--el-border-color-light);
  }

  :deep(.el-table__body tr:hover > td.el-table__cell) {
    background: var(--table-row-hover, var(--el-color-primary-light-9));
  }

  :deep(.el-table__body .cell) {
    padding: 12px 14px;
    line-height: 1.4;
  }

  :deep(.el-checkbox__input.is-checked .el-checkbox__inner),
  :deep(.el-checkbox__input.is-indeterminate .el-checkbox__inner) {
    background-color: var(--el-color-primary);
    border-color: var(--el-color-primary);
  }

  :deep(.el-checkbox__inner:hover) {
    border-color: var(--el-color-primary);
  }
}

.ltc-cell-text {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  color: var(--el-text-color-primary);

  &--mono {
    font-family: 'JetBrains Mono', monospace;
    font-size: 12.5px;
  }
}

.ltc-empty {
  padding: 48px 16px;
  text-align: center;
  color: var(--el-text-color-secondary);
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
}

.ltc-tag {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 12px;
  font-weight: 500;
  padding: 3px 9px;
  border: none;

  &::before {
    content: '';
    width: 5px;
    height: 5px;
    border-radius: 50%;
    background: currentColor;
    flex-shrink: 0;
  }

  &--ok {
    background: var(--okl);
    color: var(--ok);
  }
  &--wn {
    background: var(--wnl);
    color: var(--wn);
  }
  &--er {
    background: var(--erl);
    color: var(--el-color-danger);
  }
  &--in {
    background: var(--inl);
    color: var(--in);
  }
}
</style>
