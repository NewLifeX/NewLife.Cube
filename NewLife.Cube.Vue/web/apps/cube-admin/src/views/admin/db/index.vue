<script setup lang="ts">
/**
 * 数据库管理（M3 自定义页）
 *
 * 元数据表达不了：数据库清单、表结构/实体结构诊断、模型差异与备份动作。
 * 数据全部来自 `DbController` 的 JSON 接口，状态与动作收在 `useDbAdmin`，本文件只做渲染。
 */
import { onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { columnFlag, precisionText } from '@newlifex/cube-vue/core/utils/dbSchema';
import { useDbAdmin } from './useDbAdmin';

const { t } = useI18n();
const {
  databases,
  loading,
  schemaVisible,
  schemaLoading,
  schemaMode,
  schemas,
  diffVisible,
  diffLoading,
  diffs,
  loadDatabases,
  openSchema,
  openDiff,
  compact,
  backup,
  downloadSchema,
} = useDbAdmin();

onMounted(() => loadDatabases());
</script>

<template>
  <div class="db-page">
    <div class="page-header">
      <h1>{{ t('db.title') }}</h1>
      <p>{{ t('db.subtitle') }}</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>{{ t('db.listTitle') }}</span>
          <el-button size="small" :loading="loading" @click="loadDatabases()">
            {{ t('grid.refresh') }}
          </el-button>
        </div>
      </template>

      <el-table v-loading="loading" :data="databases" size="small" border>
        <el-table-column prop="name" :label="t('common.name')" min-width="120" />
        <el-table-column prop="type" :label="t('db.type')" width="110" />
        <el-table-column prop="version" :label="t('db.version')" min-width="150" show-overflow-tooltip />
        <el-table-column prop="driver" :label="t('db.driver')" min-width="150" show-overflow-tooltip />
        <el-table-column prop="driverVersion" :label="t('db.driverVersion')" width="120" />

        <el-table-column :label="t('db.entities')" width="90" align="center">
          <template #default="{ row }">
            <el-link type="primary" @click="openSchema('entities', row.name)">{{ row.entities }}</el-link>
          </template>
        </el-table-column>
        <el-table-column :label="t('db.tables')" width="90" align="center">
          <template #default="{ row }">
            <el-link type="primary" @click="openSchema('tables', row.name)">{{ row.tables }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="backups" :label="t('db.backups')" width="90" align="center" />

        <el-table-column :label="t('common.actions')" width="400" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openSchema('tables', row.name)">
              {{ t('db.viewTables') }}
            </el-button>
            <el-button link type="primary" @click="openSchema('entities', row.name)">
              {{ t('db.viewEntities') }}
            </el-button>
            <el-button link type="primary" @click="openDiff(row.name)">{{ t('db.viewDiff') }}</el-button>
            <el-button link type="primary" @click="compact(row.name)">{{ t('db.compact') }}</el-button>
            <el-button link type="primary" @click="backup(row.name, false)">{{ t('db.backup') }}</el-button>
            <el-button link type="primary" @click="backup(row.name, true)">{{ t('db.backupAndCompress') }}</el-button>
            <el-button link type="primary" @click="downloadSchema(row.name)">{{ t('db.downloadSchema') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog
      v-model="schemaVisible"
      :title="schemaMode === 'tables' ? t('db.tableStructureTitle') : t('db.entityStructureTitle')"
      width="80%"
      top="6vh"
    >
      <div v-loading="schemaLoading" class="schema-body">
        <el-empty v-if="!schemas.length" :description="t('common.noData')" />
        <div v-for="item in schemas" :key="item.tableName" class="schema-table">
          <h3 class="schema-title">
            {{ item.displayName || item.name }}
            <span class="muted">({{ item.tableName }}) [{{ item.count }}]</span>
          </h3>
          <el-table :data="item.columns" size="small" border max-height="360">
            <el-table-column prop="name" :label="t('db.columnName')" min-width="140" show-overflow-tooltip />
            <el-table-column prop="displayName" :label="t('db.displayName')" min-width="140" show-overflow-tooltip />
            <el-table-column prop="type" :label="t('db.dataType')" width="120" />
            <el-table-column :label="t('db.length')" width="80" align="center">
              <template #default="{ row }">{{ row.length || '' }}</template>
            </el-table-column>
            <el-table-column :label="t('db.precision')" width="110" align="center">
              <template #default="{ row }">{{ precisionText(row) }}</template>
            </el-table-column>
            <el-table-column :label="t('db.keyFlag')" width="70" align="center">
              <template #default="{ row }">{{ columnFlag(row) }}</template>
            </el-table-column>
            <el-table-column :label="t('db.nullable')" width="70" align="center">
              <template #default="{ row }">{{ row.nullable ? '' : 'N' }}</template>
            </el-table-column>
            <el-table-column prop="description" :label="t('db.description')" min-width="180" show-overflow-tooltip />
          </el-table>
        </div>
      </div>
      <template #footer>
        <el-button @click="schemaVisible = false">{{ t('button.close') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="diffVisible" :title="t('db.diffTitle')" width="70%" top="6vh">
      <div v-loading="diffLoading" class="diff-body">
        <el-empty v-if="!diffs.length" :description="t('db.diffEmpty')" />
        <div v-for="item in diffs" :key="item.tableName" class="diff-item">
          <h3 class="schema-title">
            {{ item.name }} <span class="muted">({{ item.tableName }})</span>
            <el-tag v-if="!item.hasEntityModel" size="small" type="warning">
              {{ t('db.noEntityModel') }}
            </el-tag>
          </h3>
          <p class="diff-columns">{{ t('db.extraColumns') }}: {{ item.extraColumns.join(', ') }}</p>
          <pre class="diff-xml">{{ item.xmlFragment }}</pre>
        </div>
      </div>
      <template #footer>
        <el-button @click="diffVisible = false">{{ t('button.close') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page-header {
  margin-bottom: 20px;
}

.page-header h1 {
  margin: 0 0 8px;
  font-size: 24px;
  font-weight: 500;
}

.page-header p {
  margin: 0;
  color: var(--el-text-color-secondary);
  font-size: 14px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.schema-table + .schema-table {
  margin-top: 16px;
}

.schema-title {
  margin: 0 0 8px;
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.muted {
  margin-left: 6px;
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
}

.diff-item + .diff-item {
  margin-top: 16px;
}

.diff-columns {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--el-text-color-regular);
}

.diff-xml {
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
