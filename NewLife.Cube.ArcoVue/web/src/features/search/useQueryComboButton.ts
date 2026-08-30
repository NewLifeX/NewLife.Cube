import { computed, nextTick, ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import type { SavedQuery } from '@/core/utils/viewProfile';

/** 查询组合按钮（OSC-0016 + 面板重构）：无状态组件，全部状态由 DefaultList / viewProfile store 持有。 */

/** QueryComboButton 组件 props 类型（与 QueryComboButton.vue defineProps 泛型逐字一致） */
interface QueryComboButtonProps {
  /** 预定义查询列表 */
  queries: SavedQuery[];
  /** 当前应用的预定义查询 id（会话内存） */
  activeQueryId: string | null;
  /** 当前表单参数是否与 activeQuery 不一致（不一致时条目不显示 ✓，应用标记保留） */
  paramsDirty: boolean;
  /** 当前参数是否可保存为预定义（cleanSearchParams 后非空） */
  canSave: boolean;
  /** 是否存在第二行（多余）查询条件字段 */
  hasMoreFields: boolean;
  /** 第二行字段数（用于「展开更多条件（N）」） */
  moreFieldCount: number;
  /** 面板当前是否展开（收起显示一行、展开显示第二行） */
  expanded: boolean;
}

/** QueryComboButton 组件 emits 类型（与 QueryComboButton.vue defineEmits 泛型逐字一致） */
interface QueryComboButtonEmits {
  search: [];
  reset: [];
  /** 打开自定义查询构建器（OSC-260830a1b2） */
  custom: [];
  /** 展开 / 收起第二行条件 */
  toggleExpand: [];
  apply: [id: string];
  save: [name: string];
  rename: [id: string, name: string];
  delete: [id: string];
}

type QueryComboButtonEmit = <K extends keyof QueryComboButtonEmits>(event: K, ...args: QueryComboButtonEmits[K]) => void;

/** QueryComboButton 组件全部业务 TS：下拉菜单分发与命名弹层（自 QueryComboButton.vue script setup 原样搬移） */
export function useQueryComboButton(props: QueryComboButtonProps, emit: QueryComboButtonEmit) {
  const canRename = computed(() => !!props.activeQueryId);

  /** 已应用且参数一致时条目显示 ✓ */
  function isApplied(id: string): boolean {
    return id === props.activeQueryId && !props.paramsDirty;
  }

  const modalVisible = ref(false);
  const modalTitle = ref('保存为预定义查询');
  const modalMode = ref<'save' | 'rename'>('save');
  const modalName = ref('');
  const modalInputRef = ref<unknown>(null);

  function openModal(mode: 'save' | 'rename') {
    modalMode.value = mode;
    modalTitle.value = mode === 'save' ? '保存为预定义查询' : '重命名查询';
    modalName.value = '';
    // 重命名：预填原方案名；弹层打开后聚焦并全选（OSC-260830a1b2）
    if (mode === 'rename' && props.activeQueryId) {
      const cur = props.queries.find((q) => q.id === props.activeQueryId);
      modalName.value = cur?.name ?? '';
    }
    modalVisible.value = true;
    if (mode === 'rename') {
      nextTick(() => {
        const comp = modalInputRef.value as { focus?: () => void; $el?: HTMLElement } | null;
        comp?.focus?.();
        const input = comp?.$el?.querySelector('input') as HTMLInputElement | null;
        input?.select();
      });
    }
  }

  function onSelect(value: string | number | Record<string, unknown> | undefined) {
    const key = typeof value === 'string' ? value : '';
    if (key === '__custom') {
      emit('custom');
      return;
    }
    if (key === '__reset') {
      emit('reset');
      return;
    }
    if (key === '__toggle') {
      emit('toggleExpand');
      return;
    }
    if (key === '__save') {
      openModal('save');
      return;
    }
    if (key === '__rename') {
      openModal('rename');
      return;
    }
    if (key.startsWith('__apply:')) {
      emit('apply', key.slice('__apply:'.length));
    }
  }

  function onModalOk(): boolean {
    const name = modalName.value.trim();
    if (!name) {
      Message.warning('请输入查询名称');
      return false;
    }
    if (modalMode.value === 'save') emit('save', name);
    else if (props.activeQueryId) emit('rename', props.activeQueryId, name);
    modalVisible.value = false;
    return true;
  }

  function onDelete(id: string) {
    emit('delete', id);
  }

  return {
    canRename,
    isApplied,
    modalVisible,
    modalTitle,
    modalName,
    modalInputRef,
    onSelect,
    onModalOk,
    onDelete,
  };
}
