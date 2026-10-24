<template>
  <div class="access-rule-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('rule.title') }}</h3>
          <el-button type="primary" @click="handleAdd">{{ t('rule.add') }}</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" :label="t('common.id')" width="80" />
        <el-table-column prop="name" :label="t('common.name')" />
        <el-table-column prop="url" :label="t('rule.url')" />
        <el-table-column prop="priority" :label="t('rule.priority')" width="100" />
        <el-table-column prop="actionKind" :label="t('rule.action')" width="100">
          <template #default="scope">
            <el-tag :type="getActionKindType(scope.row.actionKind)">
              {{ getActionKindText(scope.row.actionKind) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="limitTimes" :label="t('rule.limitTimes')" width="100" />
        <el-table-column prop="updateTime" :label="t('common.updateTime')" />
        <el-table-column :label="t('rule.status')" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? t('common.enable') : t('rule.disabled') }}
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

    <!-- 访问规则表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? t('rule.add') : t('rule.edit')" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="120px">
        <el-form-item :label="t('rule.name')" prop="name">
          <el-input v-model="form.name" :placeholder="t('form.placeholder', { field: t('rule.name') })" />
        </el-form-item>
        <el-form-item :label="t('common.enable')" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('rule.priority')" prop="priority">
          <el-input-number v-model="form.priority" :min="0" :max="999" :placeholder="t('rule.priorityPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('rule.url')" prop="url">
          <el-input v-model="form.url" :placeholder="t('rule.matchPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('rule.userAgent')" prop="userAgent">
          <el-input v-model="form.userAgent" :placeholder="t('rule.matchPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('rule.ip')" prop="ip">
          <el-input v-model="form.ip" :placeholder="t('rule.matchPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('rule.loginedUser')" prop="loginedUser">
          <el-input v-model="form.loginedUser" :placeholder="t('rule.matchPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('rule.action')" prop="actionKind">
          <el-select v-model="form.actionKind" :placeholder="t('form.select.placeholder', { field: t('rule.action') })">
            <el-option :label="t('rule.allow')" :value="0" />
            <el-option :label="t('rule.block')" :value="1" />
            <el-option :label="t('rule.throttle')" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('rule.blockCode')" prop="blockCode" v-if="form.actionKind === 1">
          <el-input-number v-model="form.blockCode" :min="100" :max="599" :placeholder="t('rule.blockCodePlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('rule.blockContent')" prop="blockContent" v-if="form.actionKind === 1">
          <el-input v-model="form.blockContent" type="textarea" :placeholder="t('rule.blockContentPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('rule.limitDimension')" prop="limitDimension" v-if="form.actionKind === 2">
          <el-select v-model="form.limitDimension" :placeholder="t('form.select.placeholder', { field: t('rule.limitDimension') })">
            <el-option label="IP" :value="1" />
            <el-option :label="t('rule.dimensionUser')" :value="2" />
            <el-option :label="t('rule.dimensionGlobal')" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('rule.limitCycle')" prop="limitCycle" v-if="form.actionKind === 2">
          <el-input-number v-model="form.limitCycle" :min="1" :placeholder="t('rule.limitCyclePlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('rule.limitTimes')" prop="limitTimes" v-if="form.actionKind === 2">
          <el-input-number v-model="form.limitTimes" :min="1" :placeholder="t('rule.limitTimesPlaceholder')" />
        </el-form-item>
        <el-form-item :label="t('common.remark')" prop="remark">
          <el-input v-model="form.remark" type="textarea" :placeholder="t('form.placeholder', { field: t('common.remark') })" />
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

const { t } = useI18n();

// 定义访问规则类型接口
interface AccessRule {
  id: number;
  name: string;
  enable: boolean;
  priority: number;
  url: string;
  userAgent: string;
  ip: string;
  loginedUser: string;
  actionKind: number;
  blockCode: number;
  blockContent: string;
  limitDimension: number;
  limitCycle: number;
  limitTimes: number;
  createUserID: number;
  createTime: string;
  createIP: string;
  updateUserID: number;
  updateTime: string;
  updateIP: string;
  remark: string;
}

// 表格数据
const tableData = ref<AccessRule[]>([]);
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
const form = reactive<AccessRule>({
  id: 0,
  name: '',
  enable: true,
  priority: 0,
  url: '',
  userAgent: '',
  ip: '',
  loginedUser: '',
  actionKind: 0,
  blockCode: 404,
  blockContent: '',
  limitDimension: 1,
  limitCycle: 600,
  limitTimes: 100,
  createUserID: 0,
  createTime: '',
  createIP: '',
  updateUserID: 0,
  updateTime: '',
  updateIP: '',
  remark: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: t('form.placeholder', { field: t('rule.name') }), trigger: 'blur' }
  ],
  priority: [
    { required: true, message: t('form.placeholder', { field: t('rule.priority') }), trigger: 'blur' }
  ],
  actionKind: [
    { required: true, message: t('form.select.placeholder', { field: t('rule.action') }), trigger: 'change' }
  ]
});

// 获取处理方式文本
const getActionKindText = (actionKind: number) => {
  const map: Record<number, string> = {
    0: t('rule.allow'),
    1: t('rule.block'),
    2: t('rule.throttle')
  };
  return map[actionKind] || t('rule.unknown');
};

// 获取处理方式标签类型
const getActionKindType = (actionKind: number): 'success' | 'danger' | 'warning' | 'info' => {
  const map: Record<number, 'success' | 'danger' | 'warning'> = {
    0: 'success',
    1: 'danger',
    2: 'warning'
  };
  return map[actionKind] || 'info';
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
    const data = await request.get('/Admin/AccessRule', {
      params: queryParams
    });

    const { list, page } = apiDataToList<AccessRule>(data);
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
    name: '',
    enable: true,
    priority: 0,
    url: '',
    userAgent: '',
    ip: '',
    loginedUser: '',
    actionKind: 0,
    blockCode: 404,
    blockContent: '',
    limitDimension: 1,
    limitCycle: 600,
    limitTimes: 100,
    createUserID: 0,
    createTime: '',
    createIP: '',
    updateUserID: 0,
    updateTime: '',
    updateIP: '',
    remark: '',
  });
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: AccessRule) => {
  formType.value = 'edit';
  Object.assign(form, { ...row });
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: AccessRule) => {
  handleDeleteOperation(
    () => request.delete(`/Admin/AccessRule/${row.id}`),
    loadData,
    t('rule.deleteConfirm')
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/AccessRule', form);
    } else {
      await request.put(`/Admin/AccessRule/${form.id}`, form);
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
