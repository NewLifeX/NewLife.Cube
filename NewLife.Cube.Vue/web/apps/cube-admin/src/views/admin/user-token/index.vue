<template>
  <div>
    <div class="page-header">
      <h1>用户令牌管理</h1>
      <p>管理用户访问令牌和会话信息</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>用户令牌列表</span>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon>
            添加令牌
          </el-button>
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
          v-loading="loading"
          style="width: 100%"
        >
          <el-table-column type="selection" width="55" />
          <el-table-column prop="id" label="编号" width="80" />
          <el-table-column prop="userId" label="用户ID" width="100" />
          <el-table-column prop="userName" label="用户名" />
          <el-table-column prop="tokenType" label="令牌类型" />
          <el-table-column prop="accessToken" label="访问令牌" show-overflow-tooltip />
          <el-table-column prop="refreshToken" label="刷新令牌" show-overflow-tooltip />
          <el-table-column prop="enable" label="状态" width="80">
            <template #default="{ row }">
              <el-tag :type="row.enable ? 'success' : 'danger'">
                {{ row.enable ? '启用' : '禁用' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="expire" label="过期时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.expire) }}
            </template>
          </el-table-column>
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
          <el-table-column label="操作" width="200" fixed="right">
            <template #default="{ row }">
              <el-button size="small" @click="handleDetail(row)">
                详情
              </el-button>
              <el-button size="small" @click="handleEdit(row)">
                编辑
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 分页 -->
      <CubeListPager
        :total="queryParams.total"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </el-card>

    <!-- 添加/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑用户令牌' : '添加用户令牌'"
      width="600px"
    >
      <el-form
        :model="formData"
        :rules="formRules"
        ref="formRef"
        label-width="120px"
        v-loading="loading"
      >
        <el-form-item label="用户ID" prop="userId">
          <el-input v-model="formData.userId" placeholder="请输入用户ID" />
        </el-form-item>
        <el-form-item label="令牌类型" prop="tokenType">
          <el-select v-model="formData.tokenType" placeholder="请选择令牌类型" style="width: 100%">
            <el-option
              v-for="item in tokenTypeOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="访问令牌" prop="accessToken">
          <el-input
            v-model="formData.accessToken"
            placeholder="请输入访问令牌"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
        <el-form-item label="刷新令牌" prop="refreshToken">
          <el-input
            v-model="formData.refreshToken"
            placeholder="请输入刷新令牌"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="formData.enable" />
        </el-form-item>
        <el-form-item label="过期时间" prop="expire">
          <el-date-picker
            v-model="formData.expire"
            type="datetime"
            placeholder="选择过期时间"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DD HH:mm:ss"
          />
        </el-form-item>
        <el-form-item label="描述" prop="remark">
          <el-input
            v-model="formData.remark"
            placeholder="请输入描述"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitForm" :loading="loading">
          {{ isEdit ? '更新' : '创建' }}
        </el-button>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailVisible" title="用户令牌详情" width="800px">
      <div v-loading="loading">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="编号">{{ detailData.id }}</el-descriptions-item>
          <el-descriptions-item label="用户ID">{{ detailData.userId }}</el-descriptions-item>
          <el-descriptions-item label="用户名">{{ detailData.userName }}</el-descriptions-item>
          <el-descriptions-item label="令牌类型">{{ detailData.tokenType }}</el-descriptions-item>
          <el-descriptions-item label="访问令牌" :span="2">
            <div style="word-break: break-all;">{{ detailData.accessToken }}</div>
          </el-descriptions-item>
          <el-descriptions-item label="刷新令牌" :span="2">
            <div style="word-break: break-all;">{{ detailData.refreshToken }}</div>
          </el-descriptions-item>
          <el-descriptions-item label="启用状态">
            <el-tag :type="detailData.enable ? 'success' : 'danger'">
              {{ detailData.enable ? '启用' : '禁用' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="过期时间">{{ formatDate(detailData.expire) }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ formatDate(detailData.createTime) }}</el-descriptions-item>
          <el-descriptions-item label="更新时间">{{ formatDate(detailData.updateTime) }}</el-descriptions-item>
          <el-descriptions-item label="创建地址">{{ detailData.createIP }}</el-descriptions-item>
          <el-descriptions-item label="更新地址">{{ detailData.updateIP }}</el-descriptions-item>
          <el-descriptions-item label="描述" :span="2">{{ detailData.remark }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'
import { request } from 'cube-front/core/utils/request';
import { apiDataToList, handleDeleteOperation, handleFormSubmit } from 'cube-front/core/utils/api-helpers';
import CubeListToolbarSearch from 'cube-front/core/components/CubeListToolbarSearch.vue';
import CubeListPager from 'cube-front/core/components/CubeListPager.vue';
import { pageInfoDefault } from 'cube-front/core/types/common';
import type { BaseEntity } from 'cube-front/core/types/common';

// 用户令牌类型接口，继承 BaseEntity
interface UserToken extends BaseEntity {
  userId: number;
  userName: string;
  tokenType: string;
  accessToken: string;
  refreshToken: string;
  enable: boolean;
  expire: string;
  createIP: string;
  updateIP: string;
  remark: string;
}

// 初始令牌表单数据
const initialTokenForm: Partial<UserToken> = {
  id: 0,
  userId: 0,
  tokenType: '',
  accessToken: '',
  refreshToken: '',
  enable: true,
  remark: ''
};

// 表格数据
const tableData = ref<UserToken[]>([]);
const loading = ref(false);

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 详情相关
const detailVisible = ref(false);
const detailData = ref<UserToken>({} as UserToken);

// 编辑表单相关
const dialogVisible = ref(false);
const formData = reactive<Partial<UserToken>>({ ...initialTokenForm });
const formRef = ref<FormInstance | null>(null);
const isEdit = ref(false);

// 令牌类型选项
const tokenTypeOptions = [
  { label: 'OAuth', value: 'OAuth' },
  { label: 'JWT', value: 'JWT' },
  { label: 'Web', value: 'Web' },
  { label: 'App', value: 'App' },
]

// 表单验证规则
const formRules: FormRules = {
  userId: [
    { required: true, message: '请输入用户ID', trigger: 'blur' }
  ],
  tokenType: [
    { required: true, message: '请选择令牌类型', trigger: 'change' }
  ],
  accessToken: [
    { required: true, message: '请输入访问令牌', trigger: 'blur' }
  ]
};

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadUserTokenData();
};

// 加载用户令牌数据
const loadUserTokenData = async () => {
  loading.value = true;
  try {
    const data = await request.get('/Admin/UserToken', {
      params: queryParams
    });
    const { list, page } = apiDataToList<UserToken>(data);
    tableData.value = list;
    queryParams.total = page?.totalCount || list.length;
  } catch {
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.value = false;
  }
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

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 查看详情
const handleDetail = async (row: UserToken) => {
  try {
    const data = await request.get('/Admin/UserToken', { params: { id: row.id } });
    const { data: tokenData } = data;
    detailData.value = tokenData || row;
    detailVisible.value = true;
  } catch (error) {
    ElMessage.error('获取详情失败');
    console.error('获取令牌详情失败:', error);
  }
};

// 新增令牌
const handleAdd = () => {
  Object.assign(formData, { ...initialTokenForm });
  isEdit.value = false;
  dialogVisible.value = true;
};

// 编辑令牌
const handleEdit = (row: UserToken) => {
  Object.assign(formData, {
    id: row.id,
    userId: row.userId,
    tokenType: row.tokenType,
    accessToken: row.accessToken,
    refreshToken: row.refreshToken,
    enable: row.enable,
    remark: row.remark
  });
  isEdit.value = true;
  dialogVisible.value = true;
};

// 删除令牌
const handleDelete = (row: UserToken) => {
  handleDeleteOperation(
    () => request.delete('/Admin/UserToken', { params: { id: row.id } }),
    loadUserTokenData,
    '确认删除该令牌吗？'
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (isEdit.value) {
      await request.put('/Admin/UserToken', formData);
    } else {
      await request.post('/Admin/UserToken', formData);
    }
  };

  const onSuccess = () => {
    dialogVisible.value = false;
    loadUserTokenData();
  };

  await handleFormSubmit(formRef.value, apiCall, onSuccess);
};

// 格式化日期
const formatDate = (dateStr?: string) => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
}

onMounted(() => {
  loadUserTokenData();
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
