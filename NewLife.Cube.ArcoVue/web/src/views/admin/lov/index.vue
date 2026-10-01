<template>
  <div class="lov-page list-surface">
    <!-- 表面面板：与实体 DefaultList 的 list-panel--table 同源外壳与内边距 -->
    <div class="list-panel list-panel--table">
      <!-- 工具栏：与 list-topbar 相同左右分区 -->
      <div class="list-topbar">
        <a-space>
          <a-button v-if="flags.canAdd" type="primary" size="small" @click="openAdd">+ 添加值集</a-button>
        </a-space>
        <a-space>
          <a-input
            v-model="keyword"
            placeholder="关键字"
            allow-clear
            size="small"
            style="width: 200px"
            @press-enter="load"
          />
          <a-button type="primary" size="small" @click="load">搜索</a-button>
          <a-button :loading="loading" size="small" @click="load">刷新</a-button>
        </a-space>
      </div>

      <a-alert v-if="!flags.canView" type="warning" show-icon class="lov-alert">
        无权查看值集
      </a-alert>
      <a-alert v-else-if="error" type="warning" show-icon class="lov-alert">{{ error }}</a-alert>

      <a-spin v-if="flags.canView" :loading="loading" style="display: block">
        <a-table
          class="lov-table"
          :data="filteredRows"
          :pagination="false"
          row-key="id"
          size="small"
          :bordered="false"
          :scroll="{ x: '100%' }"
        >
          <template #columns>
            <a-table-column title="编码" data-index="lovCode" :width="220" />
            <a-table-column title="名称" data-index="name" :width="160" />
            <a-table-column title="类型" :width="110">
              <template #cell="{ record }">
                {{ lovTypeLabel(record.type) }}
              </template>
            </a-table-column>
            <a-table-column title="启用" :width="80">
              <template #cell="{ record }">
                <a-tag :color="record.enabled ? 'green' : 'red'" size="small">
                  {{ record.enabled ? '是' : '否' }}
                </a-tag>
              </template>
            </a-table-column>
            <a-table-column title="操作" :width="220" fixed="right">
              <template #cell="{ record }">
                <a-space :size="4">
                  <a-button size="mini" type="text" @click="openConfig(record)">
                    配置
                  </a-button>
                  <a-button
                    v-if="flags.canEdit"
                    size="mini"
                    type="text"
                    @click="openEdit(record)"
                  >
                    编辑
                  </a-button>
                  <a-button
                    v-if="flags.canDelete"
                    size="mini"
                    type="text"
                    status="danger"
                    @click="confirmDelete(record)"
                  >
                    删除
                  </a-button>
                </a-space>
              </template>
            </a-table-column>
          </template>
          <template #empty>
            <a-empty description="暂无自定义值集" />
          </template>
        </a-table>
      </a-spin>
    </div>

    <a-drawer
      v-model:visible="defVisible"
      :title="defMode === 'add' ? '新增值集' : '编辑值集'"
      :width="drawerWidth"
      placement="right"
      unmount-on-close
    >
      <a-alert v-if="formError" type="warning" show-icon class="lov-alert">{{ formError }}</a-alert>
      <a-form :model="form" layout="vertical">
        <a-form-item label="类型" required>
          <a-select v-model="form.type" :disabled="defMode === 'edit'">
            <a-option value="ENUM">枚举</a-option>
            <a-option value="LIST">自定义列表</a-option>
          </a-select>
        </a-form-item>
        <a-form-item label="编码" required>
          <a-input
            v-model="form.lovCode"
            :disabled="defMode === 'edit'"
            :placeholder="form.type === 'ENUM' ? 'Enum.Status 或 Status' : 'List.Users 或 Users'"
            :max-length="64"
          />
        </a-form-item>
        <a-form-item label="名称" required>
          <a-input v-model="form.name" :max-length="40" />
        </a-form-item>
        <a-form-item label="取值字段">
          <a-input v-model="form.valueField" placeholder="id" />
        </a-form-item>
        <a-form-item label="显示字段">
          <a-input v-model="form.labelField" placeholder="name" />
        </a-form-item>
        <a-form-item label="启用">
          <a-switch v-model="form.enabled" />
        </a-form-item>
        <a-form-item label="备注">
          <a-textarea v-model="form.remark" :auto-size="{ minRows: 2, maxRows: 4 }" />
        </a-form-item>
      </a-form>
      <template #footer>
        <a-space>
          <a-button @click="defVisible = false">取消</a-button>
          <a-button type="primary" :loading="saving" @click="saveDef">保存</a-button>
        </a-space>
      </template>
    </a-drawer>

    <LovConfigDrawer
      v-model:visible="configVisible"
      :lov-id="configId"
      :title="configTitle"
      :width="configDrawerWidth"
      :can-edit="flags.canEdit"
    />
  </div>
</template>

<script setup lang="ts">
/**
 * Admin/Lov 值集管理页（OSC-2610019c9d）：工具栏 + 表格 + 定义/配置抽屉。
 * 表面/表头样式对齐实体 DefaultList（list-surface / list-panel / VTable headerStyle）。
 */
import LovConfigDrawer from './LovConfigDrawer.vue';
import { lovTypeLabel } from '@/core/utils/lovAdmin';
import { useLovPage } from './useLovPage';

const {
  keyword,
  filteredRows,
  loading,
  error,
  saving,
  flags,
  defVisible,
  defMode,
  form,
  formError,
  drawerWidth,
  configDrawerWidth,
  configVisible,
  configId,
  configTitle,
  load,
  openAdd,
  openEdit,
  openConfig,
  saveDef,
  confirmDelete,
} = useLovPage();
</script>

<style scoped>
.lov-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
}
/* list-surface / list-panel / list-topbar 与 DefaultList 同源变量外壳；本页 scoped 补一份避免依赖全局类 */
.lov-page.list-surface {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.lov-page :deep(.list-panel) {
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
  padding: 16px;
  background: var(--color-bg-2);
  border: none;
  border-radius: 8px;
  overflow-x: auto;
  overflow-y: visible;
}
.lov-page :deep(.list-panel--table) {
  padding-top: 12px;
}
.lov-page :deep(.list-topbar) {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 8px;
  min-height: var(--cube-density-control-height, 32px);
}
.lov-alert {
  margin-bottom: 12px;
}
/* 表头/行高对齐实体列表：fill-2 表头、紧凑单元格（约 32px 行） */
.lov-table :deep(.arco-table-th) {
  background-color: var(--color-fill-2);
  color: var(--color-text-2);
  font-weight: 500;
  font-size: 13px;
  height: 32px;
  border-bottom: 1px solid var(--color-border-2);
  border-right: none;
}
.lov-table :deep(.arco-table-th .arco-table-cell) {
  padding: 4px 8px;
}
.lov-table :deep(.arco-table-td) {
  background-color: var(--color-bg-2);
  color: var(--color-text-1);
  font-size: 13px;
  height: 36px;
  border-bottom: 1px solid var(--color-border-2);
}
.lov-table :deep(.arco-table-td .arco-table-cell) {
  padding: 4px 8px;
}
.lov-table :deep(.arco-table-tr:hover .arco-table-td) {
  background-color: var(--color-fill-1);
}
.lov-table :deep(.arco-table-border),
.lov-table :deep(.arco-table-border .arco-table-container),
.lov-table :deep(.arco-table-border .arco-table-tr .arco-table-th),
.lov-table :deep(.arco-table-border .arco-table-tr .arco-table-td) {
  border-right: none;
}
</style>
