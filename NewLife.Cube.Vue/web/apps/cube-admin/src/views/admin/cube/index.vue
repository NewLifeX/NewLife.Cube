<script setup lang="ts">
/**
 * 设置中心：把多个配置页串成二级 Tab。
 *
 * - 分组与顺序由 `settingsGroups` 描述（页面编排，不是字段文案）
 * - 每个 Tab 的标题、字段名、提示、校验全部来自该控制器的 `/meta`（后端按 Accept-Language 本地化）
 * - 保存仍走各自的 `PUT /api/{area}/{controller}`，与单对象页一致
 * - 挂载时并发取各 Tab 的 meta 得到标题；字段值在激活该 Tab 时才取
 * - 权限不足的 Tab 显示不可用提示，不影响其它 Tab
 */
import { ref, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import request from '@newlifex/cube-vue/core/utils/request';
import { serializeSubmitModel } from '@newlifex/cube-vue/core/utils/fieldControl';
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field';
import FormContent from '@newlifex/cube-vue/core/views/components/FormContent.vue';
import FormActions from '@newlifex/cube-vue/core/views/components/FormActions.vue';
import FormPageHeader from '@newlifex/cube-vue/core/views/components/FormPageHeader.vue';
import { backendFieldsToFormFields } from '@newlifex/cube-vue/core/utils/fieldMeta';
import type { BackendField } from '@newlifex/cube-vue/core/engine/types';

interface TabState {
  /** 控制器名，如 Core */
  controller: string;
  /** 标题，来自 /meta 的 entity.displayName */
  title: string;
  fields: FieldMeta[];
  model: Record<string, any>;
  metaLoaded: boolean;
  valuesLoaded: boolean;
  loading: boolean;
  saving: boolean;
  /** 无权限或加载失败时的提示 */
  error: string;
}

/** 设置分组：菜单里只保留一个「设置中心」入口，其余设置页在这里以 Tab 呈现 */
// 只放单对象配置页（ConfigController）。邮件/短信/OAuth 配置是列表实体（EntityController），
// 各自以 /meta 动态列表页承接，不能塞进「单对象表单」的 Tab。
const settingsGroups = ['Core', 'Cube', 'Sys', 'XCode', 'Star'];

const area = 'Admin';
const route = useRoute();
const router = useRouter();
const { t } = useI18n();

const tabs = ref<TabState[]>(
  settingsGroups.map((controller) => ({
    controller,
    title: controller,
    fields: [],
    model: {},
    metaLoaded: false,
    valuesLoaded: false,
    loading: false,
    saving: false,
    error: '',
  })),
);

const active = ref<string>('');
const pageTitle = ref('');

const apiPrefix = (controller: string) => `/api/${area}/${controller}`;

/** 取字段元数据（含本地化标题），用于 Tab 标题与表单结构 */
async function loadMeta(tab: TabState) {
  if (tab.metaLoaded || tab.error) return;
  try {
    const res: any = await request({ url: `${apiPrefix(tab.controller)}/meta`, method: 'get' });
    const meta = (res?.data ?? res) as { entity?: { displayName?: string }, form?: BackendField[] };
    const form = Array.isArray(meta?.form) ? meta.form : [];
    tab.fields = backendFieldsToFormFields(form);
    tab.title = meta?.entity?.displayName || tab.controller;
    tab.metaLoaded = true;
  } catch {
    tab.error = t('settingsCenter.unavailable');
  }
}

/** 取配置值 */
async function loadValues(tab: TabState) {
  if (tab.valuesLoaded || tab.error || !tab.metaLoaded) return;
  tab.loading = true;
  try {
    const res: any = await request({ url: apiPrefix(tab.controller), method: 'get' });
    tab.model = res?.data ?? {};
    tab.valuesLoaded = true;
  } catch {
    tab.error = t('settingsCenter.unavailable');
  } finally {
    tab.loading = false;
  }
}

async function activate(controller: string) {
  const tab = tabs.value.find((item) => item.controller === controller);
  if (!tab) return;

  await loadMeta(tab);
  await loadValues(tab);
}

async function save(tab: TabState) {
  tab.saving = true;
  try {
    const payload = serializeSubmitModel(tab.model, tab.fields);
    await request({ url: apiPrefix(tab.controller), method: 'put', data: payload });
    ElMessage.success(t('common.saveSuccess'));
  } catch {
    /* 错误已由 request 拦截器提示 */
  } finally {
    tab.saving = false;
  }
}

/** 支持 ?tab=Sys 深链 */
function pickInitialTab() {
  const queried = String(route.query.tab ?? '');
  const matched = tabs.value.find((tab) => tab.controller.toLowerCase() === queried.toLowerCase());
  return matched?.controller ?? tabs.value[0]?.controller ?? '';
}

watch(active, (value) => {
  if (!value) return;
  void activate(value);
  if (String(route.query.tab ?? '') !== value) router.replace({ query: { ...route.query, tab: value } });
});

onMounted(async () => {
  pageTitle.value = String(route.meta?.title ?? '');
  active.value = pickInitialTab();

  // 先并发取标题，再加载当前 Tab 的值
  await Promise.all(tabs.value.map((tab) => loadMeta(tab)));
  const current = tabs.value.find((tab) => tab.controller === active.value);
  if (current) await loadValues(current);
});
</script>

<template>
  <div class="settings-center">
    <FormPageHeader :title="pageTitle" />
    <el-tabs v-model="active" class="settings-tabs">
      <el-tab-pane v-for="tab in tabs" :key="tab.controller" :label="tab.title" :name="tab.controller" lazy>
        <div v-if="tab.error" class="settings-error">{{ tab.error }}</div>
        <div v-else v-loading="tab.loading">
          <FormContent
            v-if="tab.valuesLoaded"
            :fields="tab.fields"
            :model-value="tab.model"
            :api-prefix="apiPrefix(tab.controller)"
            @update:model-value="(v: Record<string, any>) => (tab.model = v)"
          />
          <FormActions v-if="tab.valuesLoaded" :loading="tab.saving" @submit="save(tab)" />
        </div>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<style lang="scss" scoped>
.settings-center {
  padding: 16px 20px;
  background: var(--el-bg-color-overlay);
  border-radius: var(--el-border-radius-base);
}

.settings-tabs :deep(.el-tabs__header) {
  margin-bottom: 16px;
}

.settings-error {
  padding: 32px;
  color: var(--el-text-color-secondary);
  text-align: center;
}
</style>
