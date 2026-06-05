<template>
  <div class="cube-info-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>Cube信息</h3>
          <el-button type="primary" @click="handleGetInfo">获取信息</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="状态">
          <el-input v-model="searchForm.state" placeholder="请输入状态" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="key" label="键" />
        <el-table-column prop="value" label="值" />
        <el-table-column prop="description" label="描述" />
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
import { ref, reactive, onMounted } from 'vue';
import { request } from 'cube-front/core/utils/request';

// 定义接口类型
interface CubeInfo {
  state?: string;
}

// 定义数据类型
interface CubeInfoData {
  key: string;
  value: string;
  description: string;
}

// 表格数据
const tableData = ref<CubeInfoData[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);

// 查询表单
const searchForm = reactive<CubeInfo>({
  state: '',
});

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/Info', {
      params: {
        state: searchForm.state,
      },
    });

    // 处理响应数据
    if (response && typeof response === 'object') {
      const dataArray: CubeInfoData[] = [];
      Object.keys(response).forEach((key) => {
        const value = response[key as keyof typeof response];
        dataArray.push({
          key,
          value: String(value),
          description: `${key}的值`,
        });
      });
      tableData.value = dataArray;
      total.value = dataArray.length;
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

// 获取信息
const handleGetInfo = () => {
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

// 搜索
const handleSearch = () => {
  currentPage.value = 1;
  loadData();
};

// 重置搜索
const resetSearch = () => {
  searchForm.state = '';
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

.search-form {
  margin-bottom: 20px;
}
.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
