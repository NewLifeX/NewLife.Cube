import { computed, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import {
  groupFieldCandidates,
  pushGroupField,
  removeGroupField,
  moveGroupField,
} from '@/core/utils/viewMapping';
import { normalizeGroup, type ViewGroup } from '@/core/utils/viewProfile';

/** GroupPopover 组件 props 类型（与 GroupPopover.vue defineProps 泛型逐字一致） */
interface GroupPopoverProps {
  visible: boolean;
  fields: FieldMeta[];
  modelValue: ViewGroup;
  canSave: boolean;
}

/** GroupPopover 组件 emits 类型（与 GroupPopover.vue defineEmits 泛型逐字一致） */
interface GroupPopoverEmits {
  'update:visible': [boolean];
  apply: [ViewGroup];
  save: [ViewGroup];
}

type GroupPopoverEmit = <K extends keyof GroupPopoverEmits>(event: K, ...args: GroupPopoverEmits[K]) => void;

/** GroupPopover 组件全部业务 TS：分组字段草稿编辑与保存/应用（自 GroupPopover.vue script setup 原样搬移） */
export function useGroupPopover(props: GroupPopoverProps, emit: GroupPopoverEmit) {
  const draft = ref<ViewGroup>([]);

  const labelOf = (name: string): string => {
    const f = props.fields.find((x) => x.name === name);
    return f?.displayName || name;
  };

  const candidateFields = computed(() => {
    const used = new Set(draft.value);
    return groupFieldCandidates(props.fields).filter((f) => !used.has(f.name));
  });

  function syncDraftFromProps() {
    draft.value = normalizeGroup(props.modelValue);
  }

  function addField(name: unknown) {
    if (typeof name !== 'string' || !name) return;
    draft.value = pushGroupField(draft.value, name);
  }

  function removeAt(i: number) {
    draft.value = removeGroupField(draft.value, i);
  }

  function move(i: number, delta: -1 | 1) {
    draft.value = moveGroupField(draft.value, i, delta);
  }

  function clearDraft() {
    draft.value = [];
  }

  function emitApply() {
    emit('apply', normalizeGroup(draft.value));
    close();
  }

  function emitSave() {
    emit('save', normalizeGroup(draft.value));
  }

  function close() {
    emit('update:visible', false);
  }

  function onVisibleChange(v: boolean) {
    if (v) syncDraftFromProps();
    emit('update:visible', v);
  }

  watch(
    () => props.visible,
    (v) => {
      if (v) syncDraftFromProps();
    },
  );

  return {
    draft,
    labelOf,
    candidateFields,
    addField,
    removeAt,
    move,
    clearDraft,
    emitApply,
    emitSave,
    close,
    onVisibleChange,
  };
}
