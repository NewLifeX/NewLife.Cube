<template>
  <div class="lov-page">
    <!-- 页面标题栏 -->
    <div class="page-header">
      <h2 class="page-title">值集管理</h2>
      <el-button type="primary" @click="handleAdd">新增值集</el-button>
    </div>

    <!-- 搜索栏 -->
    <el-form :inline="true" :model="searchForm" class="search-bar">
      <el-form-item label="关键词">
        <el-input v-model="searchForm.q" placeholder="编码/名称搜索" clearable
          @keyup.enter="handleSearch" />
      </el-form-item>
      <el-form-item label="类型">
        <el-select v-model="searchForm.type" placeholder="全部" clearable style="width: 120px">
          <el-option label="枚举型" value="ENUM" />
          <el-option label="列表型" value="LIST" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="handleSearch">查询</el-button>
        <el-button @click="handleReset">重置</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="tableData" v-loading="loading" border style="width: 100%">
      <el-table-column prop="id" label="编号" width="70" />
      <el-table-column prop="lovCode" label="值集编码" min-width="200">
        <template #default="{ row }">
          <el-tag :type="row.type === 'ENUM' ? 'primary' : 'success'" size="small">
            {{ row.type }}
          </el-tag>
          <code style="margin-left: 8px">{{ row.lovCode }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="name" label="名称" width="160" />
      <el-table-column prop="type" label="类型" width="80" />
      <el-table-column prop="source" label="来源" width="80">
        <template #default="{ row }">
          <el-tag v-if="row.source === 'AUTO'" type="info" size="small">自动</el-tag>
          <el-tag v-else size="small">手工</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="enabled" label="启用" width="70">
        <template #default="{ row }">
          <el-tag :type="row.enabled ? 'success' : 'danger'" size="small">
            {{ row.enabled ? '是' : '否' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime" label="创建时间" width="160" />
      <el-table-column label="操作" width="220" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
          <el-button type="primary" size="small" @click="handleConfig(row)">
            {{ row.type === 'ENUM' ? '枚举值' : '列表配置' }}
          </el-button>
          <el-button type="danger" size="small" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <CubeListPager :current-page="page" :page-size="size" :total="total"
      :on-current-change="(p: number) => { page = p; loadData(); }"
      :on-size-change="(s: number) => { size = s; loadData(); }" />

    <!-- 新增/编辑弹窗 -->
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑值集' : '新增值集'" width="600px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="值集类型" required>
          <el-select v-model="form.type" placeholder="选择类型" style="width: 100%" @change="onTypeChange">
            <el-option label="枚举型 (ENUM)" value="ENUM" />
            <el-option label="列表型 (LIST)" value="LIST" />
          </el-select>
        </el-form-item>
        <el-form-item label="值集编码" required>
          <el-input v-model="form.lovCode">
            <template #prepend v-if="form.type === 'ENUM'">Enum.</template>
            <template #prepend v-else-if="form.type === 'LIST'">List.</template>
          </el-input>
          <div class="form-tip">
            完整编码 = 前缀 + 语义名，如：{{ form.type === 'ENUM' ? 'Enum.OrderStatus' : 'List.Department' }}
          </div>
        </el-form-item>
        <el-form-item label="显示名称">
          <el-input v-model="form.name" placeholder="值集的显示名称" />
        </el-form-item>
        <template v-if="form.type === 'LIST'">
          <el-form-item label="值字段">
            <el-input v-model="form.valueField" placeholder="如 userId" />
          </el-form-item>
          <el-form-item label="标签字段">
            <el-input v-model="form.labelField" placeholder="如 userName" />
          </el-form-item>
        </template>
        <el-form-item label="启用">
          <el-switch v-model="form.enabled" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="saving">保存</el-button>
      </template>
    </el-dialog>

    <!-- 值集配置弹窗 -->
    <LovConfig
      v-model="configDialogVisible"
      :lov-def-id="configLovDefId"
      @saved="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { usePageApi } from '@/composables/usePageApi';
import CubeListPager from 'cube-front/core/components/CubeListPager.vue';
import LovConfig from './config.vue';

const api = usePageApi('Admin', 'Lov');

const tableData = ref<any[]>([]);
const loading = ref(false);
const saving = ref(false);
const page = ref(1);
const size = ref(10);
const total = ref(0);
const dialogVisible = ref(false);
const editingId = ref<number | null>(null);

// 配置弹窗
const configDialogVisible = ref(false);
const configLovDefId = ref(0);

const form = ref({
  lovCode: '',
  name: '',
  type: 'ENUM' as string,
  valueField: '',
  labelField: '',
  source: 'MANUAL' as string,
  enabled: true,
});

const searchQuery = ref('');
const searchForm = ref({ q: '', type: '' });

function onTypeChange(type: string) {
  if (form.value.lovCode.startsWith('Enum.') || form.value.lovCode.startsWith('List.')) {
    form.value.lovCode = '';
  }
}

function handleSearch() {
  searchQuery.value = searchForm.value.q;
  page.value = 1;
  loadData();
}
function handleReset() {
  searchForm.value = { q: '', type: '' };
  searchQuery.value = '';
  page.value = 1;
  loadData();
}

async function loadData() {
  loading.value = true;
  try {
    const params: any = { pageIndex: page.value, pageSize: size.value };
    if (searchForm.value.q) params.Q = searchForm.value.q;
    if (searchForm.value.type) params.type = searchForm.value.type;
    const res = await api.getList(params);
    // 响应格式: { code, data: [...], page: { pageIndex, pageSize, totalCount } }
    tableData.value = res?.data ?? res ?? [];
    total.value = res?.page?.totalCount ?? 0;
  } catch (err: any) {
    ElMessage.error(err?.message || '加载失败');
  } finally {
    loading.value = false;
  }
}

function handleAdd() {
  editingId.value = null;
  form.value = { lovCode: '', name: '', type: 'ENUM', valueField: '', labelField: '', source: 'MANUAL', enabled: true };
  dialogVisible.value = true;
}

function handleEdit(row: any) {
  editingId.value = row.id;
  form.value = {
    lovCode: row.lovCode || '',
    name: row.name || '',
    type: row.type || 'ENUM',
    valueField: row.valueField || '',
    labelField: row.labelField || '',
    source: row.source || 'MANUAL',
    enabled: row.enabled ?? true,
  };
  dialogVisible.value = true;
}

function handleConfig(row: any) {
  configLovDefId.value = row.id;
  configDialogVisible.value = true;
}

async function handleSave() {
  saving.value = true;
  try {
    const data = { ...form.value };
    let savedId = 0;
    if (editingId.value) {
      data.id = editingId.value;
      data.lovCode = form.value.type === 'ENUM' ? 'Enum.' + form.value.lovCode.replace(/^Enum\./, '') : 'List.' + form.value.lovCode.replace(/^List\./, '');
      await api.update(data);
      savedId = editingId.value;
      ElMessage.success('更新成功');
    } else {
      data.lovCode = form.value.type === 'ENUM' ? 'Enum.' + form.value.lovCode.replace(/^Enum\./, '') : 'List.' + form.value.lovCode.replace(/^List\./, '');
      const res = await api.add(data);
      savedId = res?.data?.id || res?.id || 0;
      ElMessage.success('新增成功');
    }
    dialogVisible.value = false;
    await loadData();

    // 新增列表型值集后自动打开配置弹窗
    if (!editingId.value && form.value.type === 'LIST' && savedId) {
      configLovDefId.value = savedId;
      configDialogVisible.value = true;
    }
  } catch (err: any) {
    ElMessage.error(err?.message || '保存失败');
  } finally {
    saving.value = false;
  }
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(`确定要删除值集 "${row.lovCode}" 吗？`, '确认删除', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning',
    });
    await api.remove(row.id);
    ElMessage.success('删除成功');
    await loadData();
  } catch (err: any) {
    if (err !== 'cancel') ElMessage.error(err?.message || '删除失败');
  }
}

onMounted(() => loadData());
</script>

<style scoped>
.lov-page {
  padding: 0;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.page-title {
  font-size: 18px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin: 0;
}

.search-bar {
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  padding: 12px 16px;
  margin-bottom: 16px;
}

.form-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}
</style>
