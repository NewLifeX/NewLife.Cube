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
  justify-content: flex-end;
  background: var(--card);
  border: 1px solid var(--bd);
  border-radius: var(--r);
  box-shadow: var(--sh);
  padding: 10px 16px;
}

.lp-pagination {
  :deep(.el-pagination) {
    flex-wrap: wrap;
    justify-content: flex-end;
    gap: 8px 12px;
    font-family: 'Fira Sans', system-ui, sans-serif;
    color: var(--t3);
  }

  :deep(.el-pagination__total),
  :deep(.el-pagination__jump) {
    font-size: 13px;
    color: var(--t3);
  }

  :deep(.el-select .el-input__wrapper),
  :deep(.el-pagination__editor.el-input .el-input__wrapper) {
    border-radius: 6px;
    box-shadow: 0 0 0 1px var(--bd) inset;
    background: var(--el-fill-color-blank);
  }

  :deep(.btn-prev),
  :deep(.btn-next),
  :deep(.el-pager li) {
    min-width: 32px;
    height: 32px;
    border-radius: 6px;
    border: 1px solid var(--bd);
    background: var(--el-fill-color-blank);
    color: var(--t2);
    transition:
      background 0.13s var(--ease),
      border-color 0.13s var(--ease),
      color 0.13s var(--ease);
  }

  :deep(.btn-prev:disabled),
  :deep(.btn-next:disabled) {
    opacity: 0.4;
    background: var(--el-disabled-bg-color);
    border-color: var(--bd);
  }

  :deep(.btn-prev:not(:disabled):hover),
  :deep(.btn-next:not(:disabled):hover),
  :deep(.el-pager li:hover) {
    background: var(--ac-l);
    border-color: var(--ac-b);
    color: var(--ac);
  }

  :deep(.el-pager li.is-active) {
    background: var(--ac);
    border-color: var(--ac);
    color: var(--text-inverse);
  }
}

@media (max-width: 768px) {
  .list-pagination {
    justify-content: flex-start;
  }

  .lp-pagination {
    width: 100%;

    :deep(.el-pagination) {
      justify-content: flex-start;
    }
  }
}
</style>
