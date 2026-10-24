<template>
  <div>
    <div class="page-header">
      <h1>{{ t('token.title') }}</h1>
      <p>{{ t('token.subtitle') }}</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>{{ t('token.listTitle') }}</span>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon>
            {{ t('token.add') }}
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
          <el-table-column prop="id" :label="t('common.id')" width="80" />
          <el-table-column prop="userId" :label="t('token.userId')" width="100" />
          <el-table-column prop="userName" :label="t('token.userName')" />
          <el-table-column prop="tokenType" :label="t('token.type')" />
          <el-table-column prop="accessToken" :label="t('token.accessToken')" show-overflow-tooltip />
          <el-table-column prop="refreshToken" :label="t('token.refreshToken')" show-overflow-tooltip />
          <el-table-column prop="enable" :label="t('rule.status')" width="80">
            <template #default="{ row }">
              <el-tag :type="row.enable ? 'success' : 'danger'">
                {{ row.enable ? t('common.enable') : t('rule.disabled') }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="expire" :label="t('token.expire')" width="160">
            <template #default="{ row }">
              {{ formatDate(row.expire) }}
            </template>
          </el-table-column>
          <el-table-column prop="createTime" :label="t('dept.createTime')" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column :label="t('common.remark')" min-width="120" show-overflow-tooltip>
            <template #default="scope">
              {{ scope.row.remark || '-' }}
            </template>
          </el-table-column>
          <el-table-column :label="t('common.actions')" width="200" fixed="right">
            <template #default="{ row }">
              <el-button size="small" @click="handleDetail(row)">
                {{ t('token.detail') }}
              </el-button>
              <el-button size="small" @click="handleEdit(row)">
                {{ t('button.edit') }}
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">
                {{ t('button.delete') }}
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
      :title="isEdit ? t('token.editTitle') : t('token.addTitle')"
      width="600px"
    >
      <el-form
        :model="formData"
        :rules="formRules"
        ref="formRef"
        label-width="120px"
        v-loading="loading"
      >
        <el-form-item :label="t('token.userId')" prop="userId">
          <el-input v-model="formData.userId" :placeholder="t('form.placeholder', { field: t('token.userId') })" />
        </el-form-item>
        <el-form-item :label="t('token.type')" prop="tokenType">
          <el-select v-model="formData.tokenType" :placeholder="t('form.select.placeholder', { field: t('token.type') })" style="width: 100%">
            <el-option
              v-for="item in tokenTypeOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('token.accessToken')" prop="accessToken">
          <el-input
            v-model="formData.accessToken"
            :placeholder="t('form.placeholder', { field: t('token.accessToken') })"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
        <el-form-item :label="t('token.refreshToken')" prop="refreshToken">
          <el-input
            v-model="formData.refreshToken"
            :placeholder="t('form.placeholder', { field: t('token.refreshToken') })"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
        <el-form-item :label="t('common.enable')" prop="enable">
          <el-switch v-model="formData.enable" />
        </el-form-item>
        <el-form-item :label="t('token.expire')" prop="expire">
          <el-date-picker
            v-model="formData.expire"
            type="datetime"
            :placeholder="t('token.expirePlaceholder')"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DD HH:mm:ss"
          />
        </el-form-item>
        <el-form-item :label="t('token.description')" prop="remark">
          <el-input
            v-model="formData.remark"
            :placeholder="t('form.placeholder', { field: t('token.description') })"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('button.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm" :loading="loading">
          {{ isEdit ? t('token.update') : t('token.create') }}
        </el-button>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailVisible" :title="t('token.detailTitle')" width="800px">
      <div v-loading="loading">
        <el-descriptions :column="2" border>
          <el-descriptions-item :label="t('common.id')">{{ detailData.id }}</el-descriptions-item>
          <el-descriptions-item :label="t('token.userId')">{{ detailData.userId }}</el-descriptions-item>
          <el-descriptions-item :label="t('token.userName')">{{ detailData.userName }}</el-descriptions-item>
          <el-descriptions-item :label="t('token.type')">{{ detailData.tokenType }}</el-descriptions-item>
          <el-descriptions-item :label="t('token.accessToken')" :span="2">
            <div style="word-break: break-all;">{{ detailData.accessToken }}</div>
          </el-descriptions-item>
          <el-descriptions-item :label="t('token.refreshToken')" :span="2">
            <div style="word-break: break-all;">{{ detailData.refreshToken }}</div>
          </el-descriptions-item>
          <el-descriptions-item :label="t('common.enable')">
            <el-tag :type="detailData.enable ? 'success' : 'danger'">
              {{ detailData.enable ? t('common.enable') : t('rule.disabled') }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item :label="t('token.expire')">{{ formatDate(detailData.expire) }}</el-descriptions-item>
          <el-descriptions-item :label="t('dept.createTime')">{{ formatDate(detailData.createTime) }}</el-descriptions-item>
          <el-descriptions-item :label="t('common.updateTime')">{{ formatDate(detailData.updateTime) }}</el-descriptions-item>
          <el-descriptions-item :label="t('token.createIP')">{{ detailData.createIP }}</el-descriptions-item>
          <el-descriptions-item :label="t('token.updateIP')">{{ detailData.updateIP }}</el-descriptions-item>
          <el-descriptions-item :label="t('token.description')" :span="2">{{ detailData.remark }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <template #footer>
        <el-button @click="detailVisible = false">{{ t('token.close') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { Plus } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList, handleDeleteOperation, handleFormSubmit } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

const { t } = useI18n();

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
    { required: true, message: t('form.placeholder', { field: t('token.userId') }), trigger: 'blur' }
  ],
  tokenType: [
    { required: true, message: t('form.select.placeholder', { field: t('token.type') }), trigger: 'change' }
  ],
  accessToken: [
    { required: true, message: t('form.placeholder', { field: t('token.accessToken') }), trigger: 'blur' }
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
    ElMessage.error(t('token.detailFailed'));
    console.error(t('token.detailLoadFailed'), error);
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
    () => request.delete(`/Admin/UserToken/${row.id}`),
    loadUserTokenData,
    t('token.deleteConfirm')
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (isEdit.value) {
      await request.put(`/Admin/UserToken/${formData.id}`, formData);
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
