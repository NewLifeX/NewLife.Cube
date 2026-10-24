<template>
  <div class="user-connect-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('connect.title') }}</h3>
        </div>
      </template>

      <CubeListToolbarSearch :on-search="SearchData" :on-reset="ResetData" :on-callback="callback" />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" :label="t('common.id')" width="80" />
        <el-table-column prop="provider" :label="t('connect.provider')" />
        <el-table-column prop="openID" :label="t('connect.openID')" />
        <el-table-column prop="userName" :label="t('user.userName')" />
        <el-table-column prop="nickName" :label="t('connect.nickName')" />
        <el-table-column prop="avatar" :label="t('connect.avatar')" width="80">
          <template #default="scope">
            <el-avatar v-if="scope.row.avatar" :src="scope.row.avatar" :size="40" />
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="refreshToken" :label="t('connect.refreshToken')" show-overflow-tooltip />
        <el-table-column prop="expire" :label="t('connect.expire')" />
        <el-table-column prop="updateTime" :label="t('common.updateTime')" />
        <el-table-column :label="t('common.enable')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('common.remark')" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column :label="t('common.actions')" width="120">
          <template #default="scope">
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">{{ t('button.delete') }}</el-button>
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
import { useI18n } from 'vue-i18n';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList, handleDeleteOperation } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

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
const { t } = useI18n();

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
    () => request.delete(`/Admin/UserConnect/${row.id}`),
    loadData,
    t('connect.deleteConfirm')
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
