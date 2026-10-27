<script setup lang="ts">
/**
 * 表单页（默认模板）
 *
 * 自动模式下从后端 meta 拉取 form / add 字段元数据，归一为 `FieldMeta[]`
 * 交给 `FormContent` 统一渲染（控件解析由 fieldControl.ts 的 resolveControl 完成，
 * 本文件不再维护本地 TYPE_TO_FORM_TYPE 映射，彻底消除与列表页的不一致）。
 */
import { inject, provide, defineAsyncComponent, ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
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
import { backendFieldsToFormFields } from '../utils/fieldMeta';
import type { FieldMeta } from '../types/field';
import type { BackendField } from '../engine/types';

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
const { t } = useI18n();

const registry = inject(
  PageSectionRegistryKey,
  {} as Record<string, Record<string, () => Promise<{ default: unknown }>>>,
);
const pageOverrides = registry[route.path.toLowerCase()] ?? {};

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
const auto = computed(() => !props.fields);

const internalModelValue = ref<Record<string, unknown>>({});
const internalFields = ref<FieldMeta[]>([]);
const isSubmitting = ref(false);
/** FormContent 暴露的校验入口（注入组件同样支持） */
const formRef = ref<{ validate?: () => boolean } | null>(null);

async function fetchPageMeta() {
  if (!auto.value) return;
  try {
    console.log('[DefaultForm] meta:', apiPrefix.value + '/meta');
    const res = await request({ url: apiPrefix.value + '/meta', method: 'get' });
    const meta = (res?.data ?? res) as {
      form?: BackendField[];
      add?: BackendField[];
      /** 旧服务端字段 */
      addForm?: BackendField[];
      /** 旧服务端字段 */
      editForm?: BackendField[];
    };
    // 新服务端：form 为编辑表单，add 省略时回退 form；旧服务端字段保留兼容
    const form = Array.isArray(meta.form) ? meta.form : [];
    const add = Array.isArray(meta.add) ? meta.add : form;
    const legacyEdit = Array.isArray(meta.editForm) ? meta.editForm : [];
    const legacyAdd = Array.isArray(meta.addForm) ? meta.addForm : [];
    const editFields = form.length ? form : legacyEdit;
    const addFields = add.length ? add : legacyAdd.length ? legacyAdd : editFields;
    const sourceFields = isEdit.value ? editFields : addFields;
    internalFields.value = backendFieldsToFormFields(sourceFields.length ? sourceFields : editFields);
  } catch (err) {
    console.error('[DefaultForm] meta failed:', err);
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
  if (formRef.value?.validate && !formRef.value.validate()) return;

  if (auto.value) {
    if (isSubmitting.value) return;
    isSubmitting.value = true;
    try {
      const model = serializeSubmitModel(internalModelValue.value, renderFields.value);
      if (isEdit.value) {
        const editId = route.query.id ?? model.id ?? model.Id;
        await request({ url: `${apiPrefix.value}/${editId}`, method: 'put', data: model });
        ElMessage.success(t('common.updateSuccess'));
      } else {
        await request({ url: apiPrefix.value, method: 'post', data: model });
        ElMessage.success(t('common.addSuccess'));
      }
      router.back();
    } catch (err: any) {
      // 全局响应拦截器已统一弹出错误提示，业务层无需重复 toast
      console.error('[DefaultForm] save failed:', err);
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
          ref="formRef"
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
  background: var(--el-bg-color-page);

  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-thumb {
    background: var(--el-border-color-light);
    border-radius: 3px;
  }
}
</style>
