<template>
  <div class="department-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('dept.title') }}</h3>
          <el-button type="primary" @click="handleAdd">{{ t('dept.add') }}</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
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
        <el-table-column prop="name" :label="t('dept.name')" min-width="140" show-overflow-tooltip />
        <el-table-column prop="code" :label="t('dept.code')" width="110" show-overflow-tooltip />
        <el-table-column :label="t('dept.fullName')" min-width="160" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.fullName || '-' }}
          </template>
        </el-table-column>
        <el-table-column :label="t('dept.parent')" min-width="110" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.parentName || '-' }}
          </template>
        </el-table-column>
        <el-table-column :label="t('dept.path')" min-width="180" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.path || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="level" :label="t('dept.level')" width="60" align="center" />
        <el-table-column prop="sort" :label="t('common.sort')" width="60" align="center" />
        <el-table-column :label="t('common.enable')" width="70" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'" size="small">
              {{ scope.row.enable ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('dept.visible')" width="70" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.visible ? 'success' : 'danger'" size="small">
              {{ scope.row.visible ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('dept.manager')" width="90" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.managerName || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="createUser" :label="t('dept.createUser')" width="90" show-overflow-tooltip />
        <el-table-column prop="createTime" :label="t('dept.createTime')" width="150" show-overflow-tooltip />
        <el-table-column :label="t('common.remark')" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column :label="t('common.actions')" width="150" fixed="right" align="center">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">{{ t('button.edit') }}</el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">{{ t('button.delete') }}</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 显示统计信息 -->
      <div class="table-footer">
        <span>{{ t('dept.total', { count: total }) }}</span>
      </div>
    </el-card>

    <!-- 部门表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? t('dept.add') : t('dept.edit')" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item :label="t('dept.name')" prop="name">
          <el-input v-model="form.name" :placeholder="t('form.placeholder', { field: t('dept.name') })" />
        </el-form-item>
        <el-form-item :label="t('dept.code')" prop="code">
          <el-input v-model="form.code" :placeholder="t('form.placeholder', { field: t('dept.code') })" />
        </el-form-item>
        <el-form-item :label="t('dept.fullName')" prop="fullName">
          <el-input v-model="form.fullName" :placeholder="t('form.placeholder', { field: t('dept.fullName') })" />
        </el-form-item>
        <el-form-item :label="t('dept.parent')" prop="parentID">
          <el-select v-model="form.parentID" :placeholder="t('form.select.placeholder', { field: t('dept.parent') })" clearable filterable>
            <el-option :label="t('dept.noParent')" :value="0" />
            <el-option
              v-for="dept in departmentOptions"
              :key="dept.id"
              :label="`${dept.name}（${dept.code || dept.id}）`"
              :value="dept.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('common.sort')" prop="sort">
          <el-input-number v-model="form.sort" :min="0" :placeholder="t('dept.sortInLevel')" />
        </el-form-item>
        <el-form-item :label="t('dept.manager')" prop="managerId">
          <el-select v-model="form.managerId" :placeholder="t('form.select.placeholder', { field: t('dept.manager') })" clearable filterable>
            <el-option :label="t('dept.noManager')" :value="0" />
            <!-- 这里需要用户列表，暂时留空 -->
          </el-select>
        </el-form-item>
        <el-form-item :label="t('dept.currentPath')" v-if="formType === 'edit'">
          <el-input :value="form.path" readonly />
        </el-form-item>
        <el-form-item :label="t('dept.createInfo')" v-if="formType === 'edit'">
          <el-input :value="`${form.createUser} (${form.createTime})`" readonly />
        </el-form-item>
        <el-form-item :label="t('common.enable')" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('dept.visible')" prop="visible">
          <el-switch v-model="form.visible" :active-value="true" :inactive-value="false" />
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
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

const { t } = useI18n();

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
    { required: true, message: t('form.placeholder', { field: t('dept.name') }), trigger: 'blur' }
  ],
  code: [
    { required: true, message: t('form.placeholder', { field: t('dept.code') }), trigger: 'blur' }
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
    console.error(t('dept.loadFailed'), error);
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
    () => request.delete(`/Admin/Department/${row.id}`),
    loadData,
    t('dept.deleteConfirm')
  );
};

// 提交表单
const submitForm = async () => {
  // 清理列表行原始字段回传造成的键冲突：行数据同时含 camelCase 开关
  // （enable/visible）与 PascalCase 旧字段（Enable/Visible 整型），handleEdit 整行
  // Object.assign 后表单同时携带两组键。后端大小写不敏感绑定下靠后的 PascalCase 旧值
  // 会覆盖开关新值（如 Visible:1 覆盖 visible:false），导致“编辑启用/可见性不生效”。
  const payload: Record<string, unknown> = { ...form };
  delete payload.Enable;
  delete payload.Visible;

  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/Department', payload);
    } else {
      await request.put(`/Admin/Department/${payload.id}`, payload);
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
