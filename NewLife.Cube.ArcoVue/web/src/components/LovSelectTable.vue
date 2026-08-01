<template>
  <a-modal v-model:visible="visibleProxy" title="选择值集" :width="720" :footer="false" unmount-on-close>
    <a-space style="margin-bottom: 12px">
      <a-input v-model="keyword" placeholder="关键词" allow-clear style="width: 220px" @press-enter="load" />
      <a-button type="primary" @click="load">查询</a-button>
    </a-space>
    <a-table
      :columns="columns"
      :data="rows"
      :loading="loading"
      :pagination="pagination"
      row-key="id"
      @page-change="onPage"
      @row-click="onRow"
    />
  </a-modal>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { fetchLovListData } from '@/core/utils/lov-api';

const props = defineProps<{
  visible: boolean;
  lovCode: string;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  select: [row: Record<string, unknown>];
}>();

const visibleProxy = computed({
  get: () => props.visible,
  set: (v: boolean) => emit('update:visible', v),
});

const loading = ref(false);
const rows = ref<Record<string, unknown>[]>([]);
const keyword = ref('');
const pagination = reactive({ current: 1, pageSize: 20, total: 0, showTotal: true });

const columns = [
  { title: '值', dataIndex: 'value' },
  { title: '名称', dataIndex: 'label' },
  { title: '名称', dataIndex: 'name' },
];

async function load() {
  if (!props.lovCode) return;
  loading.value = true;
  try {
    const res = await fetchLovListData({
      lovCode: props.lovCode,
      params: keyword.value ? { q: keyword.value, name: keyword.value } : {},
      pageNum: pagination.current,
      pageSize: pagination.pageSize,
    });
    rows.value = (res.data ?? []).map((r, i) => {
      const row = r as Record<string, unknown>;
      return {
        ...row,
        id: row.id ?? row.Id ?? row.value ?? i,
        value: row.value ?? row.Value ?? row.id ?? row.Id,
        label: row.label ?? row.Label ?? row.name ?? row.Name,
        name: row.name ?? row.Name,
      };
    });
    pagination.total = res.total ?? rows.value.length;
  } catch {
    rows.value = [];
    pagination.total = 0;
  } finally {
    loading.value = false;
  }
}

function onPage(page: number) {
  pagination.current = page;
  load();
}

function onRow(record: Record<string, unknown>) {
  emit('select', record);
}

watch(
  () => props.visible,
  (v) => {
    if (v) {
      pagination.current = 1;
      load();
    }
  },
);
</script>
