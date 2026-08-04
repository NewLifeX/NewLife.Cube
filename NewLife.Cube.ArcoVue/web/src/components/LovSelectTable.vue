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
import { computed, reactive, ref, watch } from 'vue';
import { fetchLovListData, fetchLovMeta } from '@/core/utils/lov-api';
import { getValueByKey } from '@/core/utils/url';
import type { LovListMeta, LovSearchField } from '@/core/types/lov';

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

const visibleProxy = computed({
  get: () => props.visible,
  set: (v: boolean) => emit('update:visible', v),
});

const loading = ref(false);
const rows = ref<Record<string, unknown>[]>([]);
const keyword = ref('');
const pagination = reactive({ current: 1, pageSize: 20, total: 0, showTotal: true });
const selectedKeys = ref<(string | number)[]>([]);
const searched = ref(false);
/** 搜索条件：key = LOV 搜索字段名 */
const searchValues = reactive<Record<string, unknown>>({});
/** 枚举搜索字段选项缓存（refLovCode → options） */
const enumOptions = reactive<Record<string, { value: string; label: string }[]>>({});

const valueField = computed(() => props.meta?.valueField || 'value');
const labelField = computed(() => props.meta?.labelField || 'label');
const rowKey = computed(() => valueField.value);
const title = computed(() => `选择值集${props.multiple ? '（可多选）' : ''}`);

const searchFields = computed(() => props.meta?.searchFields ?? []);
const tableColumns = computed(() => props.meta?.tableColumns ?? []);

const columns = computed(() => {
  if (tableColumns.value.length) {
    return tableColumns.value.map((c) => ({
      title: c.title || c.field,
      dataIndex: c.field,
      width: c.width && c.width > 0 ? c.width : undefined,
      render: ({ record }: { record: Record<string, unknown> }) =>
        cellText(record, c.field),
    }));
  }
  return [
    {
      title: labelField.value,
      dataIndex: labelField.value,
      render: ({ record }: { record: Record<string, unknown> }) => cellText(record, labelField.value),
    },
  ];
});

const rowSelection = computed(() =>
  props.multiple ? { type: 'checkbox', showCheckedAll: true } : undefined,
);

function cellText(row: Record<string, unknown>, field: string): string {
  const v = getValueByKey(row, field);
  return v == null ? '-' : String(v);
}

function rowValue(row: Record<string, unknown>): unknown {
  return getValueByKey(row, valueField.value);
}

function isEnumSearch(sf: LovSearchField): boolean {
  return (
    (sf.componentType || '').toLowerCase() === 'select' &&
    !!sf.refLovCode &&
    sf.refLovCode.startsWith('Enum.')
  );
}

function enumOptionsOf(sf: LovSearchField): { value: string; label: string }[] {
  const code = sf.refLovCode || '';
  if (enumOptions[code]) return enumOptions[code];
  fetchLovMeta(code)
    .then((meta) => {
      const item = meta.meta?.find((m) => m.lovCode === code);
      if (item?.type === 'ENUM') enumOptions[code] = item.options;
    })
    .catch(() => undefined);
  return enumOptions[code] ?? [];
}

function buildParams(): Record<string, unknown> {
  const params: Record<string, unknown> = {};
  for (const sf of searchFields.value) {
    const v = searchValues[sf.field];
    if (v != null && v !== '') params[sf.field] = v;
  }
  return params;
}

async function load() {
  if (!props.lovCode) return;
  loading.value = true;
  try {
    const params = buildParams();
    if (Object.keys(params).length) params.q = keyword.value || params.q;
    const res = await fetchLovListData({
      lovCode: props.lovCode,
      params,
      pageNum: pagination.current,
      pageSize: pagination.pageSize,
    });
    rows.value = (res.data ?? []) as Record<string, unknown>[];
    pagination.total = res.total ?? rows.value.length;
    searched.value = Object.keys(params).length > 0;
  } catch {
    rows.value = [];
    pagination.total = 0;
  } finally {
    loading.value = false;
  }
}

function onSearch() {
  pagination.current = 1;
  load();
}

function onReset() {
  Object.keys(searchValues).forEach((k) => delete searchValues[k]);
  keyword.value = '';
  pagination.current = 1;
  load();
}

function onPage(page: number) {
  pagination.current = page;
  load();
}

function onRowClick(record: Record<string, unknown>) {
  // 多选由复选框管理，行点击不触发选择，避免误关弹窗
  if (props.multiple) return;
  emit('confirm', [rowValue(record)]);
  visibleProxy.value = false;
}

function onConfirm() {
  emit('confirm', [...selectedKeys.value]);
  visibleProxy.value = false;
}

function onCancel() {
  // 丢弃临时勾选，恢复初始选中
  selectedKeys.value = [...initialKeys.value];
  visibleProxy.value = false;
}

/** 打开弹窗时的初始选中：由外部 modelValue（valueField 值列表）推导 */
const initialKeys = ref<(string | number)[]>([]);

watch(
  () => props.visible,
  (v) => {
    if (v) {
      pagination.current = 1;
      initialKeys.value = toKeys(props.modelValue);
      selectedKeys.value = [...initialKeys.value];
      load();
    }
  },
);

function toKeys(v: unknown): (string | number)[] {
  if (v == null || v === '') return [];
  const arr = Array.isArray(v) ? v : [v];
  return arr.filter((x) => x != null && x !== '').map((x) => String(x));
}
</script>
