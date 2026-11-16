<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
  columnFlag,
  normalizeDbList,
  normalizeDiffList,
  normalizeSchemaList,
  precisionText,
  type DbDiffView,
  type DbItemView,
  type DbSchemaTableView
} from '@newlifex/page-logic';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import PageHeader from '../components/page-header.vue';

defineOptions({ name: 'DbAdminPage' });

const databases = ref<DbItemView[]>([]);
const loading = ref(false);
const acting = ref('');

const schemaVisible = ref(false);
const schemaLoading = ref(false);
const schemaMode = ref<'tables' | 'entities'>('tables');
const schemas = ref<DbSchemaTableView[]>([]);

const diffVisible = ref(false);
const diffLoading = ref(false);
const diffs = ref<DbDiffView[]>([]);

async function loadDatabases() {
  loading.value = true;
  try {
    const res = await cubeApi.client.get('/Admin/Db');
    databases.value = normalizeDbList(res.data);
  } catch {
    databases.value = [];
    ElMessage.error($t('page.list.loadFailed'));
  } finally {
    loading.value = false;
  }
}

async function openSchema(mode: 'tables' | 'entities', name: string) {
  schemaMode.value = mode;
  schemaVisible.value = true;
  schemaLoading.value = true;
  schemas.value = [];
  try {
    const res = await cubeApi.page.getAction('Admin/Db', mode, { name });
    schemas.value = normalizeSchemaList(res);
  } catch {
    schemas.value = [];
  } finally {
    schemaLoading.value = false;
  }
}

async function openDiff(name: string) {
  diffVisible.value = true;
  diffLoading.value = true;
  diffs.value = [];
  try {
    const res = await cubeApi.page.getAction('Admin/Db', 'diff', { name });
    diffs.value = normalizeDiffList(res);
  } catch {
    diffs.value = [];
  } finally {
    diffLoading.value = false;
  }
}

async function compact(name: string) {
  acting.value = `${name}:compact`;
  try {
    await cubeApi.client.post('/Admin/Db/compact', null, { params: { name } });
    ElMessage.success($t('dbAdmin.compactSuccess'));
    await loadDatabases();
  } catch {
    ElMessage.error($t('dbAdmin.compactFailed'));
  } finally {
    acting.value = '';
  }
}

async function backup(name: string, compress: boolean) {
  if (compress) {
    try {
      await ElMessageBox.confirm($t('dbAdmin.compressConfirm'), $t('dbAdmin.confirmAction'), {
        confirmButtonText: $t('common.confirm'),
        cancelButtonText: $t('common.cancel'),
        type: 'warning'
      });
    } catch {
      return;
    }
  }

  acting.value = `${name}:${compress ? 'compress' : 'backup'}`;
  try {
    const url = compress ? '/Admin/Db/BackupAndCompress' : '/Admin/Db/Backup';
    await cubeApi.client.post(url, null, { params: { name } });
    ElMessage.success(compress ? $t('dbAdmin.compressSuccess') : $t('dbAdmin.backupSuccess'));
    await loadDatabases();
  } catch {
    ElMessage.error(compress ? $t('dbAdmin.compressFailed') : $t('dbAdmin.backupFailed'));
  } finally {
    acting.value = '';
  }
}

async function downloadSchema(name: string) {
  acting.value = `${name}:download`;
  try {
    const res = await cubeApi.client.get<Blob>('/Admin/Db/Download', {
      params: { name },
      responseType: 'blob'
    });
    const url = window.URL.createObjectURL(res.data);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${name}.xml`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  } catch {
    ElMessage.error($t('dbAdmin.downloadFailed'));
  } finally {
    acting.value = '';
  }
}

onMounted(loadDatabases);
</script>

<template>
  <div class="db-page">
    <PageHeader :title="$t('dbAdmin.title')" :subtitle="$t('dbAdmin.subtitle')">
      <template #extra>
        <ElButton :loading="loading" @click="loadDatabases">{{ $t('common.refresh') }}</ElButton>
      </template>
    </PageHeader>

    <ElCard shadow="never" class="db-page__card">
      <template #header>
        <span class="db-page__card-title">{{ $t('dbAdmin.listTitle') }}</span>
      </template>

      <ElTable v-loading="loading" :data="databases" border stripe>
        <ElTableColumn prop="name" :label="$t('dbAdmin.name')" min-width="120" />
        <ElTableColumn prop="type" :label="$t('dbAdmin.type')" width="90" align="center" />
        <ElTableColumn prop="version" :label="$t('dbAdmin.version')" min-width="130" show-overflow-tooltip />
        <ElTableColumn prop="driver" :label="$t('dbAdmin.driver')" min-width="160" show-overflow-tooltip />
        <ElTableColumn prop="driverVersion" :label="$t('dbAdmin.driverVersion')" width="120" />
        <ElTableColumn :label="$t('dbAdmin.entities')" width="90" align="center">
          <template #default="{ row }">
            <ElLink type="primary" @click="openSchema('entities', row.name)">{{ row.entities }}</ElLink>
          </template>
        </ElTableColumn>
        <ElTableColumn :label="$t('dbAdmin.tables')" width="90" align="center">
          <template #default="{ row }">
            <ElLink type="primary" @click="openSchema('tables', row.name)">{{ row.tables }}</ElLink>
          </template>
        </ElTableColumn>
        <ElTableColumn prop="backups" :label="$t('dbAdmin.backups')" width="90" align="center" />
        <ElTableColumn :label="$t('common.action')" width="330" fixed="right">
          <template #default="{ row }">
            <ElButton link type="primary" @click="openSchema('tables', row.name)">{{ $t('dbAdmin.viewTables') }}</ElButton>
            <ElButton link type="primary" @click="openSchema('entities', row.name)">
              {{ $t('dbAdmin.viewEntities') }}
            </ElButton>
            <ElButton link type="primary" @click="openDiff(row.name)">{{ $t('dbAdmin.viewDiff') }}</ElButton>
            <ElButton
              link
              type="primary"
              :disabled="acting === `${row.name}:compact`"
              @click="compact(row.name)"
            >
              {{ $t('dbAdmin.compact') }}
            </ElButton>
            <ElDropdown trigger="click">
              <ElButton link type="primary">
                {{ $t('dbAdmin.backup') }}
                <SvgIcon icon="mdi:chevron-down" class="ml-4px text-icon" />
              </ElButton>
              <template #dropdown>
                <ElDropdownMenu>
                  <ElDropdownItem @click="backup(row.name, false)">{{ $t('dbAdmin.backup') }}</ElDropdownItem>
                  <ElDropdownItem @click="backup(row.name, true)">{{ $t('dbAdmin.backupAndCompress') }}</ElDropdownItem>
                  <ElDropdownItem divided @click="downloadSchema(row.name)">
                    {{ $t('dbAdmin.downloadSchema') }}
                  </ElDropdownItem>
                </ElDropdownMenu>
              </template>
            </ElDropdown>
          </template>
        </ElTableColumn>
        <template #empty>
          <ElEmpty :description="$t('common.noData')" :image-size="90" />
        </template>
      </ElTable>
    </ElCard>

    <ElDialog
      v-model="schemaVisible"
      :title="schemaMode === 'tables' ? $t('dbAdmin.tableStructureTitle') : $t('dbAdmin.entityStructureTitle')"
      width="80%"
      top="6vh"
      destroy-on-close
    >
      <div v-loading="schemaLoading" class="db-page__schema">
        <ElEmpty v-if="!schemas.length" :description="$t('common.noData')" />
        <div v-for="item in schemas" :key="item.tableName" class="db-page__schema-table">
          <h3 class="db-page__schema-title">
            {{ item.displayName || item.name }}
            <span class="db-page__muted">({{ item.tableName }}) [{{ item.count }}]</span>
          </h3>
          <ElTable :data="item.columns" border stripe max-height="360">
            <ElTableColumn prop="name" :label="$t('dbAdmin.columnName')" min-width="140" show-overflow-tooltip />
            <ElTableColumn prop="displayName" :label="$t('dbAdmin.displayName')" min-width="140" show-overflow-tooltip />
            <ElTableColumn prop="type" :label="$t('dbAdmin.dataType')" width="120" />
            <ElTableColumn :label="$t('dbAdmin.length')" width="80" align="center">
              <template #default="{ row }">{{ row.length || '' }}</template>
            </ElTableColumn>
            <ElTableColumn :label="$t('dbAdmin.precision')" width="110" align="center">
              <template #default="{ row }">{{ precisionText(row) }}</template>
            </ElTableColumn>
            <ElTableColumn :label="$t('dbAdmin.keyFlag')" width="70" align="center">
              <template #default="{ row }">{{ columnFlag(row) }}</template>
            </ElTableColumn>
            <ElTableColumn :label="$t('dbAdmin.nullable')" width="80" align="center">
              <template #default="{ row }">{{ row.nullable ? $t('common.yesOrNo.yes') : $t('common.yesOrNo.no') }}</template>
            </ElTableColumn>
            <ElTableColumn prop="description" :label="$t('dbAdmin.description')" min-width="180" show-overflow-tooltip />
          </ElTable>
        </div>
      </div>
      <template #footer>
        <ElButton @click="schemaVisible = false">{{ $t('common.close') }}</ElButton>
      </template>
    </ElDialog>

    <ElDialog v-model="diffVisible" :title="$t('dbAdmin.diffTitle')" width="70%" top="6vh" destroy-on-close>
      <div v-loading="diffLoading" class="db-page__diff">
        <ElEmpty v-if="!diffs.length" :description="$t('dbAdmin.diffEmpty')" />
        <div v-for="item in diffs" :key="item.tableName" class="db-page__diff-item">
          <h3 class="db-page__schema-title">
            {{ item.name }} <span class="db-page__muted">({{ item.tableName }})</span>
            <ElTag v-if="!item.hasEntityModel" size="small" type="warning">
              {{ $t('dbAdmin.noEntityModel') }}
            </ElTag>
          </h3>
          <p class="db-page__diff-columns">{{ $t('dbAdmin.extraColumns') }}: {{ item.extraColumns.join(', ') }}</p>
          <pre class="db-page__diff-xml">{{ item.xmlFragment }}</pre>
        </div>
      </div>
      <template #footer>
        <ElButton @click="diffVisible = false">{{ $t('common.close') }}</ElButton>
      </template>
    </ElDialog>
  </div>
</template>

<style scoped>
.db-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.db-page :deep(.el-card__header) {
  padding-top: 16px;
}

.db-page__card-title {
  font-weight: 700;
}

.db-page__schema-table + .db-page__schema-table {
  margin-top: 16px;
}

.db-page__schema-title {
  margin: 0 0 8px;
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.db-page__muted {
  margin-left: 6px;
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
}

.db-page__diff-item + .db-page__diff-item {
  margin-top: 16px;
}

.db-page__diff-columns {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--el-text-color-regular);
}

.db-page__diff-xml {
  margin: 0;
  padding: 12px;
  max-height: 280px;
  overflow: auto;
  font-size: 12px;
  line-height: 1.6;
  background: var(--el-fill-color-light);
  border-radius: var(--el-border-radius-base);
}
</style>
