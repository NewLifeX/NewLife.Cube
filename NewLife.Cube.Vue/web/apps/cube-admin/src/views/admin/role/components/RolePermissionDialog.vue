<script setup lang="ts">
/**
 * 角色菜单权限弹窗（M2 自定义区块）。
 *
 * 数据来自后端 `/Admin/Role/PermissionTree`：菜单树 + 每菜单可用权限位 + 当前角色已授权位；
 * 保存走 `/Admin/Role/SavePermission`，权限串格式 `资源ID#权限位`，逗号分隔（与 MVC SetPermission 一致）。
 */
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import request from '@newlifex/cube-vue/core/utils/request';

interface PermissionFlag {
  value: number;
  name: string;
}

interface MenuNode {
  id: number;
  name: string;
  displayName?: string;
  flags: PermissionFlag[];
  granted: number;
  children?: MenuNode[];
}

const props = defineProps<{ modelValue: boolean; roleId: number; roleName: string }>();
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; saved: [] }>();

const { t } = useI18n();

const visible = ref(props.modelValue);
const loading = ref(false);
const saving = ref(false);
const tree = ref<MenuNode[]>([]);
/** { 菜单ID: { 权限位: 是否勾选 } } */
const checked = ref<Record<number, Record<number, boolean>>>({});

watch(
  () => props.modelValue,
  (value) => {
    visible.value = value;
    if (value) void load();
  },
);
watch(visible, (value) => emit('update:modelValue', value));

function flatten(nodes: MenuNode[], output: MenuNode[] = []): MenuNode[] {
  for (const node of nodes) {
    output.push(node);
    if (node.children?.length) flatten(node.children, output);
  }
  return output;
}

async function load() {
  loading.value = true;
  try {
    const res: any = await request({ url: '/api/Admin/Role/PermissionTree', method: 'get', params: { roleId: props.roleId } });
    const data = res?.data ?? res;
    tree.value = Array.isArray(data?.tree) ? data.tree : [];

    const state: Record<number, Record<number, boolean>> = {};
    for (const node of flatten(tree.value)) {
      state[node.id] = {};
      for (const flag of node.flags ?? []) state[node.id][flag.value] = (node.granted & flag.value) !== 0;
    }
    checked.value = state;
  } catch {
    ElMessage.error(t('common.loadFailed'));
    tree.value = [];
  } finally {
    loading.value = false;
  }
}

async function save() {
  saving.value = true;
  try {
    const parts: string[] = [];
    for (const [menuId, flags] of Object.entries(checked.value)) {
      const value = Object.entries(flags)
        .filter(([, on]) => on)
        .reduce((sum, [flag]) => sum + Number(flag), 0);
      if (value > 0) parts.push(`${menuId}#${value}`);
    }

    await request({ url: '/api/Admin/Role/SavePermission', method: 'post', data: { roleId: props.roleId, permission: parts.join(',') } });
    ElMessage.success(t('common.saveSuccess'));
    emit('saved');
    visible.value = false;
  } catch {
    /* 错误已由 request 拦截器提示 */
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <el-dialog v-model="visible" :title="`${t('role.menuPermission')} - ${props.roleName}`" width="640px">
    <div v-loading="loading" class="permission-tree">
      <el-tree :data="tree" node-key="id" default-expand-all :expand-on-click-node="false">
        <template #default="{ data }">
          <span class="permission-node">
            <span class="permission-name">{{ data.displayName || data.name }}</span>
            <el-checkbox
              v-for="flag in data.flags"
              :key="flag.value"
              v-model="checked[data.id][flag.value]"
              class="permission-flag"
            >
              {{ flag.name }}
            </el-checkbox>
          </span>
        </template>
      </el-tree>
    </div>

    <template #footer>
      <el-button @click="visible = false">{{ t('button.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" @click="save">{{ t('button.save') }}</el-button>
    </template>
  </el-dialog>
</template>

<style lang="scss" scoped>
.permission-tree {
  max-height: 60vh;
  overflow-y: auto;
}

.permission-node {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.permission-name {
  min-width: 140px;
  color: var(--el-text-color-primary);
}

.permission-flag {
  margin-right: 4px;
}
</style>
