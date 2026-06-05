<script setup lang="ts">
import { inject, provide, defineAsyncComponent, ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import type { Component } from 'vue';
import request from 'cube-front/core/utils/request';
import {
  FormPageHeaderKey,
  FormContentKey,
  FormActionsKey,
  PageSectionRegistryKey,
  SectionKeyMap,
} from 'cube-front/core/composables/useSections';

import DefaultFormPageHeader from 'cube-front/core/views/components/FormPageHeader.vue';
import DefaultFormContent from 'cube-front/core/views/components/FormContent.vue';
import DefaultFormActions from 'cube-front/core/views/components/FormActions.vue';
import { routeToApiPrefix } from '../utils/url';

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
  mapField?: string;
}

interface FormField {
  key: string;
  label: string;
  type: 'text' | 'email' | 'tel' | 'select' | 'textarea' | 'radio';
  required?: boolean;
  fullWidth?: boolean;
  placeholder?: string;
  options?: Array<{ value: string; label: string; }>;
  error?: string;
}

interface Props {
  title?: string;
  subtitle?: string;
  fields?: FormField[];
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
  {} as Record<string, Record<string, () => Promise<{ default: unknown; }>>>,
);
const pageOverrides = registry[route.path] ?? {};

for (const [name, loader] of Object.entries(pageOverrides)) {
  const key = SectionKeyMap[name];
  if (key) {
    provide(key, defineAsyncComponent(loader as () => Promise<{ default: Component; }>));
  }
}

const PageHeaderComp = inject(FormPageHeaderKey, DefaultFormPageHeader);
const FormContentComp = inject(FormContentKey, DefaultFormContent);
const FormActionsComp = inject(FormActionsKey, DefaultFormActions);



const apiPrefix = computed(() => routeToApiPrefix(route.path));
const isEdit = computed(() => !!(route.query.id || route.query.id?.toString() === '0'));

// 未传入 fields 时启动自动模式
const auto = computed(() => !!!props.fields);

const pageMeta = ref<{ addForm: BackendField[]; editForm: BackendField[]; } | null>(null);
const internalModelValue = ref<Record<string, unknown>>({});
const internalFields = ref<FormField[]>([]);
const isSubmitting = ref(false);

const TYPE_TO_FORM_TYPE: Record<string, FormField['type']> = {
  String: 'text',
  Int32: 'text',
  Int64: 'text',
  Decimal: 'text',
  Double: 'text',
  Boolean: 'select',
  DateTime: 'text',
};

function backendFieldsToFormFields(fields: BackendField[]): FormField[] {
  return fields
    .filter((f) => !f.primaryKey && !f.readOnly)
    .map((f) => {
      const type = TYPE_TO_FORM_TYPE[f.typeName] ?? 'text';
      const item: FormField = { key: f.name, label: f.displayName || f.name, type };
      if (!f.nullable && !f.primaryKey) {
        item.required = true;
      }
      if (f.length && f.length > 50 && type === 'text') {
        item.type = 'textarea';
      }
      if (f.description) {
        item.placeholder = f.description;
      }
      if (f.typeName === 'Boolean') {
        item.type = 'select';
        item.options = [
          { value: 'true', label: '是' },
          { value: 'false', label: '否' },
        ];
      }
      return item;
    });
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
    if (res && res.data) {
      internalModelValue.value = { ...res.data };
    } else if (res && typeof res === 'object') {
      internalModelValue.value = { ...res };
    } else {
      internalModelValue.value = {};
    }
  } catch (err) {
    console.error('[DefaultForm] Detail failed:', err);
  }
}

async function handleSubmit() {
  if (auto.value) {
    if (isSubmitting.value) return;
    isSubmitting.value = true;
    try {
      const model = internalModelValue.value;
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
const renderModelValue = computed(() => (auto.value ? internalModelValue.value : (props.modelValue ?? {})));

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
        <component :is="FormContentComp" :fields="renderFields" :model-value="renderModelValue"
          @update:model-value="handleModelUpdate" />
      </slot>
      <slot name="actions">
        <component :is="FormActionsComp" :show-continue="showContinue && !auto" @submit="handleSubmit"
          @continue="handleContinue" @cancel="handleCancel" />
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
