<template>
  <div class="cb-table">
    <el-table
      :data="data"
      :loading="dataSet.loading"
      stripe
      v-bind="$attrs"
      @sort-change="handleSortChange"
      @filter-change="handleFilterChange"
    >
      <template v-if="columns && columns.length">
        <el-table-column
          v-for="col in columns"
          :key="String(col.prop)"
          :prop="col.prop ? String(col.prop) : undefined"
          :label="col.label"
          :width="col.width"
          :min-width="col.minWidth"
          :sortable="col.sortable"
          :align="col.align"
          :header-align="col.headerAlign"
          :fixed="col.fixed"
          :type="col.type"
          :index="col.index"
          :formatter="
            (row, column, cellValue, index) => {
              return col.formatter
                ? col.formatter(
                    row,
                    {
                      ...(column as any),
                    },
                    cellValue,
                    index,
                  )
                : '';
            }
          "
          :filters="col.filters"
          :filter-method="(value, row, column) => col.filterMethod?.(value, row, column as any)"
          :filter-multiple="col.filterMultiple"
          :filtered-value="col.filteredValue"
          :show-overflow-tooltip="typeof col.showOverflowTooltip === 'boolean' ? col.showOverflowTooltip : false"
          :class-name="col.className"
          :label-class-name="col.labelClassName"
          :resizable="col.resizable"
          :sort-method="col.sortMethod"
          :sort-by="col.sortBy"
          :selectable="col.selectable"
          :reserve-selection="col.reserveSelection"
          :column-key="col.columnKey"
        >
          <template v-if="col.render" #default="{ row, $index }">
            <component :is="col.render(col.prop ? row[col.prop] : null, row, $index)" />
          </template>
        </el-table-column>
      </template>
      <slot v-else></slot>
    </el-table>

    <div class="pagination-wrapper" v-if="showPagination">
      <el-pagination
        :current-page="currentPage"
        :page-size="dataSet.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="
          (page) => {
            currentPage = page;
          }
        "
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import type { DataSet } from '../dataset/data-set/DataSet';

defineOptions({
  name: 'CbTable',
});

type Props = {
   
  dataSet: DataSet<any, any>;
  showPagination?: boolean;
};
const props = defineProps<Props>();

const columns = computed(() => {
  // 兼容 getColumns 方法不存在的情况
  return typeof props.dataSet.getColumns === 'function' ? props.dataSet.getColumns() : [];
});

const currentPage = ref(1);
const total = ref(0);

const data = computed(() => {
  return props.dataSet.data;
});

const handleSortChange = ({ prop, order }: { prop: string; order: string }) => {
  query({ sortField: prop, sortOrder: order });
};

const handleFilterChange = (filters: Record<string, unknown>) => {
  query({ filters });
};

const handleSizeChange = (size: number) => {
  query({ currentPage: currentPage.value, pageSize: size });
};

/**
 *  查询方法
 * @param params { currentPage?: number; pageSize?: number; [key: string]: any }
 */
function query(params: { currentPage?: number; pageSize?: number; [key: string]: unknown } = {}) {
  const ds = props.dataSet;
  if (params.currentPage !== undefined) {
    ds.currentPage = params.currentPage;
  }
  if (params.pageSize !== undefined) {
    ds.pageSize = params.pageSize;
  }

  delete params.currentPage;
  delete params.pageSize;

  ds.read(params);
}

watch(
  () => props.dataSet.totalCount,
  (newValue) => {
    total.value = newValue;
  },
  { immediate: true },
);

// 兼容性处理：如果dataSet没有totalCount属性，则使用data.length
watch(
  () => props.dataSet.data,
  () => {
    if (typeof props.dataSet.totalCount === 'undefined') {
      total.value = props.dataSet.data.length;
    }
  },
);
watch(currentPage, (val, oldVal) => {
  if (val !== oldVal) {
    query({ currentPage: val, pageSize: props.dataSet.pageSize });
  }
});
</script>

<style scoped>
.cb-table {
  width: 100%;
}
.cb-table :deep(.el-table) {
  --el-table-border-color: transparent;
  border: none;
}
.pagination-wrapper {
  margin-top: 16px;
  text-align: right;
}
</style>
