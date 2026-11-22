<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import {
  collectRolePermissionColumns,
  flattenRolePermissionTree,
  getPermissionCheckState,
  isMenuFlagGranted,
  parsePermissionString,
  serializePermissionString,
  setMenuPermission,
  toggleMenuPermissionFlag
} from '@newlifex/page-logic';
import type { RolePermissionNode, RolePermissionRow } from '@newlifex/page-logic';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import { useAuthStore } from '@/store/modules/auth';

defineOptions({ name: 'RolePermissionPicker' });

interface Props {
  /** 权限串 menuId#flags,... */
  modelValue?: string;
  /** 角色实体路径前缀，如 Admin/Role */
  apiType: string;
  /** 编辑时的角色编号；新增时为 0，借用当前用户角色拉取权限树 */
  roleId?: number;
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  roleId: 0
});

const emit = defineEmits<{
  'update:modelValue': [value: string];
}>();

const authStore = useAuthStore();
const loading = ref(false);
const error = ref('');
const rows = ref<RolePermissionRow[]>([]);
const grants = ref<Map<number, number>>(parsePermissionString(props.modelValue));

const columns = computed(() => collectRolePermissionColumns(rows.value));
const gridStyle = computed(() => ({
  // 权限列按标题内容自适应宽度，避免“释放内存”等较长标题换行
  gridTemplateColumns: `minmax(220px, 1fr) minmax(64px, max-content) repeat(${columns.value.length}, minmax(64px, max-content))`
}));
const grantedCount = computed(() => rows.value.filter(row => rowState(row) !== 'none').length);

watch(
  () => props.modelValue,
  value => {
    grants.value = parsePermissionString(value);
  }
);

function rowState(row: RolePermissionRow) {
  return getPermissionCheckState(grants.value.get(row.id), row.flags);
}

function hasFlag(row: RolePermissionRow, value: number) {
  return row.flags.some(flag => flag.value === value);
}

function isGranted(menuId: number, value: number) {
  return isMenuFlagGranted(grants.value.get(menuId), value);
}

function toggleRow(row: RolePermissionRow, granted: boolean) {
  grants.value = setMenuPermission(grants.value, row.id, row.flags, granted);
  emit('update:modelValue', serializePermissionString(grants.value));
}

function toggleFlag(row: RolePermissionRow, value: number, granted: boolean) {
  grants.value = toggleMenuPermissionFlag(grants.value, row.id, row.flags, value, granted);
  emit('update:modelValue', serializePermissionString(grants.value));
}

async function load() {
  const roleId = props.roleId > 0 ? props.roleId : Number(authStore.cubeInfo.roleID) || 0;
  if (!roleId) {
    error.value = $t('page.form.permissionLoadFailed');
    return;
  }

  loading.value = true;
  error.value = '';
  try {
    const res = await cubeApi.page.getAction<{ tree?: RolePermissionNode[] }>(props.apiType, 'PermissionTree', {
      roleId
    });
    rows.value = flattenRolePermissionTree(res?.data?.tree);
  } catch {
    error.value = $t('page.form.permissionLoadFailed');
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div v-loading="loading" class="role-permission">
    <ElAlert v-if="error" :title="error" type="warning" show-icon :closable="false" />
    <template v-else>
      <div class="role-permission__tip">
        {{ $t('page.form.permissionHint') }}
        <span class="role-permission__count">{{ grantedCount }} / {{ rows.length }}</span>
      </div>
      <div class="role-permission__scroll">
        <div class="role-permission__grid" :style="gridStyle">
          <div class="role-permission__head">{{ $t('page.form.permissionMenu') }}</div>
          <div class="role-permission__head role-permission__center">{{ $t('page.form.permissionGranted') }}</div>
          <div v-for="column in columns" :key="column.value" class="role-permission__head role-permission__center">
            {{ column.name }}
          </div>

          <template v-for="(row, index) in rows" :key="row.id">
            <div
              class="role-permission__cell role-permission__menu"
              :class="{ 'role-permission__cell--alt': index % 2 === 1 }"
              :style="{ paddingLeft: `${8 + row.depth * 18}px` }"
            >
              <SvgIcon v-if="row.depth > 0" icon="mdi:subdirectory-arrow-right" class="role-permission__branch" />
              <span class="truncate">{{ row.displayName }}</span>
              <span class="role-permission__code">{{ row.name }}</span>
            </div>
            <div class="role-permission__cell role-permission__center" :class="{ 'role-permission__cell--alt': index % 2 === 1 }">
              <ElCheckbox
                :model-value="rowState(row) !== 'none'"
                :indeterminate="rowState(row) === 'some'"
                :disabled="row.flags.length === 0"
                @change="checked => toggleRow(row, Boolean(checked))"
              />
            </div>
            <div
              v-for="column in columns"
              :key="column.value"
              class="role-permission__cell role-permission__center"
              :class="{ 'role-permission__cell--alt': index % 2 === 1 }"
            >
              <ElCheckbox
                v-if="hasFlag(row, column.value)"
                :model-value="isGranted(row.id, column.value)"
                @change="checked => toggleFlag(row, column.value, Boolean(checked))"
              />
              <span v-else class="role-permission__na">·</span>
            </div>
          </template>
        </div>
      </div>
      <div v-if="!loading && !rows.length" class="role-permission__empty">{{ $t('page.form.permissionNone') }}</div>
    </template>
  </div>
</template>

<style scoped>
.role-permission {
  width: 100%;
}

.role-permission__tip {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.role-permission__count {
  flex: none;
  font-variant-numeric: tabular-nums;
}

.role-permission__scroll {
  max-height: 340px;
  overflow: auto;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 4px;
}

.role-permission__grid {
  display: grid;
  min-width: 100%;
  width: max-content;
  font-size: 13px;
}

.role-permission__head {
  position: sticky;
  top: 0;
  z-index: 2;
  padding: 8px;
  font-weight: 600;
  white-space: nowrap;
  /* 表头必须不透明，否则滚动时下方行的复选框会透出遮挡列标题 */
  background: var(--el-fill-color-light);
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.role-permission__cell {
  padding: 6px 8px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.role-permission__cell--alt {
  background: var(--el-fill-color-lighter);
}

.role-permission__menu {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.role-permission__branch {
  flex: none;
  color: var(--el-text-color-placeholder);
}

.role-permission__code {
  flex: none;
  font-size: 11px;
  color: var(--el-text-color-placeholder);
}

.role-permission__center {
  display: flex;
  align-items: center;
  justify-content: center;
}

.role-permission__na {
  color: var(--el-text-color-placeholder);
}

.role-permission__empty {
  padding: 12px 0;
  text-align: center;
  color: var(--el-text-color-secondary);
}
</style>
