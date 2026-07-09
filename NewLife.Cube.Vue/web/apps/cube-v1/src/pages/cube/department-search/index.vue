<template>
  <div class="cube-department-search-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>部门搜索</h3>
          <el-button type="primary" @click="handleSearch">搜索部门</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="上级部门ID">
          <el-input-number v-model="searchForm.parentid" :min="-1" placeholder="上级部门ID" />
        </el-form-item>
        <el-form-item label="关键字">
          <el-input v-model="searchForm.key" placeholder="请输入关键字" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="name" label="部门名称" min-width="150" />
        <el-table-column prop="fullName" label="全名" min-width="200" />
        <el-table-column prop="parentId" label="上级部门ID" width="120" />
        <el-table-column prop="sort" label="排序" width="80" />
        <el-table-column prop="manager" label="负责人" width="100" />
        <el-table-column prop="phone" label="电话" width="120" />
        <el-table-column prop="address" label="地址" min-width="150" />
        <el-table-column prop="createTime" label="创建时间" width="160" />
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
import { request } from '@newlifex/cube-vue/core/utils/request';

// 定义搜索参数类型
interface DepartmentSearchParams {
  parentid: number;
  key: string;
}

// 定义部门数据类型
interface DepartmentData {
  id: number;
  name: string;
  fullName: string;
  parentId: number;
  sort: number;
  manager: string;
  phone: string;
  address: string;
  createTime: string;
  enable: boolean;
}

// 表格数据
const tableData = ref<DepartmentData[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);

// 查询表单
const searchForm = reactive<DepartmentSearchParams>({
  parentid: -1,
  key: '',
});

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/DepartmentSearch', {
      params: {
        parentid: searchForm.parentid,
        key: searchForm.key || undefined,
      },
    });

    // 处理响应数据
    if (Array.isArray(response)) {
      tableData.value = response.map((item, index) => ({
        id: item.id || index + 1,
        name: item.name || `部门${index + 1}`,
        fullName: item.fullName || item.name || `部门${index + 1}`,
        parentId: item.parentId || item.parentID || 0,
        sort: item.sort || 0,
        manager: item.manager || '',
        phone: item.phone || '',
        address: item.address || '',
        createTime: item.createTime || new Date().toLocaleString(),
        enable: item.enable !== false,
      }));
      total.value = response.length;
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
  searchForm.parentid = -1;
  searchForm.key = '';
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
