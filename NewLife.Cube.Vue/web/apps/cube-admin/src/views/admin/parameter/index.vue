<template>
  <div class="parameter-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>参数管理</h3>
          <el-button type="primary" @click="handleAdd">新增参数</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="name" label="参数名称" />
        <el-table-column prop="displayName" label="显示名称" />
        <el-table-column prop="value" label="参数值" />
        <el-table-column prop="kind" label="类别" width="100">
          <template #default="scope">
            <el-tag :type="getKindType(scope.row.kind)">
              {{ getKindText(scope.row.kind) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="category" label="分类" />
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

    <!-- 参数表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? '新增参数' : '编辑参数'" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="参数名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入参数名称" />
        </el-form-item>
        <el-form-item label="显示名称" prop="displayName">
          <el-input v-model="form.displayName" placeholder="请输入显示名称" />
        </el-form-item>
        <el-form-item label="参数值" prop="value">
          <el-input v-model="form.value" type="textarea" placeholder="请输入参数值" />
        </el-form-item>
        <el-form-item label="参数类别" prop="kind">
          <el-select v-model="form.kind" placeholder="请选择参数类别">
            <el-option label="普通" :value="0" />
            <el-option label="系统" :value="1" />
            <el-option label="用户" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="分类" prop="category">
          <el-input v-model="form.category" placeholder="请输入分类" />
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
// ...existing code...
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 定义参数类型接口，继承 BaseEntity
interface Parameter extends BaseEntity {
  tenantId: number;
  category: string;
  name: string;
  displayName: string;
  kind: number;
  value: string;
  long: number;
  double: number;
  boolean: boolean;
  dateTime: string;
  enable: boolean;
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
const tableData = ref<Parameter[]>([]);
const loading = ref(false);

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 表单相关
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const form = reactive<Parameter>({
  id: 0,
  tenantId: 0,
  category: '',
  name: '',
  displayName: '',
  kind: 0,
  value: '',
  long: 0,
  double: 0,
  boolean: false,
  dateTime: '',
  enable: true,
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
    { required: true, message: '请输入参数名称', trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: '请输入显示名称', trigger: 'blur' }
  ]
});

// 获取类别文本
const getKindText = (kind: number) => {
  const map: Record<number, string> = {
    0: '普通',
    1: '系统',
    2: '用户'
  };
  return map[kind] || '未知';
};

// 获取类别标签类型
const getKindType = (kind: number) => {
  const map: Record<number, string> = {
    0: '',
    1: 'warning',
    2: 'info'
  };
  return map[kind] || '';
};

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadData();
};

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const data = await request.get('/Admin/Parameter', {
      params: queryParams
    });
    // 处理不同的响应格式
    if (data && typeof data === 'object' && 'data' in data && 'page' in data) {
      // 包含分页信息的响应
      tableData.value = Array.isArray(data.data) ? data.data : [];
      queryParams.total = data.page?.totalCount || tableData.value.length;
    } else if (Array.isArray(data)) {
      // 直接返回数组的响应（无分页信息）
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
  Object.assign(form, {
    id: 0,
    tenantId: 0,
    category: '',
    name: '',
    displayName: '',
    kind: 0,
    value: '',
    long: 0,
    double: 0,
    boolean: false,
    dateTime: '',
    enable: true,
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
const handleEdit = (row: Parameter) => {
  formType.value = 'edit';
  Object.assign(form, { ...row });
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: Parameter) => {
  ElMessageBox.confirm('确认删除该参数吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  })
    .then(async () => {
      try {
        await request.delete('/Admin/Parameter', {
          params: { id: row.id }
        });
        loadData();
      } catch {
        // 错误提示已经在 request 拦截器中自动处理
      }
    })
    .catch(() => { });
};

// 提交表单
const submitForm = async () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        if (formType.value === 'add') {
          await request.post('/Admin/Parameter', form);
        } else {
          await request.put('/Admin/Parameter', form);
        }
        dialogVisible.value = false;
        loadData();
      } catch {
        // 错误提示已经在 request 拦截器中自动处理
      }
    }
  });
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
