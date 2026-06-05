<template>
  <div class="cube-pager">
    <el-pagination v-model:current-page="$data.currentPage" v-model:page-size="$data.pageSize"
      :page-sizes="$data.pageSizes" :total="$props.total" :layout="$props.layout" @size-change="onSizeChange"
      @current-change="onCurrentChange" />
  </div>
</template>

<script setup lang="ts">
import { reactive } from "vue";

// 定义组件名称
defineOptions({
  name: 'CubePager'
});

// Props 定义 - 参照 CubeSearch 的风格
const $props = defineProps({
  total: {
    type: Number,
    required: true,
  },
  currentPage: {
    type: Number,
    default: 1,
  },
  pageSize: {
    type: Number,
    default: 10,
  },
  pageSizes: {
    type: Array<number>,
    default: () => [5, 10, 20, 30, 50, 100, 500, 1000],
  },
  layout: {
    type: String,
    default: 'total, sizes, prev, pager, next, jumper',
  },
  fnPager: {
    type: Function,
    default: () => null,
  },
});

// 响应式数据 - 参照 CubeSearch 的 $data 风格
const $data = reactive({
  currentPage: $props.currentPage,
  pageSize: $props.pageSize,
  pageSizes: $props.pageSizes,
});

// 页码变更处理 - 参照 CubeSearch 的回调风格
const onCurrentChange = (page: number) => {
  $data.currentPage = page;

  // 构建参数对象，参照 CubeSearch 的 params 结构
  const params = {
    pageIndex: $data.currentPage,
    pageSize: $data.pageSize,
  };

  // 调用父组件回调，传递 type 和 params
  $props.fnPager({
    params,
    type: "onCurrentChange"
  });
};

// 每页显示条数变更处理
const onSizeChange = (size: number) => {
  $data.pageSize = size;
  $data.currentPage = 1; // 改变页面大小时重置到第一页

  // 构建参数对象
  const params = {
    pageIndex: $data.currentPage,
    pageSize: $data.pageSize,
  };

  // 调用父组件回调，传递 type 和 params
  $props.fnPager({
    params,
    type: "onSizeChange"
  });
};

// 暴露方法 - 让父组件可以获取当前分页状态
defineExpose({
  getCurrentPage: () => $data.currentPage,
  getPageSize: () => $data.pageSize,
  getPagerData: () => ({
    pageIndex: $data.currentPage,
    pageSize: $data.pageSize,
  }),
  $data
});
</script>

<style scoped>
.cube-pager {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
