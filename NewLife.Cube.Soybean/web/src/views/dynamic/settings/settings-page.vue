<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { $t } from '@/locales';
import PageHeader from '../components/page-header.vue';
import EntityList from '../components/entity-list.vue';
import SettingsForm from './settings-form.vue';
import {
  findSettingsEntry,
  loadAllSettingsMeta,
  loadSettingsMeta,
  settingsEntries,
  useSettingsMeta
} from './use-settings';

defineOptions({ name: 'SettingsPage' });

interface Props {
  /** 当前设置页控制器名，如 Core、Cube */
  controller: string;
}

const props = defineProps<Props>();

const route = useRoute();
const activeName = ref(props.controller);
const activeEntry = computed(() => findSettingsEntry(`/Admin/${activeName.value}`));
const { meta, status, titleOf } = useSettingsMeta(() => activeName.value);

// 从菜单直接进入其它设置路由时同步选中项
watch(
  () => props.controller,
  value => {
    activeName.value = value;
  }
);

const pageTitle = computed(() => $t('settingsCenter.title'));

/** 静态兜底标题：/meta 未就绪时避免 Tab 文案闪烁 */
function entryTitle(controller: string) {
  switch (controller) {
    case 'Core':
      return $t('settingsCenter.entries.Core');
    case 'Sys':
      return $t('settingsCenter.entries.Sys');
    case 'Star':
      return $t('settingsCenter.entries.Star');
    case 'XCode':
      return $t('settingsCenter.entries.XCode');
    case 'Cube':
      return $t('settingsCenter.entries.Cube');
    case 'SmsConfig':
      return $t('settingsCenter.entries.SmsConfig');
    case 'MailConfig':
      return $t('settingsCenter.entries.MailConfig');
    case 'OAuthConfig':
      return $t('settingsCenter.entries.OAuthConfig');
    case 'AccessRule':
      return $t('settingsCenter.entries.AccessRule');
    default:
      return controller;
  }
}

const tabs = computed(() =>
  settingsEntries.map(item => ({
    ...item,
    title: titleOf(item.controller) || entryTitle(item.controller)
  }))
);

/** 页内切换配置页：只替换主体内容，不改路由，保持顶部导航与标签页仍为「设置中心」 */
function handleTabChange(name: string | number) {
  const target = settingsEntries.find(item => item.controller === name);
  if (target) activeName.value = target.controller;
}

onMounted(async () => {
  // 兼容旧设置中心的 ?tab=Xxx 深链：页内选中即可，不跳转到隐藏路由
  const legacy = String(route.query.tab ?? '');
  const target = legacy
    ? settingsEntries.find(item => item.controller.toLowerCase() === legacy.toLowerCase())
    : undefined;
  if (target) activeName.value = target.controller;

  await loadAllSettingsMeta();
  // loadAll 已覆盖当前页；此处兜底处理单独进入某个设置页的场景
  await loadSettingsMeta(activeName.value);
});
</script>

<template>
  <div class="settings-page">
    <PageHeader :title="pageTitle" :subtitle="$t('settingsCenter.subtitle')" />

    <div class="settings-tabs">
      <ElTabs :model-value="activeName" @tab-change="handleTabChange">
        <ElTabPane v-for="tab in tabs" :key="tab.controller" :label="tab.title" :name="tab.controller" />
      </ElTabs>
    </div>

    <div class="settings-body">
      <div v-if="status() === 'error'" class="settings-body__unavailable">
        {{ $t('settingsCenter.unavailable') }}
      </div>
      <div v-else-if="status() !== 'ok'" v-loading="true" class="settings-body__loading" />
      <SettingsForm
        v-else-if="activeEntry?.kind === 'form'"
        :key="activeName"
        :controller="activeName"
        :meta="meta()"
      />
      <EntityList v-else :key="activeName" :type="`/Admin/${activeName}`" />
    </div>
  </div>
</template>

<style scoped>
.settings-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
  height: 100%;
  min-height: 0;
  padding: 16px;
}

.settings-page :deep(.el-card__header) {
  padding-top: 16px;
}

.settings-tabs {
  flex: none;
  padding: 0 16px;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
}

.settings-tabs :deep(.el-tabs__header) {
  margin-bottom: 0;
}

.settings-tabs :deep(.el-tabs__nav-wrap::after) {
  height: 1px;
  background-color: var(--el-border-color-light);
}

.settings-body {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
  overflow: auto;
}

.settings-body :deep(.entity-list) {
  flex: 1;
}

.settings-body__loading {
  min-height: 240px;
}

.settings-body__unavailable {
  padding: 48px 0;
  color: var(--el-text-color-secondary);
  text-align: center;
}
</style>
