<template>
  <div class="cron-job-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('cronJob.title') }}</h3>
          <el-button type="primary" @click="handleAdd">{{ t('cronJob.add') }}</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table
        :data="tableData"
        border
        style="width: 100%"
        v-loading="loading"
      >
        <el-table-column prop="id" :label="t('cronJob.id')" width="80" />
        <el-table-column prop="name" :label="t('cronJob.name')" min-width="150" />
        <el-table-column prop="displayName" :label="t('cronJob.displayName')" min-width="150" />
        <el-table-column prop="cron" :label="t('cronJob.cron')" min-width="120" />
        <el-table-column prop="server" :label="t('cronJob.server')" width="120" />
        <el-table-column prop="mode" :label="t('cronJob.mode')" width="100" />
        <el-table-column prop="enable" :label="t('cronJob.enable')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="times" :label="t('cronJob.times')" width="100" />
        <el-table-column prop="nextTime" :label="t('cronJob.nextTime')" min-width="150" />
        <el-table-column prop="updateTime" :label="t('common.updateTime')" min-width="150" />
        <el-table-column prop="description" :label="t('common.remark')" min-width="120" />
        <el-table-column :label="t('common.actions')" width="200" fixed="right">
          <template #default="scope">
            <el-button type="success" size="small" @click="handleExecute(scope.row)">
              {{ t('cronJob.executeNow') }}
            </el-button>
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">
              {{ t('button.edit') }}
            </el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">
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

    <!-- 任务表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? t('cronJob.add') : t('cronJob.edit')"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item :label="t('cronJob.taskName')" prop="name">
          <el-input v-model="form.name" :placeholder="t('cronJob.taskNameHint')" />
        </el-form-item>
        <el-form-item :label="t('cronJob.displayName')" prop="displayName">
          <el-input v-model="form.displayName" :placeholder="t('cronJob.displayNameHint')" />
        </el-form-item>
        <el-form-item :label="t('cronJob.cron')" prop="cron">
          <el-input v-model="form.cron" :placeholder="t('cronJob.cronHint')" />
          <div class="form-tip">
            {{ t('cronJob.cronFormatTip') }}<br />
            {{ t('cronJob.cronExampleTip') }}
          </div>
        </el-form-item>
        <el-form-item :label="t('cronJob.server')" prop="server">
          <el-input v-model="form.server" :placeholder="t('cronJob.serverHint')" />
        </el-form-item>
        <el-form-item :label="t('cronJob.mode')" prop="mode">
          <el-select v-model="form.mode" :placeholder="t('cronJob.modeHint')">
            <el-option label="C#" value="C#" />
            <el-option label="Url" value="Url" />
            <el-option label="Sql" value="Sql" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('cronJob.data')" prop="data">
          <el-input
            v-model="form.data"
            type="textarea"
            :rows="4"
            :placeholder="t('cronJob.dataHint')"
          />
        </el-form-item>
        <el-form-item :label="t('cronJob.startTime')" prop="start">
          <el-date-picker
            v-model="form.start"
            type="datetime"
            :placeholder="t('cronJob.startTime')"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DDTHH:mm:ss"
          />
        </el-form-item>
        <el-form-item :label="t('cronJob.endTime')" prop="end">
          <el-date-picker
            v-model="form.end"
            type="datetime"
            :placeholder="t('cronJob.endTime')"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DDTHH:mm:ss"
          />
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

// 任务类型接口，继承 BaseEntity
interface CronJob extends BaseEntity {
  name: string;
  displayName: string;
  cron: string;
  server: string;
  mode: string;
  data: string;
  start: string;
  end: string;
  enable: boolean;
  times: number;
  maxTimes: number;
  success: number;
  error: number;
  maxError: number;
  lastTime: string;
  nextTime: string;
  description: string;
}

// 响应式数据
const loading = ref(false);
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const tableData = ref<CronJob[]>([]);
const { t } = useI18n();

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 任务表单
const form = reactive<Partial<CronJob>>({
  name: '',
  displayName: '',
  cron: '',
  server: '',
  mode: 'C#',
  data: '',
  start: '',
  end: '',
  enable: true,
  description: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: t('cronJob.taskNameRequired'), trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: t('cronJob.displayNameRequired'), trigger: 'blur' }
  ],
  cron: [
    { required: true, message: t('cronJob.cronRequired'), trigger: 'blur' }
  ],
  mode: [
    { required: true, message: t('cronJob.modeRequired'), trigger: 'change' }
  ],
  data: [
    { required: true, message: t('cronJob.dataRequired'), trigger: 'blur' }
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
    const response = await request.get('/Cube/CronJob', { params: queryParams });
    const { list, page } = apiDataToList<CronJob>(response);
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

// 分页回调
const CurrentPageChange = (page: number) => {
  queryParams.pageIndex = page;
};

const PageSizeChange = (size: number) => {
  queryParams.pageSize = size;
  queryParams.pageIndex = 1;
};

// 立即执行
const handleExecute = async (row: CronJob) => {
  try {
    await request.post(`/Cube/CronJob/ExecuteNow?id=${row.id}`);
    ElMessage.success(t('cronJob.executeSubmitted'));
    fetchData();
  } catch (error) {
    ElMessage.error(t('cronJob.executeFailed'));
    console.error('执行失败:', error);
  }
};

// 新增
const handleAdd = () => {
  formType.value = 'add';
  resetForm();
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: CronJob) => {
  formType.value = 'edit';
  Object.assign(form, row);
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: CronJob) => {
  ElMessageBox.confirm(
    t('cronJob.deleteConfirm', { name: row.displayName || row.name }),
    t('fileManager.confirmDelete'),
    {
      confirmButtonText: t('button.confirm'),
      cancelButtonText: t('button.cancel'),
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/CronJob/${row.id}`);
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
    cron: '',
    server: '',
    mode: 'C#',
    data: '',
    start: '',
    end: '',
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
          await request.post('/Cube/CronJob', form);
          ElMessage.success(t('common.addSuccess'));
        } else {
          await request.put(`/Cube/CronJob/${form.id}`, form);
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

onMounted(() => {
  fetchData();
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

.form-tip {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}
</style>
