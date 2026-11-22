<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import PageHeader from '../components/page-header.vue';
import LovConfigDialog from './lov-config-dialog.vue';

defineOptions({ name: 'LovPage' });

interface LovRow {
  id: string;
  lovCode: string;
  name?: string;
  type: 'ENUM' | 'LIST';
  valueField?: string;
  labelField?: string;
  source?: 'AUTO' | 'MANUAL';
  enabled: boolean;
  remark?: string;
  createTime?: string;
  updateTime?: string;
}

const TYPE_PATH = '/Admin/Lov';

const rows = ref<LovRow[]>([]);
const loading = ref(false);
const pageIndex = ref(1);
const pageSize = ref(20);
const total = ref(0);

const search = reactive({ Q: '', type: '', source: '' });

const dialogVisible = ref(false);
const saving = ref(false);
const editingCode = ref('');
const form = reactive({
  lovCode: '',
  name: '',
  type: 'ENUM' as 'ENUM' | 'LIST',
  valueField: '',
  labelField: '',
  remark: '',
  enabled: true
});

const configVisible = ref(false);
const configCode = ref('');

const listParams = computed(() => ({
  ...(pageIndex.value > 1 ? { pageIndex: pageIndex.value } : {}),
  ...(pageSize.value !== 20 ? { pageSize: pageSize.value } : {}),
  ...(search.Q ? { Q: search.Q } : {}),
  ...(search.type ? { type: search.type } : {}),
  ...(search.source ? { source: search.source } : {})
}));

async function load() {
  loading.value = true;
  try {
    const res = await cubeApi.page.getList<LovRow>(TYPE_PATH, listParams.value);
    rows.value = (res.data ?? []) as LovRow[];
    total.value = res.page?.totalCount ?? rows.value.length;
  } catch {
    rows.value = [];
    total.value = 0;
    ElMessage.error($t('lovAdmin.loadFailed'));
  } finally {
    loading.value = false;
  }
}

function handleSearch() {
  pageIndex.value = 1;
  load();
}

function handleReset() {
  search.Q = '';
  search.type = '';
  search.source = '';
  pageIndex.value = 1;
  load();
}

function openAdd() {
  editingCode.value = '';
  Object.assign(form, {
    lovCode: '',
    name: '',
    type: 'ENUM',
    valueField: '',
    labelField: '',
    remark: '',
    enabled: true
  });
  dialogVisible.value = true;
}

function openEdit(row: LovRow) {
  editingCode.value = row.id;
  Object.assign(form, {
    lovCode: row.lovCode.replace(/^(Enum|List)\./i, ''),
    name: row.name ?? '',
    type: row.type,
    valueField: row.valueField ?? '',
    labelField: row.labelField ?? '',
    remark: row.remark ?? '',
    enabled: row.enabled !== false
  });
  dialogVisible.value = true;
}

function onTypeChange() {
  form.lovCode = form.lovCode.replace(/^(Enum|List)\./i, '');
}

async function save() {
  const code = form.lovCode.trim().replace(/^(Enum|List)\./i, '');
  if (!code) {
    ElMessage.warning($t('lovAdmin.codeRequired'));
    return;
  }

  const payload: Record<string, unknown> = {
    lovCode: `${form.type}.${code}`,
    name: form.name.trim() || code,
    type: form.type,
    valueField: form.type === 'LIST' ? form.valueField.trim() : '',
    labelField: form.type === 'LIST' ? form.labelField.trim() : '',
    remark: form.remark,
    enabled: form.enabled
  };

  saving.value = true;
  try {
    if (editingCode.value) {
      payload.id = `${form.type}.${code}`;
      await cubeApi.page.update(TYPE_PATH, payload, editingCode.value);
      ElMessage.success($t('common.updateSuccess'));
    } else {
      const res = await cubeApi.page.add(TYPE_PATH, payload);
      const saved = ((res as { data?: { id?: string } })?.data ?? {}) as { id?: string };
      ElMessage.success($t('common.addSuccess'));
      // 新建列表型值集后直接进入配置弹窗
      if (form.type === 'LIST') {
        configCode.value = saved.id || `${form.type}.${code}`;
        configVisible.value = true;
      }
    }
    dialogVisible.value = false;
    await load();
  } catch {
    ElMessage.error($t('lovAdmin.saveFailed'));
  } finally {
    saving.value = false;
  }
}

async function remove(row: LovRow) {
  try {
    await ElMessageBox.confirm(
      $t('lovAdmin.deleteConfirm', { code: row.lovCode }),
      $t('lovAdmin.confirmDelete'),
      {
        confirmButtonText: $t('common.delete'),
        cancelButtonText: $t('common.cancel'),
        type: 'warning'
      }
    );
  } catch {
    return;
  }

  try {
    await cubeApi.page.remove(TYPE_PATH, row.id);
    ElMessage.success($t('common.deleteSuccess'));
    await load();
  } catch {
    ElMessage.error($t('lovAdmin.deleteFailed'));
  }
}

function openConfig(row: LovRow) {
  configCode.value = row.id;
  configVisible.value = true;
}

onMounted(load);
</script>

<template>
  <div class="lov-page">
    <PageHeader :title="$t('lovAdmin.title')" :subtitle="$t('lovAdmin.subtitle')" />

    <ElCard shadow="never" class="lov-card">
      <ElForm :inline="true" class="lov-page__search">
        <ElFormItem :label="$t('lovAdmin.keyword')">
          <ElInput
            v-model="search.Q"
            :placeholder="$t('lovAdmin.keywordHint')"
            clearable
            style="width: 220px"
            @keyup.enter="handleSearch"
          />
        </ElFormItem>
        <ElFormItem :label="$t('lovAdmin.type')">
          <ElSelect v-model="search.type" clearable :placeholder="$t('lovAdmin.all')" style="width: 120px">
            <ElOption :label="$t('lovAdmin.typeEnum')" value="ENUM" />
            <ElOption :label="$t('lovAdmin.typeList')" value="LIST" />
          </ElSelect>
        </ElFormItem>
        <ElFormItem :label="$t('lovAdmin.source')">
          <ElSelect v-model="search.source" clearable :placeholder="$t('lovAdmin.all')" style="width: 120px">
            <ElOption :label="$t('lovAdmin.sourceAuto')" value="AUTO" />
            <ElOption :label="$t('lovAdmin.sourceManual')" value="MANUAL" />
          </ElSelect>
        </ElFormItem>
        <ElFormItem>
          <ElButton type="primary" @click="handleSearch">{{ $t('common.search') }}</ElButton>
          <ElButton @click="handleReset">{{ $t('common.reset') }}</ElButton>
        </ElFormItem>
      </ElForm>

      <div class="lov-page__toolbar">
        <ElButton type="primary" @click="openAdd">{{ $t('lovAdmin.add') }}</ElButton>
        <ElButton :loading="loading" @click="load">{{ $t('common.refresh') }}</ElButton>
      </div>

      <ElTable v-loading="loading" :data="rows" border row-key="id">
        <ElTableColumn :label="$t('lovAdmin.code')" min-width="220">
          <template #default="{ row }">
            <ElTag :type="row.type === 'ENUM' ? 'primary' : 'success'" size="small">{{ row.type }}</ElTag>
            <code class="lov-page__code">{{ row.lovCode }}</code>
          </template>
        </ElTableColumn>
        <ElTableColumn prop="name" :label="$t('lovAdmin.displayName')" min-width="140" />
        <ElTableColumn prop="valueField" :label="$t('lovAdmin.valueField')" min-width="90">
          <template #default="{ row }">{{ row.valueField || '—' }}</template>
        </ElTableColumn>
        <ElTableColumn prop="labelField" :label="$t('lovAdmin.labelField')" min-width="90">
          <template #default="{ row }">{{ row.labelField || '—' }}</template>
        </ElTableColumn>
        <ElTableColumn :label="$t('lovAdmin.source')" width="90" align="center">
          <template #default="{ row }">
            <ElTag v-if="row.source === 'AUTO'" type="info" size="small">{{ $t('lovAdmin.sourceAuto') }}</ElTag>
            <ElTag v-else size="small">{{ $t('lovAdmin.sourceManual') }}</ElTag>
          </template>
        </ElTableColumn>
        <ElTableColumn :label="$t('lovAdmin.enabled')" width="80" align="center">
          <template #default="{ row }">
            <ElTag :type="row.enabled ? 'success' : 'danger'" size="small">
              {{ row.enabled ? $t('common.yesOrNo.yes') : $t('common.yesOrNo.no') }}
            </ElTag>
          </template>
        </ElTableColumn>
        <ElTableColumn prop="createTime" :label="$t('lovAdmin.createTime')" width="165" />
        <ElTableColumn :label="$t('common.action')" width="230" fixed="right" align="center">
          <template #default="{ row }">
            <ElButton size="small" @click="openEdit(row)">{{ $t('common.edit') }}</ElButton>
            <ElButton size="small" type="primary" plain @click="openConfig(row)">
              {{ row.type === 'ENUM' ? $t('lovAdmin.enumValues') : $t('lovAdmin.listConfig') }}
            </ElButton>
            <ElButton size="small" type="danger" text @click="remove(row)">{{ $t('common.delete') }}</ElButton>
          </template>
        </ElTableColumn>
        <template #empty>
          <ElEmpty :description="$t('common.noData')" :image-size="80" />
        </template>
      </ElTable>

      <ElPagination
        v-model:current-page="pageIndex"
        v-model:page-size="pageSize"
        class="lov-page__pager"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        background
        @current-change="load"
        @size-change="
          () => {
            pageIndex = 1;
            load();
          }
        "
      />
    </ElCard>

    <ElDialog
      v-model="dialogVisible"
      :title="editingCode ? $t('lovAdmin.editTitle') : $t('lovAdmin.addTitle')"
      width="560px"
      :close-on-click-modal="false"
    >
      <ElForm :model="form" label-width="100px">
        <ElFormItem :label="$t('lovAdmin.lovType')" required>
          <ElSelect v-model="form.type" style="width: 100%" @change="onTypeChange">
            <ElOption :label="$t('lovAdmin.typeEnumFull')" value="ENUM" />
            <ElOption :label="$t('lovAdmin.typeListFull')" value="LIST" />
          </ElSelect>
        </ElFormItem>
        <ElFormItem :label="$t('lovAdmin.code')" required>
          <ElInput v-model="form.lovCode">
            <template #prepend>{{ form.type === 'ENUM' ? 'Enum.' : 'List.' }}</template>
          </ElInput>
          <div class="lov-page__tip">{{ $t('lovAdmin.codeTip') }}</div>
        </ElFormItem>
        <ElFormItem :label="$t('lovAdmin.displayName')">
          <ElInput v-model="form.name" :placeholder="$t('lovAdmin.displayNameHint')" />
        </ElFormItem>
        <template v-if="form.type === 'LIST'">
          <ElFormItem :label="$t('lovAdmin.valueField')">
            <ElInput v-model="form.valueField" :placeholder="$t('lovAdmin.valueFieldHint')" />
          </ElFormItem>
          <ElFormItem :label="$t('lovAdmin.labelField')">
            <ElInput v-model="form.labelField" :placeholder="$t('lovAdmin.labelFieldHint')" />
          </ElFormItem>
        </template>
        <ElFormItem :label="$t('lovAdmin.remark')">
          <ElInput v-model="form.remark" type="textarea" :rows="2" />
        </ElFormItem>
        <ElFormItem :label="$t('lovAdmin.enabled')">
          <ElSwitch v-model="form.enabled" />
        </ElFormItem>
      </ElForm>
      <template #footer>
        <ElButton @click="dialogVisible = false">{{ $t('common.cancel') }}</ElButton>
        <ElButton type="primary" :loading="saving" @click="save">{{ $t('common.confirm') }}</ElButton>
      </template>
    </ElDialog>

    <LovConfigDialog v-model="configVisible" :lov-code="configCode" @saved="load" />
  </div>
</template>

<style scoped>
.lov-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.lov-page :deep(.el-card__header) {
  padding-top: 16px;
}

.lov-card {
  border-radius: 8px;
}

.lov-page__search {
  margin-bottom: 4px;
}

.lov-page__toolbar {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

.lov-page__code {
  margin-left: 8px;
  font-family: var(--el-font-family-mono, monospace);
}

.lov-page__pager {
  margin-top: 16px;
  justify-content: flex-end;
}

.lov-page__tip {
  width: 100%;
  margin-top: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
