<template>
  <div class="department-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>部门管理</h3>
          <el-button type="primary" @click="handleAdd">新增部门</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-alert
        :title="`共找到 ${total} 个部门，其中根部门 ${tableData.length} 个。`"
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom: 16px"
      />

      <el-table
        :data="tableData"
        border
        style="width: 100%"
        v-loading="loading"
        row-key="id"
        default-expand-all
        :tree-props="{ children: 'children' }"
        :show-header="true"
        :highlight-current-row="true">
        <el-table-column prop="name" label="部门名称" min-width="140" show-overflow-tooltip />
        <el-table-column prop="code" label="部门代码" width="110" show-overflow-tooltip />
        <el-table-column label="部门全名" min-width="160" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.fullName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="上级部门" min-width="110" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.parentName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="部门路径" min-width="180" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.path || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="level" label="层级" width="60" align="center" />
        <el-table-column prop="sort" label="排序" width="60" align="center" />
        <el-table-column label="启用" width="70" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'" size="small">
              {{ scope.row.enable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="可见" width="70" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.visible ? 'success' : 'danger'" size="small">
              {{ scope.row.visible ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="管理者" width="90" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.managerName || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="createUser" label="创建者" width="90" show-overflow-tooltip />
        <el-table-column prop="createTime" label="创建时间" width="150" show-overflow-tooltip />
        <el-table-column label="备注" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right" align="center">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">编辑</el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 显示统计信息 -->
      <div class="table-footer">
        <span>共 {{ total }} 个部门</span>
      </div>
    </el-card>

    <!-- 部门表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? '新增部门' : '编辑部门'" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="部门名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入部门名称" />
        </el-form-item>
        <el-form-item label="部门代码" prop="code">
          <el-input v-model="form.code" placeholder="请输入部门代码" />
        </el-form-item>
        <el-form-item label="部门全名" prop="fullName">
          <el-input v-model="form.fullName" placeholder="请输入部门全名" />
        </el-form-item>
        <el-form-item label="上级部门" prop="parentID">
          <el-select v-model="form.parentID" placeholder="请选择上级部门" clearable filterable>
            <el-option label="无上级" :value="0" />
            <el-option
              v-for="dept in departmentOptions"
              :key="dept.id"
              :label="`${dept.name}（${dept.code || dept.id}）`"
              :value="dept.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" placeholder="同级内排序" />
        </el-form-item>
        <el-form-item label="管理者" prop="managerId">
          <el-select v-model="form.managerId" placeholder="请选择管理者" clearable filterable>
            <el-option label="无管理者" :value="0" />
            <!-- 这里需要用户列表，暂时留空 -->
          </el-select>
        </el-form-item>
        <el-form-item label="当前路径" v-if="formType === 'edit'">
          <el-input :value="form.path" readonly />
        </el-form-item>
        <el-form-item label="创建信息" v-if="formType === 'edit'">
          <el-input :value="`${form.createUser} (${form.createTime})`" readonly />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="可见状态" prop="visible">
          <el-switch v-model="form.visible" :active-value="true" :inactive-value="false" />
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
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 定义部门类型接口
interface Department extends BaseEntity {
  tenantId: number;
  code: string;
  name: string;
  fullName: string | null;
  parentID: number;
  level: number;
  sort: number;
  enable: boolean;
  visible: boolean;
  managerId: number;
  ex1: number;
  ex2: number;
  ex3: number;
  ex4: string | null;
  ex5: string | null;
  ex6: string | null;
  createUserID: number;
  createIP: string;
  updateUserID: number;
  updateIP: string;
  tenantName: string | null;
  managerName: string | null;
  parentName: string | null;
  parentPath: string | null;
  path: string;
  children?: Department[];
  hasChildren?: boolean;
}

// 表格数据
const tableData = ref<Department[]>([]);
const loading = ref(false);
const total = ref(0);
const departmentOptions = ref<Department[]>([]);

// 页面请求参数
const queryParams = reactive({
  q: '',// 搜索关键字
  pageIndex: 1,
  pageSize: 10000, // 获取全部数据用于构建树
});

// 表单相关
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const form = reactive<Department>({
  id: 0,
  tenantId: 0,
  code: '',
  name: '',
  fullName: null,
  parentID: 0,
  level: 0,
  sort: 0,
  enable: true,
  visible: true,
  managerId: 0,
  ex1: 0,
  ex2: 0,
  ex3: 0,
  ex4: null,
  ex5: null,
  ex6: null,
  createUser: '',
  createUserID: 0,
  createIP: '',
  createTime: '',
  updateUser: '',
  updateUserID: 0,
  updateIP: '',
  updateTime: '',
  remark: '',
  tenantName: null,
  managerName: null,
  parentName: null,
  parentPath: null,
  path: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: '请输入部门名称', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '请输入部门代码', trigger: 'blur' }
  ]
});

// 数据预处理函数，确保null值被正确处理
const preprocessDepartmentData = (data: Department[]): Department[] => {
  return data.map(item => ({
    ...item,
    fullName: item.fullName || '',
    remark: item.remark || '',
    managerName: item.managerName || '',
    parentName: item.parentName || '',
    parentPath: item.parentPath || '',
    ex4: item.ex4 || '',
    ex5: item.ex5 || '',
    ex6: item.ex6 || '',
    tenantName: item.tenantName || '',
  }));
};

// 构建树结构并排序（参考菜单页面的实现）
const buildTreeData = (data: Department[]): Department[] => {
  if (!data || data.length === 0) {
    return [];
  }

  // 创建节点映射表和根节点数组
  const nodeMap = new Map<number, Department>();
  const roots: Department[] = [];

  // 步骤1：初始化所有节点
  data.forEach((item) => {
    const node = { ...item };
    // 确保清理之前的树状属性
    node.children = [];
    delete node.hasChildren;
    nodeMap.set(item.id, node);
  });

  // 步骤2：建立父子关系
  data.forEach((item) => {
    const node = nodeMap.get(item.id)!;

    if (item.parentID === 0) {
      // parentID为0的是根节点
      roots.push(node);
    } else {
      // 找到父节点并建立关系
      const parent = nodeMap.get(item.parentID);
      if (parent) {
        if (!parent.children) {
          parent.children = [];
        }
        parent.children.push(node);
      } else {
        // 如果找不到父节点，作为根节点处理
        roots.push(node);
      }
    }
  });

  // 步骤3：清理空的children数组并排序
  const processNode = (node: Department) => {
    if (node.children && node.children.length === 0) {
      delete node.children;
    } else if (node.children && node.children.length > 0) {
      // 对子节点排序
      node.children.sort((a, b) => a.sort - b.sort);
      // 递归处理子节点
      node.children.forEach((child) => processNode(child));
    }
  };

  // 对根节点排序
  roots.sort((a, b) => a.sort - b.sort);

  // 处理所有节点
  roots.forEach((root) => processNode(root));

  return roots;
};

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  console.log(e?.type, e?.params);
  const query = Object.assign(queryParams, e?.params || {});
  console.log('queryParams:', query);
  loadData();
};

// 搜索数据处理
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e || {});
  console.log('SearchData:', queryParams);
};

// 重置数据处理
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e || {});
  console.log('ResetData:', queryParams);
};

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Admin/Department', {
      params: queryParams
    });

    let dataList: Department[] = [];

    // 处理API响应数据
    const { list } = apiDataToList<Department>(response);
    dataList = preprocessDepartmentData(list);

    // 构建树结构并排序
    tableData.value = buildTreeData(dataList);
    total.value = dataList.length;

    // 同时保存平铺的数据用于部门选项（排除当前编辑的部门）
    departmentOptions.value = dataList.filter((item) => item.id !== form.id);
  } catch (error) {
    console.error('加载部门数据失败:', error);
    tableData.value = [];
    departmentOptions.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
};

// 新增
const handleAdd = () => {
  formType.value = 'add';
  Object.assign(form, {
    id: 0,
    tenantId: 0,
    code: '',
    name: '',
    fullName: null,
    parentID: 0,
    level: 0,
    sort: 0,
    enable: true,
    visible: true,
    managerId: 0,
    ex1: 0,
    ex2: 0,
    ex3: 0,
    ex4: null,
    ex5: null,
    ex6: null,
    createUser: '',
    createUserID: 0,
    createIP: '',
    createTime: '',
    updateUser: '',
    updateUserID: 0,
    updateIP: '',
    updateTime: '',
    remark: null,
    tenantName: null,
    managerName: null,
    parentName: null,
    parentPath: null,
    path: '',
  });
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: Department) => {
  formType.value = 'edit';
  Object.assign(form, { ...row });
  // 更新部门选项，排除自己
  departmentOptions.value = departmentOptions.value.filter(item => item.id !== row.id);
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: Department) => {
  handleDeleteOperation(
    () => request.delete('/Admin/Department', { params: { id: row.id } }),
    loadData,
    '确认删除该部门吗？'
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/Department', form);
    } else {
      await request.put('/Admin/Department', form);
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

.table-footer {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
  color: #606266;
  font-size: 14px;
}
</style>
