<template>
  <div class="role-perm-tree">
    <a-space class="role-perm-tree__bar" :size="8">
      <a-button size="mini" :disabled="disabled || loading" @click="checkAll">全部选中</a-button>
      <a-button size="mini" :disabled="disabled || loading" @click="uncheckAll">全部取消</a-button>
      <a-button size="mini" :disabled="loading" @click="expandAll">展开全部</a-button>
      <a-button size="mini" :disabled="loading" @click="collapseAll">收起全部</a-button>
    </a-space>
    <a-alert v-if="error" type="warning" style="margin-bottom: 8px">{{ error }}</a-alert>
    <a-spin :loading="loading" style="width: 100%">
      <a-empty v-if="empty" description="暂无菜单" />
      <div v-else class="role-perm-tree__body" :style="nameColStyle">
        <a-tree
          :data="forest"
          v-model:expanded-keys="expandedKeys"
          :selectable="false"
        >
          <template #title="node">
            <span
              class="role-perm-tree__row"
              :class="{ 'role-perm-tree__row--leaf': !!permsOf(node).length }"
            >
              <a-checkbox
                v-if="hasNodePerms(node)"
                class="role-perm-tree__row-check"
                :model-value="nodeCheckAll(node)"
                :indeterminate="nodeCheckSome(node)"
                :disabled="disabled"
                @click.stop
                @mousedown.stop
                @update:model-value="(v: boolean) => toggleNode(node, v)"
              />
              <span class="role-perm-tree__name">{{ node.title }}</span>
              <span
                v-if="permsOf(node).length"
                class="role-perm-tree__flags"
                @click.stop
                @mousedown.stop
              >
                <a-checkbox
                  v-for="p in permsOf(node)"
                  :key="p.key"
                  :model-value="isPermChecked(p.key)"
                  :disabled="disabled"
                  @update:model-value="(v: boolean) => togglePerm(p.key, v)"
                >
                  {{ p.title }}
                </a-checkbox>
              </span>
            </span>
          </template>
        </a-tree>
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import { useRolePermTree } from './useRolePermTree';

const props = defineProps<{
  modelValue?: unknown;
  disabled?: boolean;
}>();

const emit = defineEmits<{ 'update:modelValue': [string] }>();

const {
  loading,
  error,
  forest,
  empty,
  nameColStyle,
  expandedKeys,
  isPermChecked,
  permsOf,
  togglePerm,
  hasNodePerms,
  nodeCheckAll,
  nodeCheckSome,
  toggleNode,
  checkAll,
  uncheckAll,
  expandAll,
  collapseAll,
} = useRolePermTree(props, emit);
</script>

<style scoped>
.role-perm-tree {
  width: 100%;
}
.role-perm-tree__bar {
  margin-bottom: 8px;
}
.role-perm-tree__body {
  min-height: 240px;
  max-height: 420px;
  overflow: auto;
  border: 1px solid var(--color-border-2);
  border-radius: 4px;
  padding: 8px;
}
.role-perm-tree__row {
  display: inline-flex;
  align-items: center;
  white-space: nowrap;
}
.role-perm-tree__row--leaf {
  display: inline-grid;
  grid-template-columns: auto var(--perm-name-min, 0) auto;
  column-gap: 12px;
  align-items: center;
}
.role-perm-tree__row-check {
  margin-right: 6px;
  flex: 0 0 auto;
}
.role-perm-tree__row--leaf .role-perm-tree__row-check {
  margin-right: 0;
}
.role-perm-tree__name {
  white-space: nowrap;
}
.role-perm-tree__flags {
  display: inline-flex;
  align-items: center;
  flex-wrap: nowrap;
  gap: 0 8px;
  white-space: nowrap;
}
.role-perm-tree__body :deep(.arco-tree-node-title) {
  flex: 0 0 auto;
  overflow: visible;
  white-space: nowrap;
}
.role-perm-tree__body :deep(.arco-tree-node-title-text) {
  overflow: visible;
  text-overflow: clip;
}
.role-perm-tree__flags :deep(.arco-checkbox) {
  margin-right: 0;
  padding-left: 0;
  white-space: nowrap;
}
</style>
