<template>
  <div class="user-container">
    <!-- ─── 用户列表 ─── -->
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>用户管理</h3>
          <el-button type="primary" @click="handleAdd">新增用户</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading" row-key="id">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column label="用户名" min-width="150" show-overflow-tooltip>
          <template #default="scope">
            <div class="user-cell">
              <el-avatar :size="24" :src="getAvatarUrl(scope.row.avatar)">
                {{ (scope.row.displayName || scope.row.name || '?').charAt(0) }}
              </el-avatar>
              <span>{{ scope.row.name }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="displayName" label="显示名" min-width="110" show-overflow-tooltip />
        <el-table-column label="角色" min-width="110" show-overflow-tooltip>
          <template #default="scope">{{ scope.row.roleName || '-' }}</template>
        </el-table-column>
        <el-table-column label="部门" min-width="110" show-overflow-tooltip>
          <template #default="scope">{{ scope.row.departmentName || '-' }}</template>
        </el-table-column>
        <el-table-column label="邮箱" min-width="180" show-overflow-tooltip>
          <template #default="scope">{{ scope.row.mail || '-' }}</template>
        </el-table-column>
        <el-table-column label="手机号" width="130" show-overflow-tooltip>
          <template #default="scope">{{ scope.row.mobile || '-' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="80" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'" size="small">
              {{ scope.row.enable ? '正常' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="updateTime" label="更新时间" width="160" show-overflow-tooltip />
        <el-table-column label="备注" min-width="120" show-overflow-tooltip>
          <template #default="scope">{{ scope.row.remark || '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right" align="center">
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

    <!-- ─── 用户表单弹窗 ─── -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? '新增用户' : '编辑用户'"
      width="520px"
    >
      <el-form ref="userFormRef" :model="userForm" :rules="userFormRules" label-width="90px">
        <el-form-item label="用户名" prop="name">
          <el-input v-model="userForm.name" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item label="显示名称" prop="displayName">
          <el-input v-model="userForm.displayName" placeholder="请输入显示名称" />
        </el-form-item>
        <el-form-item label="性别" prop="sex">
          <el-radio-group v-model="userForm.sex">
            <el-radio :label="1">男</el-radio>
            <el-radio :label="0">女</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="邮箱" prop="mail">
          <el-input v-model="userForm.mail" placeholder="请输入邮箱" />
        </el-form-item>
        <el-form-item label="手机号" prop="mobile">
          <el-input v-model="userForm.mobile" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="角色" prop="roleID">
          <el-select v-model="userForm.roleID" placeholder="请选择角色" style="width: 100%">
            <el-option
              v-for="role in roleOptions"
              :key="role.value"
              :label="role.label"
              :value="role.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="部门" prop="departmentID">
          <el-select v-model="userForm.departmentID" placeholder="请选择部门" style="width: 100%">
            <el-option
              v-for="dept in departmentOptions"
              :key="dept.value"
              :label="dept.label"
              :value="dept.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="密码" prop="password" v-if="formType === 'add'">
          <el-input
            v-model="userForm.password"
            type="password"
            placeholder="请输入密码"
            show-password
          />
        </el-form-item>
        <el-form-item label="状态" prop="enable">
          <el-switch v-model="userForm.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="userForm.remark" type="textarea" :rows="2" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <div>
            <el-button
              v-if="formType === 'edit'"
              type="warning"
              size="small"
              @click="handleChangePasswordInEdit"
              >修改密码</el-button
            >
          </div>
          <div>
            <el-button @click="dialogVisible = false">取消</el-button>
            <el-button type="primary" @click="submitForm">确定</el-button>
          </div>
        </div>
      </template>
    </el-dialog>

    <!-- ─── 修改密码弹窗 ─── -->
    <el-dialog v-model="changePasswordDialogVisible" title="修改密码" width="400px">
      <el-form
        ref="changePasswordFormRef"
        :model="changePasswordForm"
        :rules="changePasswordFormRules"
        label-width="90px"
      >
        <el-form-item label="用户名">
          <el-input v-model="changePasswordForm.name" disabled />
        </el-form-item>
        <el-form-item label="旧密码" prop="oldPassword">
          <el-input
            v-model="changePasswordForm.oldPassword"
            type="password"
            placeholder="请输入旧密码"
            show-password
          />
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input
            v-model="changePasswordForm.newPassword"
            type="password"
            placeholder="请输入新密码"
            show-password
          />
        </el-form-item>
        <el-form-item label="确认密码" prop="newPassword2">
          <el-input
            v-model="changePasswordForm.newPassword2"
            type="password"
            placeholder="请再次输入新密码"
            show-password
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="changePasswordDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitChangePassword">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage, ElMessageBox } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import {
  apiDataToList,
  apiDataToSingle,
  handleDeleteOperation,
  handleFormSubmit,
} from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import type { SelectOption, BaseEntity, EnableStatus } from '@newlifex/cube-vue/core/types/common';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';

// 定义用户类型接口
interface User extends BaseEntity, EnableStatus {
  /** 用户名 */
  name: string;
  /** 显示名称 */
  displayName: string;
  /** 邮箱 */
  mail: string;
  /** 手机号 */
  mobile: string;
  /** 密码 */
  password?: string;
  /** 性别：1-男，0-女 */
  sex: number;
  /** 头像 */
  avatar?: string;
  /** 角色ID */
  roleID?: number;
  /** 部门ID */
  departmentID?: number;
  /** 注册时间 */
  registerTime: string;
  /** 最后登录时间 */
  lastLogin: string;
  /** 角色名称 */
  roleName?: string;
  /** 部门名称 */
  departmentName?: string;
}

// 定义修改密码表单接口
interface ChangePasswordForm {
  /** 用户名 */
  name: string;
  /** 旧密码 */
  oldPassword: string;
  /** 新密码 */
  newPassword: string;
  /** 确认密码 */
  newPassword2: string;
}

// 定义初始用户表单数据
const initialUserForm: User = {
  id: 0,
  name: '',
  displayName: '',
  mail: '',
  mobile: '',
  password: '',
  enable: true,
  sex: 1,
  avatar: '',
  roleID: undefined,
  departmentID: undefined,
  registerTime: '',
  lastLogin: '',
  updateTime: '',
  remark: '',
  roleName: '',
  departmentName: '',
};

// 表格数据
const tableData = ref<User[]>([]);
const loading = ref(false);

// 用户表单相关
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const userFormRef = ref<FormInstance | null>(null);
const userForm = reactive<User>({ ...initialUserForm });

// 修改密码相关
const changePasswordDialogVisible = ref(false);
const changePasswordFormRef = ref<FormInstance | null>(null);
const changePasswordForm = reactive<ChangePasswordForm>({
  name: '',
  oldPassword: '',
  newPassword: '',
  newPassword2: '',
});

// 角色选项数据
const roleOptions = ref<SelectOption[]>([]);
const roleOptionsLoaded = ref(false); // 标记角色数据是否已加载

// 部门选项数据
const departmentOptions = ref<SelectOption[]>([]);
const departmentOptionsLoaded = ref(false); // 标记部门数据是否已加载

// 页面请求参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault,
});

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  console.log(e?.type, e?.params);
  const query = Object.assign(queryParams, e?.params || {});
  console.log('queryParams:', query);
  queryUser();
};
//查询请求 - 使用新的fetchPageData方法，更简洁
const queryUser = async () => {
  loading.value = true;
  try {
    const c = await request.get('/Admin/User', { params: queryParams });
    const { list, page } = apiDataToList<User>(c);
    tableData.value = list;
    queryParams.total = page?.totalCount; // 更新总数
  } catch {
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.value = false;
  }
};

// 表单验证规则
const userFormRules = reactive<FormRules>({
  name: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 1, max: 50, message: '长度在 1 到 50 个字符', trigger: 'blur' },
  ],
  displayName: [{ min: 1, max: 50, message: '长度在 1 到 50 个字符', trigger: 'blur' }],
  password: [
    { required: formType.value === 'add', message: '请输入密码', trigger: 'blur' },
    { min: 8, max: 200, message: '长度在 8 到 200 个字符', trigger: 'blur' },
  ],
  sex: [{ required: true, message: '请选择性别', trigger: 'change' }],
  mail: [
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' },
    { max: 50, message: '长度不能超过 50 个字符', trigger: 'blur' },
  ],
  mobile: [
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' },
    { max: 50, message: '长度不能超过 50 个字符', trigger: 'blur' },
  ],
  roleID: [{ required: true, message: '请选择角色', trigger: 'change' }],
  departmentID: [{ required: true, message: '请选择部门', trigger: 'change' }],
  code: [{ max: 50, message: '长度不能超过 50 个字符', trigger: 'blur' }],
  avatar: [{ max: 200, message: '长度不能超过 200 个字符', trigger: 'blur' }],
  roleIds: [{ max: 200, message: '长度不能超过 200 个字符', trigger: 'blur' }],
  lastLoginIP: [{ max: 50, message: '长度不能超过 50 个字符', trigger: 'blur' }],
  registerIP: [{ max: 50, message: '长度不能超过 50 个字符', trigger: 'blur' }],
  ex4: [{ max: 50, message: '长度不能超过 50 个字符', trigger: 'blur' }],
  remark: [{ max: 500, message: '长度不能超过 500 个字符', trigger: 'blur' }],
});

// 修改密码表单验证规则
const changePasswordFormRules = reactive<FormRules>({
  oldPassword: [{ required: true, message: '请输入旧密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, max: 200, message: '长度在 8 到 200 个字符', trigger: 'blur' },
  ],
  newPassword2: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (_rule: unknown, value: string, callback: (error?: Error) => void) => {
        if (value !== changePasswordForm.newPassword) {
          callback(new Error('两次输入的密码不一致'));
        } else {
          callback();
        }
      },
      trigger: 'blur',
    },
  ],
});

// 加载部门数据
const loadDepartmentOptions = async (forceRefresh = false) => {
  if (!forceRefresh && departmentOptionsLoaded.value) return;
  try {
    const data = await request.get('/Admin/Department');
    const { list } = apiDataToList<{ id: number; name: string }>(data);
    departmentOptions.value = list.map((dept: { id: number; name: string }) => ({
      value: dept.id,
      label: dept.name,
    }));
    departmentOptionsLoaded.value = true;
  } catch (error) {
    console.error('加载部门数据失败:', error);
    departmentOptions.value = [];
    departmentOptionsLoaded.value = false;
  }
};

// 获取头像完整URL
const getAvatarUrl = (avatar: string): string => {
  if (!avatar) return '';
  // 如果头像路径以"/"开头，拼接当前域名
  // if (avatar.startsWith('/')) { return `${window.location.origin}${avatar}`; }
  return avatar; // 如果是完整的URL（http或https开头），直接返回
};

// 加载角色数据
const loadRoleOptions = async (forceRefresh = false) => {
  // 如果不是强制刷新且已经加载过角色数据，直接返回
  if (!forceRefresh && roleOptionsLoaded.value) {
    return;
  }
  try {
    const data = await request.get('/Admin/Role');
    const { list } = apiDataToList<{ id: number; name: string }>(data);
    roleOptions.value = list.map((role: { id: number; name: string }) => ({
      value: role.id,
      label: role.name,
    }));
    roleOptionsLoaded.value = true; // 标记为已加载
  } catch (error) {
    console.error('加载角色数据失败:', error);
    roleOptions.value = [];
    roleOptionsLoaded.value = false;
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

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
  console.log('SearchData:', queryParams);
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
  console.log('ResetData:', queryParams);
};

// 新增用户
const handleAdd = () => {
  formType.value = 'add';
  Object.assign(userForm, { ...initialUserForm });
  dialogVisible.value = true;
};

// 编辑用户 - 演示如何使用fetchSingleData获取单个用户详情
const handleEdit = async (row: User) => {
  formType.value = 'edit';
  // // 方式1：直接使用传入的row数据
  // Object.assign(userForm, {
  //   id: row.id,
  //   name: row.name,
  //   displayName: row.displayName,
  //   mail: row.mail,
  //   mobile: row.mobile,
  //   enable: row.enable,
  //   sex: row.sex,
  //   avatar: row.avatar,
  //   roleID: row.roleID,
  //   departmentID: row.departmentID,
  //   remark: row.remark
  // });

  try {
    // 方式2：请求最新数据
    const data = await request.get(`/Admin/User/Detail?id=${row.id}`, { params: queryParams });
    const userDetail = apiDataToSingle<User>(data);
    if (userDetail) {
      Object.assign(userForm, userDetail);
    }
    dialogVisible.value = true;
  } catch (error) {
    console.error('获取用户详情失败:', error);
  }
};

// 修改密码 - 在编辑弹窗中使用
const handleChangePasswordInEdit = () => {
  changePasswordForm.name = userForm.name;
  changePasswordForm.oldPassword = '';
  changePasswordForm.newPassword = '';
  changePasswordForm.newPassword2 = '';
  changePasswordDialogVisible.value = true;
};

// 清空密码 - 在编辑弹窗中使用
const handleClearPasswordInEdit = () => {
  ElMessageBox.confirm(
    `确认清空用户 [${userForm.displayName || userForm.name}] 的密码吗？清空后该用户将无法使用密码登录。`,
    '确认清空密码',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    },
  )
    .then(async () => {
      try {
        await request.post('/Admin/User/ClearPassword', null, { params: { id: userForm.id } });
        ElMessage.success('密码清空成功');
        dialogVisible.value = false; // 关闭用户编辑对话框
      } catch (error) {
        console.error('清空密码失败:', error);
        ElMessage.error('清空密码失败');
      }
    })
    .catch(() => {
      // 用户取消操作
    });
};

// 删除用户
const handleDelete = (row: User) => {
  handleDeleteOperation(
    () => request.delete('/Admin/User', { params: { id: row.id } }),
    queryUser, //() => null,
    '确认删除[' + (row.displayName || row.name) + ']用户吗？',
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (formType.value === 'add') {
      // 创建用户，过滤掉不需要的字段
      const userData = {
        name: userForm.name,
        displayName: userForm.displayName,
        mail: userForm.mail,
        mobile: userForm.mobile,
        password: userForm.password,
        enable: userForm.enable,
        sex: userForm.sex,
        avatar: userForm.avatar,
        roleID: userForm.roleID,
        departmentID: userForm.departmentID,
        remark: userForm.remark,
      };
      await request.post('/Admin/User', userData);
    } else if (formType.value === 'edit') {
      await request.put('/Admin/User', userForm);
    }
  };

  const onSuccess = () => {
    dialogVisible.value = false;
    queryUser();
  };

  await handleFormSubmit(userFormRef.value, apiCall, onSuccess);
};

// 提交修改密码表单
const submitChangePassword = async () => {
  const apiCall = async () => {
    await request.post('/Admin/User/ChangePassword', {
      name: changePasswordForm.name,
      oldPassword: changePasswordForm.oldPassword,
      newPassword: changePasswordForm.newPassword,
      newPassword2: changePasswordForm.newPassword2,
    });
  };

  const onSuccess = () => {
    changePasswordDialogVisible.value = false; // 关闭修改密码对话框
    ElMessage.success('密码修改成功');
    // 重置表单
    changePasswordForm.oldPassword = '';
    changePasswordForm.newPassword = '';
    changePasswordForm.newPassword2 = '';
  };

  await handleFormSubmit(changePasswordFormRef.value, apiCall, onSuccess);
};

// 初始化加载数据
onMounted(() => {
  queryUser();
  loadRoleOptions(true); // 页面加载时强制刷新角色数据
  loadDepartmentOptions(true); // 页面加载时强制刷新部门数据
});
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.user-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.dialog-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
