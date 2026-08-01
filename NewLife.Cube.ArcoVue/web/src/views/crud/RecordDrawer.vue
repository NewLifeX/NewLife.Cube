<template>
  <a-drawer
    :visible="visible"
    :width="width"
    unmount-on-close
    :title="title"
    placement="right"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <a-tabs v-model:active-key="activeTab">
      <a-tab-pane key="form" :title="mode === 'add' ? '新增' : mode === 'edit' ? '编辑' : '详情'">
        <FormContent
          v-if="mode !== 'detail'"
          ref="formRef"
          :fields="fields"
          :model="model"
          :type-path="typePath"
          :mode="mode === 'add' ? 'add' : 'edit'"
        />
        <a-descriptions v-else :column="1" bordered size="large">
          <a-descriptions-item
            v-for="field in fields"
            :key="field.name"
            :label="field.displayName || field.name"
          >
            {{ formatDetail(field) }}
          </a-descriptions-item>
        </a-descriptions>
      </a-tab-pane>
      <a-tab-pane key="history" title="历史" :disabled="mode === 'add'">
        <a-spin :loading="historyLoading" style="width: 100%">
          <a-empty v-if="!historyRows.length" description="暂无历史记录" />
          <a-timeline v-else>
            <a-timeline-item v-for="(row, idx) in historyRows" :key="idx">
              <div>{{ row.CreateTime || row.createTime || row.UpdateTime || '-' }}</div>
              <div>{{ row.Action || row.action || row.Remark || row.remark || JSON.stringify(row) }}</div>
            </a-timeline-item>
          </a-timeline>
        </a-spin>
      </a-tab-pane>
      <a-tab-pane key="comment" title="评论" :disabled="mode === 'add'">
        <a-alert type="info" :closable="false">
          评论能力待 OSC-0002 EntityComment 接线后启用。
        </a-alert>
      </a-tab-pane>
    </a-tabs>
    <template #footer>
      <a-space>
        <a-button @click="emit('update:visible', false)">取消</a-button>
        <a-button
          v-if="mode !== 'detail'"
          type="primary"
          :loading="saving"
          @click="onSave"
        >
          保存
        </a-button>
        <a-button v-else-if="canEdit" type="primary" @click="emit('edit')">编辑</a-button>
      </a-space>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { getValueByKey } from '@/core/utils/url';
import cubeApi from '@/api';
import FormContent from './FormContent.vue';

const props = defineProps<{
  visible: boolean;
  typePath: string;
  fields: FieldMeta[];
  model: Record<string, unknown>;
  mode: 'add' | 'edit' | 'detail';
  pkField: string;
  canEdit?: boolean;
  saving?: boolean;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  save: [];
  edit: [];
}>();

const activeTab = ref('form');
const formRef = ref<InstanceType<typeof FormContent>>();
const historyLoading = ref(false);
const historyRows = ref<Record<string, unknown>[]>([]);

const title = computed(() => {
  if (props.mode === 'add') return '新增';
  if (props.mode === 'edit') return '编辑';
  return '详情';
});

const width = computed(() => (props.fields.length > 10 ? 720 : 520));

function formatDetail(field: FieldMeta) {
  const v = getValueByKey(props.model, field.name);
  if (v == null || v === '') return '-';
  if (field.dataSource && field.dataSource[String(v)] != null) {
    return field.dataSource[String(v)];
  }
  return String(v);
}

async function loadHistory() {
  const id = getValueByKey(props.model, props.pkField);
  if (id == null || props.mode === 'add') {
    historyRows.value = [];
    return;
  }
  historyLoading.value = true;
  try {
    const res = await cubeApi.page.getList('/Admin/Log', {
      pageIndex: 0,
      pageSize: 50,
      category: props.typePath.replace(/^\//, ''),
      linkId: id,
    });
    historyRows.value = (res.data as Record<string, unknown>[]) ?? [];
  } catch {
    historyRows.value = [];
  } finally {
    historyLoading.value = false;
  }
}

async function onSave() {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }
  emit('save');
}

watch(
  () => [props.visible, activeTab.value, props.model] as const,
  ([vis, tab]) => {
    if (vis && tab === 'history') loadHistory();
    if (vis) activeTab.value = activeTab.value || 'form';
  },
);

watch(
  () => props.visible,
  (v) => {
    if (v) activeTab.value = 'form';
  },
);

defineExpose({ validate: () => formRef.value?.validate() });
</script>
