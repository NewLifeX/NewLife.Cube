<template>
  <div class="access-rule-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>访问规则管理</h3>
          <el-button type="primary" @click="handleAdd">新增规则</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="name" label="名称" />
        <el-table-column prop="url" label="URL路径" />
        <el-table-column prop="priority" label="优先级" width="100" />
        <el-table-column prop="actionKind" label="处理方式" width="100">
          <template #default="scope">
            <el-tag :type="getActionKindType(scope.row.actionKind)">
              {{ getActionKindText(scope.row.actionKind) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="limitTimes" label="限流次数" width="100" />
        <el-table-column prop="updateTime" label="更新时间" />
        <el-table-column label="状态" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '启用' : '禁用' }}
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

    <!-- 访问规则表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? '新增访问规则' : '编辑访问规则'" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="120px">
        <el-form-item label="规则名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入规则名称" />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="优先级" prop="priority">
          <el-input-number v-model="form.priority" :min="0" :max="999" placeholder="数值越大优先级越高" />
        </el-form-item>
        <el-form-item label="URL路径" prop="url">
          <el-input v-model="form.url" placeholder="支持*模糊匹配，多个逗号隔开" />
        </el-form-item>
        <el-form-item label="用户代理" prop="userAgent">
          <el-input v-model="form.userAgent" placeholder="支持*模糊匹配，多个逗号隔开" />
        </el-form-item>
        <el-form-item label="来源IP" prop="ip">
          <el-input v-model="form.ip" placeholder="支持*模糊匹配，多个逗号隔开" />
        </el-form-item>
        <el-form-item label="登录用户" prop="loginedUser">
          <el-input v-model="form.loginedUser" placeholder="支持*模糊匹配，多个逗号隔开" />
        </el-form-item>
        <el-form-item label="处理方式" prop="actionKind">
          <el-select v-model="form.actionKind" placeholder="请选择处理方式">
            <el-option label="允许" :value="0" />
            <el-option label="拦截" :value="1" />
            <el-option label="限流" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="拦截代码" prop="blockCode" v-if="form.actionKind === 1">
          <el-input-number v-model="form.blockCode" :min="100" :max="599" placeholder="如404/500/302等" />
        </el-form-item>
        <el-form-item label="拦截内容" prop="blockContent" v-if="form.actionKind === 1">
          <el-input v-model="form.blockContent" type="textarea" placeholder="拦截时返回内容" />
        </el-form-item>
        <el-form-item label="限流维度" prop="limitDimension" v-if="form.actionKind === 2">
          <el-select v-model="form.limitDimension" placeholder="请选择限流维度">
            <el-option label="IP" :value="1" />
            <el-option label="用户" :value="2" />
            <el-option label="全局" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="限流时间" prop="limitCycle" v-if="form.actionKind === 2">
          <el-input-number v-model="form.limitCycle" :min="1" placeholder="考察时间，单位秒" />
        </el-form-item>
        <el-form-item label="限流次数" prop="limitTimes" v-if="form.actionKind === 2">
          <el-input-number v-model="form.limitTimes" :min="1" placeholder="阈值达到时执行拦截" />
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
    { required: true, message: '请输入规则名称', trigger: 'blur' }
  ],
  priority: [
    { required: true, message: '请输入优先级', trigger: 'blur' }
  ],
  actionKind: [
    { required: true, message: '请选择处理方式', trigger: 'change' }
  ]
});

// 获取处理方式文本
const getActionKindText = (actionKind: number) => {
  const map: Record<number, string> = {
    0: '允许',
    1: '拦截',
    2: '限流'
  };
  return map[actionKind] || '未知';
};

// 获取处理方式标签类型
const getActionKindType = (actionKind: number) => {
  const map: Record<number, string> = {
    0: 'success',
    1: 'danger',
    2: 'warning'
  };
  return map[actionKind] || '';
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
    () => request.delete('/Admin/AccessRule', { params: { id: row.id } }),
    loadData,
    '确认删除该访问规则吗？'
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/AccessRule', form);
    } else {
      await request.put('/Admin/AccessRule', form);
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
