<template>
  <a-drawer
    :visible="visible"
    :width="width"
    unmount-on-close
    placement="right"
    class="record-drawer"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <template #title>
      <div class="drawer-title">
        <a-space v-if="showNav" :size="4" class="drawer-nav">
          <a-button type="text" size="mini" :disabled="!canPrev" @click="emit('prev')">
            上一条
          </a-button>
          <a-button type="text" size="mini" :disabled="!canNext" @click="emit('next')">
            下一条
          </a-button>
        </a-space>
        <span class="drawer-title__text">{{ title }}</span>
      </div>
    </template>

    <!-- 新建 / 只读实体：不展示历史、评论 Tab -->
    <template v-if="!showSideTabs">
      <FormContent
        v-if="mode !== 'detail'"
        ref="formRef"
        :fields="fields"
        :model="model"
        :type-path="typePath"
        :mode="mode === 'add' ? 'add' : 'edit'"
      />
      <div v-else class="detail-grouped" :style="detailLabelCssVars">
        <section
          v-for="group in detailGroups"
          :key="group.category || '__default'"
          class="detail-group"
        >
          <div v-if="group.title" class="detail-group__title">{{ group.title }}</div>
          <div class="detail-fields">
            <div v-for="field in group.fields" :key="field.name" class="detail-field">
              <div class="detail-field__label" :style="detailLabelStyle">
                {{ field.displayName || field.name }}
              </div>
              <div class="detail-field__value">{{ formatDetail(field) }}</div>
            </div>
          </div>
        </section>
      </div>
    </template>

    <a-tabs v-else v-model:active-key="activeTab">
      <a-tab-pane key="form" :title="mode === 'edit' ? '编辑' : '详情'">
        <FormContent
          v-if="mode !== 'detail'"
          ref="formRef"
          :fields="fields"
          :model="model"
          :type-path="typePath"
          mode="edit"
        />
        <div v-else class="detail-grouped" :style="detailLabelCssVars">
          <section
            v-for="group in detailGroups"
            :key="group.category || '__default'"
            class="detail-group"
          >
            <div v-if="group.title" class="detail-group__title">{{ group.title }}</div>
            <div class="detail-fields">
              <div v-for="field in group.fields" :key="field.name" class="detail-field">
                <div class="detail-field__label" :style="detailLabelStyle">
                  {{ field.displayName || field.name }}
                </div>
                <div class="detail-field__value">{{ formatDetail(field) }}</div>
              </div>
            </div>
          </section>
        </div>
      </a-tab-pane>
      <a-tab-pane key="history" title="历史">
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
      <a-tab-pane key="comment" title="评论">
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
import {
  estimateDetailLabelWidth,
  groupFieldsByCategory,
} from '@/core/utils/fieldGroups';
import cubeApi from '@/api';
import FormContent from './FormContent.vue';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    typePath: string;
    fields: FieldMeta[];
    model: Record<string, unknown>;
    mode: 'add' | 'edit' | 'detail';
    pkField: string;
    canEdit?: boolean;
    saving?: boolean;
    /** 是否显示历史/评论；新建与只读实体应为 false */
    showHistoryTabs?: boolean;
    /** 编辑/详情：当前页可见数据内上一条/下一条 */
    canPrev?: boolean;
    canNext?: boolean;
  }>(),
  { showHistoryTabs: true, canPrev: false, canNext: false },
);

const emit = defineEmits<{
  'update:visible': [boolean];
  save: [];
  edit: [];
  prev: [];
  next: [];
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

const showSideTabs = computed(
  () => props.mode !== 'add' && props.showHistoryTabs !== false,
);

const showNav = computed(() => props.mode === 'edit' || props.mode === 'detail');

const detailGroups = computed(() => groupFieldsByCategory(props.fields));

const detailLabelWidth = computed(() => estimateDetailLabelWidth(props.fields));
const detailLabelStyle = computed(() => ({
  width: `${detailLabelWidth.value}px`,
  minWidth: `${detailLabelWidth.value}px`,
  maxWidth: `${detailLabelWidth.value}px`,
}));
const detailLabelCssVars = computed(() => ({
  '--detail-label-width': `${detailLabelWidth.value}px`,
}));

function formatDetail(field: FieldMeta) {
  const v = getValueByKey(props.model, field.name);
  if (v == null || v === '') return '-';
  if (field.dataSource && field.dataSource[String(v)] != null) {
    return field.dataSource[String(v)];
  }
  if (typeof v === 'boolean') return v ? '是' : '否';
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
    if (vis && showSideTabs.value && tab === 'history') loadHistory();
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

<style scoped>
.drawer-title {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}
.drawer-nav :deep(.arco-btn) {
  padding: 0 6px;
  color: var(--color-text-2);
}
.drawer-nav :deep(.arco-btn:not(.arco-btn-disabled):hover) {
  color: rgb(var(--primary-6));
}
.drawer-title__text {
  font-weight: 500;
}

.detail-grouped {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: -4px -4px 0;
  padding: 4px;
  background: var(--color-fill-2);
  border-radius: 8px;
}
.detail-group {
  padding: 16px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.detail-group__title {
  margin-bottom: 12px;
  font-size: 14px;
  font-weight: 600;
  line-height: 22px;
  color: var(--color-text-1);
}
.detail-fields {
  display: flex;
  flex-direction: column;
  gap: 0;
  border: 1px solid var(--color-border-2);
  border-radius: var(--border-radius-small);
  overflow: hidden;
}
.detail-field {
  display: flex;
  align-items: stretch;
  gap: 0;
  min-height: 36px;
  border-bottom: 1px solid var(--color-border-2);
  background: var(--color-bg-2);
}
.detail-field:last-child {
  border-bottom: none;
}
.detail-field__label {
  flex: 0 0 auto;
  display: flex;
  align-items: flex-start;
  padding: 8px 12px;
  background-color: var(--color-fill-3);
  color: var(--color-text-2);
  text-align: left;
  line-height: 22px;
  box-sizing: border-box;
}
.detail-field__value {
  flex: 1;
  min-width: 0;
  padding: 8px 12px;
  background-color: var(--color-bg-2);
  color: var(--color-text-1);
  line-height: 22px;
  word-break: break-word;
}
</style>

<style>
/* drawer 内容挂到 body，用全局类铺灰底以衬托分组卡片 */
.record-drawer .arco-drawer-body {
  background: var(--color-fill-2);
}
</style>
