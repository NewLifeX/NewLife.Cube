<template>
  <div class="user-connect-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>用户连接管理</h3>
        </div>
      </template>

      <CubeListToolbarSearch :on-search="SearchData" :on-reset="ResetData" :on-callback="callback" />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="provider" label="提供者" />
        <el-table-column prop="openID" label="用户标识" />
        <el-table-column prop="userName" label="用户名" />
        <el-table-column prop="nickName" label="昵称" />
        <el-table-column prop="avatar" label="头像" width="80">
          <template #default="scope">
            <el-avatar v-if="scope.row.avatar" :src="scope.row.avatar" :size="40" />
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="refreshToken" label="刷新令牌" show-overflow-tooltip />
        <el-table-column prop="expire" label="过期时间" />
        <el-table-column prop="updateTime" label="更新时间" />
        <el-table-column label="启用" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="备注" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120">
          <template #default="scope">
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <CubeListPager :total="queryParams.total" :current-page="queryParams.pageIndex" :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange" :on-size-change="PageSizeChange" :on-callback="callback" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { request } from 'cube-front/core/utils/request';
import { apiDataToList, handleDeleteOperation } from 'cube-front/core/utils/api-helpers';
import CubeListToolbarSearch from 'cube-front/core/components/CubeListToolbarSearch.vue';
import CubeListPager from 'cube-front/core/components/CubeListPager.vue';
import { pageInfoDefault } from 'cube-front/core/types/common';
import type { BaseEntity } from 'cube-front/core/types/common';

// 定义用户连接类型接口，继承 BaseEntity
interface UserConnect extends BaseEntity {
  provider: string;
  openID: string;
  userName: string;
  nickName: string;
  avatar: string;
  refreshToken: string;
  expire: string;
  enable: boolean;
  remark?: string;
}


// 表格数据
const tableData = ref<UserConnect[]>([]);
const loading = ref(false);

// 页面请求参数
const queryParams = reactive({
  q: '', // 搜索关键字
  ...pageInfoDefault,
});

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadData();
};

// 查询请求
const loadData = async () => {
  loading.value = true;
  try {
    const data = await request.get('/Admin/UserConnect', { params: queryParams });
    const { list, page } = apiDataToList<UserConnect>(data);
    tableData.value = list;
    queryParams.total = page?.totalCount;
  } catch {
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

// 删除
const handleDelete = (row: UserConnect) => {
  handleDeleteOperation(
    () => request.delete('/Admin/UserConnect', { params: { id: row.id } }),
    loadData,
    '确认删除该用户连接吗？'
  );
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
