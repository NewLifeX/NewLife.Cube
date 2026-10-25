<template>
  <div class="menu-container">
    <el-card class="box-card" style="height: 100%; display: flex; flex-direction: column;">
      <template #header>
        <div class="card-header">
          <h3>{{ t('menuAdmin.title') }}</h3>
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
            <el-dropdown
              trigger="click"
              :disabled="selectedRows.length === 0"
              @command="handleBatchCommand"
            >
              <el-button type="primary" plain :disabled="selectedRows.length === 0">
                {{ t('menuAdmin.batchTitle') }}{{ selectedRows.length > 0 ? t('menuAdmin.selectedCount', { count: selectedRows.length }) : '' }}
                <el-icon class="el-icon--right"><ArrowDown /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item
                    v-for="item in batchItems"
                    :key="item.command"
                    :command="item.command"
                    :divided="item.divided"
                  >
                    {{ item.label }}
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
            <el-button type="primary" @click="handleAdd">{{ t('menuAdmin.add') }}</el-button>
          </div>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <div class="table-wrapper">
        <el-table
          ref="menuTableRef"
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
        <el-table-column prop="id" :label="t('common.id')" width="85" />
        <el-table-column :label="t('menuAdmin.name')" min-width="150">
          <template #default="scope">
            <span>{{ getDepthPrefix((scope.row as any)._depth ?? 0) }}{{ scope.row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('menuAdmin.displayName')" min-width="150">
          <template #default="scope">
            <span>{{ getDepthPrefix((scope.row as any)._depth ?? 0) }}{{ scope.row.displayName }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="url" :label="t('menuAdmin.url')" min-width="150" />
        <el-table-column prop="sort" :label="t('common.sort')" width="80" />
        <el-table-column prop="icon" :label="t('menuAdmin.icon')" width="100">
          <template #default="scope">
            <i v-if="scope.row.icon" :class="scope.row.icon"></i>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="updateTime" :label="t('common.updateTime')" min-width="150" />
        <el-table-column :label="t('menuAdmin.visible')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.visible ? 'success' : 'danger'">
              {{ scope.row.visible ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('menuAdmin.necessary')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.necessary ? 'warning' : undefined">
              {{ scope.row.necessary ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('menuAdmin.newWindow')" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.newWindow ? 'info' : undefined">
              {{ scope.row.newWindow ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('common.remark')" min-width="120" show-overflow-tooltip>
          <template #default="scope">
            {{ scope.row.remark || '-' }}
          </template>
        </el-table-column>
        <el-table-column :label="t('common.actions')" width="180" fixed="right">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">{{ t('button.edit') }}</el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">{{ t('button.delete') }}</el-button>
          </template>
        </el-table-column>
        </el-table>
      </div>
    </el-card>

    <!-- 菜单表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? t('menuAdmin.add') : t('menuAdmin.edit')"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item :label="t('menuAdmin.name')" prop="name">
          <el-input v-model="form.name" :placeholder="t('menuAdmin.nameHint')" />
        </el-form-item>
        <el-form-item :label="t('menuAdmin.displayName')" prop="displayName">
          <el-input v-model="form.displayName" :placeholder="t('menuAdmin.displayNameHint')" />
        </el-form-item>
        <el-form-item :label="t('menuAdmin.fullName')" prop="fullName">
          <el-input v-model="form.fullName" :placeholder="t('menuAdmin.fullNameHint')" />
        </el-form-item>
        <el-form-item :label="t('menuAdmin.parent')" prop="parentID">
          <el-select v-model="form.parentID" :placeholder="t('menuAdmin.parentHint')" clearable filterable>
            <el-option :label="t('menuAdmin.noParent')" :value="0" />
            <el-option
              v-for="menu in menuOptions"
              :key="menu.id"
              :label="menu.displayName || menu.name"
              :value="menu.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('menuAdmin.urlAddress')" prop="url">
          <el-input v-model="form.url" :placeholder="t('menuAdmin.urlAddressHint')" />
        </el-form-item>
        <el-form-item :label="t('common.sort')" prop="sort">
          <el-input-number v-model="form.sort" :min="0" :placeholder="t('menuAdmin.sortHint')" />
        </el-form-item>
        <el-form-item :label="t('menuAdmin.icon')" prop="icon">
          <el-input v-model="form.icon" :placeholder="t('menuAdmin.iconHint')" />
        </el-form-item>
        <el-form-item :label="t('menuAdmin.visibleStatus')" prop="visible">
          <el-switch v-model="form.visible" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('menuAdmin.necessaryMenu')" prop="necessary">
          <el-switch v-model="form.necessary" :active-value="true" :inactive-value="false" />
          <span class="form-tip">{{ t('menuAdmin.necessaryTip') }}</span>
        </el-form-item>
        <el-form-item :label="t('menuAdmin.openNewWindow')" prop="newWindow">
          <el-switch v-model="form.newWindow" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('menuAdmin.permissions')" prop="permission">
          <el-input
            v-model="form.permission"
            type="textarea"
            :placeholder="t('menuAdmin.permissionsHint')"
          />
        </el-form-item>
        <el-form-item :label="t('common.remark')" prop="remark">
          <el-input v-model="form.remark" type="textarea" :placeholder="t('tenant.remarkHint')" />
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
import { ref, reactive, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormInstance, FormRules, TableInstance } from 'element-plus';
import { ArrowDown } from '@element-plus/icons-vue';
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
const menuOptions = ref<Menu[]>([]);
const selectedRows = ref<Menu[]>([]);
const menuTableRef = ref<TableInstance>();
const { t } = useI18n();

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
  name: [{ required: true, message: t('menuAdmin.nameRequired'), trigger: 'blur' }],
  displayName: [{ required: true, message: t('menuAdmin.displayNameRequired'), trigger: 'blur' }],
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
      // 对子节点排序（按 Sort 倒序）
      node.children.sort((a, b) => (Number(b.sort) || 0) - (Number(a.sort) || 0));
      // 递归处理子节点，深度+1
      node.children.forEach((child) => processNode(child, depth + 1));
    }
  };

  // 对根节点排序（按 Sort 倒序）
  roots.sort((a, b) => (Number(b.sort) || 0) - (Number(a.sort) || 0));

  // 处理所有节点（根节点深度为0）
  roots.forEach((root) => processNode(root, 0));

  console.log('Tree built:', {
    rawCount: data.length,
    rootCount: roots.length,
    tree: roots,
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

// 批量操作项。command 形如「字段:目标值」，避免为每个开关各写一个处理函数
const batchItems = computed(() => [
  { command: 'visible:true', label: t('menuAdmin.setVisible'), divided: false },
  { command: 'visible:false', label: t('menuAdmin.setInvisible'), divided: false },
  { command: 'necessary:true', label: t('menuAdmin.setNecessary'), divided: true },
  { command: 'necessary:false', label: t('menuAdmin.setUnnecessary'), divided: false },
  { command: 'newWindow:true', label: t('menuAdmin.setOpenNewWindow'), divided: true },
  { command: 'newWindow:false', label: t('menuAdmin.cancelNewWindow'), divided: false },
]);

// 批量修改选中菜单的开关字段（可见/必要/新窗口）
const handleBatchCommand = async (command: string) => {
  const rows = selectedRows.value;
  const item = batchItems.value.find((e) => e.command === command);
  if (rows.length === 0 || !item) return;

  const [field, value] = command.split(':');

  try {
    await ElMessageBox.confirm(
      t('menuAdmin.batchConfirm', { count: rows.length, action: item.label }),
      t('menuAdmin.batchTitle'),
      {
        type: 'warning',
        confirmButtonText: t('button.confirm'),
        cancelButtonText: t('button.cancel'),
      },
    );
  } catch {
    return; // 用户取消
  }

  loading.value = true;
  let ok = 0;
  const fails: string[] = [];
  try {
    for (const row of rows) {
      // 与 submitForm 同因：行数据同时含 camelCase 开关（visible/necessary/newWindow）
      // 与 PascalCase 旧字段（Visible/Necessary/NewWindow 整型），后端大小写不敏感绑定下
      // 旧值会覆盖新值而导致开关不生效，故发送前剔除 PascalCase 冗余键；
      // children/_depth/hasChildren 是前端构建树结构的派生字段，不回传。
      const payload: Record<string, unknown> = { ...row, [field]: value === 'true' };
      delete payload.Visible;
      delete payload.Necessary;
      delete payload.NewWindow;
      delete payload.children;
      delete payload._depth;
      delete payload.hasChildren;

      try {
        await request.put(`/Admin/Menu/${payload.id}`, payload);
        ok++;
      } catch (err) {
        fails.push(`${row.displayName || row.name}: ${(err as Error)?.message || t('menuAdmin.updateFailed')}`);
      }
    }
  } finally {
    loading.value = false;
  }

  if (fails.length > 0) {
    ElMessage.error(
      t('menuAdmin.batchFailed', { ok, fail: fails.length, detail: fails.slice(0, 3).join('; ') }),
    );
  } else {
    ElMessage.success(t('menuAdmin.batchSuccess', { ok }));
  }

  // 开关影响列表标签与权限语义，重新加载；清空勾选避免对旧选择重复操作
  menuTableRef.value?.clearSelection();
  selectedRows.value = [];
  loadData();
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

    console.log('Raw menu data:', dataList);

    // 构建树结构并排序
    tableData.value = buildTreeData(dataList);

    console.log('Tree data:', tableData.value);

    // 同时保存平铺的数据用于菜单选项（排除当前编辑的菜单）
    menuOptions.value = dataList.filter((item) => item.id !== form.id);
  } catch {
    tableData.value = [];
    menuOptions.value = [];
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
    () => request.delete(`/Admin/Menu/${row.id}`),
    loadData,
    t('menuAdmin.deleteConfirm')
  );
};

// 提交表单
const submitForm = async () => {
  // 清理列表行原始字段回传造成的键冲突：行数据同时含 camelCase 布尔开关
  // （visible/necessary/newWindow）与 PascalCase 旧字段（Visible/Necessary/NewWindow 整型），
  // handleEdit 整行 Object.assign 后表单同时携带两组键。提交时后端大小写不敏感绑定下，
  // 靠后的 PascalCase 旧值会覆盖开关新值（如 Visible:1 覆盖 visible:false），
  // 导致“编辑可见性/必要/新窗口不生效”。发送前剔除 PascalCase 冗余键，仅保留开关值。
  const payload: Record<string, unknown> = { ...form };
  delete payload.Visible;
  delete payload.Necessary;
  delete payload.NewWindow;

  const apiCall = async () => {
    if (formType.value === 'add') {
      await request.post('/Admin/Menu', payload);
    } else {
      await request.put(`/Admin/Menu/${payload.id}`, payload);
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
