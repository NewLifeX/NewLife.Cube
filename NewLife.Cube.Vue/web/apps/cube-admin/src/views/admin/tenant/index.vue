<template>
  <div class="tenant-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>租户管理</h3>
          <el-button type="primary" @click="handleAdd">新增租户</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="code" label="租户代码" />
        <el-table-column prop="name" label="租户名称" />
        <el-table-column prop="managerName" label="管理者" />
        <el-table-column prop="kind" label="类型" width="100">
          <template #default="scope">
            <el-tag :type="getKindType(scope.row.kind)">
              {{ getKindText(scope.row.kind) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="updateTime" label="更新时间" />
        <el-table-column label="启用" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="备注" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180">
          <template #default="scope">
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

    <!-- 租户表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? '新增租户' : '编辑租户'" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="租户代码" prop="code">
          <el-input v-model="form.code" placeholder="请输入租户代码" />
        </el-form-item>
        <el-form-item label="租户名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入租户名称" />
        </el-form-item>
        <el-form-item label="租户类型" prop="kind">
          <el-select v-model="form.kind" placeholder="请选择租户类型">
            <el-option label="普通租户" :value="0" />
            <el-option label="系统租户" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item label="管理者" prop="managerId">
          <el-input-number v-model="form.managerId" :min="0" placeholder="管理者用户ID" />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="form.remark" type="textarea" placeholder="请输入备注" />
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
import type { FormInstance, FormRules } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList, handleDeleteOperation, handleFormSubmit } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';

// 定义租户类型接口
interface Tenant {
  id: number;
  code: string;
  name: string;
  enable: boolean;
  kind: number;
  managerId: number;
  logo: string;
  white: boolean;
  ex1: number;
  ex2: number;
  ex3: number;
  ex4: string;
  ex5: string;
  ex6: string;
  createUser: string;
  createUserID: number;
  createIP: string;
  createTime: string;
  updateUser: string;
  updateUserID: number;
  updateIP: string;
  updateTime: string;
  remark: string;
  managerName: string;
}

// 表格数据
const tableData = ref<Tenant[]>([]);
const loading = ref(false);

// 页面请求参数
const queryParams = reactive({
  q: '',// 搜索关键字
  ...pageInfoDefault,// 分页参数
});

// 表单相关
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const form = reactive<Tenant>({
  id: 0,
  code: '',
  name: '',
  enable: true,
  kind: 0,
  managerId: 0,
  logo: '',
  white: false,
  ex1: 0,
  ex2: 0,
  ex3: 0,
  ex4: '',
  ex5: '',
  ex6: '',
  createUser: '',
  createUserID: 0,
  createIP: '',
  createTime: '',
  updateUser: '',
  updateUserID: 0,
  updateIP: '',
  updateTime: '',
  remark: '',
  managerName: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  code: [
    { required: true, message: '请输入租户代码', trigger: 'blur' }
  ],
  name: [
    { required: true, message: '请输入租户名称', trigger: 'blur' }
  ]
});

// 获取类型文本
const getKindText = (kind: number) => {
  const map: Record<number, string> = {
    0: '普通租户',
    1: '系统租户'
  };
  return map[kind] || '未知';
};

// 获取类型标签类型
const getKindType = (kind: number): 'warning' | undefined => {
  const map: Record<number, 'warning' | undefined> = {
    0: undefined,
    1: 'warning'
  };
  return map[kind] || undefined;
};

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  console.log(e?.type, e?.params);
  const query = Object.assign(queryParams, e?.params || {});
  console.log('queryParams:', query);
  loadData();
};

// 查询请求
const loadData = async () => {
  loading.value = true;
  try {
    const data = await request.get('/Admin/Tenant', {
      params: queryParams
    });

    const { list, page } = apiDataToList<Tenant>(data);
    tableData.value = list;
    queryParams.total = page?.totalCount; // 更新总数
  } catch {
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.value = false;
  }
};

// 搜索数据处理
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
  console.log('SearchData:', queryParams);
};

// 重置数据处理
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
  console.log('ResetData:', queryParams);
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
  Object.assign(form, {
    id: 0,
    code: '',
    name: '',
    enable: true,
    kind: 0,
    managerId: 0,
    logo: '',
    white: false,
    ex1: 0,
    ex2: 0,
    ex3: 0,
    ex4: '',
    ex5: '',
    ex6: '',
    createUser: '',
    createUserID: 0,
    createIP: '',
    createTime: '',
    updateUser: '',
    updateUserID: 0,
    updateIP: '',
    updateTime: '',
    remark: '',
    managerName: '',
  });
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: Tenant) => {
  formType.value = 'edit';
  Object.assign(form, { ...row });
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: Tenant) => {
  handleDeleteOperation(
    () => request.delete('/Admin/Tenant', { params: { id: row.id } }),
    loadData,
    '确认删除该租户吗？'
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/Tenant', form);
    } else {
      await request.put('/Admin/Tenant', form);
    }
  };

  const onSuccess = () => {
    dialogVisible.value = false;
    loadData();
  };

  await handleFormSubmit(formRef.value, apiCall, onSuccess);
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
