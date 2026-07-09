<template>
  <div class="app-container">
    <!-- 搜索栏卡片 -->
    <el-card class="box-card">
      <div class="card-header">
        <h3>应用管理</h3>
        <el-button type="primary" @click="handleAdd">新增应用</el-button>
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
        v-if="queryParams.total > 0"
        :title="`共找到 ${queryParams.total} 条应用记录`"
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
        <el-table-column prop="name" label="名称" />
        <el-table-column prop="displayName" label="显示名" />
        <el-table-column prop="category" label="类别" />
        <el-table-column prop="version" label="版本" />
        <el-table-column prop="updateTime" label="更新时间" />
        <el-table-column label="自启动" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.autoStart ? 'success' : 'info'">
              <span :class="['status-dot', scope.row.autoStart ? 'dot-success' : 'dot-default']"></span>
              {{ scope.row.autoStart ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'info'">
              <span :class="['status-dot', scope.row.enable ? 'dot-success' : 'dot-default']"></span>
              {{ scope.row.enable ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="备注" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150">
          <template #default="scope">
            <el-button type="primary" link @click="handleEdit(scope.row)">编辑</el-button>
            <el-button type="danger" link @click="handleDelete(scope.row)">删除</el-button>
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

    <!-- 应用表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? '新增应用' : '编辑应用'"
      width="500px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="应用名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入应用名称" />
        </el-form-item>
        <el-form-item label="显示名称" prop="displayName">
          <el-input v-model="form.displayName" placeholder="请输入显示名称" />
        </el-form-item>
        <el-form-item label="类别" prop="category">
          <el-input v-model="form.category" placeholder="请输入应用类别" />
        </el-form-item>
        <el-form-item label="版本" prop="version">
          <el-input v-model="form.version" placeholder="请输入版本号" />
        </el-form-item>
        <el-form-item label="文件名" prop="fileName">
          <el-input v-model="form.fileName" placeholder="请输入文件名" />
        </el-form-item>
        <el-form-item label="参数" prop="arguments">
          <el-input v-model="form.arguments" placeholder="请输入启动参数" />
        </el-form-item>
        <el-form-item label="工作目录" prop="workingDirectory">
          <el-input v-model="form.workingDirectory" placeholder="请输入工作目录" />
        </el-form-item>
        <el-form-item label="用户名" prop="userName">
          <el-input v-model="form.userName" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item label="自启动" prop="autoStart">
          <el-switch v-model="form.autoStart" :active-value="true" :inactive-value="false" />
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
    { required: true, message: '请输入应用名称', trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: '请输入显示名称', trigger: 'blur' }
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
    `确定要删除应用"${row.displayName || row.name}"吗？`,
    '确认删除',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/App?id=${row.id}`);
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
          ElMessage.success('新增成功');
        } else {
          await request.put('/Cube/App', form);
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
