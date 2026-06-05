<template>
  <el-form :inline="true" :model="searchForm" class="cube-search-form cube-search-form-right">
    <el-form-item label="关键词">
      <el-input v-model="searchForm.q" placeholder="关键词搜索" clearable>
        <template #prefix>
          <span style="color: #bfbfbf; font-size: 14px;">&#128269;</span>
        </template>
      </el-input>
    </el-form-item>
    <el-form-item>
      <el-button type="primary" @click="handleSearch">查询</el-button>
      <el-button @click="handleReset">重置</el-button>
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { reactive } from 'vue';

// 定义组件名称
defineOptions({
  name: 'CubeListToolbarSearch'
});

// 定义搜索表单类型
// interface SearchFormData {
//   p: string;
// }

// 定义组件 props
interface Props {
  onSearch?: (searchData: Record<string, unknown>) => void;
  onReset?: (searchData:  Record<string, unknown>) => void;
  onCallback?: (searchData: Record<string, unknown>) => void;
}

const props = withDefaults(defineProps<Props>(), {
  onSearch: () => {},
  onReset: () => {},
  onCallback: () => {}
});

// // 查询表单数据
const searchForm = reactive({
  q: ''
});

// 搜索处理
const handleSearch = () => {
  if (props.onSearch) {
    props.onSearch(searchForm);
  }
  if (props.onCallback) {
    props.onCallback(searchForm);
  }
};

// 重置处理
const handleReset = () => {
  searchForm.q = '';
  if (props.onReset) {
    props.onReset(searchForm);
  }
  if (props.onCallback) {
    props.onCallback(searchForm);
  }
};
</script>

<style scoped>
.cube-search-form {
  margin-bottom: 20px;
}

.cube-search-form-right {
  display: flex;
  justify-content: flex-end;
}
</style>
