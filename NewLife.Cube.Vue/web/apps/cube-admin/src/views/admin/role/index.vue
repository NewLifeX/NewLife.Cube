<template>
  <div class="role-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>角色管理</h3>
          <el-button type="primary" @click="handleAdd">新增角色</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="name" label="角色名称" />
        <el-table-column prop="tenantName" label="租户" />
        <el-table-column prop="sort" label="排序" width="80" />
        <el-table-column prop="updateTime" label="更新时间" />
        <el-table-column label="启用" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="系统角色" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.isSystem ? 'warning' : undefined">
              {{ scope.row.isSystem ? '是' : '否' }}
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
            <el-button type="danger" size="small" @click="handleDelete(scope.row)" :disabled="scope.row.isSystem">删除</el-button>
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

    <!-- 角色表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? '新增角色' : '编辑角色'" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="角色名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入角色名称" :disabled="form.isSystem" />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="系统角色" prop="isSystem">
          <el-switch v-model="form.isSystem" :active-value="true" :inactive-value="false" :disabled="formType === 'edit'" />
          <span class="form-tip">系统角色不受数据权限约束，禁止修改名称或删除</span>
        </el-form-item>
        <el-form-item label="租户" prop="tenantId">
          <el-input-number v-model="form.tenantId" :min="0" placeholder="0表示全局角色" />
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" placeholder="排序值" />
        </el-form-item>
        <el-form-item label="权限设置" prop="permission">
          <el-input v-model="form.permission" type="textarea" :rows="4" placeholder="对不同资源的权限，逗号分隔，每个资源的权限子项竖线分隔" />
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

// 定义角色类型接口
interface Role {
  id: number;
  name: string;
  enable: boolean;
  isSystem: boolean;
  tenantId: number;
  permission: string;
  sort: number;
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
  tenantName: string;
}

// 表格数据
const tableData = ref<Role[]>([]);
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
const form = reactive<Role>({
  id: 0,
  name: '',
  enable: true,
  isSystem: false,
  tenantId: 0,
  permission: '',
  sort: 0,
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
  tenantName: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: '请输入角色名称', trigger: 'blur' }
  ]
});

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
    const data = await request.get('/Admin/Role', {
      params: queryParams
    });

    const { list, page } = apiDataToList<Role>(data);
    tableData.value = list;
    queryParams.total = page?.totalCount; // 更新总数
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

// 新增
const handleAdd = () => {
  formType.value = 'add';
  Object.assign(form, {
    id: 0,
    name: '',
    enable: true,
    isSystem: false,
    tenantId: 0,
    permission: '',
    sort: 0,
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
    tenantName: '',
  });
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: Role) => {
  formType.value = 'edit';
  Object.assign(form, { ...row });
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: Role) => {
  if (row.isSystem) {
    return;
  }

  handleDeleteOperation(
    () => request.delete('/Admin/Role', { params: { id: row.id } }),
    loadData,
    '确认删除该角色吗？'
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/Role', form);
    } else {
      await request.put('/Admin/Role', form);
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

.form-tip {
  font-size: 12px;
  color: #999;
  margin-left: 10px;
}
</style>
