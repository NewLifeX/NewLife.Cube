<script setup lang="ts">
/**
 * 单对象/配置页（对应后端 ObjectController / ConfigController）。
 * - GET  apiPrefix            -> 单对象（{code,data}）
 * - GET  apiPrefix/GetFields?kind=4 -> 编辑表单字段
 * - PUT  apiPrefix            -> 保存
 * 与列表页（index.vue）共用 FormContent 渲染，由 AutoPage 根据响应形态自动分流。
 */
import { ref, onMounted, computed } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import request from '@newlifex/cube-vue/core/utils/request';
import { routeToApiPrefix } from '@newlifex/cube-vue/core/utils/url';
import { serializeSubmitModel } from '@newlifex/cube-vue/core/utils/fieldControl';
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field';
import FormContent from './components/FormContent.vue';
import FormActions from './components/FormActions.vue';
import FormPageHeader from './components/FormPageHeader.vue';

const props = withDefaults(defineProps<{ data?: any; apiPrefix?: string }>(), {});
const route = useRoute();
const apiPrefix = computed(() => props.apiPrefix || routeToApiPrefix(String(route.path)));

const fields = ref<FieldMeta[]>([]);
const model = ref<Record<string, any>>({});
const loading = ref(false);
const saving = ref(false);

async function load() {
  loading.value = true;
  try {
    const objRes: any = await request({ url: apiPrefix.value, method: 'get' });
    model.value = objRes?.data ?? {};
    const fldRes: any = await request({ url: `${apiPrefix.value}/GetFields?kind=4`, method: 'get' });
    const raw: any[] = Array.isArray(fldRes) ? fldRes : fldRes?.data ?? [];
    // 过滤主键与只读字段（与列表编辑表单一致）
    fields.value = raw.filter((f: any) => !f.primaryKey && !f.readOnly);
  } catch (e) {
    console.error('[ObjectPage] load failed', e);
  } finally {
    loading.value = false;
  }
}

async function handleSubmit() {
  saving.value = true;
  try {
    const payload = serializeSubmitModel(model.value, fields.value);
    await request({ url: apiPrefix.value, method: 'put', data: payload });
    ElMessage.success('保存成功');
  } catch (e) {
    /* 错误已由 request 拦截器提示 */
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="object-page" v-loading="loading">
    <FormPageHeader :title="(route.meta?.title as string) || '设置'" />
    <FormContent
      :fields="fields"
      :model-value="model"
      :api-prefix="apiPrefix"
      @update:model-value="(v: Record<string, any>) => (model = v)"
    />
    <FormActions :loading="saving" @submit="handleSubmit" />
  </div>
</template>

<style lang="scss" scoped>
.object-page {
  padding: 0;
}
</style>
