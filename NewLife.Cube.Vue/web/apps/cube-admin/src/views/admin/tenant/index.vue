<template>
  <div class="tenant-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('tenant.title') }}</h3>
          <el-button type="primary" @click="handleAdd">{{ t('tenant.add') }}</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" :label="t('common.id')" width="80" />
        <el-table-column prop="code" :label="t('tenant.code')" />
        <el-table-column prop="name" :label="t('tenant.name')" />
        <el-table-column prop="managerName" :label="t('tenant.manager')" />
        <el-table-column prop="kind" :label="t('tenant.kind')" width="100">
          <template #default="scope">
            <el-tag :type="getKindType(scope.row.kind)">
              {{ getKindText(scope.row.kind) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="updateTime" :label="t('common.updateTime')" />
        <el-table-column :label="t('common.enable')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('common.remark')" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column :label="t('common.actions')" width="180">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">{{ t('button.edit') }}</el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">{{ t('button.delete') }}</el-button>
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
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? t('tenant.add') : t('tenant.edit')" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item :label="t('tenant.code')" prop="code">
          <el-input v-model="form.code" :placeholder="t('tenant.codeHint')" />
        </el-form-item>
        <el-form-item :label="t('tenant.name')" prop="name">
          <el-input v-model="form.name" :placeholder="t('tenant.nameHint')" />
        </el-form-item>
        <el-form-item :label="t('tenant.kind')" prop="kind">
          <el-select v-model="form.kind" :placeholder="t('tenant.kindHint')">
            <el-option :label="t('tenant.kindNormal')" :value="0" />
            <el-option :label="t('tenant.kindSystem')" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('tenant.manager')" prop="managerId">
          <el-input-number v-model="form.managerId" :min="0" :placeholder="t('tenant.managerHint')" />
        </el-form-item>
        <el-form-item :label="t('coreSetting.enable')" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('common.remark')" prop="remark">
          <el-input v-model="form.remark" type="textarea" :placeholder="t('tenant.remarkHint')" />
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
const { t } = useI18n();

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
    { required: true, message: t('tenant.codeRequired'), trigger: 'blur' }
  ],
  name: [
    { required: true, message: t('tenant.nameRequired'), trigger: 'blur' }
  ]
});

// 获取类型文本
const getKindText = (kind: number) => {
  const map: Record<number, string> = {
    0: t('tenant.kindNormal'),
    1: t('tenant.kindSystem')
  };
  return map[kind] || t('tenant.kindUnknown');
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
    () => request.delete(`/Admin/Tenant/${row.id}`),
    loadData,
    t('tenant.deleteConfirm')
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/Tenant', form);
    } else {
      await request.put(`/Admin/Tenant/${form.id}`, form);
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
