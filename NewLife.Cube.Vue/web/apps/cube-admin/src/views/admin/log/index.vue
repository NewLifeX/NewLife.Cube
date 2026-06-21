<template>
  <div class="log-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>审计日志</h3>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="category" label="类别" width="100" />
        <el-table-column prop="action" label="操作" width="120" />
        <el-table-column prop="linkID" label="链接" width="80" />
        <el-table-column prop="createUser" label="用户" width="100" />
        <el-table-column prop="createTime" label="时间" width="160" />
        <el-table-column prop="createIP" label="IP地址" width="120" />
        <el-table-column prop="remark" label="详细信息" show-overflow-tooltip />
        <el-table-column label="操作" width="120">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleView(scope.row)">查看</el-button>
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

    <!-- 日志详情对话框 -->
    <el-dialog v-model="detailVisible" title="日志详情" width="800px">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="编号">{{ currentLog.id }}</el-descriptions-item>
        <el-descriptions-item label="类别">{{ currentLog.category }}</el-descriptions-item>
        <el-descriptions-item label="操作">{{ currentLog.action }}</el-descriptions-item>
        <el-descriptions-item label="链接">{{ currentLog.linkID }}</el-descriptions-item>
        <el-descriptions-item label="用户">{{ currentLog.createUser }}</el-descriptions-item>
        <el-descriptions-item label="用户ID">{{ currentLog.createUserID }}</el-descriptions-item>
        <el-descriptions-item label="时间">{{ currentLog.createTime }}</el-descriptions-item>
        <el-descriptions-item label="IP地址">{{ currentLog.createIP }}</el-descriptions-item>
        <el-descriptions-item label="详细信息" :span="2">
          <div style="max-height: 300px; overflow-y: auto; white-space: pre-wrap;">{{ currentLog.remark }}</div>
        </el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { request } from 'cube-front/core/utils/request';
import CubeListToolbarSearch from 'cube-front/core/components/CubeListToolbarSearch.vue';
import CubeListPager from 'cube-front/core/components/CubeListPager.vue';
import { pageInfoDefault } from 'cube-front/core/types/common';
import type { BaseEntity } from 'cube-front/core/types/common';

// 定义日志类型接口，继承 BaseEntity
interface Log extends BaseEntity {
  category: string;
  action: string;
  linkID: number;
  success: boolean;
  createUser: string;
  createUserID: number;
  createTime: string;
  createIP: string;
  remark: string;
}

// 表格数据
const tableData = ref<Log[]>([]);
const loading = ref(false);

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 详情对话框
const detailVisible = ref(false);
const currentLog = reactive<Log>({
  id: 0,
  category: '',
  action: '',
  linkID: 0,
  success: false,
  createUser: '',
  createUserID: 0,
  createTime: '',
  createIP: '',
  remark: '',
});

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadData();
};

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Admin/Log', { params: queryParams });
    // 处理不同的响应格式
    if (response && typeof response === 'object' && 'list' in response && 'total' in response) {
      tableData.value = Array.isArray(response.list) ? response.list : [];
      queryParams.total = typeof response.total === 'number' ? response.total : 0;
    } else if (response && response.data && Array.isArray(response.data.data)) {
      tableData.value = response.data.data;
      // 处理分页信息
      if (response.data.page) {
        const pageInfo = response.data.page;
        const count = pageInfo.longTotalCount || pageInfo.totalCount;
        queryParams.total = count ? Number(count) : 0;
        queryParams.pageIndex = pageInfo.pageIndex || 1;
      } else {
        queryParams.total = response.data.data.length || 0;
      }
    } else if (response && Array.isArray(response.data)) {
      tableData.value = response.data;
      queryParams.total = response.data.length;
    } else {
      tableData.value = [];
      queryParams.total = 0;
    }
  } catch (error) {
    ElMessage.error('加载数据失败');
    console.error('加载数据失败:', error);
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

// 查看详情
const handleView = (row: Log) => {
  Object.assign(currentLog, row);
  detailVisible.value = true;
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
