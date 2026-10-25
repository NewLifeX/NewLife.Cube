<template>
  <div class="oauth-config-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('oauthConfig.title') }}</h3>
          <el-button type="primary" @click="handleAdd">{{ t('oauthConfig.add') }}</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" :label="t('common.id')" width="80" />
        <el-table-column prop="name" :label="t('oauthConfig.name')" />
        <el-table-column prop="nickName" :label="t('oauthConfig.nickName')" />
        <el-table-column prop="appId" :label="t('oauthConfig.appId')" />
        <el-table-column prop="server" :label="t('oauthConfig.server')" />
        <el-table-column prop="scope" :label="t('oauthConfig.scope')" />
        <el-table-column prop="sort" :label="t('common.sort')" width="80" />
        <el-table-column prop="updateTime" :label="t('common.updateTime')" />
        <el-table-column :label="t('common.enable')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('menuAdmin.visible')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.visible ? 'success' : 'danger'">
              {{ scope.row.visible ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('oauthConfig.autoRegister')" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.autoRegister ? 'warning' : 'info'">
              {{ scope.row.autoRegister ? t('common.yes') : t('common.no') }}
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

    <!-- OAuth配置表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? t('oauthConfig.addTitle') : t('oauthConfig.editTitle')" width="800px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="120px">
        <el-tabs v-model="activeTab">
          <!-- 基本信息 -->
          <el-tab-pane :label="t('oauthConfig.tabBasic')" name="basic">
            <el-form-item :label="t('oauthConfig.name')" prop="name">
              <el-input v-model="form.name" :placeholder="t('oauthConfig.nameHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.nickName')" prop="nickName">
              <el-input v-model="form.nickName" :placeholder="t('oauthConfig.nickNameHint')" />
            </el-form-item>
            <el-form-item :label="t('menuAdmin.icon')" prop="logo">
              <el-input v-model="form.logo" :placeholder="t('oauthConfig.logoHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.appId')" prop="appId">
              <el-input v-model="form.appId" :placeholder="t('oauthConfig.appIdHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.secret')" prop="secret">
              <el-input v-model="form.secret" type="password" :placeholder="t('oauthConfig.secretHint')" show-password />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.server')" prop="server">
              <el-input v-model="form.server" :placeholder="t('oauthConfig.serverHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.accessServer')" prop="accessServer">
              <el-input v-model="form.accessServer" :placeholder="t('oauthConfig.accessServerHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.grantType')" prop="grantType">
              <el-select v-model="form.grantType" :placeholder="t('oauthConfig.grantTypeHint')">
                <el-option label="AuthorizationCode" :value="1" />
                <el-option label="Implicit" :value="2" />
                <el-option label="ClientCredentials" :value="3" />
                <el-option label="Password" :value="4" />
              </el-select>
            </el-form-item>
            <el-form-item :label="t('oauthConfig.scope')" prop="scope">
              <el-input v-model="form.scope" :placeholder="t('oauthConfig.scopeHint')" />
            </el-form-item>
          </el-tab-pane>

          <!-- 地址配置 -->
          <el-tab-pane :label="t('oauthConfig.tabUrls')" name="urls">
            <el-form-item :label="t('oauthConfig.authUrl')" prop="authUrl">
              <el-input v-model="form.authUrl" :placeholder="t('oauthConfig.authUrlHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.accessUrl')" prop="accessUrl">
              <el-input v-model="form.accessUrl" :placeholder="t('oauthConfig.accessUrlHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.userUrl')" prop="userUrl">
              <el-input v-model="form.userUrl" :placeholder="t('oauthConfig.userUrlHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.appUrl')" prop="appUrl">
              <el-input v-model="form.appUrl" :placeholder="t('oauthConfig.appUrlHint')" />
            </el-form-item>
          </el-tab-pane>

          <!-- 高级设置 -->
          <el-tab-pane :label="t('oauthConfig.tabAdvanced')" name="advanced">
            <el-form-item :label="t('coreSetting.enable')" prop="enable">
              <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.debug')" prop="debug">
              <el-switch v-model="form.debug" :active-value="true" :inactive-value="false" />
              <span class="form-tip">{{ t('oauthConfig.debugTip') }}</span>
            </el-form-item>
            <el-form-item :label="t('menuAdmin.visibleStatus')" prop="visible">
              <el-switch v-model="form.visible" :active-value="true" :inactive-value="false" />
              <span class="form-tip">{{ t('oauthConfig.visibleTip') }}</span>
            </el-form-item>
            <el-form-item :label="t('oauthConfig.autoRegister')" prop="autoRegister">
              <el-switch v-model="form.autoRegister" :active-value="true" :inactive-value="false" />
              <span class="form-tip">{{ t('oauthConfig.autoRegisterTip') }}</span>
            </el-form-item>
            <el-form-item :label="t('oauthConfig.autoRole')" prop="autoRole">
              <el-input v-model="form.autoRole" :placeholder="t('oauthConfig.autoRoleHint')" />
            </el-form-item>
            <el-form-item :label="t('common.sort')" prop="sort">
              <el-input-number v-model="form.sort" :min="0" :placeholder="t('oauthConfig.sortHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.securityKey')" prop="securityKey">
              <el-input v-model="form.securityKey" type="textarea" :placeholder="t('oauthConfig.securityKeyHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.fieldMap')" prop="fieldMap">
              <el-input v-model="form.fieldMap" type="textarea" :placeholder="t('oauthConfig.fieldMapHint')" />
            </el-form-item>
            <el-form-item :label="t('oauthConfig.fetchAvatar')" prop="fetchAvatar">
              <el-switch v-model="form.fetchAvatar" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('common.remark')" prop="remark">
              <el-input v-model="form.remark" type="textarea" :placeholder="t('tenant.remarkHint')" />
            </el-form-item>
          </el-tab-pane>
        </el-tabs>
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
import { ElMessageBox } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 定义OAuth配置类型接口，继承 BaseEntity
interface OAuthConfig extends BaseEntity {
  name: string;
  nickName: string;
  logo: string;
  appId: string;
  secret: string;
  server: string;
  accessServer: string;
  grantType: number;
  scope: string;
  authUrl: string;
  accessUrl: string;
  userUrl: string;
  appUrl: string;
  enable: boolean;
  debug: boolean;
  visible: boolean;
  autoRegister: boolean;
  autoRole: string;
  sort: number;
  securityKey: string;
  fieldMap: string;
  fetchAvatar: boolean;
  isDeleted: boolean;
  createUserID: number;
  createTime: string;
  createIP: string;
  updateUserID: number;
  updateTime: string;
  updateIP: string;
  remark: string;
}

// 表格数据
const tableData = ref<OAuthConfig[]>([]);
const loading = ref(false);
const { t } = useI18n();

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 表单相关
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const activeTab = ref('basic');
const formRef = ref<FormInstance | null>(null);
const form = reactive<OAuthConfig>({
  id: 0,
  name: '',
  nickName: '',
  logo: '',
  appId: '',
  secret: '',
  server: '',
  accessServer: '',
  grantType: 1,
  scope: '',
  authUrl: '',
  accessUrl: '',
  userUrl: '',
  appUrl: '',
  enable: true,
  debug: false,
  visible: true,
  autoRegister: false,
  autoRole: '',
  sort: 0,
  securityKey: '',
  fieldMap: '',
  fetchAvatar: false,
  isDeleted: false,
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
    { required: true, message: t('oauthConfig.nameRequired'), trigger: 'blur' }
  ],
  appId: [
    { required: true, message: t('oauthConfig.appIdRequired'), trigger: 'blur' }
  ],
  secret: [
    { required: true, message: t('oauthConfig.secretRequired'), trigger: 'blur' }
  ]
});

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadData();
};

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const data = (await request.get('/Admin/OAuthConfig', {
      params: queryParams
    })) as unknown as {
      data?: OAuthConfig[];
      page?: { totalCount?: number };
    };
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
  activeTab.value = 'basic';
  Object.assign(form, {
    id: 0,
    name: '',
    nickName: '',
    logo: '',
    appId: '',
    secret: '',
    server: '',
    accessServer: '',
    grantType: 1,
    scope: '',
    authUrl: '',
    accessUrl: '',
    userUrl: '',
    appUrl: '',
    enable: true,
    debug: false,
    visible: true,
    autoRegister: false,
    autoRole: '',
    sort: 0,
    securityKey: '',
    fieldMap: '',
    fetchAvatar: false,
    isDeleted: false,
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
const handleEdit = (row: OAuthConfig) => {
  formType.value = 'edit';
  activeTab.value = 'basic';
  Object.assign(form, { ...row });
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: OAuthConfig) => {
  ElMessageBox.confirm(t('oauthConfig.deleteConfirm'), t('lovAdmin.confirmDelete'), {
    confirmButtonText: t('button.confirm'),
    cancelButtonText: t('button.cancel'),
    type: 'warning'
  })
    .then(async () => {
      try {
        await request.delete(`/Admin/OAuthConfig/${row.id}`);
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
      // 清理列表行原始字段回传造成的键冲突：行数据同时含 camelCase 开关
      // （enable/visible）与 PascalCase 旧字段（Enable/Visible 整型），handleEdit 整行
      // Object.assign 后表单同时携带两组键。后端大小写不敏感绑定下靠后的 PascalCase
      // 旧值会覆盖开关新值，导致“编辑启用/可见性不生效”。发送前剔除 PascalCase 冗余键。
      const payload: Record<string, unknown> = { ...form };
      delete payload.Enable;
      delete payload.Visible;

      try {
        if (formType.value === 'add') {
          await request.post('/Admin/OAuthConfig', payload);
        } else {
          await request.put(`/Admin/OAuthConfig/${payload.id}`, payload);
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

.form-tip {
  font-size: 12px;
  color: #999;
  margin-left: 10px;
}
</style>
