import { computed, onMounted, ref, watch } from 'vue';
import cubeApi from '@/api';
import {
  buildPermForest,
  checkedKeysFromRole,
  collectMenuKeys,
  collectPermLeafKeys,
  collectPermKeysUnderNode,
  findPermNode,
  leafTitleColumnEm,
  loadAllMenusForPermTree,
  nodePermCheckState,
  nodePerms,
  parseRolePermission,
  roleMapFromCheckedKeys,
  serializeRolePermission,
  toggleNodePerms,
  togglePermKey,
  type PermTreeNode,
} from '@/core/utils/rolePermission';

interface RolePermTreeProps {
  modelValue?: unknown;
  disabled?: boolean;
}

type RolePermEmit = (e: 'update:modelValue', v: string) => void;

export function useRolePermTree(props: RolePermTreeProps, emit: RolePermEmit) {
  const loading = ref(false);
  const error = ref('');
  const forest = ref<PermTreeNode[]>([]);
  const checkedKeys = ref<(string | number)[]>([]);
  const expandedKeys = ref<(string | number)[]>([]);

  const empty = computed(() => !loading.value && !error.value && forest.value.length === 0);
  const nameColStyle = computed(() => ({
    '--perm-name-min': `${leafTitleColumnEm(forest.value)}em`,
  }));

  function applyCheckedFromValue() {
    checkedKeys.value = checkedKeysFromRole(
      parseRolePermission(props.modelValue),
      forest.value,
    );
  }

  function emitFromKeys(keys: (string | number)[]) {
    if (props.disabled) return;
    checkedKeys.value = keys;
    emit('update:modelValue', serializeRolePermission(roleMapFromCheckedKeys(keys, forest.value)));
  }

  function isPermChecked(key: string): boolean {
    return checkedKeys.value.some((k) => String(k) === key);
  }

  function permsOf(node: { key?: unknown; perms?: PermTreeNode['perms'] }) {
    return nodePerms(node, forest.value);
  }

  function togglePerm(key: string, checked: unknown) {
    emitFromKeys(togglePermKey(checkedKeys.value, key, checked === true));
  }

  function nodeOf(slot: unknown): PermTreeNode | undefined {
    return findPermNode(forest.value, slot as { key?: unknown });
  }

  function nodeCheckAll(slot: unknown): boolean {
    return nodePermCheckState(checkedKeys.value, nodeOf(slot)) === 'all';
  }

  function nodeCheckSome(slot: unknown): boolean {
    return nodePermCheckState(checkedKeys.value, nodeOf(slot)) === 'some';
  }

  function hasNodePerms(slot: unknown): boolean {
    return collectPermKeysUnderNode(nodeOf(slot)).length > 0;
  }

  function toggleNode(slot: unknown, checked: unknown) {
    const n = nodeOf(slot);
    if (!n) return;
    emitFromKeys(toggleNodePerms(checkedKeys.value, n, checked === true));
  }

  function checkAll() {
    emitFromKeys(collectPermLeafKeys(forest.value));
  }

  function uncheckAll() {
    emitFromKeys([]);
  }

  function expandAll() {
    expandedKeys.value = collectMenuKeys(forest.value);
  }

  function collapseAll() {
    expandedKeys.value = [];
  }

  async function load() {
    loading.value = true;
    error.value = '';
    try {
      const r = await loadAllMenusForPermTree((type, params) =>
        cubeApi.page.getList(type, params),
      );
      forest.value = buildPermForest(r.menus);
      error.value = r.error || '';
      expandedKeys.value = collectMenuKeys(forest.value);
      applyCheckedFromValue();
    } finally {
      loading.value = false;
    }
  }

  watch(
    () => props.modelValue,
    () => {
      applyCheckedFromValue();
    },
  );

  onMounted(() => {
    void load();
  });

  return {
    loading,
    error,
    forest,
    empty,
    nameColStyle,
    checkedKeys,
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
  };
}
