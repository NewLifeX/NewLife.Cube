<template>
  <div>
    <div class="page-header">
      <h1>{{ t('oauthLog.title') }}</h1>
      <p>{{ t('oauthLog.subtitle') }}</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>{{ t('oauthLog.listTitle') }}</span>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <!-- 数据表格 -->
      <div class="table-container">
        <el-table
          :data="tableData"
          v-loading="loading.list"
          style="width: 100%"
          @selection-change="handleSelectionChange"
        >
          <el-table-column type="selection" width="55" />
          <el-table-column prop="id" :label="t('common.id')" width="80" />
          <el-table-column prop="provider" :label="t('oauthLog.provider')" width="120" />
          <el-table-column prop="userId" :label="t('desc.userID')" width="100" />
          <el-table-column prop="userName" :label="t('user.userName')" />
          <el-table-column prop="action" :label="t('auditLog.action')" />
          <el-table-column prop="success" :label="t('common.status')" width="80">
            <template #default="{ row }">
              <el-tag :type="row.success ? 'success' : 'danger'">
                {{ row.success ? t('oauthLog.success') : t('oauthLog.failed') }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="redirectUri" :label="t('oauthLog.redirectUri')" show-overflow-tooltip />
          <el-table-column prop="source" :label="t('oauthLog.source')" />
          <el-table-column prop="createIP" :label="t('desc.createIP')" />
          <el-table-column prop="createTime" :label="t('desc.createTime')" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column prop="remark" :label="t('common.remark')" show-overflow-tooltip />
          <el-table-column :label="t('common.actions')" width="120" fixed="right">
            <template #default="{ row }">
              <el-button size="small" @click="handleDetail(row)">
                {{ t('desc.detail') }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

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
    <el-dialog v-model="showDetailDialog" :title="t('oauthLog.detailTitle')" width="800px">
      <div v-loading="loading.detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item :label="t('common.id')">{{ detailData.id }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.provider')">{{ detailData.provider }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.connectId')">{{ detailData.connectId }}</el-descriptions-item>
          <el-descriptions-item :label="t('desc.userID')">{{ detailData.userId }}</el-descriptions-item>
          <el-descriptions-item :label="t('user.userName')">{{ detailData.userName }}</el-descriptions-item>
          <el-descriptions-item :label="t('auditLog.action')">{{ detailData.action }}</el-descriptions-item>
          <el-descriptions-item :label="t('common.status')">
            <el-tag :type="detailData.success ? 'success' : 'danger'">
              {{ detailData.success ? t('oauthLog.success') : t('oauthLog.failed') }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.redirectUri')" :span="2">{{ detailData.redirectUri }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.responseType')">{{ detailData.responseType }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.scope')">{{ detailData.scope }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.state')" :span="2">{{ detailData.state }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.source')">{{ detailData.source }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.accessToken')" :span="2">{{ detailData.accessToken }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.refreshToken')" :span="2">{{ detailData.refreshToken }}</el-descriptions-item>
          <el-descriptions-item :label="t('oauthLog.traceId')" :span="2">{{ detailData.traceId }}</el-descriptions-item>
          <el-descriptions-item :label="t('desc.createIP')">{{ detailData.createIP }}</el-descriptions-item>
          <el-descriptions-item :label="t('desc.createTime')">{{ formatDate(detailData.createTime) }}</el-descriptions-item>
          <el-descriptions-item :label="t('common.updateTime')">{{ formatDate(detailData.updateTime) }}</el-descriptions-item>
          <el-descriptions-item :label="t('auditLog.detail')" :span="2">
            <div style="max-height: 200px; overflow-y: auto;">
              {{ detailData.remark }}
            </div>
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <template #footer>
        <el-button @click="showDetailDialog = false">{{ t('button.close') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { request } from '@newlifex/cube-vue/core/utils/request'
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue'
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue'
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// OAuth日志接口，继承 BaseEntity
interface OAuthLog extends BaseEntity {
  provider: string
  connectId: number
  userId: number
  userName: string
  action: string
  success: boolean
  redirectUri: string
  responseType: string
  scope: string
  state: string
  source: string
  accessToken: string
  refreshToken: string
  traceId: string
  remark: string
  createIP: string
  createTime: string
  updateTime: string
}

// 表格数据
const tableData = ref<OAuthLog[]>([])
const selectedRows = ref<OAuthLog[]>([])
const { t } = useI18n()

// 详情数据
const detailData = ref<Partial<OAuthLog>>({})
const showDetailDialog = ref(false)

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 加载状态
const loading = reactive({
  list: false,
  detail: false
})

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  getOAuthLogList();
};

// 获取OAuth日志列表
const getOAuthLogList = async () => {
  try {
    loading.list = true
    const data = await request.get('/Admin/OAuthLog', {
      params: queryParams
    })
    // 处理不同的响应格式
    if (data && typeof data === 'object' && 'list' in data && 'total' in data) {
      tableData.value = Array.isArray(data.list) ? data.list : [];
      queryParams.total = Number(data.total) || 0;
    } else if (data && typeof data === 'object' && 'data' in data && 'page' in data) {
      // 使用processListResponse格式
      tableData.value = Array.isArray(data.data) ? data.data : [];
      queryParams.total = Number((data as { page?: { totalCount?: number } })?.page?.totalCount) || 0;
    } else if (Array.isArray(data)) {
      tableData.value = data;
      queryParams.total = data.length;
    } else {
      tableData.value = [];
      queryParams.total = 0;
    }
  } catch {
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.list = false
  }
}

// 获取OAuth日志详情
const getOAuthLogDetail = async (id: number) => {
  try {
    loading.detail = true

    // 详情端点：GET {type}/{id}（主键在路径中）
    const data = await request.get<OAuthLog>(`/Admin/OAuthLog/${id}`)

    if (data) {
      detailData.value = (data.data as OAuthLog) || {}
    }
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.detail = false
  }
}

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
const handleDetail = async (row: OAuthLog) => {
  await getOAuthLogDetail(row.id)
  showDetailDialog.value = true
}

// 表格选择变化
const handleSelectionChange = (selection: OAuthLog[]) => {
  selectedRows.value = selection
}

// 格式化日期
const formatDate = (dateStr?: string) => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
}

// 组件挂载时获取数据
onMounted(() => {
  getOAuthLogList();
});
</script>

<style scoped>
.page-header {
  margin-bottom: 20px;
}

.page-header h1 {
  margin: 0 0 8px 0;
  font-size: 24px;
  font-weight: 500;
}

.page-header p {
  margin: 0;
  color: #666;
  font-size: 14px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
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
