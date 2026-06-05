<template>
  <div class="cron-job-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>定时任务</h3>
          <el-button type="primary" @click="handleAdd">新增任务</el-button>
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
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="name" label="名称" min-width="150" />
        <el-table-column prop="displayName" label="显示名" min-width="150" />
        <el-table-column prop="cron" label="Cron表达式" min-width="120" />
        <el-table-column prop="server" label="服务器" width="120" />
        <el-table-column prop="mode" label="模式" width="100" />
        <el-table-column prop="enable" label="启用" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="times" label="执行次数" width="100" />
        <el-table-column prop="nextTime" label="下次执行" min-width="150" />
        <el-table-column prop="updateTime" label="更新时间" min-width="150" />
        <el-table-column prop="description" label="备注" min-width="120" />
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="scope">
            <el-button type="success" size="small" @click="handleExecute(scope.row)">立即执行</el-button>
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">编辑</el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">删除</el-button>
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
      :title="formType === 'add' ? '新增任务' : '编辑任务'"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="任务名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入任务名称" />
        </el-form-item>
        <el-form-item label="显示名称" prop="displayName">
          <el-input v-model="form.displayName" placeholder="请输入显示名称" />
        </el-form-item>
        <el-form-item label="Cron表达式" prop="cron">
          <el-input v-model="form.cron" placeholder="例如: 0 0/5 * * * ?" />
          <div class="form-tip">
            格式: 秒 分 时 日 月 周 年(可选)<br/>
            示例: 0 0/5 * * * ? (每5分钟执行一次)
          </div>
        </el-form-item>
        <el-form-item label="服务器" prop="server">
          <el-input v-model="form.server" placeholder="请输入服务器名称" />
        </el-form-item>
        <el-form-item label="模式" prop="mode">
          <el-select v-model="form.mode" placeholder="请选择模式">
            <el-option label="C#" value="C#" />
            <el-option label="Url" value="Url" />
            <el-option label="Sql" value="Sql" />
          </el-select>
        </el-form-item>
        <el-form-item label="数据" prop="data">
          <el-input
            v-model="form.data"
            type="textarea"
            :rows="4"
            placeholder="请输入执行数据(C#类名、URL地址或SQL语句)"
          />
        </el-form-item>
        <el-form-item label="开始时间" prop="start">
          <el-date-picker
            v-model="form.start"
            type="datetime"
            placeholder="开始时间"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DDTHH:mm:ss"
          />
        </el-form-item>
        <el-form-item label="结束时间" prop="end">
          <el-date-picker
            v-model="form.end"
            type="datetime"
            placeholder="结束时间"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DDTHH:mm:ss"
          />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" type="textarea" placeholder="请输入描述" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitForm">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from 'cube-front/core/utils/request';
import { apiDataToList } from 'cube-front/core/utils/api-helpers';
import CubeListToolbarSearch from 'cube-front/core/components/CubeListToolbarSearch.vue';
import CubeListPager from 'cube-front/core/components/CubeListPager.vue';
import { pageInfoDefault } from 'cube-front/core/types/common';
import type { BaseEntity } from 'cube-front/core/types/common';

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
    { required: true, message: '请输入任务名称', trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: '请输入显示名称', trigger: 'blur' }
  ],
  cron: [
    { required: true, message: '请输入Cron表达式', trigger: 'blur' }
  ],
  mode: [
    { required: true, message: '请选择模式', trigger: 'change' }
  ],
  data: [
    { required: true, message: '请输入执行数据', trigger: 'blur' }
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
    ElMessage.error('加载数据失败');
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
    ElMessage.success('任务已提交执行');
    fetchData();
  } catch (error) {
    ElMessage.error('执行失败');
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
    `确定要删除任务"${row.displayName || row.name}"吗？`,
    '确认删除',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/CronJob?id=${row.id}`);
      ElMessage.success('删除成功');
      fetchData();
    } catch (error) {
      ElMessage.error('删除失败');
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
          ElMessage.success('新增成功');
        } else {
          await request.put('/Cube/CronJob', form);
          ElMessage.success('更新成功');
        }
        dialogVisible.value = false;
    fetchData();
      } catch (error) {
        ElMessage.error(`${formType.value === 'add' ? '新增' : '更新'}失败`);
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
