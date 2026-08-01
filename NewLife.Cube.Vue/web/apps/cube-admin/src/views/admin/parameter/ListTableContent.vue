<script setup lang="ts">
/**
 * 参数管理 - 列表表格 Section 覆盖（ListTableContent）
 *
 * 默认引擎从后端 GetPage 元数据自动渲染表格，但 kind（Int32）会显示为纯数字。
 * 本 Section 覆盖仅定制 kind 列的标签渲染（普通/系统/用户），其余列保持默认行为。
 *
 * 为何使用 Section 覆盖而非整页复制：
 * 按最小覆盖原则，只替换需要定制的部分（表格内容），
 * 搜索栏、工具栏、分页、页头等仍由默认引擎提供。
 *
 * props/events 接口与默认 ListTableContent 保持一致：
 * - :fields  / :data / :loading 由父组件 index.vue 传入
 * - @edit / @delete 由父组件 index.vue 的 handleEditRow / handleDeleteRow 处理
 */
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field';
import { getKind } from './parameter-kind';

interface Props {
  fields?: FieldMeta[];
  data?: Record<string, unknown>[];
  loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  fields: () => [],
  data: () => [],
  loading: false,
});

const emit = defineEmits<{
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
}>();
</script>

<template>
  <div class="list-table-content">
    <el-table :data="data" v-loading="loading" stripe border class="ltc-table">
      <el-table-column prop="id" label="编号" width="80" align="center" />
      <el-table-column prop="name" label="参数名称" min-width="120" />
      <el-table-column prop="displayName" label="显示名称" min-width="120" />
      <el-table-column prop="value" label="参数值" min-width="160" show-overflow-tooltip />
      <!--
        kind 列：默认引擎将 Int32 渲染为纯数字，此处覆盖为标签（普通/系统/用户）。
        这是本 Section 覆盖的唯一原因——其他列与默认渲染一致，但仍在此显式声明以保持兼容。
      -->
      <el-table-column prop="kind" label="类别1" width="100" align="center">
        <template #default="scope">
          <el-tag :type="getKind(scope.row.kind).type" effect="plain" round>
            {{ getKind(scope.row.kind).text }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="category" label="分类" min-width="100" />
      <el-table-column prop="updateTime" label="更新时间" width="180" />
      <el-table-column prop="enable" label="启用" width="80" align="center">
        <template #default="scope">
          <el-tag :type="scope.row.enable ? 'success' : 'danger'" effect="plain" round>
            {{ scope.row.enable ? '是' : '否' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="备注" min-width="120" show-overflow-tooltip>
        <template #default="scope">
          {{ scope.row.remark || '-' }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180" fixed="right" align="center">
        <template #default="scope">
          <el-button type="primary" size="small" @click="emit('edit', scope.row)">编辑</el-button>
          <el-button type="danger" size="small" @click="emit('delete', scope.row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<style lang="scss" scoped>
.list-table-content {
  width: 100%;
}

.ltc-table {
  :deep(.el-table__inner-wrapper::before) {
    display: none;
  }

  :deep(.el-table__header-wrapper th.el-table__cell) {
    background: var(--el-fill-color-lighter);
    border-bottom: 2px solid var(--el-border-color-light);
  }
}
</style>
