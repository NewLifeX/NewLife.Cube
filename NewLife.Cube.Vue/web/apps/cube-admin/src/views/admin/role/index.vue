<script setup lang="ts">
/**
 * 角色管理（M2 混合页）。
 *
 * 列表、搜索、增删改、导入导出全部由 `CubeTable` 按 `/meta` 渲染；
 * 唯一自定义的是「菜单权限」——MVC 侧同样是独立的 SetPermission 视图，
 * 因此这里以行操作按钮 + 权限树弹窗承接，不退化成文本框。
 */
import { ref } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import CubeTable from '@newlifex/cube-vue/core/components/CubeTable/CubeTable.vue';
import ListPageHeader from '@newlifex/cube-vue/core/views/components/ListPageHeader.vue';
import type { CubeRowAction } from '@newlifex/cube-vue/core/components/CubeTable/types';
import RolePermissionDialog from './components/RolePermissionDialog.vue';

const route = useRoute();
const { t } = useI18n();

const permissionVisible = ref(false);
const currentRoleId = ref(0);
const currentRoleName = ref('');

const openPermission = (row: Record<string, unknown>) => {
  currentRoleId.value = Number(row.id ?? row.ID ?? 0);
  currentRoleName.value = String(row.name ?? row.Name ?? '');
  permissionVisible.value = true;
};

/** 每行追加「菜单权限」按钮（内置的查看/编辑/删除仍由 CubeTable 按权限渲染） */
const extraActions: CubeRowAction[] = [
  {
    key: 'permission',
    label: t('role.menuPermission'),
    onClick: openPermission,
  },
];
</script>

<template>
  <CubeTable :key="route.path" :extra-row-actions="extraActions">
    <template #header>
      <ListPageHeader />
    </template>
  </CubeTable>

  <RolePermissionDialog v-model="permissionVisible" :role-id="currentRoleId" :role-name="currentRoleName" />
</template>
