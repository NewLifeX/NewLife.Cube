<template>
  <div class="cube-user-search-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>用户搜索</h3>
          <el-button type="primary" @click="handleSearch">搜索用户</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="角色ID">
          <el-input-number v-model="searchForm.roleId" :min="0" placeholder="角色ID" />
        </el-form-item>
        <el-form-item label="部门ID">
          <el-input-number v-model="searchForm.departmentId" :min="0" placeholder="部门ID" />
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
        <el-table-column prop="name" label="用户名" min-width="120" />
        <el-table-column prop="displayName" label="显示名称" min-width="120" />
        <el-table-column prop="email" label="邮箱" min-width="150" />
        <el-table-column prop="mobile" label="手机号" min-width="120" />
        <el-table-column prop="roleId" label="角色ID" width="80" />
        <el-table-column prop="departmentId" label="部门ID" width="80" />
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
import { request } from 'cube-front/core/utils/request';

// 定义搜索参数类型
interface UserSearchParams {
  roleId: number;
  departmentId: number;
  key: string;
}

// 定义用户数据类型
interface UserData {
  id: number;
  name: string;
  displayName: string;
  email: string;
  mobile: string;
  roleId: number;
  departmentId: number;
  createTime: string;
  enable: boolean;
}

// 表格数据
const tableData = ref<UserData[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);

// 查询表单
const searchForm = reactive<UserSearchParams>({
  roleId: 0,
  departmentId: 0,
  key: '',
});

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/UserSearch', {
      params: {
        roleId: searchForm.roleId || undefined,
        departmentId: searchForm.departmentId || undefined,
        key: searchForm.key || undefined,
      },
    });

    // 处理响应数据
    if (Array.isArray(response)) {
      tableData.value = response.map((item, index) => ({
        id: item.id || index + 1,
        name: item.name || `用户${index + 1}`,
        displayName: item.displayName || item.name || `用户${index + 1}`,
        email: item.email || item.mail || '',
        mobile: item.mobile || item.phone || '',
        roleId: item.roleId || item.roleID || 0,
        departmentId: item.departmentId || item.departmentID || 0,
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
  searchForm.roleId = 0;
  searchForm.departmentId = 0;
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
