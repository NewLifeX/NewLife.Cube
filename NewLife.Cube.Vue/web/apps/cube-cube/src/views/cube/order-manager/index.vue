<template>
  <div class="order-manager-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>订单管理</h3>
          <div>
            <el-button type="info" @click="handleGetInfo">查询订单信息</el-button>
            <el-button type="primary" @click="handleAdd">新增订单</el-button>
          </div>
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
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="code" label="订单编号" min-width="150" />
        <el-table-column prop="name" label="订单名称" min-width="150" />
        <el-table-column prop="amount" label="金额" width="120">
          <template #default="scope">
            ¥{{ scope.row.amount?.toFixed(2) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="scope">
            <el-tag :type="getStatusType(scope.row.status)">
              {{ getStatusText(scope.row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户" width="120" />
        <el-table-column prop="createTime" label="创建时间" min-width="150" />
        <el-table-column prop="updateTime" label="更新时间" min-width="150" />
        <el-table-column label="操作" width="160">
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

    <!-- 订单表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? '新增订单' : '编辑订单'"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="订单编号" prop="code">
          <el-input v-model="form.code" placeholder="请输入订单编号" />
        </el-form-item>
        <el-form-item label="订单名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入订单名称" />
        </el-form-item>
        <el-form-item label="金额" prop="amount">
          <el-input-number v-model="form.amount" :min="0" :precision="2" placeholder="请输入金额" />
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-select v-model="form.status" placeholder="请选择状态">
            <el-option label="待处理" :value="0" />
            <el-option label="处理中" :value="1" />
            <el-option label="已完成" :value="2" />
            <el-option label="已取消" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="客户姓名" prop="customerName">
          <el-input v-model="form.customerName" placeholder="请输入客户姓名" />
        </el-form-item>
        <el-form-item label="客户电话" prop="customerPhone">
          <el-input v-model="form.customerPhone" placeholder="请输入客户电话" />
        </el-form-item>
        <el-form-item label="客户地址" prop="customerAddress">
          <el-input v-model="form.customerAddress" placeholder="请输入客户地址" />
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

    <!-- 查询订单信息对话框 -->
    <el-dialog
      v-model="infoDialogVisible"
      title="查询订单信息"
      width="500px"
    >
      <el-form :model="infoForm" label-width="100px">
        <el-form-item label="订单编号">
          <el-input v-model="infoForm.codes" placeholder="请输入订单编号，多个用逗号分隔" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="infoDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="queryOrderInfo">查询</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 订单类型接口，继承 BaseEntity
interface OrderManager extends BaseEntity {
  code: string;
  name: string;
  amount: number;
  status: number;
  customerName: string;
  customerPhone: string;
  customerAddress: string;
  remark: string;
  createUser: string;
  updateUser: string;
}

// 响应式数据
const loading = ref(false);
const dialogVisible = ref(false);
const infoDialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const tableData = ref<OrderManager[]>([]);

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  status: undefined as number | undefined,
  dateRange: [] as [string, string] | [],
  ...pageInfoDefault
});

// 订单表单
const form = reactive<Partial<OrderManager>>({
  code: '',
  name: '',
  amount: 0,
  status: 0,
  customerName: '',
  customerPhone: '',
  customerAddress: '',
  remark: '',
});

// 查询信息表单
const infoForm = reactive({
  codes: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  code: [
    { required: true, message: '请输入订单编号', trigger: 'blur' }
  ],
  name: [
    { required: true, message: '请输入订单名称', trigger: 'blur' }
  ],
  amount: [
    { required: true, message: '请输入金额', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
});

// 获取状态类型
const getStatusType = (status: number): 'success' | 'primary' | 'warning' | 'danger' | 'info' => {
  const types: Record<number, 'success' | 'primary' | 'warning' | 'danger' | 'info'> = {
    0: 'warning',
    1: 'primary',
    2: 'success',
    3: 'danger'
  };
  return types[status] || 'info';
};

// 获取状态文本
const getStatusText = (status: number) => {
  const texts = ['待处理', '处理中', '已完成', '已取消'];
  return texts[status] || '未知';
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
    const params: Record<string, unknown> = { ...queryParams };
    if (queryParams.dateRange && queryParams.dateRange.length === 2) {
      params.startTime = queryParams.dateRange[0];
      params.endTime = queryParams.dateRange[1];
    }
    const response = await request.get('/Cube/OrderManager', { params });
    const { list, page } = apiDataToList<OrderManager>(response);
    tableData.value = list;
    queryParams.total = page?.totalCount || list.length;
  } catch (error) {
    ElMessage.error('加载数据失败');
    console.error('加载数据失败:', error);
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
  Object.assign(queryParams, { q: '', status: undefined, dateRange: [], pageIndex: 1 }, e || {});
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

// 查询订单信息
const handleGetInfo = () => {
  infoForm.codes = '';
  infoDialogVisible.value = true;
};

// 执行查询订单信息
const queryOrderInfo = async () => {
  if (!infoForm.codes.trim()) {
    ElMessage.error('请输入订单编号');
    return;
  }

  try {
    const response = await request.get(`/Cube/OrderManager/GetInfo?codes=${infoForm.codes}`);
    if (response && response.data) {
      ElMessage.success('查询成功');
      console.log('订单信息:', response.data);
      // 这里可以显示查询结果
    }
    infoDialogVisible.value = false;
  } catch (error) {
    ElMessage.error('查询失败');
    console.error('查询失败:', error);
  }
};

// 新增
const handleAdd = () => {
  formType.value = 'add';
  resetForm();
  dialogVisible.value = true;
};

// 编辑
// 编辑
const handleEdit = (row: OrderManager) => {
  formType.value = 'edit';
  Object.assign(form, row);
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: OrderManager) => {
  ElMessageBox.confirm(
    `确定要删除订单"${row.code}"吗？`,
    '确认删除',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/OrderManager?id=${row.id}`);
      ElMessage.success('删除成功');
      loadData();
    } catch (error) {
      ElMessage.error('删除失败');
      console.error('删除失败:', error);
    }
  });
};

// 重置表单
const resetForm = () => {
  Object.assign(form, {
    id: undefined,
    code: '',
    name: '',
    amount: 0,
    status: 0,
    customerName: '',
    customerPhone: '',
    customerAddress: '',
    remark: '',
  });
  formRef.value?.clearValidate();
};

// 提交表单
const submitForm = () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        if (formType.value === 'add') {
          await request.post('/Cube/OrderManager', form);
          ElMessage.success('新增成功');
        } else {
          await request.put('/Cube/OrderManager', form);
          ElMessage.success('更新成功');
        }
        dialogVisible.value = false;
        loadData();
      } catch (error) {
        ElMessage.error(`${formType.value === 'add' ? '新增' : '更新'}失败`);
        console.error('操作失败:', error);
      }
    }
  });
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
