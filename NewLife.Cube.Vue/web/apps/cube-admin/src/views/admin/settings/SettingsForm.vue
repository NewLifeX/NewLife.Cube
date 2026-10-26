<script setup lang="ts">
/**
 * 设置中心单对象配置表单：字段来自共享的 `/meta`（避免与 Tab 标题重复请求），
 * 取值与保存仍走各自的 `GET/PUT /api/Admin/{controller}`。
 */
import { computed, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import request from '@newlifex/cube-vue/core/utils/request';
import { routeToApiPrefix } from '@newlifex/cube-vue/core/utils/url';
import { serializeSubmitModel } from '@newlifex/cube-vue/core/utils/fieldControl';
import { backendFieldsToFormFields } from '@newlifex/cube-vue/core/utils/fieldMeta';
import FormContent from '@newlifex/cube-vue/core/views/components/FormContent.vue';
import FormActions from '@newlifex/cube-vue/core/views/components/FormActions.vue';
import type { BackendField } from '@newlifex/cube-vue/core/engine/types';

interface Props {
  /** 当前配置页的 /meta 载荷 */
  meta?: Record<string, any>;
}

const props = withDefaults(defineProps<Props>(), { meta: undefined });

const route = useRoute();
const { t } = useI18n();

const apiPrefix = computed(() => routeToApiPrefix(String(route.path)));
const fields = computed(() =>
  backendFieldsToFormFields((props.meta?.form ?? []) as BackendField[]),
);

const model = ref<Record<string, any>>({});
const loading = ref(false);
const loaded = ref(false);
const error = ref(false);
const saving = ref(false);
/** FormContent 暴露的校验入口 */
const formRef = ref<{ validate?: () => boolean } | null>(null);

async function load() {
  loading.value = true;
  error.value = false;
  loaded.value = false;
  try {
    const res: any = await request({ url: apiPrefix.value, method: 'get' });
    model.value = res?.data ?? {};
    loaded.value = true;
  } catch {
    error.value = true;
  } finally {
    loading.value = false;
  }
}

async function save() {
  if (formRef.value?.validate && !formRef.value.validate()) return;

  saving.value = true;
  try {
    const payload = serializeSubmitModel(model.value, fields.value);
    await request({ url: apiPrefix.value, method: 'put', data: payload });
    ElMessage.success(t('common.saveSuccess'));
  } catch {
    /* 错误已由 request 拦截器提示 */
  } finally {
    saving.value = false;
  }
}

watch(
  () => props.meta,
  () => void load(),
  { immediate: true },
);
</script>

<template>
  <div v-loading="loading" class="settings-form">
    <div v-if="error" class="settings-unavailable">{{ t('settingsCenter.unavailable') }}</div>
    <template v-else-if="loaded">
      <FormContent
        ref="formRef"
        :fields="fields"
        :model-value="model"
        :api-prefix="apiPrefix"
        @update:model-value="(value: Record<string, any>) => (model = value)"
      />
      <FormActions :loading="saving" @submit="save" />
    </template>
  </div>
</template>

<style lang="scss" scoped>
.settings-form {
  min-height: 160px;
}

.settings-unavailable {
  padding: 48px 0;
  color: var(--el-text-color-secondary);
  text-align: center;
}
</style>
