<script setup lang="ts">
/**
 * 用户管理。列表仍由 CubeTable 渲染；追加「设置角色」直接改主角色与角色组。
 */
import { defineAsyncComponent, inject, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  ListPageHeaderKey,
  PageSectionRegistryKey,
} from '@newlifex/cube-vue/core/composables/useSections';
import DefaultListPageHeader from '@newlifex/cube-vue/core/views/components/ListPageHeader.vue';
import ListChartButton from '@newlifex/cube-vue/core/views/components/ListChartButton.vue';
import CubeTable from '@newlifex/cube-vue/core/components/CubeTable/CubeTable.vue';
import { routeToApiPrefix } from '@newlifex/cube-vue/core/utils/url';
import type { CubeRowAction } from '@newlifex/cube-vue/core/components/CubeTable/types';
import type { Component } from 'vue';
import UserRoleDialog from './components/UserRoleDialog.vue';

type SectionRegistry = Record<string, Record<string, () => Promise<{ default: unknown }>>>;

const route = useRoute();
const { t } = useI18n();
const apiPrefix = routeToApiPrefix(route.path);
const tableKey = ref(0);

const registry = inject(PageSectionRegistryKey, {} as SectionRegistry);
const headerLoader = registry[route.path.toLowerCase()]?.ListPageHeader;
const PageHeaderComp: Component = headerLoader
  ? defineAsyncComponent(headerLoader as () => Promise<{ default: Component }>)
  : inject(ListPageHeaderKey, DefaultListPageHeader);

const roleVisible = ref(false);
const currentUserId = ref('');
const currentUserName = ref('');

function openRole(row: Record<string, unknown>) {
  currentUserId.value = String(row.id ?? row.ID ?? '');
  currentUserName.value = String(row.displayName ?? row.name ?? row.Name ?? '');
  roleVisible.value = true;
}

/** 每行追加「设置角色」（内置编辑 / 删除仍由 CubeTable 渲染，合计 3 个直接显示） */
const extraActions: CubeRowAction[] = [
  {
    key: 'setRole',
    label: t('userAdmin.setRole'),
    onClick: openRole,
  },
];

function onSaved() {
  tableKey.value += 1;
}
</script>

<template>
  <CubeTable :key="route.path + ':' + tableKey" :extra-row-actions="extraActions">
    <template #header>
      <component :is="PageHeaderComp" />
    </template>
    <template #toolbar-extra>
      <ListChartButton :api="apiPrefix" />
    </template>
  </CubeTable>

  <UserRoleDialog
    v-model="roleVisible"
    :user-id="currentUserId"
    :user-name="currentUserName"
    @saved="onSaved"
  />
</template>
