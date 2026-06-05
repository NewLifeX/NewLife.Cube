<template>
  <el-form :inline="true"  class="cube-search-form cube-search-form-right">
    <el-form-item label="关键词">
      <el-input v-model="$data.searchText" placeholder="关键词搜索" clearable />
    </el-form-item>
    <el-form-item>
      <el-button type="primary" @click="onSearch">查询</el-button>
      <el-button @click="onReset">重置</el-button>
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { reactive } from "vue";
const $props = defineProps({
  fnSearch: {
    type: Function,
    default: () => null,
  },
});
const $data = reactive({
  searchText: "", // 搜索关键字
});
// 搜索
const onSearch = () => {
  // 可传参 params 可自定义参数
  const params = { q: $data.searchText };
  if ($props.fnSearch) { $props.fnSearch({ params, type: "search" }); }
};
// 重置
const onReset = () => {
  $data.searchText = "";
  const params = { q: $data.searchText };
  if ($props.fnSearch) { $props.fnSearch({ params, type: "reset" }); }
};
</script>

<style lang="scss" scoped>
.cube-search-form {
  margin-bottom: 20px;
}

.cube-search-form-right {
  display: flex;
  justify-content: flex-end;
}
</style>


