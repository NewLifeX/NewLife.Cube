<template>
  <div class="menu-container">
    <el-card class="box-card" style="height: 100%; display: flex; flex-direction: column;">
      <template #header>
        <div class="card-header">
          <h3>菜单管理</h3>
          <div>
            <!-- <el-button type="success" @click="handleMoveUp" :disabled="selectedRows.length !== 1">
              上移
              {{
                selectedRows.length === 1
                  ? `(${selectedRows[0].displayName || selectedRows[0].name})`
                  : ''
              }}
            </el-button>
            <el-button type="warning" @click="handleMoveDown" :disabled="selectedRows.length !== 1">
              下移
              {{
                selectedRows.length === 1
                  ? `(${selectedRows[0].displayName || selectedRows[0].name})`
                  : ''
              }}
            </el-button> -->
            <el-button type="primary" @click="handleAdd">新增菜单</el-button>
          </div>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-alert
        :title="`共找到 ${total} 个菜单项，其中根菜单 ${tableData.length} 个。`"
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom: 16px"
      />

      <div class="table-wrapper">
        <el-table
          :data="tableData"
          border
          style="width: 100%"
          v-loading="loading"
          row-key="id"
          default-expand-all
          :tree-props="{ children: 'children' }"
          @selection-change="handleSelectionChange"
          :show-header="true"
          :highlight-current-row="true"
        >
        <el-table-column type="selection" width="55" />
        <el-table-column prop="id" label="编号" width="85" />
        <el-table-column label="名称" min-width="150">
          <template #default="scope">
            <span>{{ getDepthPrefix((scope.row as any)._depth ?? 0) }}{{ scope.row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column label="显示名" min-width="150">
          <template #default="scope">
            <span>{{ getDepthPrefix((scope.row as any)._depth ?? 0) }}{{ scope.row.displayName }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="url" label="链接" min-width="150" />
        <el-table-column prop="sort" label="排序" width="80" />
        <el-table-column prop="icon" label="图标" width="100">
          <template #default="scope">
            <i v-if="scope.row.icon" :class="scope.row.icon"></i>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="updateTime" label="更新时间" min-width="150" />
        <el-table-column label="可见" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.visible ? 'success' : 'danger'">
              {{ scope.row.visible ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="必要" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.necessary ? 'warning' : undefined">
              {{ scope.row.necessary ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="新窗口" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.newWindow ? 'info' : undefined">
              {{ scope.row.newWindow ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="备注" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">编辑</el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">删除</el-button>
          </template>
        </el-table-column>
        </el-table>
      </div>
    </el-card>

    <!-- 菜单表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? '新增菜单' : '编辑菜单'"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="菜单名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入菜单名称" />
        </el-form-item>
        <el-form-item label="显示名称" prop="displayName">
          <el-input v-model="form.displayName" placeholder="请输入显示名称" />
        </el-form-item>
        <el-form-item label="菜单全名" prop="fullName">
          <el-input v-model="form.fullName" placeholder="请输入菜单全名" />
        </el-form-item>
        <el-form-item label="上级菜单" prop="parentID">
          <el-select v-model="form.parentID" placeholder="请选择上级菜单" clearable filterable>
            <el-option label="无上级" :value="0" />
            <el-option
              v-for="menu in menuOptions"
              :key="menu.id"
              :label="menu.displayName || menu.name"
              :value="menu.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="链接地址" prop="url">
          <el-input v-model="form.url" placeholder="请输入链接地址" />
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" placeholder="同级内排序" />
        </el-form-item>
        <el-form-item label="图标" prop="icon">
          <el-input v-model="form.icon" placeholder="请输入图标类名" />
        </el-form-item>
        <el-form-item label="可见状态" prop="visible">
          <el-switch v-model="form.visible" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="必要菜单" prop="necessary">
          <el-switch v-model="form.necessary" :active-value="true" :inactive-value="false" />
          <span class="form-tip">必要的菜单，必须至少有角色拥有这些权限</span>
        </el-form-item>
        <el-form-item label="新窗口打开" prop="newWindow">
          <el-switch v-model="form.newWindow" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="权限子项" prop="permission">
          <el-input
            v-model="form.permission"
            type="textarea"
            placeholder="逗号分隔，每个权限子项名值竖线分隔"
          />
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
import { handleDeleteOperation, handleFormSubmit } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 定义菜单类型接口
interface Menu extends BaseEntity {
  name: string;
  displayName: string;
  fullName: string;
  parentID: number;
  url: string;
  sort: number;
  icon: string;
  visible: boolean;
  necessary: boolean;
  newWindow: boolean;
  permission: string;
  ex1: number;
  ex2: number;
  ex3: number;
  ex4: string;
  ex5: string;
  ex6: string;
  createUserID: number;
  createIP: string;
  updateUserID: number;
  updateIP: string;
  children?: Menu[];
  hasChildren?: boolean;
}

// 表格数据
const tableData = ref<Menu[]>([]);
const loading = ref(false);
const total = ref(0);
const menuOptions = ref<Menu[]>([]);
const selectedRows = ref<Menu[]>([]);

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
const form = reactive<Menu>({
  id: 0,
  name: '',
  displayName: '',
  fullName: '',
  parentID: 0,
  url: '',
  sort: 0,
  icon: '',
  visible: true,
  necessary: false,
  newWindow: false,
  permission: '',
  ex1: 0,
  ex2: 0,
  ex3: 0,
  ex4: '',
  ex5: '',
  ex6: '',
  createUser: '',
  createUserID: 0,
  createIP: '',
  createTime: '',
  updateUser: '',
  updateUserID: 0,
  updateIP: '',
  updateTime: '',
  remark: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [{ required: true, message: '请输入菜单名称', trigger: 'blur' }],
  displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }],
});

// 构建树结构并排序（使用队列，非递归方式）
const buildTreeData = (data: Menu[]): Menu[] => {
  if (!data || data.length === 0) {
    return [];
  }

  // 创建节点映射表和根节点数组
  const nodeMap = new Map<number, Menu>();
  const roots: Menu[] = [];

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

  // 步骤3：清理空的children数组、排序并标记层级深度
  const processNode = (node: Menu, depth: number) => {
    (node as any)._depth = depth;
    if (node.children && node.children.length === 0) {
      delete node.children;
    } else if (node.children && node.children.length > 0) {
      // 对子节点排序
      node.children.sort((a, b) => a.sort - b.sort);
      // 递归处理子节点，深度+1
      node.children.forEach((child) => processNode(child, depth + 1));
    }
  };

  // 对根节点排序
  roots.sort((a, b) => a.sort - b.sort);

  // 处理所有节点（根节点深度为0）
  roots.forEach((root) => processNode(root, 0));

  console.log('构建树状结构完成:', {
    原始数据数量: data.length,
    根节点数量: roots.length,
    树结构: roots,
  });

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

// 根据层级深度生成前缀标识
const getDepthPrefix = (depth: number): string => {
  if (depth <= 0) return '';
  return '| - '.repeat(depth);
};

// 选择变化处理
const handleSelectionChange = (selection: Menu[]) => {
  selectedRows.value = selection;
};

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Admin/Menu', {
      params: queryParams,
    });

    let dataList: Menu[] = [];

    // 处理API响应数据
    if (Array.isArray(response)) {
      // 直接返回数组的响应
      dataList = response;
    } else if (response && typeof response === 'object' && 'data' in response) {
      // 标准API响应格式，从data字段取数据
      dataList = Array.isArray(response.data) ? response.data : [];
    } else {
      dataList = [];
    }

    console.log('菜单原始数据:', dataList);

    // 构建树结构并排序
    tableData.value = buildTreeData(dataList);
    total.value = dataList.length;

    console.log('构建后的树状数据:', tableData.value);

    // 同时保存平铺的数据用于菜单选项（排除当前编辑的菜单）
    menuOptions.value = dataList.filter((item) => item.id !== form.id);
  } catch {
    tableData.value = [];
    menuOptions.value = [];
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
    name: '',
    displayName: '',
    fullName: '',
    parentID: 0,
    url: '',
    sort: 0,
    icon: '',
    visible: true,
    necessary: false,
    newWindow: false,
    permission: '',
    ex1: 0,
    ex2: 0,
    ex3: 0,
    ex4: '',
    ex5: '',
    ex6: '',
    createUserID: 0,
    createIP: '',
    updateUserID: 0,
    updateIP: '',
    remark: '',
  });
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: Menu) => {
  formType.value = 'edit';
  Object.assign(form, { ...row });
  // 更新菜单选项，排除自己
  menuOptions.value = menuOptions.value.filter((item) => item.id !== row.id);
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: Menu) => {
  handleDeleteOperation(
    () => request.delete('/Admin/Menu', { params: { id: row.id } }),
    loadData,
    '确认删除该菜单吗？'
  );
};

// 提交表单
const submitForm = async () => {
  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/Menu', form);
    } else {
      await request.put('/Admin/Menu', form);
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
.menu-container {
  height: 100%;
}

.box-card {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.box-card :deep(.el-card__body) {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
}

.table-wrapper {
  flex: 1;
  overflow: auto;
  min-height: 0;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}

.form-tip {
  font-size: 12px;
  color: #999;
  margin-left: 10px;
}

/* 树状表格样式优化 */
:deep(.el-table .el-table__row) {
  cursor: pointer;
}

:deep(.el-table .el-table__row:hover) {
  background-color: #f5f7fa;
}

:deep(.el-table__expand-icon) {
  color: #409eff;
}

:deep(.el-table__indent) {
  padding-left: 20px;
}

/* 选中行样式 */
:deep(.el-table__row.is-selected) {
  background-color: #ecf5ff;
}
</style>
