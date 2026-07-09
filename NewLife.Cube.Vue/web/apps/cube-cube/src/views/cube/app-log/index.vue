<template>
  <div class="app-log-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>应用日志</h3>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table
        :data="tableData"
        style="width: 100%"
        v-loading="loading"
      >
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="appId" label="应用ID" width="80" />
        <el-table-column prop="appName" label="应用名称" />
        <el-table-column prop="action" label="操作" />
        <el-table-column prop="createTime" label="创建时间" />
        <el-table-column prop="remark" label="备注" show-overflow-tooltip />
        <el-table-column label="状态" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.success ? 'success' : 'danger'">
              {{ scope.row.success ? '成功' : '失败' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>

      <CubeListPager
        :total="queryParams.total"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </el-card>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 日志类型接口，继承 BaseEntity
interface AppLog extends BaseEntity {
  appId: number;
  appName: string;
  action: string;
  success: boolean;
  remark: string;
  createUser: string;
  createIP: string;
}

// 响应式数据
const loading = ref(false);
const tableData = ref<AppLog[]>([]);

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  fetchData();
};

// 加载数据
const fetchData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/AppLog', { params: queryParams });
    const { list, page } = apiDataToList<AppLog>(response);
    tableData.value = list;
    queryParams.total = page?.totalCount || list.length;
  } catch (error) {
    ElMessage.error('加载数据失败');
    console.error('加载数据失败:', error);
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.value = false;
  }
};

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 页码变更处理
const CurrentPageChange = (page: number) => {
  queryParams.pageIndex = page;
};

// 每页显示条数变更处理
const PageSizeChange = (size: number) => {
  queryParams.pageSize = size;
  queryParams.pageIndex = 1;
};

// 初始化
onMounted(() => {
  fetchData();
});
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}
.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
