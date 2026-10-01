<template>
  <div class="db-page">
    <!-- 主题表面：与实体对象列表 list-panel / 对象页 obj-surface 同源主题外壳 -->
    <div class="db-surface">
      <!-- 顶部工具栏：统计居左、操作居右 -->
      <div class="db-toolbar">
        <span class="db-stat">共 {{ rows.length }} 个数据库</span>
        <a-button :loading="loading" @click="load">刷新</a-button>
      </div>
      <a-alert v-if="error" type="warning" show-icon class="db-alert">{{ error }}</a-alert>
      <a-spin :loading="loading" style="display: block">
        <a-empty v-if="!loading && !rows.length" description="暂无数据库" />
        <a-grid v-else :cols="{ xs: 1, sm: 2, md: 3 }" :col-gap="16" :row-gap="16">
          <a-grid-item v-for="row in rows" :key="row.name">
            <a-card hoverable class="db-card">
              <template #title>{{ row.name }}</template>
              <a-descriptions :column="1" size="small">
                <a-descriptions-item label="类型">{{ row.type || '-' }}</a-descriptions-item>
                <a-descriptions-item label="版本">{{ row.version || '-' }}</a-descriptions-item>
                <a-descriptions-item label="备份数">{{ row.backups }}</a-descriptions-item>
              </a-descriptions>
              <!-- 卡片底部操作区（参照实体对象卡片视图）：三个操作均针对本卡片数据库 -->
              <template #actions>
                <a-space :size="4">
                  <template v-if="canBackup">
                    <a-button
                      size="mini"
                      type="primary"
                      :loading="busy"
                      @click="confirmBackup(row.name, false)"
                    >
                      数据库备份
                    </a-button>
                    <a-button
                      size="mini"
                      status="success"
                      :loading="busy"
                      @click="confirmBackup(row.name, true)"
                    >
                      备份并压缩
                    </a-button>
                  </template>
                  <a-button size="mini" @click="downloadSchema(row.name)">下载架构</a-button>
                  <template v-if="canInspect">
                    <a-button size="mini" @click="openDrawer(row.name, 'tables')">表</a-button>
                    <a-button size="mini" @click="openDrawer(row.name, 'entities')">实体</a-button>
                    <a-button size="mini" @click="openDrawer(row.name, 'diff')">差异</a-button>
                  </template>
                  <a-button
                    v-if="canCompact"
                    size="mini"
                    status="warning"
                    :loading="compactingName === row.name"
                    :disabled="!!compactingName && compactingName !== row.name"
                    @click="confirmCompact(row.name)"
                  >
                    压缩
                  </a-button>
                </a-space>
              </template>
            </a-card>
          </a-grid-item>
        </a-grid>
      </a-spin>
    </div>
    <a-drawer
      v-model:visible="drawerVisible"
      :title="drawerTitle"
      :width="drawerWidth"
      :footer="false"
      placement="right"
      unmount-on-close
    >
      <a-alert v-if="drawerError" type="warning" show-icon>{{ drawerError }}</a-alert>
      <a-spin v-else :loading="drawerLoading" style="display: block">
        <template v-if="drawerMode === 'diff'">
          <a-empty v-if="!drawerLoading && !diffRows.length" description="无差异" />
          <a-table v-else :data="diffRows" :pagination="false" size="small">
            <a-table-column title="表名" data-index="tableName" />
            <a-table-column title="实体" data-index="name">
              <template #cell="{ record }">{{ record.hasEntityModel ? record.name : '无实体' }}</template>
            </a-table-column>
            <a-table-column title="列名" data-index="columnName" />
            <a-table-column title="类型" data-index="dataType" />
          </a-table>
        </template>
        <template v-else>
          <a-empty v-if="!drawerLoading && !drawerRows.length" description="无数据" />
          <a-table v-else :data="drawerRows" :pagination="false" size="small">
            <a-table-column title="名称" data-index="name" />
            <a-table-column title="表名" data-index="tableName" />
            <a-table-column title="行数" data-index="count">
              <template #cell="{ record }">{{ record.count ?? '-' }}</template>
            </a-table-column>
          </a-table>
        </template>
      </a-spin>
    </a-drawer>
  </div>
</template>

<script setup lang="ts">
/**
 * Admin/Db 专用页（OSC-2608139feb）：数据库列表卡片 + 每卡片的备份/备份并压缩/下载架构。
 * 后端 Backup/BackupAndCompress/Download 的 name 均为连接名，操作针对指定数据库。
 */
import { useDbPage } from './useDbPage';

const {
  rows,
  loading,
  error,
  busy,
  canBackup,
  canInspect,
  canCompact,
  compactingName,
  drawerVisible,
  drawerLoading,
  drawerError,
  drawerMode,
  drawerTitle,
  drawerRows,
  diffRows,
  drawerWidth,
  load,
  confirmBackup,
  downloadSchema,
  openDrawer,
  confirmCompact,
} = useDbPage();
</script>

<style scoped>
.db-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
.db-surface {
  min-width: 0;
  padding: 16px 16px 8px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
/* 顶部工具栏：统计居左、刷新居右 */
.db-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.db-stat {
  font-size: 13px;
  color: var(--color-text-3);
}
.db-alert {
  margin-bottom: 12px;
}
.db-card {
  height: 100%;
}
/* 卡片底部操作按钮左对齐 */
.db-card :deep(.arco-card-actions) {
  justify-content: flex-start;
}
</style>
