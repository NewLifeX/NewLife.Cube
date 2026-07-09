<template>
  <div class="cube-area-childs-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>获取区域子项</h3>
          <el-button type="primary" @click="handleGetAreaChilds">获取子区域</el-button>
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
        <el-table-column label="状态" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { request } from '@newlifex/cube-vue/core/utils/request';

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
  enable: boolean;
}

// 表格数据
const tableData = ref<AreaData[]>([]);
const loading = ref(false);

// 查询表单
const searchForm = reactive<AreaParams>({
  id: 0,
});

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/AreaChilds', {
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
        enable: item.enable !== false,
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

// 获取区域子项
const handleGetAreaChilds = () => {
  loadData();
};

// 搜索
const handleSearch = () => {
  loadData();
};

// 重置搜索
const resetSearch = () => {
  searchForm.id = 0;
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
</style>
