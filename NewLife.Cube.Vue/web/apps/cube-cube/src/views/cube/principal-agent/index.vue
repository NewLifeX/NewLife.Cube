<template>
  <div class="principal-agent-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('principalAgent.title') }}</h3>
          <el-button type="primary" @click="handleAdd">{{ t('principalAgent.add') }}</el-button>
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
        <el-table-column prop="id" :label="t('cronJob.id')" width="80" />
        <el-table-column prop="name" :label="t('appAdmin.name')" min-width="150" />
        <el-table-column prop="displayName" :label="t('cronJob.displayName')" min-width="150" />
        <el-table-column prop="type" :label="t('principalAgent.type')" width="120" />
        <el-table-column prop="category" :label="t('lookup.category')" width="120" />
        <el-table-column prop="enable" :label="t('cronJob.enable')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createTime" :label="t('userSearch.createTime')" min-width="150" />
        <el-table-column prop="updateTime" :label="t('common.updateTime')" min-width="150" />
        <el-table-column :label="t('common.actions')" width="160">
          <template #default="scope">
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

    <!-- 代理表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? t('principalAgent.add') : t('principalAgent.edit')"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item :label="t('principalAgent.agentName')" prop="name">
          <el-input v-model="form.name" :placeholder="t('principalAgent.agentNameHint')" />
        </el-form-item>
        <el-form-item :label="t('cronJob.displayName')" prop="displayName">
          <el-input v-model="form.displayName" :placeholder="t('cronJob.displayNameHint')" />
        </el-form-item>
        <el-form-item :label="t('principalAgent.type')" prop="type">
          <el-select v-model="form.type" :placeholder="t('principalAgent.typeHint')">
            <el-option :label="t('principalAgent.typePersonal')" value="Personal" />
            <el-option :label="t('principalAgent.typeEnterprise')" value="Enterprise" />
            <el-option :label="t('principalAgent.typeOrganization')" value="Organization" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('lookup.category')" prop="category">
          <el-input v-model="form.category" :placeholder="t('attachmentAdmin.categoryHint')" />
        </el-form-item>
        <el-form-item :label="t('principalAgent.contactPerson')" prop="contactPerson">
          <el-input v-model="form.contactPerson" :placeholder="t('principalAgent.contactPersonHint')" />
        </el-form-item>
        <el-form-item :label="t('principalAgent.contactPhone')" prop="contactPhone">
          <el-input v-model="form.contactPhone" :placeholder="t('principalAgent.contactPhoneHint')" />
        </el-form-item>
        <el-form-item :label="t('principalAgent.contactEmail')" prop="contactEmail">
          <el-input v-model="form.contactEmail" :placeholder="t('principalAgent.contactEmailHint')" />
        </el-form-item>
        <el-form-item :label="t('deptSearch.address')" prop="address">
          <el-input v-model="form.address" :placeholder="t('principalAgent.addressHint')" />
        </el-form-item>
        <el-form-item :label="t('areaAdmin.enableStatus')" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('common.remark')" prop="remark">
          <el-input v-model="form.remark" type="textarea" :placeholder="t('principalAgent.remarkHint')" />
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
import { pageInfoDefault, type BaseEntity } from '@newlifex/cube-vue/core/types/common';
import { handleDeleteOperation, handleFormSubmit, apiDataToList } from '@newlifex/cube-vue/core/utils/api-helpers';
import { request } from '@newlifex/cube-vue/core/utils/request';
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
const { t } = useI18n();

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
    { required: true, message: t('principalAgent.agentNameRequired'), trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: t('cronJob.displayNameRequired'), trigger: 'blur' }
  ],
  type: [
    { required: true, message: t('principalAgent.typeRequired'), trigger: 'change' }
  ],
  contactEmail: [
    {
      type: 'email',
      message: t('principalAgent.emailInvalid'),
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
    () => request.delete(`/Cube/PrincipalAgent/${row.id}`),
    () => callback(),
    t('principalAgent.deleteConfirm', { name: row.displayName || row.name }),
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
      : request.put(`/Cube/PrincipalAgent/${form.id}`, form),
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
