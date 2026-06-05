<template>
  <div class="cube-list-pager">
    <el-pagination
      v-model:current-page="currentPage"
      v-model:page-size="pageSize"
      :page-sizes="pageSizes"
      :total="total"
      :layout="layout"
      @size-change="handleSizeChange"
      @current-change="handleCurrentChange"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

// 定义组件名称
defineOptions({
  name: 'CubeListPager'
});

// 定义组件 props
interface Props {
  total?: number;
  currentPage?: number;
  pageSize?: number;
  pageSizes?: number[];
  layout?: string;
  onCurrentChange?: (page: number) => void;
  onSizeChange?: (size: number) => void;
  onCallback?: () => void;
}

const props = withDefaults(defineProps<Props>(), {
  currentPage: 1,
  pageSize: 10,
  pageSizes: () => [5, 10, 20, 30, 50, 100, 500, 1000],
  layout: 'total, sizes, prev, pager, next, jumper',
  onCurrentChange: () => {},
  onSizeChange: () => {},
  onCallback: () => {}
});

// 内部分页状态
const currentPage = ref(props.currentPage);
const pageSize = ref(props.pageSize);

// 监听 props 变化，同步内部状态
watch(() => props.currentPage, (newVal) => {
  currentPage.value = newVal;
});

watch(() => props.pageSize, (newVal) => {
  pageSize.value = newVal;
});

// 页码变更处理
const handleCurrentChange = (page: number) => {
  currentPage.value = page;
  if (props.onCurrentChange) {
    props.onCurrentChange(page);
  }
  if (props.onCallback) {
    props.onCallback();
  }
};

// 每页显示条数变更处理
const handleSizeChange = (size: number) => {
  pageSize.value = size;
  currentPage.value = 1; // 改变页面大小时重置到第一页
  if (props.onSizeChange) {
    props.onSizeChange(size);
  }
  if (props.onCallback) {
    props.onCallback();
  }
};
</script>

<style scoped>
.cube-list-pager {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
