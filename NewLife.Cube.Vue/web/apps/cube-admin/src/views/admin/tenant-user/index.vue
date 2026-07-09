<template>
  <div>
    <div class="page-header">
      <h1>租户用户管理</h1>
      <p>管理租户下的用户关系及权限分配</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>租户用户列表</span>
          <div>
            <el-button type="primary" @click="showAddDialog">
              <el-icon><Plus /></el-icon>
              添加租户用户
            </el-button>
            <el-button @click="refreshList">
              <el-icon><Refresh /></el-icon>
              刷新
            </el-button>
          </div>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <!-- 数据表格 -->
      <div class="table-container">
        <el-table
          :data="tableData"
          v-loading="loading.list"
          style="width: 100%"
          @selection-change="handleSelectionChange"
        >
          <el-table-column type="selection" width="55" />
          <el-table-column prop="id" label="编号" width="80" />
          <el-table-column prop="tenantId" label="租户ID" width="100" />
          <el-table-column prop="tenantName" label="租户名称" />
          <el-table-column prop="userId" label="用户ID" width="100" />
          <el-table-column prop="userName" label="用户名称" />
          <el-table-column prop="enable" label="启用状态" width="100">
            <template #default="{ row }">
              <el-tag :type="row.enable ? 'success' : 'danger'">
                {{ row.enable ? '启用' : '禁用' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="roleId" label="主要角色ID" width="120" />
          <el-table-column prop="roleName" label="主要角色" />
          <el-table-column prop="roleNames" label="所有角色" show-overflow-tooltip />
          <el-table-column prop="createTime" label="创建时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column label="备注" min-width="120" show-overflow-tooltip>
            <template #default="scope">
              {{ scope.row.remark || '-' }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="200" fixed="right">
            <template #default="{ row }">
              <el-button size="small" @click="handleDetail(row)">
                详情
              </el-button>
              <el-button size="small" @click="handleEdit(row)">
                编辑
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <CubeListPager
        :total="queryParams.total"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </el-card>

    <!-- 添加/编辑对话框 -->
    <el-dialog
      v-model="showFormDialog"
      :title="isEdit ? '编辑租户用户' : '添加租户用户'"
      width="600px"
    >
      <el-form
        :model="formData"
        :rules="formRules"
        ref="formRef"
        label-width="120px"
        v-loading="loading.form"
      >
        <el-form-item label="租户ID" prop="tenantId">
          <el-input v-model="formData.tenantId" placeholder="请输入租户ID" />
        </el-form-item>
        <el-form-item label="用户ID" prop="userId">
          <el-input v-model="formData.userId" placeholder="请输入用户ID" />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="formData.enable" />
        </el-form-item>
        <el-form-item label="主要角色ID" prop="roleId">
          <el-input v-model="formData.roleId" placeholder="请输入主要角色ID" />
        </el-form-item>
        <el-form-item label="角色组" prop="roleIds">
          <el-input
            v-model="formData.roleIds"
            placeholder="请输入角色ID，多个用逗号分隔"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
        <el-form-item label="描述" prop="remark">
          <el-input
            v-model="formData.remark"
            placeholder="请输入描述"
            type="textarea"
            :rows="3"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="showFormDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="loading.submit">
          {{ isEdit ? '更新' : '创建' }}
        </el-button>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="showDetailDialog" title="租户用户详情" width="800px">
      <div v-loading="loading.detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="编号">{{ detailData.id }}</el-descriptions-item>
          <el-descriptions-item label="租户ID">{{ detailData.tenantId }}</el-descriptions-item>
          <el-descriptions-item label="租户名称">{{ detailData.tenantName }}</el-descriptions-item>
          <el-descriptions-item label="用户ID">{{ detailData.userId }}</el-descriptions-item>
          <el-descriptions-item label="用户名称">{{ detailData.userName }}</el-descriptions-item>
          <el-descriptions-item label="启用状态">
            <el-tag :type="detailData.enable ? 'success' : 'danger'">
              {{ detailData.enable ? '启用' : '禁用' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="主要角色ID">{{ detailData.roleId }}</el-descriptions-item>
          <el-descriptions-item label="主要角色">{{ detailData.roleName }}</el-descriptions-item>
          <el-descriptions-item label="角色组" :span="2">{{ detailData.roleIds }}</el-descriptions-item>
          <el-descriptions-item label="所有角色" :span="2">{{ detailData.roleNames }}</el-descriptions-item>
          <el-descriptions-item label="创建者">{{ detailData.createUserId }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ formatDate(detailData.createTime) }}</el-descriptions-item>
          <el-descriptions-item label="创建地址">{{ detailData.createIP }}</el-descriptions-item>
          <el-descriptions-item label="更新者">{{ detailData.updateUserId }}</el-descriptions-item>
          <el-descriptions-item label="更新时间">{{ formatDate(detailData.updateTime) }}</el-descriptions-item>
          <el-descriptions-item label="更新地址">{{ detailData.updateIP }}</el-descriptions-item>
          <el-descriptions-item label="描述" :span="2">{{ detailData.remark }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <template #footer>
        <el-button @click="showDetailDialog = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessageBox } from 'element-plus'
import { Plus, Refresh } from '@element-plus/icons-vue'
import { request } from '@newlifex/cube-vue/core/utils/request'
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue'
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue'
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 租户用户接口，继承 BaseEntity
interface TenantUser extends BaseEntity {
  tenantId: number
  tenantName: string
  userId: number
  userName: string
  enable: boolean
  roleId: number
  roleName: string
  roleIds: string
  roleNames: string
  createUserId: number
  createTime: string
  createIP: string
  updateUserId: number
  updateTime: string
  updateIP: string
  remark: string
}

// 表格数据
const tableData = ref<TenantUser[]>([])
const selectedRows = ref<TenantUser[]>([])

// 表单数据
const formData = ref<Partial<TenantUser>>({})
const detailData = ref<Partial<TenantUser>>({})

// 对话框状态
const showFormDialog = ref(false)
const showDetailDialog = ref(false)
const isEdit = ref(false)

// 表单引用
const formRef = ref()

// 表单校验规则
const formRules = {
  tenantId: [
    { required: true, message: '请输入租户ID', trigger: 'blur' }
  ],
  userId: [
    { required: true, message: '请输入用户ID', trigger: 'blur' }
  ]
}

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
})

// 加载状态
const loading = reactive({
  list: false,
  form: false,
  submit: false,
  detail: false
})

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  getTenantUserList();
};

// 获取租户用户列表
const getTenantUserList = async () => {
  try {
    loading.list = true
    const data = await request.get('/Admin/TenantUser', {
      params: queryParams
    })
    if (data && typeof data === 'object' && 'list' in data && 'page' in data) {
      tableData.value = Array.isArray(data.list) ? data.list : [];
      queryParams.total = data.page?.totalCount || 0;
    } else if (data && typeof data === 'object' && 'list' in data && 'total' in data) {
      tableData.value = Array.isArray(data.list) ? data.list : [];
      queryParams.total = data.total || 0;
    } else if (Array.isArray(data)) {
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
    loading.list = false
  }
}

// 获取租户用户详情
const getTenantUserDetail = async (id: number) => {
  try {
    loading.detail = true

    const data = await request.get('/Admin/TenantUser/Detail', {
      params: { id }
    })

    if (data) {
      detailData.value = data || {}
    }
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.detail = false
  }
}

// 创建租户用户
const createTenantUser = async (data: Partial<TenantUser>) => {
  try {
    loading.submit = true

    await request.post('/Admin/TenantUser', data)

    showFormDialog.value = false
    refreshList()
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.submit = false
  }
}

// 更新租户用户
const updateTenantUser = async (data: Partial<TenantUser>) => {
  try {
    loading.submit = true

    await request.put('/Admin/TenantUser', data)

    showFormDialog.value = false
    refreshList()
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.submit = false
  }
}

// 删除租户用户
const deleteTenantUser = async (id: number) => {
  try {
    await request.delete('/Admin/TenantUser', {
      params: { id }
    })
    refreshList()
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  }
}

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 刷新列表
const refreshList = () => {
  getTenantUserList()
}

// 显示添加对话框
const showAddDialog = () => {
  isEdit.value = false
  formData.value = {
    enable: true
  }
  showFormDialog.value = true
}

// 查看详情
const handleDetail = async (row: TenantUser) => {
  await getTenantUserDetail(row.id)
  showDetailDialog.value = true
}

// 编辑
const handleEdit = async (row: TenantUser) => {
  isEdit.value = true
  await getTenantUserDetail(row.id)
  formData.value = { ...detailData.value }
  showFormDialog.value = true
}

// 删除
const handleDelete = async (row: TenantUser) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除租户用户 "${row.userName}" 吗？`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning',
      }
    )

    await deleteTenantUser(row.id)
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除租户用户错误:', error)
    }
  }
}

// 提交表单
const handleSubmit = async () => {
  try {
    await formRef.value.validate()

    if (isEdit.value) {
      await updateTenantUser(formData.value)
    } else {
      await createTenantUser(formData.value)
    }
  } catch (error) {
    console.error('表单提交错误:', error)
  }
}

// 表格选择变化
const handleSelectionChange = (selection: TenantUser[]) => {
  selectedRows.value = selection
}

// 页码变更处理
const CurrentPageChange = (page: number) => {
  queryParams.pageIndex = page;
};

// 每页显示条数变更处理
const PageSizeChange = (size: number) => {
  queryParams.pageSize = size;
  queryParams.pageIndex = 1;
};

// 格式化日期
const formatDate = (dateStr: string) => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
}

// 组件挂载时获取数据
onMounted(() => {
  getTenantUserList();
});
</script>

<style scoped>
.page-header {
  margin-bottom: 20px;
}

.page-header h1 {
  margin: 0 0 8px 0;
  font-size: 24px;
  font-weight: 500;
}

.page-header p {
  margin: 0;
  color: #666;
  font-size: 14px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
  padding: 20px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.table-container {
  margin-bottom: 20px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
}
</style>
