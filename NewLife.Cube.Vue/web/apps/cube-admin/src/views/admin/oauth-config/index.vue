<template>
  <div class="oauth-config-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>OAuth配置管理</h3>
          <el-button type="primary" @click="handleAdd">新增配置</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="name" label="提供者名称" />
        <el-table-column prop="nickName" label="昵称" />
        <el-table-column prop="appId" label="应用标识" />
        <el-table-column prop="server" label="服务地址" />
        <el-table-column prop="scope" label="授权范围" />
        <el-table-column prop="sort" label="排序" width="80" />
        <el-table-column prop="updateTime" label="更新时间" />
        <el-table-column label="启用" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="可见" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.visible ? 'success' : 'danger'">
              {{ scope.row.visible ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="自动注册" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.autoRegister ? 'warning' : ''">
              {{ scope.row.autoRegister ? '是' : '否' }}
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

    <!-- OAuth配置表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? '新增OAuth配置' : '编辑OAuth配置'" width="800px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="120px">
        <el-tabs v-model="activeTab">
          <!-- 基本信息 -->
          <el-tab-pane label="基本信息" name="basic">
            <el-form-item label="提供者名称" prop="name">
              <el-input v-model="form.name" placeholder="请输入提供者名称" />
            </el-form-item>
            <el-form-item label="昵称" prop="nickName">
              <el-input v-model="form.nickName" placeholder="请输入昵称" />
            </el-form-item>
            <el-form-item label="图标" prop="logo">
              <el-input v-model="form.logo" placeholder="请输入图标URL" />
            </el-form-item>
            <el-form-item label="应用标识" prop="appId">
              <el-input v-model="form.appId" placeholder="请输入应用标识" />
            </el-form-item>
            <el-form-item label="应用密钥" prop="secret">
              <el-input v-model="form.secret" type="password" placeholder="请输入应用密钥" show-password />
            </el-form-item>
            <el-form-item label="服务地址" prop="server">
              <el-input v-model="form.server" placeholder="请输入服务地址" />
            </el-form-item>
            <el-form-item label="令牌服务地址" prop="accessServer">
              <el-input v-model="form.accessServer" placeholder="可以不同于验证地址的内网直达地址" />
            </el-form-item>
            <el-form-item label="授权类型" prop="grantType">
              <el-select v-model="form.grantType" placeholder="请选择授权类型">
                <el-option label="AuthorizationCode" :value="1" />
                <el-option label="Implicit" :value="2" />
                <el-option label="ClientCredentials" :value="3" />
                <el-option label="Password" :value="4" />
              </el-select>
            </el-form-item>
            <el-form-item label="授权范围" prop="scope">
              <el-input v-model="form.scope" placeholder="请输入授权范围" />
            </el-form-item>
          </el-tab-pane>

          <!-- 地址配置 -->
          <el-tab-pane label="地址配置" name="urls">
            <el-form-item label="验证地址" prop="authUrl">
              <el-input v-model="form.authUrl" placeholder="跳转SSO的验证地址" />
            </el-form-item>
            <el-form-item label="令牌地址" prop="accessUrl">
              <el-input v-model="form.accessUrl" placeholder="根据code换取令牌的地址" />
            </el-form-item>
            <el-form-item label="用户地址" prop="userUrl">
              <el-input v-model="form.userUrl" placeholder="根据令牌获取用户信息的地址" />
            </el-form-item>
            <el-form-item label="应用地址" prop="appUrl">
              <el-input v-model="form.appUrl" placeholder="域名和端口，应用系统经过反向代理重定向时指定外部地址" />
            </el-form-item>
          </el-tab-pane>

          <!-- 高级设置 -->
          <el-tab-pane label="高级设置" name="advanced">
            <el-form-item label="启用状态" prop="enable">
              <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="调试模式" prop="debug">
              <el-switch v-model="form.debug" :active-value="true" :inactive-value="false" />
              <span class="form-tip">设置处于调试状态，输出详细日志</span>
            </el-form-item>
            <el-form-item label="可见状态" prop="visible">
              <el-switch v-model="form.visible" :active-value="true" :inactive-value="false" />
              <span class="form-tip">是否在登录页面可见</span>
            </el-form-item>
            <el-form-item label="自动注册" prop="autoRegister">
              <el-switch v-model="form.autoRegister" :active-value="true" :inactive-value="false" />
              <span class="form-tip">SSO登录后，如果本地没有匹配用户，自动注册新用户</span>
            </el-form-item>
            <el-form-item label="自动角色" prop="autoRole">
              <el-input v-model="form.autoRole" placeholder="该渠道登录的用户，将会自动得到指定角色名，多个角色逗号隔开" />
            </el-form-item>
            <el-form-item label="排序" prop="sort">
              <el-input-number v-model="form.sort" :min="0" placeholder="较大者在前面" />
            </el-form-item>
            <el-form-item label="安全密钥" prop="securityKey">
              <el-input v-model="form.securityKey" type="textarea" placeholder="公钥，用于RSA加密用户密码" />
            </el-form-item>
            <el-form-item label="字段映射" prop="fieldMap">
              <el-input v-model="form.fieldMap" type="textarea" placeholder="SSO用户字段如何映射到OAuthClient内部属性" />
            </el-form-item>
            <el-form-item label="抓取头像" prop="fetchAvatar">
              <el-switch v-model="form.fetchAvatar" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="备注" prop="remark">
              <el-input v-model="form.remark" type="textarea" placeholder="请输入备注" />
            </el-form-item>
          </el-tab-pane>
        </el-tabs>
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
    { required: true, message: '请输入提供者名称', trigger: 'blur' }
  ],
  appId: [
    { required: true, message: '请输入应用标识', trigger: 'blur' }
  ],
  secret: [
    { required: true, message: '请输入应用密钥', trigger: 'blur' }
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
    const data = await request.get('/Admin/OAuthConfig', {
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
  ElMessageBox.confirm('确认删除该OAuth配置吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  })
    .then(async () => {
      try {
        await request.delete('/Admin/OAuthConfig', {
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
          await request.post('/Admin/OAuthConfig', form);
        } else {
          await request.put('/Admin/OAuthConfig', form);
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
