<template>
  <div class="lov-page">
    <!-- 页面标题栏 -->
    <div class="page-header">
      <h2 class="page-title">{{ t('lovAdmin.title') }}</h2>
      <el-button type="primary" @click="handleAdd">{{ t('lovAdmin.add') }}</el-button>
    </div>

    <!-- 搜索栏 -->
    <el-form :inline="true" :model="searchForm" class="search-bar">
      <el-form-item :label="t('lovAdmin.keyword')">
        <el-input
          v-model="searchForm.q"
          :placeholder="t('lovAdmin.keywordHint')"
          clearable
          @keyup.enter="handleSearch"
        />
      </el-form-item>
      <el-form-item :label="t('lovAdmin.type')">
        <el-select v-model="searchForm.type" :placeholder="t('lovAdmin.all')" clearable style="width: 120px">
          <el-option :label="t('lovAdmin.typeEnum')" value="ENUM" />
          <el-option :label="t('lovAdmin.typeList')" value="LIST" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="handleSearch">{{ t('button.search') }}</el-button>
        <el-button @click="handleReset">{{ t('button.reset') }}</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="tableData" v-loading="loading" border style="width: 100%">
      <el-table-column prop="id" :label="t('common.id')" width="70" />
      <el-table-column prop="lovCode" :label="t('lovAdmin.code')" min-width="200">
        <template #default="{ row }">
          <el-tag :type="row.type === 'ENUM' ? 'primary' : 'success'" size="small">
            {{ row.type }}
          </el-tag>
          <code style="margin-left: 8px">{{ row.lovCode }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="name" :label="t('common.name')" width="160" />
      <el-table-column prop="type" :label="t('lovAdmin.type')" width="80" />
      <el-table-column prop="source" :label="t('lovAdmin.source')" width="80">
        <template #default="{ row }">
          <el-tag v-if="row.source === 'AUTO'" type="info" size="small">{{ t('lovAdmin.sourceAuto') }}</el-tag>
          <el-tag v-else size="small">{{ t('lovAdmin.sourceManual') }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="enabled" :label="t('common.enable')" width="70">
        <template #default="{ row }">
          <el-tag :type="row.enabled ? 'success' : 'danger'" size="small">
            {{ row.enabled ? t('common.yes') : t('common.no') }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime" :label="t('desc.createTime')" width="160" />
      <el-table-column :label="t('common.actions')" width="220" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" size="small" @click="handleEdit(row)">{{ t('button.edit') }}</el-button>
          <el-button type="primary" size="small" @click="handleConfig(row)">
            {{ row.type === 'ENUM' ? t('lovAdmin.enumValues') : t('lovAdmin.listConfig') }}
          </el-button>
          <el-button type="danger" size="small" @click="handleDelete(row)">{{ t('button.delete') }}</el-button>
        </template>
      </el-table-column>
    </el-table>

    <CubeListPager
      :current-page="page"
      :page-size="size"
      :total="total"
      :on-current-change="
        (p: number) => {
          page = p;
          loadData();
        }
      "
      :on-size-change="
        (s: number) => {
          size = s;
          loadData();
        }
      "
    />

    <!-- 新增/编辑弹窗 -->
    <el-dialog v-model="dialogVisible" :title="editingId ? t('lovAdmin.edit') : t('lovAdmin.add')" width="600px">
      <el-form :model="form" label-width="100px">
        <el-form-item :label="t('lovAdmin.lovType')" required>
          <el-select
            v-model="form.type"
            :placeholder="t('lovAdmin.typeHint')"
            style="width: 100%"
            @change="onTypeChange"
          >
            <el-option :label="t('lovAdmin.typeEnumFull')" value="ENUM" />
            <el-option :label="t('lovAdmin.typeListFull')" value="LIST" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('lovAdmin.code')" required>
          <el-input v-model="form.lovCode">
            <template #prepend v-if="form.type === 'ENUM'">Enum.</template>
            <template #prepend v-else-if="form.type === 'LIST'">List.</template>
          </el-input>
          <div class="form-tip">
            {{ t('lovAdmin.codeTip', {
              example: form.type === 'ENUM' ? 'Enum.OrderStatus' : 'List.Department',
            }) }}
          </div>
        </el-form-item>
        <el-form-item :label="t('lovAdmin.displayName')">
          <el-input v-model="form.name" :placeholder="t('lovAdmin.displayNameHint')" />
        </el-form-item>
        <template v-if="form.type === 'LIST'">
          <el-form-item :label="t('lovAdmin.valueField')">
            <el-input v-model="form.valueField" :placeholder="t('lovAdmin.valueFieldHint')" />
          </el-form-item>
          <el-form-item :label="t('lovAdmin.labelField')">
            <el-input v-model="form.labelField" :placeholder="t('lovAdmin.labelFieldHint')" />
          </el-form-item>
        </template>
        <el-form-item :label="t('common.enable')">
          <el-switch v-model="form.enabled" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('button.cancel') }}</el-button>
        <el-button type="primary" @click="handleSave" :loading="saving">{{ t('button.save') }}</el-button>
      </template>
    </el-dialog>

    <!-- 值集配置弹窗 -->
    <LovConfig v-model="configDialogVisible" :lov-def-id="configLovDefId" @saved="loadData" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { usePageApi } from '@newlifex/cube-vue/core/composables/useCubeApi';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import LovConfig from './config.vue';

const api = usePageApi('Admin', 'Lov');
const { t } = useI18n();

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
  id: 0 as number,
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
    ElMessage.error(err?.message || t('common.loadFailed'));
  } finally {
    loading.value = false;
  }
}

function handleAdd() {
  editingId.value = null;
  form.value = {
    id: 0,
    lovCode: '',
    name: '',
    type: 'ENUM',
    valueField: '',
    labelField: '',
    source: 'MANUAL',
    enabled: true,
  };
  dialogVisible.value = true;
}

function handleEdit(row: any) {
  editingId.value = row.id;
  form.value = {
    id: row.id,
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
      data.lovCode =
        form.value.type === 'ENUM'
          ? 'Enum.' + form.value.lovCode.replace(/^Enum\./, '')
          : 'List.' + form.value.lovCode.replace(/^List\./, '');
      await api.update(data);
      savedId = editingId.value;
      ElMessage.success(t('engine.updateSuccess'));
    } else {
      data.lovCode =
        form.value.type === 'ENUM'
          ? 'Enum.' + form.value.lovCode.replace(/^Enum\./, '')
          : 'List.' + form.value.lovCode.replace(/^List\./, '');
      const res = await api.add(data);
      // @ts-ignore
      savedId = res?.data?.id || 0;
      ElMessage.success(t('engine.createSuccess'));
    }
    dialogVisible.value = false;
    await loadData();

    // 新增列表型值集后自动打开配置弹窗
    if (!editingId.value && form.value.type === 'LIST' && savedId) {
      configLovDefId.value = savedId;
      configDialogVisible.value = true;
    }
  } catch (err: any) {
    ElMessage.error(err?.message || t('engine.saveFailed'));
  } finally {
    saving.value = false;
  }
}

async function handleDelete(row: any) {
  try {
    await ElMessageBox.confirm(t('lovAdmin.deleteConfirm', { code: row.lovCode }), t('lovAdmin.confirmDelete'), {
      confirmButtonText: t('button.delete'),
      cancelButtonText: t('button.cancel'),
      type: 'warning',
    });
    await api.remove(row.id);
    ElMessage.success(t('engine.deleteSuccess'));
    await loadData();
  } catch (err: any) {
    if (err !== 'cancel') ElMessage.error(err?.message || t('engine.deleteFailed'));
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
