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
                  <a-dropdown v-if="canInspect || canCompact" trigger="click" position="br">
                    <a-button size="mini">
                      更多 <icon-park type="down" :size="12" />
                    </a-button>
                    <template #content>
                      <a-doption v-if="canInspect" @click="openDrawer(row.name)">
                        <template #icon><icon-park type="cube-three" /></template>
                        实体
                      </a-doption>
                      <a-doption
                        v-if="canCompact"
                        :disabled="!!compactingName"
                        @click="confirmCompact(row.name)"
                      >
                        <template #icon><icon-park type="tool" /></template>
                        {{ compactingName === row.name ? '压缩中…' : '压缩' }}
                      </a-doption>
                    </template>
                  </a-dropdown>
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
      <div class="db-drawer-content">
        <a-alert v-if="drawerError" type="warning" show-icon>{{ drawerError }}</a-alert>
        <div v-else-if="drawerLoading" class="db-drawer-state">
          <a-spin :loading="true" />
          <span>正在加载…</span>
        </div>
        <div v-else-if="drawerMode === 'fields'">
          <div class="db-drawer-toolbar">
            <a-link @click="backToEntities">← 返回实体列表</a-link>
            <span class="db-stat">{{ drawerEntityType }} · 共 {{ fieldRows.length }} 个字段</span>
          </div>
          <a-empty v-if="!fieldRows.length" description="无字段" />
          <!-- 列对齐 CubeNC Db/Entities.cshtml 字段架构定义：显示名在首列，字段名为技术名；均可省略+Tooltip -->
          <a-table v-else :data="fieldRows" :pagination="false" size="small" :scroll="{ x: 940 }">
            <template #columns>
              <a-table-column title="显示名" :width="110" :ellipsis="true" tooltip data-index="displayName" />
              <a-table-column title="字段名" :width="130" :ellipsis="true" tooltip data-index="name" />
              <a-table-column title="类型" :width="130" :ellipsis="true" tooltip data-index="type" />
              <a-table-column title="长度" :width="64" align="right">
                <template #cell="{ record }">{{ record.length > 0 ? record.length : '' }}</template>
              </a-table-column>
              <a-table-column title="精度" :width="88" align="right">
                <template #cell="{ record }">
                  {{ record.precision > 0 || record.scale > 0 ? `(${record.precision}, ${record.scale})` : '' }}
                </template>
              </a-table-column>
              <a-table-column title="主键" :width="64">
                <template #cell="{ record }">
                  <a-tooltip
                    v-if="record.key"
                    :content="record.key === 'AI' ? '自增' : record.key === 'PK' ? '主键' : '唯一索引'"
                  >
                    <span>{{ record.key }}</span>
                  </a-tooltip>
                </template>
              </a-table-column>
              <a-table-column title="允许空" :width="84">
                <template #cell="{ record }">{{ record.nullable ? '' : 'N' }}</template>
              </a-table-column>
              <a-table-column title="备注" :width="260" :ellipsis="true" tooltip data-index="description" />
            </template>
          </a-table>
        </div>
        <div v-else>
          <a-empty v-if="!drawerRows.length" description="无数据" />
          <!-- 列必须声明在 #columns 插槽：默认插槽会被 Arco 当作裸 <table> 内容渲染，列不注册导致表格空白（OSC-2610012e35） -->
          <a-table v-else :data="drawerRows" :pagination="false" size="small" :scroll="{ x: 680 }">
            <template #columns>
              <!-- 显示名=描述首句（点击进入数据字典）；备注=描述余下部分，置于最后一列 -->
              <a-table-column title="显示名" :width="140" :ellipsis="true" tooltip>
                <template #cell="{ record }">
                  <a-link @click="openDictionary(record)">{{ dbEntityNameText(record) }}</a-link>
                </template>
              </a-table-column>
              <a-table-column title="表名" :width="150" :ellipsis="true" tooltip data-index="tableName" />
              <a-table-column title="行数" :width="72" align="right">
                <template #cell="{ record }">{{ record.count ?? '-' }}</template>
              </a-table-column>
              <a-table-column title="备注" :width="320" :ellipsis="true" tooltip>
                <template #cell="{ record }">{{ dbEntityRemarkText(record) }}</template>
              </a-table-column>
            </template>
          </a-table>
        </div>
      </div>
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
  drawerEntityType,
  fieldRows,
  drawerWidth,
  load,
  confirmBackup,
  downloadSchema,
  openDrawer,
  openDictionary,
  backToEntities,
  dbEntityNameText,
  dbEntityRemarkText,
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
.db-drawer-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 160px;
  gap: 12px;
  color: var(--color-text-3);
}
.db-drawer-content {
  min-height: 160px;
}
.db-drawer-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
/* 数据字典表头不折行（如「允许空」）；数字列右对齐由列 align 控制 */
.db-drawer-content :deep(.arco-table-th) {
  white-space: nowrap;
}
</style>
