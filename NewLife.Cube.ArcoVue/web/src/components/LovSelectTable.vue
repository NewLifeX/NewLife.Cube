<template>
  <a-modal
    v-model:visible="visibleProxy"
    :title="title"
    :width="840"
    :footer="false"
    unmount-on-close
    @before-close="onCancel"
  >
    <!-- 搜索区：由 LOV Meta.searchFields 驱动；枚举引用字段渲染为下拉，其余为文本输入 -->
    <a-space v-if="searchFields.length" style="margin-bottom: 12px" wrap>
      <template v-for="sf in searchFields" :key="sf.field">
        <a-select
          v-if="isEnumSearch(sf)"
          v-model="searchValues[sf.field]"
          :placeholder="sf.title"
          allow-clear
          style="width: 180px"
        >
          <a-option v-for="opt in enumOptionsOf(sf)" :key="String(opt.value)" :value="opt.value">
            {{ opt.label }}
          </a-option>
        </a-select>
        <a-input
          v-else
          v-model="searchValues[sf.field]"
          :placeholder="sf.title"
          allow-clear
          style="width: 180px"
          @press-enter="onSearch"
        />
      </template>
      <a-button type="primary" @click="onSearch">查询</a-button>
      <a-button v-if="searched" @click="onReset">重置</a-button>
    </a-space>

    <a-table
      :columns="columns"
      :data="rows"
      :loading="loading"
      :pagination="pagination"
      :row-key="rowKey"
      :row-selection="rowSelection"
      v-model:selectedKeys="selectedKeys"
      :bordered="false"
      @page-change="onPage"
      @row-click="onRowClick"
    />

    <!-- 多选：底部固定取消/确认；取消不修改外部 model -->
    <a-space
      v-if="multiple"
      style="display: flex; justify-content: flex-end; margin-top: 16px"
    >
      <a-button @click="onCancel">取消</a-button>
      <a-button type="primary" :disabled="!selectedKeys.length" @click="onConfirm">
        确认
      </a-button>
    </a-space>
  </a-modal>
</template>

<script setup lang="ts">
import type { LovListMeta } from '@/core/types/lov';
import { useLovSelectTable } from './useLovSelectTable';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    lovCode: string;
    /** LIST LOV 元数据（valueField/labelField/tableColumns/searchFields）；缺省时回退 value/label 约定 */
    meta?: LovListMeta | null;
    multiple?: boolean;
    modelValue?: unknown;
  }>(),
  { meta: null, multiple: false },
);

const emit = defineEmits<{
  'update:visible': [boolean];
  confirm: [values: unknown[]];
}>();

const {
  visibleProxy,
  loading,
  rows,
  pagination,
  selectedKeys,
  searched,
  searchValues,
  rowKey,
  title,
  searchFields,
  columns,
  rowSelection,
  isEnumSearch,
  enumOptionsOf,
  onSearch,
  onReset,
  onPage,
  onRowClick,
  onConfirm,
  onCancel,
} = useLovSelectTable(props, emit);
</script>
