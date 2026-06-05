<template>
  <div class="cube-get-area-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>获取区域</h3>
          <el-button type="primary" @click="handleGetArea">获取区域信息</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="区域ID">
          <el-input-number v-model="searchForm.id" :min="0" placeholder="区域ID" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="name" label="区域名称" min-width="150" />
        <el-table-column prop="fullName" label="全名" min-width="200" />
        <el-table-column prop="parentId" label="上级区域ID" width="120" />
        <el-table-column prop="level" label="级别" width="80" />
        <el-table-column prop="code" label="代码" width="100" />
        <el-table-column prop="pinyin" label="拼音" width="120" />
        <el-table-column prop="latitude" label="纬度" width="100" />
        <el-table-column prop="longitude" label="经度" width="100" />
        <el-table-column label="状态" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
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

// 定义搜索参数类型
interface AreaParams {
  id: number;
}

// 定义区域数据类型
interface AreaData {
  id: number;
  name: string;
  fullName: string;
  parentId: number;
  level: number;
  code: string;
  pinyin: string;
  latitude: number;
  longitude: number;
  enable: boolean;
}

// 表格数据
const tableData = ref<AreaData[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);

// 查询表单
const searchForm = reactive<AreaParams>({
  id: 0,
});

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/GetArea', {
      params: {
        id: searchForm.id || undefined,
      },
    });

    // 处理响应数据
    if (Array.isArray(response)) {
      tableData.value = response.map((item, index) => ({
        id: item.id || index + 1,
        name: item.name || `区域${index + 1}`,
        fullName: item.fullName || item.name || `区域${index + 1}`,
        parentId: item.parentId || item.parentID || 0,
        level: item.level || 0,
        code: item.code || '',
        pinyin: item.pinyin || '',
        latitude: item.latitude || 0,
        longitude: item.longitude || 0,
        enable: item.enable !== false,
      }));
      total.value = response.length;
    } else if (response && typeof response === 'object') {
      // 如果返回单个对象，转换为数组
      tableData.value = [{
        id: response.id || 1,
        name: response.name || '区域',
        fullName: response.fullName || response.name || '区域',
        parentId: response.parentId || response.parentID || 0,
        level: response.level || 0,
        code: response.code || '',
        pinyin: response.pinyin || '',
        latitude: response.latitude || 0,
        longitude: response.longitude || 0,
        enable: response.enable !== false,
      }];
      total.value = 1;
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

// 获取区域
const handleGetArea = () => {
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
  searchForm.id = 0;
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
