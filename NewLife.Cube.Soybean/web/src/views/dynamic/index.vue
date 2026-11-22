<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import Workbench from '../workbench/index.vue';
import EntityList from './components/entity-list.vue';
import SettingsPage from './settings/settings-page.vue';
import DbPage from './pages/db-page.vue';
import ServerInfoPage from './pages/server-info.vue';
import WidgetPage from './pages/widget-page.vue';
import LovPage from './pages/lov-page.vue';
import { findSettingsEntry } from './settings/use-settings';

defineOptions({ name: 'DynamicPage' });

interface Props {
  /** 菜单原始路径，如 /Admin/User */
  menuPath?: string;
  /** 是否为工作台页面 */
  workbench?: boolean;
}

const props = defineProps<Props>();

const route = useRoute();

const isWorkbench = computed(() => props.workbench === true || props.menuPath === '/workbench');

const currentPath = computed(() => (props.menuPath || route.path).replace(/\/+$/, ''));

/** 菜单路径 → 实体接口前缀：/admin/user → /Admin/User */
const apiType = computed(() => {
  const segments = currentPath.value
    .split('/')
    .filter(Boolean)
    .map(segment => segment.charAt(0).toUpperCase() + segment.slice(1));
  return segments.length ? `/${segments.join('/')}` : '';
});

const title = computed(() => (route.meta.title as string) || (route.meta.i18nKey as string) || '');

const normalizedPath = computed(() => currentPath.value.toLowerCase());

const settingsEntry = computed(() => findSettingsEntry(currentPath.value));

const isWidget = computed(() => normalizedPath.value === '/cube/widget');
const isServerInfo = computed(() => normalizedPath.value === '/admin/index/main');
const isDb = computed(() => normalizedPath.value === '/admin/db');
const isLov = computed(() => normalizedPath.value === '/admin/lov');
</script>

<template>
  <Workbench v-if="isWorkbench" />
  <WidgetPage v-else-if="isWidget" />
  <ServerInfoPage v-else-if="isServerInfo" />
  <DbPage v-else-if="isDb" />
  <LovPage v-else-if="isLov" />
  <SettingsPage v-else-if="settingsEntry" :controller="settingsEntry.controller" />
  <EntityList v-else :key="apiType" :type="apiType" :title="title" />
</template>
