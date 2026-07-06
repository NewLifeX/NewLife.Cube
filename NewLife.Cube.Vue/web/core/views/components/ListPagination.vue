<!-- eslint-disable vue/multi-word-component-names -->
<script setup lang="ts">
interface Props {
  total?: number;
  pageSize?: number;
  currentPage?: number;
  pageSizes?: number[];
}

const props = withDefaults(defineProps<Props>(), {
  total: 1247,
  pageSize: 20,
  currentPage: 1,
  pageSizes: () => [10, 20, 50, 100],
});

const emit = defineEmits<{
  'update:currentPage': [page: number];
  'update:pageSize': [size: number];
}>();

function handleCurrentChange(page: number) {
  emit('update:currentPage', page);
}

function handleSizeChange(size: number) {
  emit('update:pageSize', size);
  emit('update:currentPage', 1);
}
</script>

<template>
  <div class="list-pagination">
    <el-pagination
      class="lp-pagination"
      :current-page="currentPage"
      :page-size="pageSize"
      :page-sizes="pageSizes"
      :total="total"
      layout="total, sizes, prev, pager, next, jumper"
      @current-change="handleCurrentChange"
      @size-change="handleSizeChange"
    />
  </div>
</template>

<style lang="scss" scoped>
.list-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

.lp-pagination {
  width: 100%;

  :deep(.el-pagination) {
    flex-wrap: wrap;
    justify-content: space-between;
    align-items: center;
    gap: 12px;
    font-family: var(--el-font-family);
    color: var(--el-text-color-secondary);
  }

  :deep(.el-pagination__total),
  :deep(.el-pagination__jump) {
    font-size: 13px;
    color: var(--el-text-color-secondary);
  }

  :deep(.el-select .el-input__wrapper),
  :deep(.el-pagination__editor.el-input .el-input__wrapper) {
    min-height: 32px;
    border-radius: var(--el-border-radius-base);
    background: var(--el-bg-color);
    border: 1px solid var(--el-border-color-light);
    box-shadow: none;
  }

  :deep(.el-select .el-input__wrapper.is-focus),
  :deep(.el-pagination__editor.el-input .el-input__wrapper.is-focus) {
    border-color: var(--el-color-primary);
    box-shadow: 0 0 0 1px var(--el-color-primary) inset;
  }

  :deep(.btn-prev),
  :deep(.btn-next),
  :deep(.el-pager li) {
    min-width: 32px;
    height: 32px;
    border-radius: var(--el-border-radius-base);
    border: 1px solid var(--el-border-color-light);
    background: var(--el-bg-color);
    color: var(--el-text-color-regular);
    transition:
      background 0.15s ease,
      border-color 0.15s ease,
      color 0.15s ease;
    font-weight: 500;
  }

  :deep(.btn-prev:disabled),
  :deep(.btn-next:disabled) {
    opacity: 0.35;
    background: var(--el-disabled-bg-color);
    border-color: var(--el-border-color-lighter);
    cursor: not-allowed;
  }

  :deep(.btn-prev:not(:disabled):hover),
  :deep(.btn-next:not(:disabled):hover),
  :deep(.el-pager li:hover) {
    background: var(--el-color-primary-light-9);
    border-color: var(--el-color-primary);
    color: var(--el-color-primary);
  }

  :deep(.el-pager li.is-active) {
    background: var(--el-color-primary);
    border-color: var(--el-color-primary);
    color: var(--el-color-white);
    font-weight: 600;
  }
}

@media (max-width: 768px) {
  .lp-pagination {
    :deep(.el-pagination) {
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }
  }
}
</style>
