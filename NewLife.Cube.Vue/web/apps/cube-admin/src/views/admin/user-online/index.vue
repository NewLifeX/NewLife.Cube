<template>
  <div class="user-online-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('onlineUser.title') }}</h3>
        </div>
      </template>

      <CubeListToolbarSearch :on-search="SearchData" :on-reset="ResetData" :on-callback="callback" />

      <el-table :data="tableData" style="width: 100%" v-loading="loading" border>
          <el-table-column type="selection" width="55" />
          <el-table-column prop="id" :label="t('common.id')" width="80" />
          <el-table-column prop="userID" :label="t('desc.userID')" width="100" />
          <el-table-column prop="name" :label="t('user.userName')" />
          <el-table-column prop="sessionID" :label="t('desc.sessionID')" show-overflow-tooltip />
          <el-table-column prop="oAuthProvider" :label="t('onlineUser.provider')" />
          <el-table-column prop="times" :label="t('onlineUser.times')" width="80" />
          <el-table-column prop="page" :label="t('onlineUser.page')" show-overflow-tooltip />
          <el-table-column prop="platform" :label="t('onlineUser.platform')" />
          <el-table-column prop="device" :label="t('onlineUser.device')" show-overflow-tooltip />
          <el-table-column prop="brower" :label="t('onlineUser.browser')" show-overflow-tooltip />
          <el-table-column prop="status" :label="t('common.status')" />
          <el-table-column prop="onlineTime" :label="t('onlineUser.onlineTimeSeconds')" width="120" />
          <el-table-column prop="address" :label="t('onlineUser.address')" />
          <el-table-column prop="createIP" :label="t('desc.createIP')" />
          <el-table-column prop="createTime" :label="t('desc.createTime')" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column :label="t('common.remark')" min-width="120" show-overflow-tooltip>
            <template #default="scope">
              {{ scope.row.remark || '-' }}
            </template>
          </el-table-column>
          <el-table-column :label="t('common.actions')" width="120" fixed="right">
            <template #default="{ row }">
              <el-button size="small" @click="handleDetail(row)">
                {{ t('desc.detail') }}
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">
                {{ t('button.delete') }}
              </el-button>
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

    <!-- 详情对话框 -->
    <el-dialog v-model="showDetailDialog" :title="t('onlineUser.detailTitle')" width="600px">
      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('common.id')">{{ detailData.id }}</el-descriptions-item>
        <el-descriptions-item :label="t('desc.userID')">{{ detailData.userID }}</el-descriptions-item>
        <el-descriptions-item :label="t('user.userName')">{{ detailData.name }}</el-descriptions-item>
        <el-descriptions-item :label="t('desc.sessionID')">{{ detailData.sessionID }}</el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.provider')">{{ detailData.oAuthProvider }}</el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.times')">{{ detailData.times }}</el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.page')">{{ detailData.page }}</el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.platform')">{{ detailData.platform }}</el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.os')">{{ detailData.oS }}</el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.device')">{{ detailData.device }}</el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.browser')">{{ detailData.brower }}</el-descriptions-item>
        <el-descriptions-item :label="t('common.status')">{{ detailData.status }}</el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.onlineTime')">
          {{ detailData.onlineTime }}{{ t('onlineUser.seconds') }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('onlineUser.address')">{{ detailData.address }}</el-descriptions-item>
        <el-descriptions-item :label="t('desc.createIP')">{{ detailData.createIP }}</el-descriptions-item>
        <el-descriptions-item :label="t('desc.createTime')">{{ formatDate(detailData.createTime) }}</el-descriptions-item>
        <el-descriptions-item :label="t('common.remark')">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>

    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { request } from '@newlifex/cube-vue/core/utils/request'
import { apiDataToList, handleDeleteOperation } from '@newlifex/cube-vue/core/utils/api-helpers'
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue'
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue'
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 在线用户接口，继承 BaseEntity
interface UserOnline extends BaseEntity {
  userID: number
  name: string
  sessionID: string
  oAuthProvider: string
  times: number
  page: string
  platform: string
  oS: string
  device: string
  brower: string
  netType: string
  deviceId: string
  status: string
  onlineTime: number
  lastError: string
  address: string
  traceId: string
  createIP: string
  createTime: string
  updateIP: string
  updateTime: string
  remark?: string
}

// 表格数据
const tableData = ref<UserOnline[]>([])
const loading = ref(false)
const { t } = useI18n()

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 详情数据
const detailData = ref<Partial<UserOnline>>({})
const showDetailDialog = ref(false)

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadUserOnlineData();
};

// 加载在线用户数据
const loadUserOnlineData = async () => {
  loading.value = true
  try {
    const data = await request.get('/Admin/UserOnline', {
      params: queryParams
    })
    const { list, page } = apiDataToList<UserOnline>(data)
    tableData.value = list
    queryParams.total = page?.totalCount || list.length
  } catch {
    tableData.value = []
    queryParams.total = 0
  } finally {
    loading.value = false
  }
}

// 页码变更处理
const CurrentPageChange = (page: number) => {
  queryParams.pageIndex = page;
};

// 每页显示条数变更处理
const PageSizeChange = (size: number) => {
  queryParams.pageSize = size;
  queryParams.pageIndex = 1;
};

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 获取在线用户详情
const getUserOnlineDetail = async (id: number) => {
  try {
    const response = await request.get(`/Admin/UserOnline/Detail`, {
      params: { id }
    })

    console.log('Detail API response:', response)

    // 使用 processListResponse 的逻辑来处理响应数据
    let detailResult: UserOnline | null = null

    if (response && typeof response === 'object') {
      // 如果响应有 data 字段，优先使用 data
      if ('data' in response && response.data) {
        detailResult = response.data as UserOnline
      }
      // 如果响应本身就是数据对象（包含 id 字段）
      else if ('id' in response) {
        detailResult = response as unknown as UserOnline
      }
    }

    detailData.value = detailResult || {}
    console.log('Detail data applied:', detailData.value)
  } catch (error) {
    console.error('Failed to load user details:', error)
    detailData.value = {}
  }
}
// 查看详情
const handleDetail = async (row: UserOnline) => {
  await getUserOnlineDetail(row.id)
  showDetailDialog.value = true
}

// 删除在线用户
const handleDelete = (row: UserOnline) => {
  handleDeleteOperation(
    () => request.delete(`/Admin/UserOnline/${row.id}`),
    loadUserOnlineData,
    t('onlineUser.deleteConfirm', { name: row.name }),
  )
}

// 格式化日期
const formatDate = (dateStr?: string) => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
}

// 组件挂载时获取数据
onMounted(() => {
  loadUserOnlineData();
});
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.cube-search-form {
  margin-bottom: 20px;
  padding: 20px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.table-container {
  margin-bottom: 20px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
}
</style>
