<template>
  <div class="user-online-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>在线用户管理</h3>
        </div>
      </template>

      <CubeListToolbarSearch :on-search="SearchData" :on-reset="ResetData" :on-callback="callback" />

      <el-table :data="tableData" style="width: 100%" v-loading="loading" border>
          <el-table-column type="selection" width="55" />
          <el-table-column prop="id" label="编号" width="80" />
          <el-table-column prop="userID" label="用户ID" width="100" />
          <el-table-column prop="name" label="用户名" />
          <el-table-column prop="sessionID" label="会话ID" show-overflow-tooltip />
          <el-table-column prop="oAuthProvider" label="登录方" />
          <el-table-column prop="times" label="次数" width="80" />
          <el-table-column prop="page" label="页面" show-overflow-tooltip />
          <el-table-column prop="platform" label="平台" />
          <el-table-column prop="device" label="设备" show-overflow-tooltip />
          <el-table-column prop="brower" label="浏览器" show-overflow-tooltip />
          <el-table-column prop="status" label="状态" />
          <el-table-column prop="onlineTime" label="在线时间(秒)" width="120" />
          <el-table-column prop="address" label="地址" />
          <el-table-column prop="createIP" label="创建地址" />
          <el-table-column prop="createTime" label="创建时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column label="备注" min-width="120" show-overflow-tooltip>
            <template #default="scope">
              {{ scope.row.remark || '-' }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120" fixed="right">
            <template #default="{ row }">
              <el-button size="small" @click="handleDetail(row)">
                详情
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">
                删除
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
    <el-dialog v-model="showDetailDialog" title="在线用户详情" width="600px">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="编号">{{ detailData.id }}</el-descriptions-item>
        <el-descriptions-item label="用户ID">{{ detailData.userID }}</el-descriptions-item>
        <el-descriptions-item label="用户名">{{ detailData.name }}</el-descriptions-item>
        <el-descriptions-item label="会话ID">{{ detailData.sessionID }}</el-descriptions-item>
        <el-descriptions-item label="登录方">{{ detailData.oAuthProvider }}</el-descriptions-item>
        <el-descriptions-item label="次数">{{ detailData.times }}</el-descriptions-item>
        <el-descriptions-item label="页面">{{ detailData.page }}</el-descriptions-item>
        <el-descriptions-item label="平台">{{ detailData.platform }}</el-descriptions-item>
        <el-descriptions-item label="操作系统">{{ detailData.oS }}</el-descriptions-item>
        <el-descriptions-item label="设备">{{ detailData.device }}</el-descriptions-item>
        <el-descriptions-item label="浏览器">{{ detailData.brower }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ detailData.status }}</el-descriptions-item>
        <el-descriptions-item label="在线时间">{{ detailData.onlineTime }}秒</el-descriptions-item>
        <el-descriptions-item label="地址">{{ detailData.address }}</el-descriptions-item>
        <el-descriptions-item label="创建地址">{{ detailData.createIP }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatDate(detailData.createTime) }}</el-descriptions-item>
        <el-descriptions-item label="备注">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>

    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue'
import { request } from 'cube-front/core/utils/request'
import { apiDataToList, handleDeleteOperation } from 'cube-front/core/utils/api-helpers'
import CubeListToolbarSearch from 'cube-front/core/components/CubeListToolbarSearch.vue'
import CubeListPager from 'cube-front/core/components/CubeListPager.vue'
import { pageInfoDefault } from 'cube-front/core/types/common';
import type { BaseEntity } from 'cube-front/core/types/common';

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

    console.log('详情API返回数据:', response)

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
    console.log('设置的详情数据:', detailData.value)
  } catch (error) {
    console.error('获取用户详情失败:', error)
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
    () => request.delete('/Admin/UserOnline', { params: { id: row.id } }),
    loadUserOnlineData,
    `确认删除在线用户 "${row.name}" 吗？`
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
