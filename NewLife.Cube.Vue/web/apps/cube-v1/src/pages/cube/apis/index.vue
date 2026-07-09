<template>
  <div class="cube-apis-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>Cube API列表</h3>
          <el-button type="primary" @click="handleGetApis">获取API列表</el-button>
        </div>
      </template>

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="name" label="API名称" min-width="150" />
        <el-table-column prop="url" label="URL" min-width="200" />
        <el-table-column prop="method" label="请求方法" width="100" />
        <el-table-column prop="description" label="描述" min-width="200" />
        <el-table-column prop="createTime" label="创建时间" width="160" />
      </el-table>

      <div class="pagination">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { request } from '@newlifex/cube-vue/core/utils/request';

// 定义数据类型
interface ApiData {
  id: number;
  name: string;
  url: string;
  method: string;
  description: string;
  createTime: string;
}

// 表格数据
const tableData = ref<ApiData[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/Apis');

    // 处理响应数据
    if (Array.isArray(response)) {
      tableData.value = response.map((item, index) => ({
        id: index + 1,
        name: item.name || `API-${index + 1}`,
        url: item.url || item.path || '',
        method: item.method || 'GET',
        description: item.description || item.summary || '',
        createTime: new Date().toLocaleString(),
      }));
      total.value = response.length;
    } else if (response && typeof response === 'object') {
      // 如果返回的是对象，尝试解析为API列表
      const apiList: ApiData[] = [];
      Object.keys(response).forEach((key, index) => {
        apiList.push({
          id: index + 1,
          name: key,
          url: key,
          method: 'GET',
          description: `${key} API`,
          createTime: new Date().toLocaleString(),
        });
      });
      tableData.value = apiList;
      total.value = apiList.length;
    } else {
      tableData.value = [];
      total.value = 0;
    }
  } catch {
    tableData.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
};

// 获取API列表
const handleGetApis = () => {
  loadData();
};

// 页码变更处理
const handleCurrentChange = (page: number) => {
  currentPage.value = page;
  loadData();
};

// 每页显示条数变更处理
const handleSizeChange = (size: number) => {
  pageSize.value = size;
  currentPage.value = 1;
  loadData();
};

// 初始化加载数据
onMounted(() => {
  loadData();
});
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
