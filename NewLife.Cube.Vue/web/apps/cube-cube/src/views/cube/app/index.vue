<template>
  <div class="app-container">
    <!-- 搜索栏卡片 -->
    <el-card class="box-card">
      <div class="card-header">
        <h3>{{ t('appAdmin.title') }}</h3>
        <el-button type="primary" @click="handleAdd">{{ t('appAdmin.add') }}</el-button>
      </div>
      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />
    </el-card>

    <!-- 表格卡片 -->
    <el-card class="box-card">
      <el-alert
        v-if="(queryParams.total ?? 0) > 0"
        :title="t('appAdmin.foundRecords', { n: queryParams.total ?? 0 })"
        type="info"
        show-icon
        :closable="false"
        style="margin-bottom: 16px;"
      />

      <el-table
        :data="tableData"
        style="width: 100%"
        v-loading="loading"
        stripe
      >
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="name" :label="t('appAdmin.name')" />
        <el-table-column prop="displayName" :label="t('cronJob.displayName')" />
        <el-table-column prop="category" :label="t('appAdmin.category')" />
        <el-table-column prop="version" :label="t('appAdmin.version')" />
        <el-table-column prop="updateTime" :label="t('common.updateTime')" />
        <el-table-column :label="t('appAdmin.autoStart')" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.autoStart ? 'success' : 'info'">
              <span :class="['status-dot', scope.row.autoStart ? 'dot-success' : 'dot-default']"></span>
              {{ scope.row.autoStart ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('userSearch.status')" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'info'">
              <span :class="['status-dot', scope.row.enable ? 'dot-success' : 'dot-default']"></span>
              {{ scope.row.enable ? t('common.enable') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('common.remark')" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column :label="t('common.actions')" width="150">
          <template #default="scope">
            <el-button type="primary" link @click="handleEdit(scope.row)">
              {{ t('button.edit') }}
            </el-button>
            <el-button type="danger" link @click="handleDelete(scope.row)">
              {{ t('button.delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <CubeListPager
        :total="queryParams.total ?? 0"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </el-card>

    <!-- 应用表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? t('appAdmin.add') : t('appAdmin.edit')"
      width="500px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item :label="t('appAdmin.appName')" prop="name">
          <el-input v-model="form.name" :placeholder="t('appAdmin.appNameHint')" />
        </el-form-item>
        <el-form-item :label="t('cronJob.displayName')" prop="displayName">
          <el-input v-model="form.displayName" :placeholder="t('cronJob.displayNameHint')" />
        </el-form-item>
        <el-form-item :label="t('appAdmin.category')" prop="category">
          <el-input v-model="form.category" :placeholder="t('appAdmin.categoryHint')" />
        </el-form-item>
        <el-form-item :label="t('appAdmin.version')" prop="version">
          <el-input v-model="form.version" :placeholder="t('appAdmin.versionHint')" />
        </el-form-item>
        <el-form-item :label="t('appAdmin.fileName')" prop="fileName">
          <el-input v-model="form.fileName" :placeholder="t('appAdmin.fileNameHint')" />
        </el-form-item>
        <el-form-item :label="t('appAdmin.arguments')" prop="arguments">
          <el-input v-model="form.arguments" :placeholder="t('appAdmin.argumentsHint')" />
        </el-form-item>
        <el-form-item :label="t('appAdmin.workingDirectory')" prop="workingDirectory">
          <el-input
            v-model="form.workingDirectory"
            :placeholder="t('appAdmin.workingDirectoryHint')"
          />
        </el-form-item>
        <el-form-item :label="t('ssoToken.username')" prop="userName">
          <el-input v-model="form.userName" :placeholder="t('ssoToken.usernameHint')" />
        </el-form-item>
        <el-form-item :label="t('appAdmin.autoStart')" prop="autoStart">
          <el-switch v-model="form.autoStart" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('areaAdmin.enableStatus')" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('lookup.description')" prop="description">
          <el-input
            v-model="form.description"
            type="textarea"
            :placeholder="t('cronJob.descriptionHint')"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('button.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm">{{ t('button.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 应用类型接口，继承 BaseEntity
interface App extends BaseEntity {
  name: string;
  displayName: string;
  category: string;
  version: string;
  fileName: string;
  arguments: string;
  workingDirectory: string;
  userName: string;
  autoStart: boolean;
  enable: boolean;
  description: string;
}

// 响应式数据
const loading = ref(false);
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const tableData = ref<App[]>([]);
const { t } = useI18n();

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 应用表单
const form = reactive<Partial<App>>({
  name: '',
  displayName: '',
  category: '',
  version: '',
  fileName: '',
  arguments: '',
  workingDirectory: '',
  userName: '',
  autoStart: false,
  enable: true,
  description: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: t('appAdmin.appNameRequired'), trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: t('cronJob.displayNameRequired'), trigger: 'blur' }
  ]
});

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  fetchData();
};

// 加载数据
const fetchData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/App', { params: queryParams });
    const { list, page } = apiDataToList<App>(response);
    tableData.value = list;
    queryParams.total = page?.totalCount || list.length;
  } catch (error) {
    ElMessage.error(t('common.loadFailed'));
    console.error('加载数据失败:', error);
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

// 新增
const handleAdd = () => {
  formType.value = 'add';
  resetForm();
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: App) => {
  formType.value = 'edit';
  Object.assign(form, row);
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: App) => {
  ElMessageBox.confirm(
    t('appAdmin.deleteConfirm', { name: row.displayName || row.name }),
    t('fileManager.confirmDelete'),
    {
      confirmButtonText: t('button.confirm'),
      cancelButtonText: t('button.cancel'),
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/App/${row.id}`);
      ElMessage.success(t('common.deleteSuccess'));
      fetchData();
    } catch (error) {
      ElMessage.error(t('common.deleteFailed'));
      console.error('删除失败:', error);
    }
  });
};

// 重置表单
const resetForm = () => {
  Object.assign(form, {
    id: undefined,
    name: '',
    displayName: '',
    category: '',
    version: '',
    fileName: '',
    arguments: '',
    workingDirectory: '',
    userName: '',
    autoStart: false,
    enable: true,
    description: '',
  });
  formRef.value?.clearValidate();
};

// 提交表单
const submitForm = () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        if (formType.value === 'add') {
          await request.post('/Cube/App', form);
          ElMessage.success(t('common.addSuccess'));
        } else {
          await request.put(`/Cube/App/${form.id}`, form);
          ElMessage.success(t('common.updateSuccess'));
        }
        dialogVisible.value = false;
        fetchData();
      } catch (error) {
        ElMessage.error(
          formType.value === 'add' ? t('common.addFailed') : t('common.updateFailed')
        );
        console.error('操作失败:', error);
      }
    }
  });
};

// 初始化
onMounted(() => {
  fetchData();
});
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.card-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #1d2129;
}

.box-card + .box-card {
  margin-top: 16px;
}

/* 状态圆点 */
.status-dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  margin-right: 6px;
  vertical-align: middle;
}
.dot-success {
  background-color: #16a34a;
}
.dot-default {
  background-color: #999;
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
