<script setup lang="ts">
/**
 * 表单页（默认模板）
 *
 * 自动模式下从后端 GetPage 拉取 addForm / editForm 字段元数据，归一为 `FieldMeta[]`
 * 交给 `FormContent` 统一渲染（控件解析由 fieldControl.ts 的 resolveControl 完成，
 * 本文件不再维护本地 TYPE_TO_FORM_TYPE 映射，彻底消除与列表页的不一致）。
 */
import { inject, provide, defineAsyncComponent, ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import type { Component } from 'vue';
import request from '@newlifex/cube-vue/core/utils/request';
import {
  FormPageHeaderKey,
  FormContentKey,
  FormActionsKey,
  PageSectionRegistryKey,
  SectionKeyMap,
} from '@newlifex/cube-vue/core/composables/useSections';

import DefaultFormPageHeader from '@newlifex/cube-vue/core/views/components/FormPageHeader.vue';
import DefaultFormContent from '@newlifex/cube-vue/core/views/components/FormContent.vue';
import DefaultFormActions from '@newlifex/cube-vue/core/views/components/FormActions.vue';
import { routeToApiPrefix } from '../utils/url';
import { serializeSubmitModel } from '../utils/fieldControl';
import type { FieldMeta } from '../types/field';

/** 后端下发的原始字段（DataField 归一结构） */
interface BackendField {
  name: string;
  displayName: string;
  description?: string;
  typeName: string;
  itemType?: string;
  length?: number;
  nullable?: boolean;
  primaryKey?: boolean;
  readOnly?: boolean;
  lovCode?: string;
  multiple?: boolean;
}

interface Props {
  title?: string;
  subtitle?: string;
  fields?: FieldMeta[];
  modelValue?: Record<string, unknown>;
  showContinue?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  showContinue: true,
});

const emit = defineEmits<{
  submit: [];
  continue: [];
  cancel: [];
  'update:modelValue': [val: Record<string, unknown>];
}>();

const route = useRoute();
const router = useRouter();

const registry = inject(
  PageSectionRegistryKey,
  {} as Record<string, Record<string, () => Promise<{ default: unknown }>>>,
);
const pageOverrides = registry[route.path] ?? {};

for (const [name, loader] of Object.entries(pageOverrides)) {
  const key = SectionKeyMap[name];
  if (key) {
    provide(key, defineAsyncComponent(loader as () => Promise<{ default: Component }>));
  }
}

const PageHeaderComp = inject(FormPageHeaderKey, DefaultFormPageHeader);
const FormContentComp = inject(FormContentKey, DefaultFormContent);
const FormActionsComp = inject(FormActionsKey, DefaultFormActions);

const apiPrefix = computed(() => routeToApiPrefix(route.path));
const isEdit = computed(() => !!(route.query.id || route.query.id?.toString() === '0'));

// 未传入 fields 时启动自动模式
const auto = computed(() => !!!props.fields);

const pageMeta = ref<{ addForm: BackendField[]; editForm: BackendField[] } | null>(null);
const internalModelValue = ref<Record<string, unknown>>({});
const internalFields = ref<FieldMeta[]>([]);
const isSubmitting = ref(false);

/** 后端字段 → 统一 FieldMeta（透传 lovCode / multiple / description 等元数据） */
function backendFieldsToFormFields(fields: BackendField[]): FieldMeta[] {
  return fields
    .filter((f) => !f.primaryKey && !f.readOnly)
    .map((f) => ({
      name: f.name,
      displayName: f.displayName,
      description: f.description,
      typeName: f.typeName,
      itemType: f.itemType,
      length: f.length,
      nullable: f.nullable,
      primaryKey: f.primaryKey,
      readOnly: f.readOnly,
      lovCode: f.lovCode,
      multiple: f.multiple,
    }));
}

async function fetchPageMeta() {
  if (!auto.value) return;
  try {
    console.log('[DefaultForm] GetPage:', apiPrefix.value + '/GetPage');
    const res = await request({ url: apiPrefix.value + '/GetPage', method: 'get' });
    const meta = res as any;
    pageMeta.value = meta;
    const sourceFields = isEdit.value ? meta.editForm : meta.addForm;
    if (sourceFields && sourceFields.length > 0) {
      internalFields.value = backendFieldsToFormFields(sourceFields);
    } else {
      internalFields.value = backendFieldsToFormFields(meta.addForm ?? meta.editForm ?? []);
    }
  } catch (err) {
    console.error('[DefaultForm] GetPage failed:', err);
  }
}

async function fetchDetail() {
  if (!auto.value || !isEdit.value) return;
  const id = route.query.id;
  try {
    const url = apiPrefix.value + '/' + id;
    console.log('[DefaultForm] Detail:', url);
    const res: any = await request({ url, method: 'get' });
    const data = res?.data ?? res ?? {};
    // 将字符串 "true"/"false" 转换为布尔值（Enable 等）
    internalModelValue.value = Object.fromEntries(
      Object.entries(data).map(([k, v]) => [k, v === 'true' ? true : v === 'false' ? false : v]),
    );
  } catch (err) {
    console.error('[DefaultForm] Detail failed:', err);
  }
}

async function handleSubmit() {
  if (auto.value) {
    if (isSubmitting.value) return;
    isSubmitting.value = true;
    try {
      const model = serializeSubmitModel(internalModelValue.value, renderFields.value);
      if (isEdit.value) {
        await request({ url: apiPrefix.value, method: 'put', data: model });
        ElMessage.success('更新成功');
      } else {
        await request({ url: apiPrefix.value, method: 'post', data: model });
        ElMessage.success('新增成功');
      }
      router.back();
    } catch (err: any) {
      const msg = err?.response?.data?.message || err?.message || '操作失败';
      ElMessage.error(msg);
    } finally {
      isSubmitting.value = false;
    }
  } else {
    emit('submit');
  }
}

/**
 * 提交前序列化：将 multipleSelect 字段（后端以 String 列存储）的 string[] 合并为逗号分隔字符串，
 * 避免 System.Text.Json 将数组绑定到 String 属性时报错。同时兼容 itemType=文件的其它来源。
 * （实现见 utils/fieldControl.serializeSubmitModel，与列表页 submitDialog 共用）
 */
function handleCancel() {
  if (auto.value) {
    router.back();
  } else {
    emit('cancel');
  }
}

function handleContinue() {
  if (!auto.value) {
    emit('continue');
  }
}

function handleModelUpdate(val: Record<string, unknown>) {
  if (auto.value) {
    internalModelValue.value = val;
  } else {
    emit('update:modelValue', val);
  }
}

const renderFields = computed(() => (auto.value ? internalFields.value : (props.fields ?? [])));
const renderModelValue = computed(() =>
  auto.value ? internalModelValue.value : (props.modelValue ?? {}),
);

onMounted(async () => {
  if (auto.value) {
    await fetchPageMeta();
    await fetchDetail();
  }
});
</script>

<template>
  <div class="form-page">
    <slot name="header">
      <component :is="PageHeaderComp" :title="title" :subtitle="subtitle" />
    </slot>
    <div class="fp-body">
      <slot name="form">
        <component
          :is="FormContentComp"
          :fields="renderFields"
          :model-value="renderModelValue"
          :api-prefix="apiPrefix"
          @update:model-value="handleModelUpdate"
        />
      </slot>
      <slot name="actions">
        <component
          :is="FormActionsComp"
          :show-continue="showContinue && !auto"
          @submit="handleSubmit"
          @continue="handleContinue"
          @cancel="handleCancel"
        />
      </slot>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.form-page {
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.fp-body {
  flex: 1;
  overflow-y: auto;
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: var(--bg);

  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-thumb {
    background: #c8d4c8;
    border-radius: 3px;
  }
}
</style>
