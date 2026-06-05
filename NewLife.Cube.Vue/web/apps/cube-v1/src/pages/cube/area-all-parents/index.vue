<template>
  <div class="cube-area-all-parents-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>获取区域所有父项</h3>
          <el-button type="primary" @click="handleGetData">获取数据</el-button>
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
        <el-table-column prop="level" label="级别" width="80" />
        <el-table-column prop="code" label="代码" width="100" />
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { request } from 'cube-front/core/utils/request';

interface SearchForm {
  id: number;
}

interface AreaData {
  id: number;
  name: string;
  fullName: string;
  level: number;
  code: string;
}

const tableData = ref<AreaData[]>([]);
const loading = ref(false);

const searchForm = reactive<SearchForm>({
  id: 0,
});

const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/AreaAllParents', {
      params: {
        id: searchForm.id || undefined,
      },
    });

    if (Array.isArray(response)) {
      tableData.value = response.map((item, index) => ({
        id: item.id || index + 1,
        name: item.name || `区域${index + 1}`,
        fullName: item.fullName || item.name || `区域${index + 1}`,
        level: item.level || 0,
        code: item.code || '',
      }));
    } else {
      tableData.value = [];
    }
  } catch {
    tableData.value = [];
  } finally {
    loading.value = false;
  }
};

const handleGetData = () => {
  loadData();
};

const handleSearch = () => {
  loadData();
};

const resetSearch = () => {
  searchForm.id = 0;
  loadData();
};

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
</style>
