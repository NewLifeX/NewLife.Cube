<template>
  <div class="user-page">
    <!-- ─── 页头 ─── -->
    <div class="page-header">
      <div class="header-brand">
        <div class="brand-icon">
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
          >
            <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M23 21v-2a4 4 0 00-3-3.87" />
            <path d="M16 3.13a4 4 0 010 7.75" />
          </svg>
        </div>
        <div>
          <p class="brand-sup">系统管理</p>
          <h1 class="brand-title">用户管理</h1>
        </div>
      </div>
      <div class="header-right">
        <div class="count-stat">
          <span class="count-num">{{ queryParams.total || 0 }}</span>
          <span class="count-label">用户总数</span>
        </div>
        <button class="hdr-btn" @click="queryUser">
          <svg
            width="13"
            height="13"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <polyline points="23 4 23 10 17 10" />
            <polyline points="1 20 1 14 7 14" />
            <path d="M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15" />
          </svg>
          刷新
        </button>
        <button class="hdr-btn prime" @click="handleAdd">
          <svg
            width="13"
            height="13"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
          >
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          新增用户
        </button>
      </div>
    </div>

    <!-- ─── 搜索栏 ─── -->
    <div class="search-bar">
      <div class="search-input-wrap">
        <svg
          class="search-icon"
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
        >
          <circle cx="11" cy="11" r="8" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>
        <input
          class="search-input"
          v-model="searchQ"
          placeholder="搜索用户名、显示名称、邮箱..."
          @keyup.enter="doSearch"
        />
        <button
          v-if="searchQ"
          class="search-clear"
          @click="
            searchQ = '';
            doSearch();
          "
        >
          ✕
        </button>
      </div>
      <button class="search-btn primary" @click="doSearch">搜索</button>
      <button class="search-btn ghost" @click="doReset">重置</button>
    </div>

    <!-- ─── 数据表格 ─── -->
    <div class="data-panel" v-loading="loading">
      <!-- 表头 -->
      <div class="row-head">
        <div class="col col-user">用户信息</div>
        <div class="col col-role">角色</div>
        <div class="col col-dept">部门</div>
        <div class="col col-contact">联系方式</div>
        <div class="col col-status">状态</div>
        <div class="col col-time">更新时间</div>
        <div class="col col-ops">操作</div>
      </div>

      <!-- 数据行 -->
      <TransitionGroup name="rows" tag="div" class="rows-body">
        <div
          v-for="user in tableData"
          :key="user.id"
          class="data-row"
          :class="{ 'is-off': !user.enable }"
        >
          <div class="col col-user">
            <div class="avatar" :class="user.sex === 1 ? 'avatar--male' : 'avatar--female'">
              <img
                v-if="user.avatar && !avatarError[user.id]"
                :src="getAvatarUrl(user.avatar)"
                @error="avatarError[user.id] = true"
              />
              <span v-else class="avatar-init">{{
                (user.displayName || user.name || '?').charAt(0)
              }}</span>
            </div>
            <div class="user-info">
              <span class="user-dname">{{ user.displayName || user.name }}</span>
              <span class="user-uname">@{{ user.name }}</span>
            </div>
          </div>
          <div class="col col-role">
            <span v-if="user.roleName" class="chip chip--role">{{ user.roleName }}</span>
            <span v-else class="nil">—</span>
          </div>
          <div class="col col-dept">
            <span v-if="user.departmentName" class="dept-tag">{{ user.departmentName }}</span>
            <span v-else class="nil">—</span>
          </div>
          <div class="col col-contact">
            <div v-if="user.mail" class="contact-line">
              <svg
                width="11"
                height="11"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <path
                  d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"
                />
                <polyline points="22,6 12,13 2,6" />
              </svg>
              {{ user.mail }}
            </div>
            <div v-if="user.mobile" class="contact-line">
              <svg
                width="11"
                height="11"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <rect x="5" y="2" width="14" height="20" rx="2" ry="2" />
                <line x1="12" y1="18" x2="12.01" y2="18" />
              </svg>
              {{ user.mobile }}
            </div>
            <span v-if="!user.mail && !user.mobile" class="nil">—</span>
          </div>
          <div class="col col-status">
            <div class="status-pill" :class="user.enable ? 'status-ok' : 'status-off'">
              <i class="status-dot"></i>
              {{ user.enable ? '正常' : '禁用' }}
            </div>
          </div>
          <div class="col col-time">
            <span class="time-val" :title="user.updateTime">{{ formatTime(user.updateTime) }}</span>
          </div>
          <div class="col col-ops">
            <button class="op-btn edit-btn" @click="handleEdit(user)">
              <svg
                width="11"
                height="11"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
              </svg>
              编辑
            </button>
            <button class="op-btn del-btn" @click="handleDelete(user)">
              <svg
                width="11"
                height="11"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <polyline points="3 6 5 6 21 6" />
                <path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6" />
                <path d="M10 11v6M14 11v6" />
              </svg>
              删除
            </button>
          </div>
        </div>
      </TransitionGroup>

      <!-- 空状态 -->
      <div v-if="!loading && tableData.length === 0" class="empty-state">
        <svg
          width="52"
          height="52"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1"
        >
          <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M23 21v-2a4 4 0 00-3-3.87" />
          <path d="M16 3.13a4 4 0 010 7.75" />
        </svg>
        <p>暂无用户数据</p>
      </div>
    </div>

    <!-- ─── 分页 ─── -->
    <div class="pager-bar">
      <span class="pager-info"
        >共 <b>{{ queryParams.total || 0 }}</b> 条记录</span
      >
      <CubeListPager
        :total="queryParams.total"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </div>

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
              v-if="formType === 'edit1111'"
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
import { request } from 'cube-front/core/utils/request';
import {
  apiDataToList,
  apiDataToSingle,
  handleDeleteOperation,
  handleFormSubmit,
} from 'cube-front/core/utils/api-helpers';
import CubeListPager from 'cube-front/core/components/CubeListPager.vue';
import type { SelectOption, BaseEntity, EnableStatus } from 'cube-front/core/types/common';
import { pageInfoDefault } from 'cube-front/core/types/common';

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
  roleID: number;
  /** 部门ID */
  departmentID: number;
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
  roleID: 0,
  departmentID: 0,
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

// 页面请求参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault,
});

// 搜索关键字（绑定输入框）
const searchQ = ref('');
// 头像加载失败记录
const avatarError = ref<Record<number, boolean>>({});

// 时间友好格式化
const formatTime = (time: string): string => {
  if (!time) return '—';
  const d = new Date(time);
  if (isNaN(d.getTime())) return time;
  const diff = Date.now() - d.getTime();
  const mins = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);
  const days = Math.floor(diff / 86400000);
  if (mins < 1) return '刚刚';
  if (hours < 1) return `${mins} 分钟前`;
  if (days < 1) return `${hours} 小时前`;
  if (days < 30) return `${days} 天前`;
  return d.toLocaleDateString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit' });
};

// 搜索操作
const doSearch = () => {
  Object.assign(queryParams, { q: searchQ.value, pageIndex: 1 });
  queryUser();
};

const doReset = () => {
  searchQ.value = '';
  Object.assign(queryParams, { q: '', pageIndex: 1 });
  queryUser();
};

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
    const c = await request.get('/Admin/User/', { params: queryParams });
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
});
</script>

<style lang="scss" scoped>
/* ─── 页面容器 ─── */
.user-page {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 20px 24px;
  min-height: 100%;
  background: var(--bg-primary);
  color: var(--text-primary);
}

/* ─── 页头 ─── */
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 24px;
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  position: relative;
  overflow: hidden;

  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 1px;
    background: linear-gradient(90deg, transparent 0%, var(--accent) 50%, transparent 100%);
    opacity: 0.5;
  }
}

.header-brand {
  display: flex;
  align-items: center;
  gap: 14px;
}

.brand-icon {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  background: rgba(74, 222, 128, 0.08);
  border: 1px solid rgba(74, 222, 128, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--accent);
  flex-shrink: 0;
}

.brand-sup {
  font-size: 11px;
  color: var(--text-muted);
  letter-spacing: 0.5px;
  margin: 0 0 3px;
}

.brand-title {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  letter-spacing: -0.2px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.count-stat {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  margin-right: 6px;
}

.count-num {
  font-size: 24px;
  font-weight: 800;
  color: var(--accent);
  line-height: 1;
  font-variant-numeric: tabular-nums;
  letter-spacing: -1px;
}

.count-label {
  font-size: 10px;
  color: var(--text-muted);
  margin-top: 1px;
}

.hdr-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  font-size: 13px;
  font-weight: 500;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all 0.15s;
  border: 1px solid var(--border-subtle);
  background: transparent;
  color: var(--text-secondary);

  &:hover {
    background: rgba(255, 255, 255, 0.05);
    color: var(--text-primary);
    border-color: rgba(255, 255, 255, 0.12);
  }

  &.prime {
    background: var(--accent);
    border-color: var(--accent);
    color: #0a0e14;
    font-weight: 600;

    &:hover {
      filter: brightness(1.1);
      box-shadow: 0 0 18px rgba(74, 222, 128, 0.35);
    }
  }
}

/* ─── 搜索栏 ─── */
.search-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
}

.search-input-wrap {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 12px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  transition:
    border-color 0.15s,
    background 0.15s;

  &:focus-within {
    border-color: rgba(74, 222, 128, 0.5);
    background: rgba(74, 222, 128, 0.04);
  }
}

.search-icon {
  color: var(--text-muted);
  flex-shrink: 0;
}

.search-input {
  flex: 1;
  background: transparent;
  border: none;
  outline: none;
  padding: 10px 0;
  font-size: 13px;
  color: var(--text-primary);

  &::placeholder {
    color: var(--text-muted);
  }
}

.search-clear {
  background: transparent;
  border: none;
  cursor: pointer;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1;
  padding: 3px 5px;
  border-radius: 4px;

  &:hover {
    color: var(--text-primary);
  }
}

.search-btn {
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 500;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all 0.15s;
  white-space: nowrap;

  &.primary {
    background: var(--accent);
    border: 1px solid var(--accent);
    color: #0a0e14;

    &:hover {
      filter: brightness(1.1);
      box-shadow: 0 0 12px rgba(74, 222, 128, 0.3);
    }
  }

  &.ghost {
    background: transparent;
    border: 1px solid var(--border-subtle);
    color: var(--text-secondary);

    &:hover {
      background: rgba(255, 255, 255, 0.05);
      color: var(--text-primary);
    }
  }
}

/* ─── 数据面板 ─── */
.data-panel {
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  overflow: hidden;
  overflow-x: auto;
  flex: 1;
}

.row-head {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr 1.6fr 80px 96px 118px;
  padding: 10px 20px;
  background: rgba(255, 255, 255, 0.02);
  border-bottom: 1px solid var(--border-subtle);

  .col {
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 0.3px;
    color: var(--text-muted);
    white-space: nowrap;
  }
}

.rows-body {
  display: contents;
}

.data-row {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr 1.6fr 80px 96px 118px;
  padding: 13px 20px;
  border-bottom: 1px solid var(--border-subtle);
  transition: background 0.12s;
  align-items: center;

  &:last-child {
    border-bottom: none;
  }

  &:hover {
    background: rgba(74, 222, 128, 0.03);

    .op-btn {
      opacity: 1;
    }
  }

  &.is-off {
    opacity: 0.5;
  }
}

.col {
  display: flex;
  align-items: center;
}
.col-user {
  gap: 12px;
}

/* ─── 头像 ─── */
.avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
  }

  &.avatar--male {
    background: linear-gradient(135deg, #0ea5e9 0%, #38bdf8 100%);
  }
  &.avatar--female {
    background: linear-gradient(135deg, #f43f5e 0%, #fb7185 100%);
  }
}

.avatar-init {
  font-size: 14px;
  font-weight: 700;
  color: #fff;
  line-height: 1;
  text-transform: uppercase;
}

.user-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.user-dname {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-uname {
  font-size: 11px;
  color: var(--text-muted);
  font-family: 'SFMono-Regular', Consolas, 'Cascadia Code', monospace;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ─── 角色 ─── */
.chip--role {
  display: inline-flex;
  padding: 3px 10px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 600;
  background: rgba(74, 222, 128, 0.1);
  color: var(--accent);
  border: 1px solid rgba(74, 222, 128, 0.2);
  white-space: nowrap;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
}

.dept-tag {
  font-size: 12px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 100%;
}

.nil {
  color: var(--text-muted);
  font-size: 14px;
}

/* ─── 联系方式 ─── */
.col-contact {
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;
}

.contact-line {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  color: var(--text-secondary);
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;

  svg {
    flex-shrink: 0;
    color: var(--text-muted);
  }
}

/* ─── 状态 ─── */
.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 500;

  &.status-ok {
    color: var(--accent);

    .status-dot {
      background: var(--accent);
      box-shadow: 0 0 6px rgba(74, 222, 128, 0.8);
      animation: dot-pulse 2.5s infinite;
    }
  }

  &.status-off {
    color: var(--text-muted);
    .status-dot {
      background: var(--text-muted);
    }
  }
}

.status-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  flex-shrink: 0;
  display: inline-block;
}

@keyframes dot-pulse {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.6;
    transform: scale(1.3);
  }
}

.time-val {
  font-size: 12px;
  color: var(--text-muted);
  white-space: nowrap;
}

/* ─── 操作按钮 ─── */
.col-ops {
  gap: 6px;
}

.op-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 5px 10px;
  font-size: 12px;
  font-weight: 500;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.12s;
  opacity: 0.8;
  white-space: nowrap;

  &.edit-btn {
    background: rgba(74, 222, 128, 0.07);
    border: 1px solid rgba(74, 222, 128, 0.18);
    color: var(--accent);

    &:hover {
      background: rgba(74, 222, 128, 0.14);
      border-color: var(--accent);
      opacity: 1;
    }
  }

  &.del-btn {
    background: rgba(251, 113, 133, 0.07);
    border: 1px solid rgba(251, 113, 133, 0.18);
    color: #fb7185;

    &:hover {
      background: rgba(251, 113, 133, 0.14);
      border-color: #fb7185;
      opacity: 1;
    }
  }
}

/* ─── 空状态 ─── */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
  padding: 64px 20px;
  color: var(--text-muted);

  p {
    font-size: 14px;
    margin: 0;
  }
}

/* ─── 分页条 ─── */
.pager-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
}

.pager-info {
  font-size: 13px;
  color: var(--text-muted);

  b {
    color: var(--text-primary);
    font-weight: 600;
  }
}

/* ─── 行过渡动画 ─── */
.rows-enter-active,
.rows-leave-active {
  transition:
    opacity 0.18s ease,
    transform 0.18s ease;
}
.rows-enter-from,
.rows-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* ─── 弹窗底部 ─── */
.dialog-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
