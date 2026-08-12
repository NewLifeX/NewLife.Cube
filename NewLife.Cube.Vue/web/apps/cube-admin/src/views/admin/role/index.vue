<template>
  <div class="role-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>角色管理</h3>
          <el-button type="primary" @click="handleAdd">新增角色</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="编号" width="80" />
        <el-table-column prop="name" label="角色名称" />
        <el-table-column prop="tenantName" label="租户" />
        <el-table-column prop="sort" label="排序" width="80" />
        <el-table-column prop="updateTime" label="更新时间" />
        <el-table-column label="启用" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="系统角色" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.isSystem ? 'warning' : undefined">
              {{ scope.row.isSystem ? '是' : '否' }}
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
            <el-button type="danger" size="small" @click="handleDelete(scope.row)" :disabled="scope.row.isSystem">删除</el-button>
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

    <!-- 角色表单对话框 -->
    <el-dialog v-model="dialogVisible" :title="formType === 'add' ? '新增角色' : '编辑角色'" width="720px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="角色名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入角色名称" :disabled="form.isSystem" />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="系统角色" prop="isSystem">
          <el-switch v-model="form.isSystem" :active-value="true" :inactive-value="false" :disabled="formType === 'edit'" />
          <span class="form-tip">系统角色不受数据权限约束，禁止修改名称或删除</span>
        </el-form-item>
        <el-form-item label="租户" prop="tenantId">
          <el-select v-model="form.tenantId" placeholder="请选择租户" style="width: 100%">
            <el-option label="全局角色" :value="0" />
            <el-option
              v-for="tenant in tenantOptions"
              :key="tenant.value"
              :label="tenant.label"
              :value="tenant.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" placeholder="排序值" />
        </el-form-item>
        <el-form-item label="菜单权限" prop="permission">
          <div class="perm-tree-wrap">
            <div class="perm-tree-actions">
              <el-button size="small" @click="checkAllMenus">全部选中</el-button>
              <el-button size="small" @click="uncheckAllMenus">全部取消</el-button>
              <el-button size="small" @click="expandAll">展开全部</el-button>
              <el-button size="small" @click="collapseAll">收起全部</el-button>
            </div>
            <el-tree
              ref="menuTreeRef"
              :data="menuTree"
              show-checkbox
              node-key="id"
              :default-expanded-keys="defaultExpandedKeys"
              :props="{
                label: 'displayName',
                children: 'children'
              }"
              class="perm-tree"
            >
              <template #default="{ data }">
                <span class="perm-tree-node">
                  <span>{{ data.displayName || data.name }}</span>
                  <span v-if="data.icon" class="perm-tree-icon">{{ data.icon }}</span>
                  <span v-if="data.isPermission" class="perm-tag">权限</span>
                </span>
              </template>
            </el-tree>
          </div>
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
import { ref, reactive, onMounted, nextTick } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList, apiDataToSingle, handleDeleteOperation, handleFormSubmit } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';

// 定义角色类型接口
interface Role {
  id: number;
  name: string;
  enable: boolean;
  isSystem: boolean;
  tenantId?: number;
  permission: string;
  sort: number;
  ex1: number;
  ex2: number;
  ex3: number;
  ex4: string;
  ex5: string;
  ex6: string;
  createUser: string;
  createUserID: number;
  createIP: string;
  createTime: string;
  updateUser: string;
  updateUserID: number;
  updateIP: string;
  updateTime: string;
  remark: string;
  tenantName: string;
}

// 菜单树节点接口
interface MenuTreeNode {
  id: number;
  name: string;
  displayName: string;
  parentID?: number;
  icon?: string;
  children?: MenuTreeNode[];
  /** 是否为权限子项（非真实菜单） */
  isPermission?: boolean;
  /** 权限标识值（如 1=Detail, 2=Insert, 4=Update, 8=Delete） */
  permFlag?: number;
  /** 所属菜单ID（权限子项专用） */
  menuId?: number;
}

// 租户选项数据
const tenantOptions = ref<{ value: number; label: string }[]>([]);
const tenantOptionsLoaded = ref(false);

// 加载租户选项
const loadTenantOptions = async () => {
  if (tenantOptionsLoaded.value) return;
  try {
    const data = await request.get('/Admin/Tenant');
    const { list } = apiDataToList<{ id: number; name: string; code: string }>(data);
    if (list && list.length > 0) {
      tenantOptions.value = list.map((t: { id: number; name: string }) => ({
        value: t.id,
        label: t.name,
      }));
    }
    tenantOptionsLoaded.value = true;
  } catch (error) {
    console.error('加载租户数据失败:', error);
    tenantOptions.value = [];
    tenantOptionsLoaded.value = false;
  }
};

// 菜单树组件引用
const menuTreeRef = ref<any>(null);

// 菜单树数据
const menuTree = ref<MenuTreeNode[]>([]);
const defaultExpandedKeys = ref<number[]>([]);
const menuTreeLoaded = ref(false);

// 当前角色已有的权限映射 { menuId: PermissionFlags }
const rolePermissions = ref<Record<number, number>>({});

// 表格数据
const tableData = ref<Role[]>([]);
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
const form = reactive<Role>({
  id: 0,
  name: '',
  enable: true,
  isSystem: false,
  tenantId: undefined,
  permission: '',
  sort: 0,
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
  tenantName: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: '请输入角色名称', trigger: 'blur' }
  ],
  tenantId: [
    { required: true, message: '请选择租户', trigger: 'change' }
  ]
});

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
    const data = await request.get('/Admin/Role', {
      params: queryParams
    });

    const { list, page } = apiDataToList<Role>(data);
    tableData.value = list;
    queryParams.total = page?.totalCount; // 更新总数
  } catch {
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.value = false;
  }
};

// ─── 菜单树与权限配置 ───

// 标准 PermissionFlags 名称
const permissionFlagNames: Record<number, string> = {
  1: '查看',
  2: '新增',
  4: '修改',
  8: '删除',
};

// 将扁平菜单列表转换为树形结构（含权限子项）
const buildMenuTree = (items: MenuTreeNode[]): MenuTreeNode[] => {
  const nodeMap = new Map<number, MenuTreeNode>();
  const roots: MenuTreeNode[] = [];
  const flagNames = permissionFlagNames;

  // 初始化所有节点
  for (const item of items) {
    nodeMap.set(item.id, {
      id: item.id,
      name: item.name,
      displayName: item.displayName || item.name,
      parentID: item.parentID,
      icon: item.icon,
      children: [],
    });
  }

  // 建立父子关系
  for (const item of items) {
    const node = nodeMap.get(item.id)!;
    const pid = item.parentID ?? 0;

    if (pid === 0) {
      roots.push(node);
    } else {
      const parent = nodeMap.get(pid);
      if (parent) {
        if (!parent.children) parent.children = [];
        parent.children.push(node);
      } else {
        roots.push(node);
      }
    }
  }

  // 为叶节点添加权限子项
  const addPermissionChildren = (nodes: MenuTreeNode[]) => {
    for (const node of nodes) {
      if (node.children && node.children.length > 0) {
        // 递归处理子节点
        addPermissionChildren(node.children);
      } else {
        // 叶节点：添加权限子项
        const permChildren: MenuTreeNode[] = [];
        for (const [flagKey, flagName] of Object.entries(flagNames)) {
          permChildren.push({
            id: -(node.id * 100 + parseInt(flagKey)), // 负数 ID 避免冲突
            name: flagName,
            displayName: flagName,
            parentID: node.id,
            isPermission: true,
            permFlag: parseInt(flagKey),
            menuId: node.id,
          });
        }
        node.children = permChildren;
      }
    }
  };

  addPermissionChildren(roots);

  return roots;
};

// 加载菜单树数据
const loadMenuTree = async () => {
  if (menuTreeLoaded.value) return;
  try {
    const data = await request.get('/Admin/Menu');
    const { list } = apiDataToList<MenuTreeNode>(data);
    if (list && list.length > 0) {
      // 构建树
      menuTree.value = buildMenuTree(list);
      // 默认展开第1级
      defaultExpandedKeys.value = menuTree.value.map(n => n.id);
    }
    menuTreeLoaded.value = true;
  } catch (error) {
    console.error('加载菜单树失败:', error);
    menuTree.value = [];
    menuTreeLoaded.value = false;
  }
};

// 解析角色权限字符串，设置树选中状态
// 权限字符串格式： "MenuID#PermissionFlags,MenuID#PermissionFlags"  例如 "1#1,2#7,3#15"
const applyPermissionToTree = (permStr: string) => {
  const checkedKeys: number[] = [];
  const perms: Record<number, number> = {};

  if (permStr) {
    for (const part of permStr.split(',')) {
      const [menuIdStr, flagStr] = part.split('#');
      const menuId = parseInt(menuIdStr);
      const flags = parseInt(flagStr || '0');
      if (isNaN(menuId)) continue;
      perms[menuId] = flags;
      // 菜单本身选中
      checkedKeys.push(menuId);
    }
  }

  rolePermissions.value = perms;

  // 设置权限子项选中
  nextTick(() => {
    const tree = menuTreeRef.value;
    if (!tree) return;

    // 先清空所有选中
    tree.setCheckedKeys([]);

    // 设置菜单选中
    for (const menuId of checkedKeys) {
      tree.setChecked(menuId, true, false);
    }

    // 设置权限子项选中
    for (const [menuIdStr, flags] of Object.entries(perms)) {
      const menuId = parseInt(menuIdStr);
      if (isNaN(menuId)) continue;
      // 检查每个标准权限位
      const flagValues = [1, 2, 4, 8];
      for (const fv of flagValues) {
        if ((flags & fv) !== 0) {
          const permNodeId = -(menuId * 100 + fv);
          tree.setChecked(permNodeId, true, false);
        }
      }
    }
  });
};

// 从树选中状态构建权限字符串
const buildPermissionFromTree = (): string => {
  const tree = menuTreeRef.value;
  if (!tree) return form.permission || '';

  const halfCheckedKeys = tree.getHalfCheckedKeys() as number[];
  const checkedKeys = tree.getCheckedKeys() as number[];
  // 合并全选和半选的菜单（排除权限子项节点）
  const allMenuIds = new Set([
    ...halfCheckedKeys.filter(k => k > 0),
    ...checkedKeys.filter(k => k > 0),
  ]);
  // 获取所有选中/半选的权限子项
  const permNodes = checkedKeys.filter(k => k < 0) as number[];

  // 按菜单分组权限
  const permMap: Record<number, number> = {};
  for (const nodeId of permNodes) {
    const menuId = Math.floor((-nodeId) / 100);
    const flag = (-nodeId) % 100;
    if (!permMap[menuId]) permMap[menuId] = 0;
    permMap[menuId] |= flag;
  }

  // 对于全选但没有权限子项的菜单，授予所有权限
  for (const menuId of allMenuIds) {
    if (!permMap[menuId]) {
      permMap[menuId] = 1 | 2 | 4 | 8; // 全部权限
    }
  }

  // 构建权限字符串
  const parts: string[] = [];
  for (const [menuId, flags] of Object.entries(permMap).sort(
    ([a], [b]) => parseInt(a) - parseInt(b),
  )) {
    parts.push(`${menuId}#${flags}`);
  }

  return parts.join(',');
};

// 全选/取消/展开/收起
const checkAllMenus = () => {
  const tree = menuTreeRef.value;
  if (!tree) return;
  const allIds = getAllNodeIds(menuTree.value);
  for (const id of allIds) {
    tree.setChecked(id, true, false);
  }
};

const uncheckAllMenus = () => {
  const tree = menuTreeRef.value;
  if (!tree) return;
  tree.setCheckedKeys([]);
};

const expandAll = () => {
  const allIds = getAllNodeIds(menuTree.value);
  defaultExpandedKeys.value = allIds;
};

const collapseAll = () => {
  defaultExpandedKeys.value = [];
};

// 递归获取所有节点 ID
const getAllNodeIds = (nodes: MenuTreeNode[]): number[] => {
  const ids: number[] = [];
  for (const node of nodes) {
    ids.push(node.id);
    if (node.children) ids.push(...getAllNodeIds(node.children));
  }
  return ids;
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

// 新增
const handleAdd = async () => {
  formType.value = 'add';
  Object.assign(form, {
    id: 0,
    name: '',
    enable: true,
    isSystem: false,
    tenantId: undefined,
    permission: '',
    sort: 0,
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
    tenantName: '',
  });
  await loadMenuTree();
  await loadTenantOptions();
  rolePermissions.value = {};
  // 先显示对话框，再在 nextTick 中操作树组件
  dialogVisible.value = true;
  nextTick(() => {
    menuTreeRef.value?.setCheckedKeys([]);
  });
};

// 编辑
const handleEdit = async (row: Role) => {
  formType.value = 'edit';
  // 先使用行数据填充表单
  Object.assign(form, { ...row });
  try {
    // 再通过 Detail 接口获取完整数据（包括 permission 字段）
    const data = await request.get(`/Admin/Role/Detail?id=${row.id}`);
    const detail = apiDataToSingle<Role>(data);
    if (detail) {
      Object.assign(form, detail);
    }
  } catch (error) {
    console.error('获取角色详情失败:', error);
  }
  await loadMenuTree();
  await loadTenantOptions();
  // 先显示对话框（渲染树组件），applyPermissionToTree 内部用 nextTick 保证树就绪
  dialogVisible.value = true;
  applyPermissionToTree(form.permission || '');
};

// 删除
const handleDelete = (row: Role) => {
  if (row.isSystem) {
    return;
  }

  handleDeleteOperation(
    () => request.delete('/Admin/Role', { params: { id: row.id } }),
    loadData,
    '确认删除该角色吗？'
  );
};

// 提交表单
const submitForm = async () => {
  // 从树中构建权限字符串，写入 form.permission 由模型绑定提交给后端
  const permStr = buildPermissionFromTree();
  form.permission = permStr;

  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/Role', form);
    } else {
      await request.put('/Admin/Role', form);
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
.perm-tree-wrap {
  border: 1px solid var(--el-border-color-light);
  border-radius: 4px;
  padding: 8px;
  max-height: 400px;
  overflow-y: auto;
}

.perm-tree-actions {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
  flex-wrap: wrap;
}

.perm-tree {
  min-height: 100px;
}

.perm-tree-node {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
}

.perm-tree-icon {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.perm-tag {
  font-size: 11px;
  background: var(--el-color-info-light-9);
  color: var(--el-color-info);
  padding: 0 6px;
  border-radius: 3px;
  line-height: 18px;
}

.form-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-left: 8px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}
</style>
