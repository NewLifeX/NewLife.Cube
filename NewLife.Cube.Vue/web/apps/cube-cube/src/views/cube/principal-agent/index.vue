<template>
  <div class="principal-agent-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>主体代理</h3>
          <el-button type="primary" @click="handleAdd">新增代理</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table
        :data="tableData"
        style="width: 100%"
        v-loading="loading"
      >
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="name" label="名称" min-width="150" />
        <el-table-column prop="displayName" label="显示名" min-width="150" />
        <el-table-column prop="type" label="类型" width="120" />
        <el-table-column prop="category" label="分类" width="120" />
        <el-table-column prop="enable" label="启用" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" min-width="150" />
        <el-table-column prop="updateTime" label="更新时间" min-width="150" />
        <el-table-column label="操作" width="160">
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

    <!-- 代理表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? '新增代理' : '编辑代理'"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="代理名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入代理名称" />
        </el-form-item>
        <el-form-item label="显示名称" prop="displayName">
          <el-input v-model="form.displayName" placeholder="请输入显示名称" />
        </el-form-item>
        <el-form-item label="类型" prop="type">
          <el-select v-model="form.type" placeholder="请选择类型">
            <el-option label="个人" value="Personal" />
            <el-option label="企业" value="Enterprise" />
            <el-option label="机构" value="Organization" />
          </el-select>
        </el-form-item>
        <el-form-item label="分类" prop="category">
          <el-input v-model="form.category" placeholder="请输入分类" />
        </el-form-item>
        <el-form-item label="联系人" prop="contactPerson">
          <el-input v-model="form.contactPerson" placeholder="请输入联系人" />
        </el-form-item>
        <el-form-item label="联系电话" prop="contactPhone">
          <el-input v-model="form.contactPhone" placeholder="请输入联系电话" />
        </el-form-item>
        <el-form-item label="联系邮箱" prop="contactEmail">
          <el-input v-model="form.contactEmail" placeholder="请输入联系邮箱" />
        </el-form-item>
        <el-form-item label="地址" prop="address">
          <el-input v-model="form.address" placeholder="请输入地址" />
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

import { pageInfoDefault, type BaseEntity } from 'cube-front/core/types/common';
import { handleDeleteOperation, handleFormSubmit, apiDataToList } from 'cube-front/core/utils/api-helpers';
import { request } from 'cube-front/core/utils/request';
import type { FormInstance, FormRules } from 'element-plus';

// 定义主体代理类型接口，继承 BaseEntity
interface PrincipalAgent extends BaseEntity {
  name: string;
  displayName: string;
  type: string;
  category: string;
  contactPerson: string;
  contactPhone: string;
  contactEmail: string;
  address: string;
  enable: boolean;
}


// 响应式数据
const loading = ref(false);
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const tableData = ref<PrincipalAgent[]>([]);

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 代理表单
const form = reactive<Partial<PrincipalAgent>>({
  name: '',
  displayName: '',
  type: '',
  category: '',
  contactPerson: '',
  contactPhone: '',
  contactEmail: '',
  address: '',
  enable: true,
  remark: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: '请输入代理名称', trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: '请输入显示名称', trigger: 'blur' }
  ],
  type: [
    { required: true, message: '请选择类型', trigger: 'change' }
  ],
  contactEmail: [
    {
      type: 'email',
      message: '请输入正确的邮箱地址',
      trigger: ['blur', 'change']
    }
  ]
});

// 统一回调
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadData();
};

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/PrincipalAgent', {
      params: queryParams
    });
    const { list, page } = apiDataToList<PrincipalAgent>(response);
    tableData.value = list;
    queryParams.total = page?.totalCount || list.length;
  } catch {
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


// 新增
const handleAdd = () => {
  formType.value = 'add';
  resetForm();
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: PrincipalAgent) => {
  formType.value = 'edit';
  Object.assign(form, row);
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: PrincipalAgent) => {
  handleDeleteOperation(
    () => request.delete(`/Cube/PrincipalAgent?id=${row.id}`),
    () => callback(),
    `确定要删除代理"${row.displayName || row.name}"吗？`
  );
};


// 重置表单
const resetForm = () => {
  Object.assign(form, {
    id: undefined,
    name: '',
    displayName: '',
    type: '',
    category: '',
    contactPerson: '',
    contactPhone: '',
    contactEmail: '',
    address: '',
    enable: true,
    remark: '',
  });
  formRef.value?.clearValidate();
};


// 提交表单
const submitForm = () => {
  handleFormSubmit(
    formRef.value,
    () => formType.value === 'add'
      ? request.post('/Cube/PrincipalAgent', form)
      : request.put('/Cube/PrincipalAgent', form),
    () => {
      dialogVisible.value = false;
      callback();
    }
  );
};

// 初始化
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
